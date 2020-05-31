
// -----------------------------------------------------------------------
// <copyright file="Sterowanie.xaml.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    using System;
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for Sterowanie.xaml
    /// </summary>
    public partial class Sterowanie : UserControl
    {
        #region fields
        /// <summary>
        /// Referencja do głównego okna
        /// </summary>
        public MainWindow m_mainWnd;

        /// <summary>
        /// Liczba plików przesłanych w bieżącej sesji
        /// </summary>
        public int m_filesCount = 0;

        /// <summary>
        /// Lista ostatnio transferowanych plików
        /// </summary>
        public ObservableCollection<FtpFileModel> m_fileInfo = new ObservableCollection<FtpFileModel>();

        /// <summary>
        /// Lista ostatnio zarejestrowanych błędów i ostrzeżeń
        /// </summary>
        public ObservableCollection<FtpErrorModel> m_errInfo = new ObservableCollection<FtpErrorModel>();
        #endregion

        #region constructor
        public Sterowanie()
        {
            InitializeComponent();

            this.cbSyncMode.ItemsSource = Enum.GetValues(typeof(eSyncFileMode));
            this.lvFilesLog.ItemsSource = m_fileInfo;
            this.lvErrLog.ItemsSource = m_errInfo;
        }
        #endregion

        #region UI handlers
        /// <summary>
        /// Uruchamia realizację zaplanowanych transferów
        /// </summary>
        private void OnStartSync(object sender, RoutedEventArgs e)
        {
            string hostWithBadDir = CheckLocDirs();
            if (string.IsNullOrEmpty(hostWithBadDir)) {
                btRunSync.IsEnabled = false;
                btStopSync.IsEnabled = true;
                m_mainWnd.m_dispatcher = new FtpDispatcher(m_mainWnd);
                m_mainWnd.m_dispatcher.Start();
                m_mainWnd.m_tbSerwery.StartHotfolders();
            } else
                MessageBox.Show($"Katalog lokalny {hostWithBadDir} jest niepoprawny", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Zatrzymuje realizację zaplanowanych transferów
        /// </summary>
        private void OnStopSync(object sender, RoutedEventArgs e)
        {
            btRunSync.IsEnabled = true;
            btStopSync.IsEnabled = false;
            m_mainWnd.m_dispatcher?.Stop();
            m_mainWnd.m_tbSerwery.StopHotfolders();
            m_mainWnd.ShowErrorInfo(eSeverityCode.NextSync, string.Empty);
        }

        /// <summary>
        /// Usuwa bieżącą zawartość listboxów prezentujących informacje o ostatnich operacjacg
        /// </summary>
        private void OnClearLog(object sender, System.Windows.RoutedEventArgs e)
        {
            MessageBoxResult drQuest = MessageBox.Show("Czy chcesz wyczyścić okienka logów?", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (drQuest == MessageBoxResult.Yes) {
                m_errInfo.Clear();
                m_fileInfo.Clear();
                lbLog.Items.Clear();
            }
        }
        #endregion

        #region private
        /// <summary>
        /// Sprawdza, czy katalogi lokalne są prawidłowe
        /// </summary>
        /// <returns>Niepoprawny katalog</returns>
        private string CheckLocDirs()
        {
            foreach (var enp in m_mainWnd.m_tbSerwery.m_endpoints)
                if (!System.IO.Directory.Exists(enp.LocalDirectory))
                    return enp.LocalDirectory;

            return string.Empty;
        }
        #endregion

    }
}
