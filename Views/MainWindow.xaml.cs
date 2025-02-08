
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
    public MainWindow(IFtpRepository repository)
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

        this.Title = $"FtpDiligent [instance {FtpDiligentGlobals.Instance}]";
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
            m_tbSterowanie.GuiShowInfo(code, message);
        else
            Dispatcher.Invoke(m_tbSterowanie.GuiShowInfo, code, message);
    }

    public void ShowStatus(object sender, TransferNotificationEventArgs arg)
    {
        if (Dispatcher.CheckAccess())
            m_tbSterowanie.GuiShowInfo(arg.severity, arg.message);
        else
            Dispatcher.Invoke(m_tbSterowanie.GuiShowInfo, arg.severity, arg.message);
    }

    public void ShowNotification(object sender, FileTransferredEventArgs arg)
    {
        if (Dispatcher.CheckAccess())
            m_tbSterowanie.GuiShowTransferDetails(arg);
        else
            Dispatcher.Invoke(m_tbSterowanie.GuiShowTransferDetails, arg);
    }
    #endregion

    #region private
    /// <summary>
    /// Inicjalizuje EventLog, gdy działa z prawami Admina
    /// </summary>
    private void CheckEventLog()
    {
        try {
            if (FtpDiligentGlobals.TraceLevel > 0 && !EventLog.SourceExists(FtpDiligentGlobals.EventLog))
                EventLog.CreateEventSource(FtpDiligentGlobals.EventLog, FtpDiligentGlobals.EventLog);
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
        Int32.TryParse(settings["InstanceID"], out FtpDiligentGlobals.Instance);
        Int32.TryParse(settings["HotfolderInterval"], out FtpDiligentGlobals.HotfolderInterval);
        FtpDiligentGlobals.TraceLevel = (eSeverityCode)traceLevel;
        m_mailer = new SendEmails(settings["ErrorsMailTo"], settings["SendGridKey"]);
        FtpDiligentGlobals.CheckTransferedStorage = bool.Parse(settings["CheckTransferedFile"]);

        if (!Enum.TryParse<eSyncFileMode>(settings["SyncMethod"], out FtpDiligentGlobals.SyncMode)) {
            m_tbSterowanie.GuiShowInfo(eSeverityCode.Warning, "Parametr SyncMethod ma nieprawidłową wartość.");
            FtpDiligentGlobals.SyncMode = eSyncFileMode.UniqueDateAndSizeInDatabase;
        }

        try {
            CultureInfo.CurrentUICulture = new CultureInfo(settings["CultureInfo"]);
        } catch {
            m_tbSterowanie.GuiShowInfo(eSeverityCode.Warning, "Parametr CultureInfo ma nieprawidłową wartość.");
        }
    }

    /// <summary>
    /// Inicjalizuje identyfikator instancji przy pierwszym uruchomieniu
    /// </summary>
    private void CheckInstanceInitialization()
    {
        if (FtpDiligentGlobals.Instance > 0)
            return;

        string errmsg, localHostname = Dns.GetHostName();
        (FtpDiligentGlobals.Instance, errmsg) = m_repository.InitInstance(localHostname);
        if (!string.IsNullOrEmpty(errmsg))
            m_tbSterowanie.GuiShowInfo(eSeverityCode.Error, errmsg);
    }

    /// <summary>
    /// Zapisuje aktualne ustawienia konfiguracyjne
    /// </summary>
    private void SaveConfig()
    {
        string syncMethod = FtpDiligentGlobals.SyncMode.ToString();
        Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        config.AppSettings.Settings.Remove("SyncMethod");
        config.AppSettings.Settings.Add("SyncMethod", syncMethod);
        config.AppSettings.Settings.Remove("InstanceId");
        config.AppSettings.Settings.Add("InstanceId", FtpDiligentGlobals.Instance.ToString());
        config.Save(ConfigurationSaveMode.Modified);
    }
    #endregion
}
