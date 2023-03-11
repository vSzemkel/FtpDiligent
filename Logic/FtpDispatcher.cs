
// -----------------------------------------------------------------------
// <copyright file="FtpDispatcher.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;

public sealed class FtpDispatcher
{
    #region fields
    /// <summary>
    /// Ile czasu (ms) odczekać po błędzie pobierania harmonogramu przed ponowieniem
    /// </summary>
    public const int m_retryWaitTime = 10 * 60 * 1000;

    /// <summary>
    /// Zlicza przetransportowane pliki.
    /// Nie ma sensu restartować dispatchera, który dotąd nic nie przes�a�
    /// </summary>
    public int m_filesTransfered;

    /// <summary>
    /// Do wstrzymywania w�tku roboczego w oczekiwaniu na planowany czas uruchomienia
    /// </summary>
    private AutoResetEvent m_are = new AutoResetEvent(false);

    /// <summary>
    /// Klient bazy danych
    /// </summary>
    private IFtpDiligentDatabaseClient m_database { get; set; }
    #endregion

    #region properties
    /// <summary>
    /// Pozwala dostosowa� wygl�d interfejsu do stanu przetwarzania
    /// Ustawiana na czas wykonywania transferu
    /// </summary>
    public bool InProgress { get; set; }
    #endregion

    #region constructor
    /// <summary>
    /// Konstruktor FtpDispatchera
    /// </summary>
    /// <param name="wnd">G��wne okno aplikacji WPF</param>
    public FtpDispatcher(IFtpDiligentDatabaseClient database)
    {
        m_database = database;
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
        int lastSchedule = 0;

        while (InProgress) {
            var (schedule, errmsg) = m_database.GetNextSync(FtpDispatcherGlobals.Instance);
            if (!string.IsNullOrEmpty(errmsg)) {
                if (errmsg == "0")
                    FtpDispatcherGlobals.ShowError(eSeverityCode.NextSync, "Nie zaplanowano �adnych pozycji w harmonogramie");
                else {
                    FtpDispatcherGlobals.ShowError(eSeverityCode.Error, errmsg);
                    FtpDispatcherGlobals.ShowError(eSeverityCode.Warning, $"Wstrzymanie pracy na {m_retryWaitTime / 60 / 1000} minut po b��dzie");
                    lastSchedule = 0;
                    Thread.Sleep(m_retryWaitTime);
                    Sterowanie.s_execute();
                }
                return;
            }

            int currentSchedule = schedule.Hash;
            if (currentSchedule == lastSchedule) {
                Thread.Sleep(5000);
                continue;
            }

            lastSchedule = currentSchedule;
            if (schedule.xx > 0)
                FtpDispatcherGlobals.ShowError(eSeverityCode.NextSync, $"Najbli�szy transfer plik�w z harmonogramu {schedule.name} zaplanowano na {schedule.nextSyncTime:dd/MM/yyyy HH:mm}");
            else
                FtpDispatcherGlobals.ShowError(eSeverityCode.NextSync, "Do ko�ca tygodnia nie zaplanowano �adnych transfer�w");

            if (DateTime.Now < schedule.nextSyncTime)
                m_are.WaitOne(schedule.nextSyncTime.Subtract(DateTime.Now), false);

            if (schedule.xx > 0 && InProgress)
                ThreadPool.QueueUserWorkItem(ExecuteFtpTransfer, schedule);
        } // while

        if (!InProgress)
            FtpDispatcherGlobals.ShowError(eSeverityCode.Message, "Pobieranie przerwane przez u�ytkownika");
    }

    /// <summary>
    /// Wykonuje transfer plik�w na podstawie zaplanowanej pozycji harmonogramu
    /// Jest uruchamiana przez dispatcher o odpowiedniej porze i tylko dla poprawnych pozycji harmonogramu
    /// </summary>
    /// <param name="iSchXX">
    /// Je�li dodatni, to identyfikator pozycji harmonogramu uruchomionej automatycznie,
    /// je�li ujemny, to identyfikator endpointu, dla kt�rego transfer uruchomiono r�cznie
    /// </param>
    private void ExecuteFtpTransfer(object o)
    {
        FtpScheduleModel schedule = (FtpScheduleModel)o;
        int key = schedule.xx;
        var (endpoint, errmsg) = m_database.SelectEndpoint(key).Result;
        if (!string.IsNullOrEmpty(errmsg)) {
            if (errmsg == "0")
                errmsg = "Brak definicji endpointu dla harmonogramu: " + key;

            FtpDispatcherGlobals.ShowError(eSeverityCode.Error, errmsg);
            return;
        }

        string remote = endpoint.host + endpoint.remDir;
        FtpSyncModel log = new FtpSyncModel() { xx = key, syncTime = endpoint.nextSync };
        IFtpUtility fu = IFtpUtility.Create(endpoint, this, FtpDispatcherGlobals.SyncMode);
        eFtpDirection eDirection = endpoint.direction;

        if (key < 0)
            FtpDispatcherGlobals.ShowError(eSeverityCode.Message, $"Rozpocz�to transfer plik�w z serwera {remote}");
        else
            FtpDispatcherGlobals.ShowError(eSeverityCode.Message, $"Rozpocz�to zaplanowany transfer plik�w {schedule.name} z serwera {remote}");

        try { // transferuj pliki
            #region pobieranie
            if (eDirection.HasFlag(eFtpDirection.Get)) {
                log.files = fu.Download();
                if (log.files == null) {
                    FtpDispatcherGlobals.ShowError(eSeverityCode.TransferError, $"Pobieranie plik�w z serwera {remote} zako�czy�o si� niepowodzeniem");
                    return;
                }

                // loguj zmiany
                int filesTransfered = log.files.Length;
                if (filesTransfered == 0) {
                    m_database.LogActivation(log);
                    FtpDispatcherGlobals.ShowError(eSeverityCode.Message, $"Na serwerze {remote} nie znaleziono plik�w odpowiednich do pobrania");
                } else {
                    log.direction = eFtpDirection.Get;
                    m_database.LogSync(log);
                    FtpDispatcherGlobals.ShowError(eSeverityCode.Message, $"Pobrano {filesTransfered} plik�w z serwera {remote}");
                }
            }
            #endregion

            #region wstawianie
            if (eDirection.HasFlag(eFtpDirection.Put)) {
                log.files = fu.Upload();
                if (log.files == null) {
                    FtpDispatcherGlobals.ShowError(eSeverityCode.TransferError, $"Wstawianie plik�w na serwer {remote} zako�czy�o si� niepowodzeniem");
                    return;
                }

                // loguj zmiany
                int filesTransfered = log.files.Length;
                if (filesTransfered == 0) {
                    m_database.LogActivation(log);
                    FtpDispatcherGlobals.ShowError(eSeverityCode.Message, $"Nie znaleziono plik�w do wstawienia na serwer {remote}");
                } else {
                    log.direction = eFtpDirection.Put;
                    m_database.LogSync(log);
                    FtpDispatcherGlobals.ShowError(eSeverityCode.Message, $"Wstawiono {filesTransfered} plik�w na serwer {remote}");
                }
            }
            #endregion
        } catch (FtpUtilityException fex) {
            FtpDispatcherGlobals.ShowError(eSeverityCode.TransferError, fex.Message);
        } catch (Exception se) {
            FtpDispatcherGlobals.ShowError(eSeverityCode.TransferError, se.Message);
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
        m_filesTransfered = 0;
        ThreadPool.QueueUserWorkItem(DispatchFtpThread);
    }

    /// <summary>
    /// Inicjuje w w�tku z puli p�tl� przetwarzania ��da� pobrania plik�w z endpoint�w ftp
    /// </summary>
    /// <param name="endpoint">Endpoint, dla kt�rego symulujemy wywo�anie z harmonogramu</param>
    public void StartNow(FtpEndpoint endpoint)
    {
        bool oldInProgress = InProgress;
        InProgress = true;
        m_filesTransfered = 0;
        ThreadPool.QueueUserWorkItem(ExecuteFtpTransfer, new FtpScheduleModel() {
            xx = -endpoint.XX
        });
        InProgress = oldInProgress;
    }

    /// <summary>
    /// Przerywa oczekuj�cy w�tek i pozwala na zako�czenie pracy dispatchera
    /// </summary>
    public void Stop()
    {
        InProgress = false;
        m_are.Set();
        FtpDispatcherGlobals.ShowError(eSeverityCode.Message, $"Zatrzymano przetwarzanie. Skopiowano {m_filesTransfered} plik�w.");
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
            Instance = FtpDispatcherGlobals.Instance,
            FileName = sFileName,
            FileSize = lLength,
            FileDate = dtDate
        };

        var (status, errmsg) = m_database.VerifyFile(file);
        if (!string.IsNullOrEmpty(errmsg)) {
            FtpDispatcherGlobals.ShowError(eSeverityCode.Error, errmsg);
            return false;
        }

        return status;
    }
    #endregion
}
