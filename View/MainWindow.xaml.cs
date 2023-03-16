
// -----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using Autofac;

using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Windows;

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
    public Sterowanie m_tbSterowanie;

    /// <summary>
    /// Zakładka Serwery
    /// </summary>
    public Serwery m_tbSerwery;

    /// <summary>
    /// Zakładka do edycji danych serwera, leniwie inicjalizowana
    /// </summary>
    public SerweryDetails _m_tbSerweryDetails;

    /// <summary>
    /// Zakładka Harmonogramy
    /// </summary>
    public Harmonogramy m_tbHarmonogramy;

    /// <summary>
    /// Zakładka do edycji danych harmonogramu, leniwie inicjalizowana
    /// </summary>
    public HarmonogramyDetails _m_tbHarmonogramyDetails;

    /// <summary>
    /// Klient bazy danych
    /// </summary>
    private IFtpDiligentDatabaseClient m_database;
    #endregion

    #region properties
    public eSyncFileMode m_syncModeProp { 
        get => FtpDispatcherGlobals.SyncMode;
        set { FtpDispatcherGlobals.SyncMode = value; }
    }

    public SerweryDetails m_tbSerweryDetails {
        get {
            if (_m_tbSerweryDetails == null) {
                _m_tbSerweryDetails = new SerweryDetails(this, m_database);
                _m_tbSerweryDetails.m_mainWnd = this;
                tabSerweryDetails.Content = _m_tbSerweryDetails;
            }

            return _m_tbSerweryDetails;
        }
    }

    public HarmonogramyDetails m_tbHarmonogramyDetails {
        get {
            if (_m_tbHarmonogramyDetails == null) {
                _m_tbHarmonogramyDetails = new HarmonogramyDetails(this, m_database);
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
    /// <param name="database">Klient bazy danych</param>
    public MainWindow(IFtpDiligentDatabaseClient database)
    {
        InitializeComponent();
        var diMainWindow = new TypedParameter(typeof(MainWindow), this);
        m_tbSerwery = FtpDispatcherGlobals.AutofacScope.Resolve<Serwery>(diMainWindow);
        m_tbSterowanie = FtpDispatcherGlobals.AutofacScope.Resolve<Sterowanie>(diMainWindow);
        m_tbHarmonogramy = FtpDispatcherGlobals.AutofacScope.Resolve<Harmonogramy>(diMainWindow);
        m_tbSterowanie.tbFilesCount.DataContext = 0;
        m_tbSterowanie.cbSyncMode.DataContext = this;
        this.tabSterowanie.Content = m_tbSterowanie;
        this.tabSerwery.Content = m_tbSerwery;
        this.tabHarmonogramy.Content = m_tbHarmonogramy;

        FtpDispatcherGlobals.ShowError = this.ShowErrorInfo;
        this.Title = $"FtpDiligent [instance {FtpDispatcherGlobals.Instance}]";

        m_database = database;
        m_tbSerwery.LoadEndpoints();
        m_tbHarmonogramy.LoadSchedules(0);

        CheckEventLog();
        LoadConfig();
        CheckInstanceInitialization();
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
    #endregion

    #region private
    /// <summary>
    /// Aktualizuje kontrolki na zakładce Sterowanie
    /// </summary>
    /// <param name="code">Kategoria powiadomienia</param>
    /// <param name="message">Treść powiadomienia</param>
    private void ShowErrorInfoInternal(eSeverityCode code, string message)
    {
        switch (code) {
            case eSeverityCode.NextSync:
                m_tbSterowanie.tbNextSync.Text = message;
                break;
            case eSeverityCode.Message:
                m_tbSterowanie.lbLog.Items.Insert(0, $"{DateTime.Now:dd/MM/yyyy HH:mm} {message}");
                if (FtpDispatcherGlobals.TraceLevel.HasFlag(eSeverityCode.Message))
                    EventLog.WriteEntry(FtpDispatcherGlobals.EventLog, message, EventLogEntryType.Information);
                break;
            case eSeverityCode.FileInfo:
                BindFileInfo(message);
                if (FtpDispatcherGlobals.TraceLevel.HasFlag(eSeverityCode.FileInfo))
                    EventLog.WriteEntry(FtpDispatcherGlobals.EventLog, message, EventLogEntryType.SuccessAudit);
                break;
            case eSeverityCode.Warning:
                m_tbSterowanie.m_errInfo.Insert(0, new FtpErrorModel() { Category = code, Message = message });
                if (FtpDispatcherGlobals.TraceLevel.HasFlag(eSeverityCode.Warning))
                    EventLog.WriteEntry(FtpDispatcherGlobals.EventLog, message, EventLogEntryType.Warning);
                break;
            case eSeverityCode.TransferError:
                m_tbSterowanie.RestartScheduler();
                m_mailer.Run(message);
                goto case eSeverityCode.Error;
            case eSeverityCode.Error:
                m_tbSterowanie.m_errInfo.Insert(0, new FtpErrorModel() { Category = code, Message = message });
                if (FtpDispatcherGlobals.TraceLevel.HasFlag(eSeverityCode.Error))
                    EventLog.WriteEntry(FtpDispatcherGlobals.EventLog, message, EventLogEntryType.Error);
                break;
        }
    }

    /// <summary>
    /// Parsuje tekstową informację o przetworzonym pliku,
    /// aktualizuje liste plików i licznik
    /// </summary>
    /// <param name="message">eFtpDirection|Name|Size|Date</param>
    private void BindFileInfo(string message)
    {
        var items = message.Split('|');
        var list = m_tbSterowanie.m_fileInfo;
        list.Insert(0, new FtpFileModel() {
            Instance = Byte.Parse(items[0]),
            FileName = items[1],
            FileSize = long.Parse(items[2]),
            FileDate = DateTime.FromBinary(long.Parse(items[3]))
        });

        if (list.Count > m_fileLogSize)
            list.RemoveAt(m_fileLogSize);
        m_tbSterowanie.tbFilesCount.DataContext = ++m_tbSterowanie.m_filesCount;
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
        m_mailer = new SendEmails(this, settings["ErrorsMailTo"], settings["SendGridKey"]);
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
        (FtpDispatcherGlobals.Instance, errmsg) = m_database.InitInstance(localHostname);
        if (!string.IsNullOrEmpty(errmsg))
            ShowErrorInfoInternal(eSeverityCode.Error, errmsg);
    }

    /// <summary>
    /// Zapisuje aktualne ustawienia konfiguracyjne
    /// </summary>
    private void SaveConfig()
    {
        string syncMethod = m_tbSterowanie.cbSyncMode.SelectedValue.ToString();
        Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        config.AppSettings.Settings.Remove("SyncMethod");
        config.AppSettings.Settings.Add("SyncMethod", syncMethod);
        config.AppSettings.Settings.Remove("InstanceId");
        config.AppSettings.Settings.Add("InstanceId", FtpDispatcherGlobals.Instance.ToString());
        config.Save(ConfigurationSaveMode.Modified);
    }
    #endregion
}
