
// -----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
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
        /// Nazwa aplikacyjnego EventLogu
        /// </summary>
        public const string m_eventLog = "FtpDiligent";

        /// <summary>
        /// Długość logu transferowanych plików
        /// </summary>
        private const int m_fileLogSize = 100;

        /// <summary>
        /// Identyfikator instancji workera
        /// </summary>
        public int m_instance;

        /// <summary>
        /// Algorytm klasyfikacji plików do transferu
        /// </summary>
        private eSyncFileMode m_syncMode;

        /// <summary>
        /// Poziom logowania komunikatów
        /// </summary>
        public eSeverityCode m_traceLevel;

        /// <summary>
        /// Czy po transferowniu pliku zweryfikować jego rozmiar
        /// </summary>
        static public bool s_checkTransferedStorage;

        /// <summary>
        /// Co ile sekund sprawdzamy, czy pliki w hotfolderze są w pełni zapisane
        /// </summary>
        public int m_hotfolderInterval;

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
        /// Wrapper do metody ShowErrorInfoInternal
        /// </summary>
        public static Action<eSeverityCode, string> s_showError;

        /// <summary>
        /// Klient bazy danych
        /// </summary>
        private IFtpDiligentDatabaseClient m_database;
        #endregion

        #region properties
        public eSyncFileMode m_syncModeProp { 
            get => m_syncMode;
            set { m_syncMode = value; }
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
        public MainWindow(IFtpDiligentDatabaseClient database)
        {
            InitializeComponent();
            m_database = database;
            m_tbSterowanie = new Sterowanie(this, database);
            m_tbSterowanie.tbFilesCount.DataContext = 0;
            m_tbSterowanie.cbSyncMode.DataContext = this;
            this.tabSterowanie.Content = m_tbSterowanie;

            m_tbSerwery = new Serwery(this, database);
            this.tabSerwery.Content = m_tbSerwery;
            m_tbHarmonogramy = new Harmonogramy(this, database);
            this.tabHarmonogramy.Content = m_tbHarmonogramy;

            CheckEventLog();
            LoadConfig();
            CheckInstanceInitialization();

            s_showError = (code, msg) => this.ShowErrorInfo(code, msg);
            this.Title = $"FtpDiligent [instance {m_instance}]";

            m_tbSerwery.LoadEndpoints();
            m_tbHarmonogramy.LoadSchedules(0);
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
                Dispatcher.Invoke(s_showError, code, message);
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
                    if (m_traceLevel.HasFlag(eSeverityCode.Message))
                        EventLog.WriteEntry(m_eventLog, message, EventLogEntryType.Information);
                    break;
                case eSeverityCode.FileInfo:
                    BindFileInfo(message);
                    if (m_traceLevel.HasFlag(eSeverityCode.FileInfo))
                        EventLog.WriteEntry(m_eventLog, message, EventLogEntryType.SuccessAudit);
                    break;
                case eSeverityCode.Warning:
                    m_tbSterowanie.m_errInfo.Insert(0, new FtpErrorModel() { Category = code, Message = message });
                    if (m_traceLevel.HasFlag(eSeverityCode.Warning))
                        EventLog.WriteEntry(m_eventLog, message, EventLogEntryType.Warning);
                    break;
                case eSeverityCode.TransferError:
                    m_tbSterowanie.RestartScheduler();
                    m_mailer.Run(message);
                    goto case eSeverityCode.Error;
                case eSeverityCode.Error:
                    m_tbSterowanie.m_errInfo.Insert(0, new FtpErrorModel() { Category = code, Message = message });
                    if (m_traceLevel.HasFlag(eSeverityCode.Error))
                        EventLog.WriteEntry(m_eventLog, message, EventLogEntryType.Error);
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
                if (m_traceLevel > 0 && !EventLog.SourceExists(m_eventLog))
                    EventLog.CreateEventSource(m_eventLog, m_eventLog);
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
            Int32.TryParse(settings["InstanceID"], out m_instance);
            Int32.TryParse(settings["HotfolderInterval"], out m_hotfolderInterval);
            m_traceLevel = (eSeverityCode)traceLevel;
            m_mailer = new SendEmails(this, settings["ErrorsMailTo"], settings["SendGridKey"]);
            s_checkTransferedStorage = bool.Parse(settings["CheckTransferedFile"]);

            if (!Enum.TryParse<eSyncFileMode>(settings["SyncMethod"], out m_syncMode)) {
                ShowErrorInfoInternal(eSeverityCode.Warning, "Parametr SyncMethod ma nieprawidłową wartość.");
                m_syncMode = eSyncFileMode.UniqueDateAndSizeInDatabase;
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
            if (m_instance > 0)
                return;

            string errmsg, localHostname = Dns.GetHostName();
            (m_instance, errmsg) = m_database.InitInstance(localHostname);
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
            config.AppSettings.Settings.Add("InstanceId", m_instance.ToString());
            config.Save(ConfigurationSaveMode.Modified);
        }
        #endregion
    }
}
