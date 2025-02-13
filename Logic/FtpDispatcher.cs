
// -----------------------------------------------------------------------
// <copyright file="FtpDispatcher.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;
using System.Threading;

using Prism.Events;

using FtpDiligent.Events;

public sealed class FtpDispatcher : IFtpDispatcher
{

    #region fields
    /// <summary>
    /// Repozytorium danych
    /// </summary>
    private IFtpRepository m_repository;

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
    #endregion

    #region properties
    /// <summary>
    /// Pozwala dostosować wygląd interfejsu do stanu przetwarzania
    /// Ustawiana na czas wykonywania transferu
    /// </summary>
    public bool InProgress { get; set; }

    /// <summary>
    /// Pozwala dostosować wygląd interfejsu do stanu przetwarzania
    /// Ustawiana na czas wykonywania transferu
    /// </summary>
    public bool InManualRun { get; set; }
    #endregion

    #region events
    /// <summary>
    /// Rozgłasza status operacji transferu pliku
    /// </summary>
    private StatusEvent DispatcherStatusNotification;
    #endregion

    #region constructor
    /// <summary>
    /// Konstruktor FtpDispatchera
    /// </summary>
    /// <param name="wnd">Główne okno aplikacji WPF</param>
    public FtpDispatcher(IEventAggregator eventAggr, IFtpRepository repository)
    {
        m_repository = repository;
        DispatcherStatusNotification = eventAggr.GetEvent<StatusEvent>();
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
        InManualRun = true;
        m_filesTransfered = 0;
        ThreadPool.QueueUserWorkItem(ExecuteFtpTransfer, new FtpScheduleModel() {
            xx = -endpoint.XX
        });
    }

    /// <summary>
    /// Przerywa oczekujący wątek i pozwala na zakończenie pracy dispatchera
    /// </summary>
    public void Stop()
    {
        InProgress = false;
        m_are.Set();
        NotifyTransferStatus(eSeverityCode.Message, $"Zatrzymano przetwarzanie. Skopiowano {m_filesTransfered} plików.");
    }

    /// <summary>
    /// Używana w trybie: UniqueDateAndSizeInDatabase. Sprawdza, czy z danej instancji FtpGetWorkera pobrano już dany plik
    /// </summary>
    /// <param name="sFileName">Nazwa liku</param>
    /// <param name="lLength">Długość pliku</param>
    /// <param name="dtDate">Data ostatniej modyfikacji pliku</param>
    /// <returns>Czy z danej instancji FtpGetWorkera pobrano już dany plik</returns>
    public bool CheckDatabase(string sFileName, long lLength, DateTime dtDate)
    {
        var file = new FtpFileModel() {
            Instance = FtpDiligentGlobals.Instance,
            FileName = sFileName,
            FileSize = lLength,
            FileDate = dtDate
        };

        var (status, errmsg) = m_repository.VerifyFile(file);
        if (!string.IsNullOrEmpty(errmsg)) {
            NotifyTransferStatus(eSeverityCode.Error, errmsg);
            return false;
        }

        return status;
    }

    /// <summary>
    /// Udostępnia liczbę przesłanych plików
    /// </summary>
    public int GetNumberOfFilesTransferred() => m_filesTransfered;

    /// <summary>
    /// Zlicza przesłane pliki, wołane tylko przez GUI thread
    /// </summary>
    public int NotifyFileTransfer() => ++m_filesTransfered;
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
            var (schedule, errmsg) = m_repository.GetNextSync(FtpDiligentGlobals.Instance);
            if (!string.IsNullOrEmpty(errmsg)) {
                if (errmsg == "0")
                    NotifyTransferStatus(eSeverityCode.NextSync, "Nie zaplanowano żadnych pozycji w harmonogramie");
                else {
                    NotifyTransferStatus(eSeverityCode.Error, errmsg);
                    NotifyTransferStatus(eSeverityCode.Warning, $"Wstrzymanie pracy na {m_retryWaitTime / 60 / 1000} minut po błędzie");
                    lastSchedule = 0;
                    Thread.Sleep(m_retryWaitTime);
                    FtpDiligentGlobals.StartProcessing();
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
                NotifyTransferStatus(eSeverityCode.NextSync, $"Najbliższy transfer plików z harmonogramu {schedule.name} zaplanowano na {schedule.nextSyncTime:dd/MM/yyyy HH:mm}");
            else
                NotifyTransferStatus(eSeverityCode.NextSync, "Do końca tygodnia nie zaplanowano żadnych transferów.");

            if (DateTime.Now < schedule.nextSyncTime)
                m_are.WaitOne(schedule.nextSyncTime.Subtract(DateTime.Now), false);

            if (schedule.xx > 0 && InProgress)
                ThreadPool.QueueUserWorkItem(ExecuteFtpTransfer, schedule);
        } // while

        if (!InProgress)
            NotifyTransferStatus(eSeverityCode.Message, "Pobieranie przerwane przez użytkownika");
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
        var (endpoint, errmsg) = m_repository.SelectEndpoint(key).Result;
        if (!string.IsNullOrEmpty(errmsg)) {
            if (errmsg == "0")
                errmsg = "Brak definicji endpointu dla harmonogramu: " + key;

            NotifyTransferStatus(eSeverityCode.Error, errmsg);
            return;
        }

        string remote = endpoint.host + endpoint.remDir;
        FtpSyncModel log = new() { xx = key, syncTime = endpoint.nextSync };
        IFtpUtility fu = IFtpUtility.Create(endpoint, this, FtpDiligentGlobals.SyncMode);
        eFtpDirection eDirection = endpoint.direction;

        if (key < 0)
            NotifyTransferStatus(eSeverityCode.Message, $"Rozpoczęto transfer plików z serwera {remote}");
        else
            NotifyTransferStatus(eSeverityCode.Message, $"Rozpoczęto zaplanowany transfer plików {schedule.name} z serwera {remote}");

        try { // transferuj pliki
            #region pobieranie
            if (eDirection.HasFlag(eFtpDirection.Get)) {
                log.files = fu.Download();
                if (log.files == null) {
                    NotifyTransferStatus(eSeverityCode.TransferError, $"Pobieranie plików z serwera {remote} zakończyło się niepowodzeniem");
                    return;
                }

                // loguj zmiany
                int filesTransfered = log.files.Length;
                if (filesTransfered == 0) {
                    m_repository.LogActivation(log);
                    NotifyTransferStatus(eSeverityCode.Message, $"Na serwerze {remote} nie znaleziono plików odpowiednich do pobrania");
                } else {
                    log.direction = eFtpDirection.Get;
                    m_repository.LogSync(log);
                    NotifyTransferStatus(eSeverityCode.Message, $"Pobrano {filesTransfered} plików z serwera {remote}");
                }
            }
            #endregion

            #region wstawianie
            if (eDirection.HasFlag(eFtpDirection.Put)) {
                log.files = fu.Upload();
                if (log.files == null) {
                    NotifyTransferStatus(eSeverityCode.TransferError, $"Wstawianie plików na serwer {remote} zakończyło się niepowodzeniem");
                    return;
                }

                // loguj zmiany
                int filesTransfered = log.files.Length;
                if (filesTransfered == 0) {
                    m_repository.LogActivation(log);
                    NotifyTransferStatus(eSeverityCode.Message, $"Nie znaleziono plików do wstawienia na serwer {remote}");
                } else {
                    log.direction = eFtpDirection.Put;
                    m_repository.LogSync(log);
                    NotifyTransferStatus(eSeverityCode.Message, $"Wstawiono {filesTransfered} plików na serwer {remote}");
                }
            }
            #endregion
        } catch (FtpUtilityException fex) {
            NotifyTransferStatus(eSeverityCode.TransferError, fex.Message);
        } catch (Exception se) {
            NotifyTransferStatus(eSeverityCode.TransferError, se.Message);
        }
    }

    /// <summary>
    /// Triggers an DispatcherStatusNotification event with provided arguments
    /// </summary>
    /// <param name="severity">Severity code</param>
    /// <param name="message">Description</param>
    private void NotifyTransferStatus(eSeverityCode severity, string message) => DispatcherStatusNotification.Publish(new StatusEventArgs(severity, message));
    #endregion
}
