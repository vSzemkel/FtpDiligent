
// -----------------------------------------------------------------------
// <copyright file="FtpDispatcher.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;
using System.Threading;

public sealed class FtpDispatcher : IFtpDispatcher
{
    #region fields
    /// <summary>
    /// Ile czasu (ms) odczekać po błędzie pobierania harmonogramu przed ponowieniem
    /// </summary>
    private const int m_retryWaitTime = 10 * 60 * 1000;

    /// <summary>
    /// Co ile czasu (ms) sprawdzać czy scheduler pokazuje kolejny transfer
    /// </summary>
    private const int m_refractoryWaitTime = 5000;

    /// <summary>
    /// Zlicza przetransportowane pliki.
    /// Nie ma sensu restartować dispatchera, który dotąd nic nie przesłał
    /// </summary>
    private int m_filesTransfered;

    /// <summary>
    /// Do wstrzymywania wątku roboczego w oczekiwaniu na planowany czas uruchomienia
    /// </summary>
    private AutoResetEvent m_are = new(false);

    /// <summary>
    /// Klient bazy danych
    /// </summary>
    private IFtpDiligentDatabaseClient m_database { get; set; }
    #endregion

    #region properties
    /// <summary>
    /// Pozwala dostosować wygląd interfejsu do stanu przetwarzania
    /// Ustawiana na czas wykonywania transferu
    /// </summary>
    public bool InProgress { get; set; }
    #endregion

    #region constructor
    /// <summary>
    /// Konstruktor FtpDispatchera
    /// </summary>
    /// <param name="wnd">Główne okno aplikacji WPF</param>
    public FtpDispatcher(IFtpDiligentDatabaseClient database)
    {
        m_database = database;
    }
    #endregion

    #region private methods
    /// <summary>
    /// Implementacja pętli obsługującej żądania pobrania plików z endpointu ftp
    /// Jeżeli czas żądania upłynął, wykonuje je bezzwłocznie, jeśli nie, to zasypia 
    /// do czasu wymagalności żądania. Zaimplementowano metodę wyjścia z pętli na
    /// życzenie użytkownika po wywołaniu Stop(); Wartość iSchXX==-1 oznacza brak 
    /// żądań do końca tygodnia, towarzyszy jej data początku nowego tygodnia.
    /// </summary>
    /// <param name="o">Nie używany</param>
    private void DispatchFtpThread(object o)
    {
        int lastSchedule = 0;

        while (InProgress) {
            var (schedule, errmsg) = m_database.GetNextSync(FtpDispatcherGlobals.Instance);
            if (!string.IsNullOrEmpty(errmsg)) {
                if (errmsg == "0")
                    FtpDispatcherGlobals.ShowError(eSeverityCode.NextSync, "Nie zaplanowano żadnych pozycji w harmonogramie");
                else {
                    FtpDispatcherGlobals.ShowError(eSeverityCode.Error, errmsg);
                    FtpDispatcherGlobals.ShowError(eSeverityCode.Warning, $"Wstrzymanie pracy na {m_retryWaitTime / 60 / 1000} minut po błędzie");
                    lastSchedule = 0;
                    Thread.Sleep(m_retryWaitTime);
                    FtpDispatcherGlobals.StartProcessing();
                }
                return;
            }

            int currentSchedule = schedule.Hash;
            if (currentSchedule == lastSchedule) {
                Thread.Sleep(m_refractoryWaitTime);
                continue;
            }

            lastSchedule = currentSchedule;
            if (schedule.xx > 0)
                FtpDispatcherGlobals.ShowError(eSeverityCode.NextSync, $"Najbliższy transfer plików z harmonogramu {schedule.name} zaplanowano na {schedule.nextSyncTime:dd/MM/yyyy HH:mm}");
            else
                FtpDispatcherGlobals.ShowError(eSeverityCode.NextSync, "Do końca tygodnia nie zaplanowano żadnych transferów.");

            if (DateTime.Now < schedule.nextSyncTime)
                m_are.WaitOne(schedule.nextSyncTime.Subtract(DateTime.Now), false);

            if (schedule.xx > 0 && InProgress)
                ThreadPool.QueueUserWorkItem(ExecuteFtpTransfer, schedule);
        } // while

        if (!InProgress)
            FtpDispatcherGlobals.ShowError(eSeverityCode.Message, "Pobieranie przerwane przez u�ytkownika");
    }

    /// <summary>
    /// Wykonuje transfer plików na podstawie zaplanowanej pozycji harmonogramu
    /// Jest uruchamiana przez dispatcher o odpowiedniej porze i tylko dla poprawnych pozycji harmonogramu
    /// </summary>
    /// <param name="iSchXX">
    /// Jeśli dodatni, to identyfikator pozycji harmonogramu uruchomionej automatycznie,
    /// jeśli ujemny, to identyfikator endpointu, dla którego transfer uruchomiono ręcznie
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
        FtpSyncModel log = new() { xx = key, syncTime = endpoint.nextSync };
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
    /// Inicjuje w wątku z puli pętlą przetwarzania żądań pobranie plików z endpointów ftp
    /// </summary>
    public void Start()
    {
        InProgress = true;
        m_filesTransfered = 0;
        ThreadPool.QueueUserWorkItem(DispatchFtpThread);
    }

    /// <summary>
    /// Inicjuje w wątku z puli pętlą przetwarzania żądań
    /// niezwłoczne pobranie plików z konkretnego endpointu Ftp
    /// </summary>
    /// <param name="endpoint">Endpoint, dla którego symulujemy wywołanie z harmonogramu</param>
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
    /// Przerywa oczekujący wątek i pozwala na zakończenie pracy dispatchera
    /// </summary>
    public void Stop()
    {
        InProgress = false;
        m_are.Set();
        FtpDispatcherGlobals.ShowError(eSeverityCode.Message, $"Zatrzymano przetwarzanie. Skopiowano {m_filesTransfered} plików.");
    }

    /// <summary>
    /// Używana w trybie: UniqueDateAndSizeInDatabase. Sprawdza, czy z danej instancji FtpGetWorkera pobrano ju� dany plik
    /// </summary>
    /// <param name="sFileName">Nazwa liku</param>
    /// <param name="lLength">Długość pliku</param>
    /// <param name="dtDate">Data ostatniej modyfikacji pliku</param>
    /// <returns>Czy z danej instancji FtpGetWorkera pobrano już dany plik</returns>
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

    /// <summary>
    /// Udostępnia liczbę przesłanych plików
    /// </summary>
    public int GetNumberOfFilesTransferred() => m_filesTransfered;

    /// <summary>
    /// Zlicza przesłane pliki
    /// </summary>
    public void NotifyFileTransfer() => ++m_filesTransfered;
    #endregion
}
