
// -----------------------------------------------------------------------
// <copyright file="SerweryDetails.xaml.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for Sterowanie.xaml
    /// </summary>
    public partial class SerweryDetails : UserControl
    {
        #region fields
        /// <summary>
        /// Referencja do głównego okna
        /// </summary>
        public MainWindow m_mainWnd;

        /// <summary>
        /// Tryb edycji (Insert lub Update)
        /// </summary>
        public eDbOperation m_mode;

        /// <summary>
        /// Lista serwerów zdefiniowanych dla bieżącej instancji
        /// </summary>
        public IEditableCollectionView m_endpoints;
        #endregion

        #region constructors
        public SerweryDetails()
        {
            InitializeComponent();

            cbProtocol.ItemsSource = Enum.GetValues(typeof(eFtpProtocol));
            cbMode.ItemsSource = Enum.GetValues(typeof(eFtpTransferMode));
            cbDirection.ItemsSource = new eFtpDirection[] {eFtpDirection.Get, eFtpDirection.Put, eFtpDirection.Get|eFtpDirection.Put, eFtpDirection.HotfolderPut };
        }
        #endregion

        #region UI handlers
        /// <summary>
        /// Zatwierdzenie zmian
        /// </summary>
        private void OnCommit(object sender, RoutedEventArgs e)
        {
            string errmsg = string.Empty;

            if (m_mode == eDbOperation.Insert) {
                var endpoint = m_endpoints.CurrentAddItem as FtpEndpoint;
                endpoint.Instance = m_mainWnd.m_instance;
                SanitizeDirectories(ref endpoint);
                errmsg = FtpDiligentDatabaseClient.ModifyEndpoint(endpoint.GetModel(), m_mode);
                if (string.IsNullOrEmpty(errmsg)) {
                    endpoint.XX = IFtpDiligentDatabaseClient.m_lastInsertedKey;
                    m_endpoints.CommitNew();
                }
            } else {
                var endpoint = m_endpoints.CurrentEditItem as FtpEndpoint;
                SanitizeDirectories(ref endpoint);
                errmsg = FtpDiligentDatabaseClient.ModifyEndpoint(endpoint.GetModel(), m_mode);
                if (string.IsNullOrEmpty(errmsg))
                    m_endpoints.CommitEdit();
            }

            if (string.IsNullOrEmpty(errmsg))
                RestoreTabControl();
            else
                m_mainWnd.ShowErrorInfo(eSeverityCode.Error, errmsg);
        }

        /// <summary>
        /// Porzucenie zmian
        /// </summary>
        private void OnCancel(object sender, RoutedEventArgs e)
        {
            if (m_mode == eDbOperation.Insert)
                m_endpoints.CancelNew();
            else
                m_endpoints.CancelEdit();

            RestoreTabControl();
        }

        /// <summary>
        /// Naciśniecie ESC jest równoznaczna z porzuceniem zmian
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape) {
                e.Handled = true;
                OnCancel(null, null);
            }
        }
        #endregion

        #region private
        /// <summary>
        /// Uzupełnia nazwy katalogów, gdy są niepełe
        /// </summary>
        /// <param name="enp">Modyfikowany endpoint</param>
        private void SanitizeDirectories(ref FtpEndpoint enp)
        {
            if (!enp.RemoteDirectory.StartsWith("/"))
                enp.RemoteDirectory = "/" + enp.RemoteDirectory;
            if (!enp.LocalDirectory.EndsWith("\\"))
                enp.LocalDirectory += "\\";
        }

        /// <summary>
        /// Przywraca zakładkę do trybu nieedycyjnego
        /// </summary>
        private void RestoreTabControl()
        {
            m_mainWnd.tabSerweryDetails.Visibility = Visibility.Collapsed;
            m_mainWnd.tabSerwery.Visibility = Visibility.Visible;
            m_mainWnd.tcMain.SelectedIndex = 1;
        }
        #endregion
    }
}
