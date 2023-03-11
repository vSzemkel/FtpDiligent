
// -----------------------------------------------------------------------
// <copyright file="Serwery.xaml.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for Sterowanie.xaml
    /// </summary>
    public partial class Serwery : UserControl
    {
        #region fields
        /// <summary>
        /// Referencja do głównego okna
        /// </summary>
        public MainWindow m_mainWnd;

        /// <summary>
        /// Zarządza wątkami roboczymi
        /// </summary>
        //public FtpDispatcher m_dispatcher;

        /// <summary>
        /// Lista serwerów zdefiniowanych dla bieżącej instancji
        /// </summary>
        public ObservableCollection<FtpEndpoint> m_endpoints;

        /// <summary>
        /// Lista monitorowanych hotfolderów
        /// </summary>
        private List<FtpHotfolderWatcher> m_hotfolders = new List<FtpHotfolderWatcher>();

        /// <summary>
        /// Klient bazy danych
        /// </summary>
        private IFtpDiligentDatabaseClient m_database { get; set; }
        #endregion

        #region properties
        /// <summary>
        /// Serwer wskazany w tabeli lub null
        /// </summary>
        public FtpEndpoint m_selectedEndpoint {
            get {
                return lvSerwery.SelectedItem as FtpEndpoint;
            }
        }
        #endregion

        #region constructors
        public Serwery(MainWindow wnd, IFtpDiligentDatabaseClient database)
        {
            InitializeComponent();
            m_mainWnd = wnd;
            m_database = database;
        }
        #endregion

        #region UI handlers
        private void OnAdd(object sender, RoutedEventArgs e)
        {
            var details = m_mainWnd.m_tbSerweryDetails;
            details.m_endpoints = lvSerwery.Items as IEditableCollectionView;
            details.m_mode = eDbOperation.Insert;
            details.DataContext = details.m_endpoints.AddNew();

            SwitchTabControl();
        }

        private void OnChange(object sender, RoutedEventArgs e)
        {
            var details = m_mainWnd.m_tbSerweryDetails;
            details.m_endpoints = lvSerwery.Items as IEditableCollectionView;
            details.m_endpoints.EditItem(lvSerwery.SelectedItem);
            details.m_mode = eDbOperation.Update;
            details.DataContext = lvSerwery.SelectedItem;

            SwitchTabControl();
        }

        private void OnRemove(object sender, RoutedEventArgs e)
        {
            var endpoint = lvSerwery.SelectedItem as FtpEndpoint;
            var collection = lvSerwery.Items as IEditableCollectionView;
            if (MessageBoxResult.Yes == MessageBox.Show($"Czy usunąć serwer {endpoint.Host}{endpoint.RemoteDirectory} ?", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Question)) {
                var errmsg = m_database.ModifyEndpoint(endpoint.GetModel(), eDbOperation.Delete);
                if (string.IsNullOrEmpty(errmsg))
                    collection.Remove(endpoint);
                else
                    m_mainWnd.ShowErrorInfo(eSeverityCode.Error, errmsg);
            }
        }

        /// <summary>
        /// Odświeża listę serwerów
        /// </summary>
        private void OnRelo(object sender, RoutedEventArgs e)
        {
            LoadEndpoints();
        }

        private void OnTest(object sender, RoutedEventArgs e)
        {
            var endpoint = lvSerwery.SelectedItem as FtpEndpoint;
            if (endpoint != null) {
                Cursor = Cursors.Wait;

                string errmsg = string.Empty;
                var fu = IFtpUtility.Create(endpoint.GetModel(), m_mainWnd);

                bool isErr = !fu.CheckConnection(ref errmsg);

                if (!fu.CheckLocalDirectory()) {
                    isErr = true;
                    errmsg += "\nKatalog lokalny nie istnieje"; 
                }

                Cursor = Cursors.Arrow;
                MessageBox.Show(errmsg, isErr ? "Ostrzeżenie" : "Info", MessageBoxButton.OK, isErr ? MessageBoxImage.Error : MessageBoxImage.Information);
            } else
                MessageBox.Show("Nie wybrano serwera do sprawdzenia.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnSync(object sender, RoutedEventArgs e)
        {
            var endpoint = lvSerwery.SelectedItem as FtpEndpoint;
            if (endpoint != null) {
                var disp = new FtpDispatcher(m_mainWnd.m_syncModeProp, FtpDispatcherGlobals.ShowError, this.m_database);
                disp.StartNow(endpoint);
                m_mainWnd.tcMain.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Switch to edit mode on double click
        /// </summary>
        private void OnDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lvSerwery.SelectedIndex >= 0)
                OnChange(sender, null);
        }
        #endregion

        #region public
        public void LoadEndpoints()
        {
            //m_endpoints = FtpDiligentDesignTimeClient.GetEndpoints(m_mainWnd.m_instance);
            var (tab, errmsg) = m_database.GetEndpoints(FtpDispatcherGlobals.Instance);
            if (!string.IsNullOrEmpty(errmsg)) {
                FtpDispatcherGlobals.ShowError(eSeverityCode.Error, errmsg);
                m_endpoints = new ObservableCollection<FtpEndpoint>();
            } else
                m_endpoints = m_database.GetEndpointsCollection(tab);

            lvSerwery.DataContext = m_endpoints;
        }

        /// <summary>
        /// Uruchamia monitorowanie i transfer PUT
        /// </summary>
        public void StartHotfolders()
        {
            foreach (FtpEndpoint enp in m_endpoints)
                if ((enp.Direction & eFtpDirection.HotfolderPut) > 0) {
                    var fhw = new FtpHotfolderWatcher(enp.GetModel(), m_mainWnd, m_database);
                    fhw.StartWatching();
                    m_hotfolders.Add(fhw);
                }
        }

        /// <summary>
        /// Zatrzymuje monitorowanie
        /// </summary>
        public void StopHotfolders()
        {
            foreach (FtpHotfolderWatcher fhw in m_hotfolders)
                fhw.StopWatching();

            m_hotfolders.Clear();
        }
        #endregion

        #region private
        private void SwitchTabControl()
        {
            m_mainWnd.tabSerwery.Visibility = Visibility.Collapsed;
            m_mainWnd.tabSerweryDetails.Visibility = Visibility.Visible;
            m_mainWnd.tcMain.SelectedIndex = 2;
        }
        #endregion
    }
}
