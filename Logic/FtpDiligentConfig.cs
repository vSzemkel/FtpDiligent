
// -----------------------------------------------------------------------
// <copyright file="FtpDiligentConfig.cs">
// <legal>Copyright (c) Marcin Buchwald, February 2025</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Windows;

using Prism.Events;

using FtpDiligent.Events;

/// <summary>
/// Ta implementacja posługuje się tradycyjnym plikiem XML app.config
/// W czasie działania programu ustawienia przechowywane są jako statyczne właściwości klasy FtpDiligentGlobals
/// </summary>
public sealed class FtpDiligentConfig : IFtpDiligentConfig
{
    #region fields
    /// <summary>
    /// Repozytorium danych
    /// </summary>
    private IEventAggregator m_eventAggr;

    /// <summary>
    /// Repozytorium danych
    /// </summary>
    private IFtpRepository m_repository;
    #endregion

    #region constructors
    public FtpDiligentConfig(IEventAggregator eventAggr, IFtpRepository repository)
    {
        m_eventAggr = eventAggr;
        m_repository = repository;
        InitializationStatusNotification = eventAggr.GetEvent<StatusEvent>();
    }
    #endregion

    #region events
    /// <summary>
    /// Rozgłasza status operacji inicjalizacji aplikacji
    /// </summary>
    private StatusEvent InitializationStatusNotification;
    #endregion

    #region public
    /// <summary>
    /// Inicjalizuje konfigurację programu
    /// </summary>
    public void LoadConfig()
    {
        CheckEventLog();

        byte traceLevel;
        var settings = ConfigurationManager.AppSettings;
        Byte.TryParse(settings["TraceLevel"], out traceLevel);
        Int32.TryParse(settings["InstanceID"], out FtpDiligentGlobals.Instance);
        Int32.TryParse(settings["HotfolderInterval"], out FtpDiligentGlobals.HotfolderInterval);
        FtpDiligentGlobals.EventAggregator = m_eventAggr;
        FtpDiligentGlobals.TraceLevel = (eSeverityCode)traceLevel;
        FtpDiligentGlobals.Mailer = new SendEmails(m_eventAggr, settings["ErrorsMailTo"], settings["SendGridKey"]);
        FtpDiligentGlobals.CheckTransferedStorage = bool.Parse(settings["CheckTransferedFile"]);

        if (!Enum.TryParse<eSyncFileMode>(settings["SyncMethod"], out FtpDiligentGlobals.SyncMode)) {
            InitializationStatusNotification.Publish(new StatusEventArgs(eSeverityCode.Warning, "Parametr SyncMethod ma nieprawidłową wartość."));
            FtpDiligentGlobals.SyncMode = eSyncFileMode.UniqueDateAndSizeInDatabase;
        }

        try {
            CultureInfo.CurrentUICulture = new CultureInfo(settings["CultureInfo"]);
        } catch{
            InitializationStatusNotification.Publish(new StatusEventArgs(eSeverityCode.Warning, "Parametr CultureInfo ma nieprawidłową wartość."));
        }

        CheckInstanceInitialization();
    }

    /// <summary>
    /// Zapisuje aktualne ustawienia konfiguracyjne
    /// </summary>
    public void SaveConfig()
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
    /// Inicjalizuje identyfikator instancji przy pierwszym uruchomieniu
    /// </summary>
    private void CheckInstanceInitialization()
    {
        if (FtpDiligentGlobals.Instance > 0)
            return;

        string errmsg, localHostname = Dns.GetHostName();
        (FtpDiligentGlobals.Instance, errmsg) = m_repository.InitInstance(localHostname);
        if (!string.IsNullOrEmpty(errmsg))
            InitializationStatusNotification.Publish(new StatusEventArgs(eSeverityCode.Error, errmsg));
    }
    #endregion
}
