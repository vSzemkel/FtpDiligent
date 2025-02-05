
// -----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent.Views;

using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Windows;

using FtpDiligent.ViewModels;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    #region fields
    /// <summary>
    /// Długość logu transferowanych plików
    /// </summary>
    private const int m_fileLogSize = 100;

    /// <summary>
    /// Wysyła mailem powiadomienia o błędach
    /// </summary>
    public SendEmails m_mailer;

    /// <summary>
    /// Zakładka Sterowanie
    /// </summary>
    public SterowanieViewModel m_tbSterowanie;

    /// <summary>
    /// Zakładka Serwery
    /// </summary>
    public SerweryViewModel m_tbSerwery;

    /// <summary>
    /// Zakładka do edycji danych serwera, leniwie inicjalizowana
    /// </summary>
    private SerweryDetails _m_tbSerweryDetails;

    /// <summary>
    /// Zakładka Harmonogramy
    /// </summary>
    public HarmonogramyViewModel m_tbHarmonogramy;

    /// <summary>
    /// Zakładka do edycji danych harmonogramu, leniwie inicjalizowana
    /// </summary>
    private HarmonogramyDetails _m_tbHarmonogramyDetails;

    /// <summary>
    /// Repozytorium danych
    /// </summary>
    private IFtpRepository m_repository;
    #endregion

    #region properties
    public eSyncFileMode m_syncModeProp { 
        get => FtpDispatcherGlobals.SyncMode;
        set { FtpDispatcherGlobals.SyncMode = value; }
    }

    public SerweryDetails m_tbSerweryDetails {
        get {
            if (_m_tbSerweryDetails == null) {
                _m_tbSerweryDetails = new SerweryDetails(this, m_repository);
                _m_tbSerweryDetails.m_mainWnd = this;
                tabSerweryDetails.Content = _m_tbSerweryDetails;
            }

            return _m_tbSerweryDetails;
        }
    }

    public HarmonogramyDetails m_tbHarmonogramyDetails {
        get {
            if (_m_tbHarmonogramyDetails == null) {
                _m_tbHarmonogramyDetails = new HarmonogramyDetails(this, m_repository);
                _m_tbHarmonogramyDetails.m_mainWnd = this;
                tabHarmonogramyDetails.Content = _m_tbHarmonogramyDetails;
            }

            return _m_tbHarmonogramyDetails;
        }
    }
    #endregion

    #region constructor
    /// <summary>
    /// Konstruktor okna głównego
    /// </summary>
    /// <param name="repository">Repozytorium danych</param>
    public MainWindow(IFtpRepository repository, IFtpDispatcher dispatcher)
    {
        InitializeComponent();
        m_repository = repository;
        FtpUtilityBase.FileTransferred += ShowNotification;
        FtpUtilityBase.TransferStatusNotification += ShowStatus;
        FtpDispatcher.DispatcherStatusNotification += ShowStatus;
        SendEmails.MailNotificationStatus += ShowStatus;

        CheckEventLog();
        LoadConfig();
        CheckInstanceInitialization();

        this.Title = $"FtpDiligent [instance {FtpDispatcherGlobals.Instance}]";
    }
    #endregion

    #region handlers
    /// <summary>
    /// Gdy okno zostało zamknięte
    /// </summary>
    private void Window_Closed(object sender, EventArgs e)
    {
        SaveConfig();
    }
    #endregion

    #region public
    public void ShowErrorInfo(eSeverityCode code, string message)
    {
        if (Dispatcher.CheckAccess())
            ShowErrorInfoInternal(code, message);
        else
            Dispatcher.Invoke(ShowErrorInfoInternal, code, message);
    }

    public void ShowStatus(object sender, TransferNotificationEventArgs arg)
    {
        if (Dispatcher.CheckAccess())
            ShowErrorInfoInternal(arg.severity, arg.message);
        else
            Dispatcher.Invoke(ShowErrorInfoInternal, arg.severity, arg.message);
    }

    public void ShowNotification(object sender, FileTransferredEventArgs arg)
    {
        if (Dispatcher.CheckAccess())
            ShowTransferDetails(arg);
        else
            Dispatcher.Invoke(ShowTransferDetails, arg);
    }
    #endregion

    #region private
    /// <summary>
    /// Aktualizuje kontrolki na zakładce Sterowanie
    /// </summary>
    /// <param name="code">Kategoria powiadomienia</param>
    /// <param name="message">Treść powiadomienia</param>
    private void ShowErrorInfoInternal(eSeverityCode code, string message)
    {
        if (code == eSeverityCode.NextSync)
            m_tbSterowanie.NextSyncDateTime = message;
        else {
            m_tbSterowanie.ErrorLog.Insert(0, new FtpErrorModel() { Category = code, Message = message });
            if (FtpDispatcherGlobals.TraceLevel.HasFlag(eSeverityCode.Warning))
                EventLog.WriteEntry(FtpDispatcherGlobals.EventLog, message, EventLogEntryType.Warning);
        }
    }

    /// <summary>
    /// Aktualizuje informacje o przesyłaniu plików na zakładce Sterowanie
    /// </summary>
    /// <param name="arg">Szczegóły operacji</param>
    private void ShowTransferDetails(FileTransferredEventArgs arg)
    {
        switch (arg.severity) {
            case eSeverityCode.Message:
                m_tbSterowanie.MessageLog.Insert(0, $"{DateTime.Now:dd/MM/yyyy HH:mm} {arg.message}");
                if (FtpDispatcherGlobals.TraceLevel.HasFlag(eSeverityCode.Message))
                    EventLog.WriteEntry(FtpDispatcherGlobals.EventLog, arg.message, EventLogEntryType.Information);
                break;
            case eSeverityCode.FileInfo:
                BindFileInfo(arg);
                if (FtpDispatcherGlobals.TraceLevel.HasFlag(eSeverityCode.FileInfo))
                    EventLog.WriteEntry(FtpDispatcherGlobals.EventLog, arg.message, EventLogEntryType.SuccessAudit);
                break;
            case eSeverityCode.Warning:
                m_tbSterowanie.ErrorLog.Insert(0, new FtpErrorModel() { Category = arg.severity, Message = arg.message });
                if (FtpDispatcherGlobals.TraceLevel.HasFlag(eSeverityCode.Warning))
                    EventLog.WriteEntry(FtpDispatcherGlobals.EventLog, arg.message, EventLogEntryType.Warning);
                break;
            case eSeverityCode.TransferError:
                m_tbSterowanie.RestartScheduler();
                m_mailer.Run(arg.message);
                goto case eSeverityCode.Error;
            case eSeverityCode.Error:
                m_tbSterowanie.ErrorLog.Insert(0, new FtpErrorModel() { Category = arg.severity, Message = arg.message });
                if (FtpDispatcherGlobals.TraceLevel.HasFlag(eSeverityCode.Error))
                    EventLog.WriteEntry(FtpDispatcherGlobals.EventLog, arg.message, EventLogEntryType.Error);
                break;
        }
    }

    /// <summary>
    /// Parsuje tekstową informację o przetworzonym pliku,
    /// aktualizuje liste plików i licznik
    /// </summary>
    /// <param name="message">eFtpDirection|Name|Size|Date</param>
    private void BindFileInfo(FileTransferredEventArgs arg)
    {
        var list = m_tbSterowanie.FtpFileLog;
        list.Insert(0, new FtpFileModel() {
            Instance = (byte)arg.direction,
            FileName = arg.file.FullName,
            FileSize = arg.file.Length,
            FileDate = arg.file.LastWriteTime
        });

        if (list.Count > m_fileLogSize)
            list.RemoveAt(m_fileLogSize);

        m_tbSterowanie.NotifyFileTransfer();
    }

    /// <summary>
    /// Inicjalizuje EventLog, gdy działa z prawami Admina
    /// </summary>
    private void CheckEventLog()
    {
        try {
            if (FtpDispatcherGlobals.TraceLevel > 0 && !EventLog.SourceExists(FtpDispatcherGlobals.EventLog))
                EventLog.CreateEventSource(FtpDispatcherGlobals.EventLog, FtpDispatcherGlobals.EventLog);
        } catch (System.Security.SecurityException) {
            MessageBox.Show($"Aby dokończyć instalację, uruchom {System.Reflection.Assembly.GetExecutingAssembly().Location} po raz pierwszy jako Administrator.", "Wymagana inicjalizacja", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    /// <summary>
    /// Inicjalizuje konfigurację programu
    /// </summary>
    private void LoadConfig()
    {
        byte traceLevel;
        var settings = ConfigurationManager.AppSettings;
        Byte.TryParse(settings["TraceLevel"], out traceLevel);
        Int32.TryParse(settings["InstanceID"], out FtpDispatcherGlobals.Instance);
        Int32.TryParse(settings["HotfolderInterval"], out FtpDispatcherGlobals.HotfolderInterval);
        FtpDispatcherGlobals.TraceLevel = (eSeverityCode)traceLevel;
        m_mailer = new SendEmails(settings["ErrorsMailTo"], settings["SendGridKey"]);
        FtpDispatcherGlobals.CheckTransferedStorage = bool.Parse(settings["CheckTransferedFile"]);

        if (!Enum.TryParse<eSyncFileMode>(settings["SyncMethod"], out FtpDispatcherGlobals.SyncMode)) {
            ShowErrorInfoInternal(eSeverityCode.Warning, "Parametr SyncMethod ma nieprawidłową wartość.");
            FtpDispatcherGlobals.SyncMode = eSyncFileMode.UniqueDateAndSizeInDatabase;
        }

        try {
            CultureInfo.CurrentUICulture = new CultureInfo(settings["CultureInfo"]);
        } catch {
            ShowErrorInfoInternal(eSeverityCode.Warning, "Parametr CultureInfo ma nieprawidłową wartość.");
        }
    }

    /// <summary>
    /// Inicjalizuje identyfikator instancji przy pierwszym uruchomieniu
    /// </summary>
    private void CheckInstanceInitialization()
    {
        if (FtpDispatcherGlobals.Instance > 0)
            return;

        string errmsg, localHostname = Dns.GetHostName();
        (FtpDispatcherGlobals.Instance, errmsg) = m_repository.InitInstance(localHostname);
        if (!string.IsNullOrEmpty(errmsg))
            ShowErrorInfoInternal(eSeverityCode.Error, errmsg);
    }

    /// <summary>
    /// Zapisuje aktualne ustawienia konfiguracyjne
    /// </summary>
    private void SaveConfig()
    {
        string syncMethod = FtpDispatcherGlobals.SyncMode.ToString();
        Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        config.AppSettings.Settings.Remove("SyncMethod");
        config.AppSettings.Settings.Add("SyncMethod", syncMethod);
        config.AppSettings.Settings.Remove("InstanceId");
        config.AppSettings.Settings.Add("InstanceId", FtpDispatcherGlobals.Instance.ToString());
        config.Save(ConfigurationSaveMode.Modified);
    }
    #endregion
}
