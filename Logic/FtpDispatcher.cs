
// -----------------------------------------------------------------------
// <copyright file="FtpDispatcher.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    using System;
    using System.Threading;

    public sealed class FtpDispatcher
    {
        #region fields
        /// <summary>
        /// Referencja do g��wnego okna
        /// </summary>
        public MainWindow m_mainWnd;

        /// <summary>
        /// Do wstrzymywania w�tku roboczego w oczekiwaniu na planowany czas uruchomienia
        /// </summary>
        private AutoResetEvent m_are = new AutoResetEvent(false);
        #endregion

        #region properties
        /// <summary>
        /// Pozwala dostosowa� wygl�d interfejsu do stanu przetwarzania
        /// Ustawiana na czas wykonywania transferu
        /// </summary>
        public bool InProgress { get; set; }

        /// <summary>
        /// Sformatowany zapis czasu bie��cego do log�w i komunikat�w
        /// </summary>
        public string Teraz
        {
            get { return DateTime.Now.ToString("dd/MM/yyyy HH:mm"); }
        }
        #endregion

        #region constructor
        /// <summary>
        /// Konstruktor FtpDispatchera
        /// </summary>
        /// <param name="wnd">G��wne okno aplikacji WPF</param>
        public FtpDispatcher(MainWindow wnd)
        {
            m_mainWnd = wnd;
        }
        #endregion

        #region private methods
        /// <summary>
        /// Implementacja p�tli obs�uguj�cej ��dania pobrania plik�w z endpointu ftp
        /// Je�eli czas ��dania up�yn��, wykonuje je bezzw�ocznie, je�li nie, to zasypia 
        /// do czasu wymagalno�ci ��dania. Zaimplementowano metod� wyj�cia z p�tli na
        /// �yczenie u�ytkownika po wywo�aniu Stop(); Warto�� iSchXX==-1 oznacza brak 
        /// ��da� do ko�ca tygodnia, towarzyszy jej data pocz�tku nowego tygodnia.
        /// </summary>
        /// <param name="o">Nie u�ywany</param>
        private void DispatchFtpThread(object o)
        {
            int iLastSchXX = -2;
            DateTime dtLastScheduleTime = DateTime.MaxValue;

            while (InProgress) {
                var (schedule, errmsg) = FtpDiligentDatabaseClient.GetNextSync(m_mainWnd.m_instance);
                if (!string.IsNullOrEmpty(errmsg)) {
                    if (errmsg == "0")
                        m_mainWnd.ShowErrorInfo(eSeverityCode.NextSync, "Nie zaplanowano �adnych pozycji w harmonogramie");
                    else
                        m_mainWnd.ShowErrorInfo(eSeverityCode.Error, $"{this.Teraz} {errmsg}");
                    return;
                }

                if (schedule.xx == iLastSchXX && schedule.nextSyncTime == dtLastScheduleTime) {
                    Thread.Sleep(5000);
                    continue;
                } else {
                    iLastSchXX = schedule.xx;
                    dtLastScheduleTime = schedule.nextSyncTime;
                    if (schedule.xx > 0)
                        m_mainWnd.ShowErrorInfo(eSeverityCode.NextSync, "Najbli�szy transfer plik�w z harmonogramu " + schedule.name + " zaplanowano na " + schedule.nextSyncTime.ToString("dd/MM/yyyy HH:mm"));
                    else
                        m_mainWnd.ShowErrorInfo(eSeverityCode.NextSync, "Do ko�ca tygodnia nie zaplanowano �adnych transfer�w");
                }

                if (DateTime.Now < schedule.nextSyncTime)
                    m_are.WaitOne(schedule.nextSyncTime.Subtract(DateTime.Now), false);

                if (schedule.xx > 0 && InProgress)
                    ThreadPool.QueueUserWorkItem(ExecuteFtpTransfer, schedule.xx);
            } // while

            if (!InProgress)
                m_mainWnd.ShowErrorInfo(eSeverityCode.Message, this.Teraz + " Pobieranie przerwane przez u�ytkownika");
        }

        /// <summary>
        /// Wykonuje transfer plik�w na podstawie zaplanowanej pozycji harmonogramu
        /// Jest uruchamiana przez dispatcher o odpowiedniej porze i tylko dla poprawnych pozycji harmonogramu
        /// </summary>
        /// <param name="iSchXX">
        /// Je�li dodatni, to identyfikator pozycji harmonogramu uruchomionej automatycznie,
        /// je�li ujemny, to identyfikator endpointu, dla kt�rego transfer uruchomiono r�cznie
        /// </param>
        public void ExecuteFtpTransfer(object iSchXX)
        {
            bool oldInProgress = true;
            int schedule = (int)iSchXX;
            var (endpoint, errmsg) = FtpDiligentDatabaseClient.SelectEndpoint(schedule).Result;
            if (!string.IsNullOrEmpty(errmsg)) {
                if (errmsg == "0")
                    errmsg = "Brak definicji endpointu dla harmonogramu: " + iSchXX;

                m_mainWnd.ShowErrorInfo(eSeverityCode.Error, $"{this.Teraz} {errmsg}");
                return;
            }

            try {
                FtpSyncModel log = new FtpSyncModel();
                FtpUtility fu = new FtpUtility(endpoint, this, m_mainWnd.m_syncMode);
                DateTime dtNewRefreshTime = log.syncTime = endpoint.nextSync;
                eFtpDirection eDirection = endpoint.direction;
                string sHost = endpoint.host + endpoint.remDir;
                log.xx = schedule;

                if (schedule < 0) {
                    m_mainWnd.ShowErrorInfo(eSeverityCode.Message, this.Teraz + " Rozpocz�to transfer plik�w z serwera " + sHost);
                    oldInProgress = InProgress;
                    InProgress = true;
                } else
                    m_mainWnd.ShowErrorInfo(eSeverityCode.Message, this.Teraz + " Rozpocz�to zaplanowany transfer plik�w z serwera " + sHost);

                // transferuj pliki

                #region pobieranie
                if ((eDirection & eFtpDirection.Get) > 0) {
                    if (!fu.Download(ref log)) {
                        m_mainWnd.ShowErrorInfo(eSeverityCode.TransferError, this.Teraz + " Pobieranie plik�w z serwera " + sHost + " zako�czy�o si� niepowodzeniem");
                        return;
                    }

                    if (schedule < 0)
                        InProgress = oldInProgress;

                    // loguj zmiany
                    if (log.fileNames.Length == 0) {
                        FtpDiligentDatabaseClient.LogActivation(log);
                        m_mainWnd.ShowErrorInfo(eSeverityCode.Message, this.Teraz + " Na serwerze " + sHost + " nie znaleziono plik�w odpowiednich do pobrania");
                    } else {
                        log.direction = eFtpDirection.Get;
                        FtpDiligentDatabaseClient.LogSync(log);
                        m_mainWnd.ShowErrorInfo(eSeverityCode.Message, this.Teraz + " Pobrano " + log.fileNames.Length + " plik�w z serwera " + sHost);
                    }
                }
                #endregion

                #region wstawianie
                if ((eDirection & eFtpDirection.Put) > 0) {
                    if (!fu.Upload(ref log)) {
                        m_mainWnd.ShowErrorInfo(eSeverityCode.TransferError, this.Teraz + " Wstawianie plik�w na serwer " + sHost + " zako�czy�o si� niepowodzeniem");
                        return;
                    }

                    if (schedule < 0)
                        InProgress = oldInProgress;

                    // loguj zmiany
                    if (log.fileNames.Length == 0) {
                        FtpDiligentDatabaseClient.LogActivation(log);
                        m_mainWnd.ShowErrorInfo(eSeverityCode.Message, this.Teraz + " Nie znaleziono plik�w do wstawienia na serwer " + sHost);
                    } else {
                        log.direction = eFtpDirection.Put;
                        FtpDiligentDatabaseClient.LogSync(log);
                        m_mainWnd.ShowErrorInfo(eSeverityCode.Message, this.Teraz + " Wstawiono " + log.fileNames.Length + " plik�w na serwer " + sHost);
                    }
                }
                #endregion

            } catch (FtpUtilityException fex) {
                m_mainWnd.ShowErrorInfo(eSeverityCode.TransferError, fex.Message);
            } catch (System.Exception se) {
                m_mainWnd.ShowErrorInfo(eSeverityCode.TransferError, se.Message);
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Inicjuje w w�tku z puli p�tl� przetwarzania ��da� pobrania plik�w z endpoint�w ftp
        /// </summary>
        public void Start()
        {
            InProgress = true;
            ThreadPool.QueueUserWorkItem(DispatchFtpThread);
        }

        /// <summary>
        /// Inicjuje w w�tku z puli p�tl� przetwarzania ��da� pobrania plik�w z endpoint�w ftp
        /// </summary>
        public void StartNow(int iEndXX)
        {
            ThreadPool.QueueUserWorkItem(ExecuteFtpTransfer, -iEndXX);
        }

        /// <summary>
        /// Przerywa oczekuj�cy w�tek i pozwala na zako�czenie pracy dispatchera
        /// </summary>
        public void Stop()
        {
            InProgress = false;
            m_are.Set();
            m_mainWnd.ShowErrorInfo(eSeverityCode.Message, this.Teraz + " Zatrzymano przetwarzanie.");
        }

        /// <summary>
        /// U�ywana w trybie: UniqueDateAndSizeInDatabase. Sprawdza, czy z danej instancji FtpGetWorkera pobrano ju� dany plik
        /// </summary>
        /// <param name="sFileName">Nazwa liku</param>
        /// <param name="lLength">D�ugo�� pliku</param>
        /// <param name="dtDate">Data ostatniej modyfikacji pliku</param>
        /// <returns>Czy z danej instancji FtpGetWorkera pobrano ju� dany plik</returns>
        public bool CheckDatabase(string sFileName, long lLength, DateTime dtDate)
        {
            var file = new FtpFileModel() {
                Instance = m_mainWnd.m_instance,
                FileName = sFileName,
                FileSize = lLength,
                FileDate = dtDate
            };

            var (status, errmsg) = FtpDiligentDatabaseClient.VerifyFile(file);
            if (!string.IsNullOrEmpty(errmsg)) {
                m_mainWnd.ShowErrorInfo(eSeverityCode.Error, $"{this.Teraz} {errmsg}");
                return false;
            }

            return status;
        }
        #endregion
    }
}
