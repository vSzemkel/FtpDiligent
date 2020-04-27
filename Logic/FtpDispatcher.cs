
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
        /// Referencja do g³ównego okna
        /// </summary>
        public MainWindow m_mainWnd;

        /// <summary>
        /// Do wstrzymywania w¹tku roboczego w oczekiwaniu na planowany czas uruchomienia
        /// </summary>
        private AutoResetEvent m_are = new AutoResetEvent(false);
        #endregion

        #region properties
        /// <summary>
        /// Pozwala dostosowaæ wygl¹d interfejsu do stanu przetwarzania
        /// Ustawiana na czas wykonywania transferu
        /// </summary>
        public bool InProgress { get; set; }

        /// <summary>
        /// Sformatowany zapis czasu bie¿¹cego do logów i komunikatów
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
        /// <param name="wnd">G³ówne okno aplikacji WPF</param>
        public FtpDispatcher(MainWindow wnd)
        {
            m_mainWnd = wnd;
        }
        #endregion

        #region private methods
        /// <summary>
        /// Implementacja pêtli obs³uguj¹cej ¿¹dania pobrania plików z endpointu ftp
        /// Je¿eli czas ¿¹dania up³yn¹³, wykonuje je bezzw³ocznie, jeœli nie, to zasypia 
        /// do czasu wymagalnoœci ¿¹dania. Zaimplementowano metodê wyjœcia z pêtli na
        /// ¿yczenie u¿ytkownika po wywo³aniu Stop(); Wartoœæ iSchXX==-1 oznacza brak 
        /// ¿¹dañ do koñca tygodnia, towarzyszy jej data pocz¹tku nowego tygodnia.
        /// </summary>
        /// <param name="o">Nie u¿ywany</param>
        private void DispatchFtpThread(object o)
        {
            int iLastSchXX = -2;
            DateTime dtLastScheduleTime = DateTime.MaxValue;

            while (InProgress) {
                var (schedule, errmsg) = FtpDiligentDatabaseClient.GetNextSync(m_mainWnd.m_instance);
                if (!string.IsNullOrEmpty(errmsg)) {
                    if (errmsg == "0")
                        m_mainWnd.ShowErrorInfo(eSeverityCode.NextSync, "Nie zaplanowano ¿adnych pozycji w harmonogramie");
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
                        m_mainWnd.ShowErrorInfo(eSeverityCode.NextSync, "Najbli¿szy transfer plików z harmonogramu " + schedule.name + " zaplanowano na " + schedule.nextSyncTime.ToString("dd/MM/yyyy HH:mm"));
                    else
                        m_mainWnd.ShowErrorInfo(eSeverityCode.NextSync, "Do koñca tygodnia nie zaplanowano ¿adnych transferów");
                }

                if (DateTime.Now < schedule.nextSyncTime)
                    m_are.WaitOne(schedule.nextSyncTime.Subtract(DateTime.Now), false);

                if (schedule.xx > 0 && InProgress)
                    ThreadPool.QueueUserWorkItem(ExecuteFtpTransfer, schedule.xx);
            } // while

            if (!InProgress)
                m_mainWnd.ShowErrorInfo(eSeverityCode.Message, this.Teraz + " Pobieranie przerwane przez u¿ytkownika");
        }

        /// <summary>
        /// Wykonuje transfer plików na podstawie zaplanowanej pozycji harmonogramu
        /// Jest uruchamiana przez dispatcher o odpowiedniej porze i tylko dla poprawnych pozycji harmonogramu
        /// </summary>
        /// <param name="iSchXX">
        /// Jeœli dodatni, to identyfikator pozycji harmonogramu uruchomionej automatycznie,
        /// jeœli ujemny, to identyfikator endpointu, dla którego transfer uruchomiono rêcznie
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
                    m_mainWnd.ShowErrorInfo(eSeverityCode.Message, this.Teraz + " Rozpoczêto transfer plików z serwera " + sHost);
                    oldInProgress = InProgress;
                    InProgress = true;
                } else
                    m_mainWnd.ShowErrorInfo(eSeverityCode.Message, this.Teraz + " Rozpoczêto zaplanowany transfer plików z serwera " + sHost);

                // transferuj pliki

                #region pobieranie
                if ((eDirection & eFtpDirection.Get) > 0) {
                    if (!fu.Download(ref log)) {
                        m_mainWnd.ShowErrorInfo(eSeverityCode.TransferError, this.Teraz + " Pobieranie plików z serwera " + sHost + " zakoñczy³o siê niepowodzeniem");
                        return;
                    }

                    if (schedule < 0)
                        InProgress = oldInProgress;

                    // loguj zmiany
                    if (log.fileNames.Length == 0) {
                        FtpDiligentDatabaseClient.LogActivation(log);
                        m_mainWnd.ShowErrorInfo(eSeverityCode.Message, this.Teraz + " Na serwerze " + sHost + " nie znaleziono plików odpowiednich do pobrania");
                    } else {
                        log.direction = eFtpDirection.Get;
                        FtpDiligentDatabaseClient.LogSync(log);
                        m_mainWnd.ShowErrorInfo(eSeverityCode.Message, this.Teraz + " Pobrano " + log.fileNames.Length + " plików z serwera " + sHost);
                    }
                }
                #endregion

                #region wstawianie
                if ((eDirection & eFtpDirection.Put) > 0) {
                    if (!fu.Upload(ref log)) {
                        m_mainWnd.ShowErrorInfo(eSeverityCode.TransferError, this.Teraz + " Wstawianie plików na serwer " + sHost + " zakoñczy³o siê niepowodzeniem");
                        return;
                    }

                    if (schedule < 0)
                        InProgress = oldInProgress;

                    // loguj zmiany
                    if (log.fileNames.Length == 0) {
                        FtpDiligentDatabaseClient.LogActivation(log);
                        m_mainWnd.ShowErrorInfo(eSeverityCode.Message, this.Teraz + " Nie znaleziono plików do wstawienia na serwer " + sHost);
                    } else {
                        log.direction = eFtpDirection.Put;
                        FtpDiligentDatabaseClient.LogSync(log);
                        m_mainWnd.ShowErrorInfo(eSeverityCode.Message, this.Teraz + " Wstawiono " + log.fileNames.Length + " plików na serwer " + sHost);
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
        /// Inicjuje w w¹tku z puli pêtlê przetwarzania ¿¹dañ pobrania plików z endpointów ftp
        /// </summary>
        public void Start()
        {
            InProgress = true;
            ThreadPool.QueueUserWorkItem(DispatchFtpThread);
        }

        /// <summary>
        /// Inicjuje w w¹tku z puli pêtlê przetwarzania ¿¹dañ pobrania plików z endpointów ftp
        /// </summary>
        public void StartNow(int iEndXX)
        {
            ThreadPool.QueueUserWorkItem(ExecuteFtpTransfer, -iEndXX);
        }

        /// <summary>
        /// Przerywa oczekuj¹cy w¹tek i pozwala na zakoñczenie pracy dispatchera
        /// </summary>
        public void Stop()
        {
            InProgress = false;
            m_are.Set();
            m_mainWnd.ShowErrorInfo(eSeverityCode.Message, this.Teraz + " Zatrzymano przetwarzanie.");
        }

        /// <summary>
        /// U¿ywana w trybie: UniqueDateAndSizeInDatabase. Sprawdza, czy z danej instancji FtpGetWorkera pobrano ju¿ dany plik
        /// </summary>
        /// <param name="sFileName">Nazwa liku</param>
        /// <param name="lLength">D³ugoœæ pliku</param>
        /// <param name="dtDate">Data ostatniej modyfikacji pliku</param>
        /// <returns>Czy z danej instancji FtpGetWorkera pobrano ju¿ dany plik</returns>
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
