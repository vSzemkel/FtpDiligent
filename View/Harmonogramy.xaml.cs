
// -----------------------------------------------------------------------
// <copyright file="Harmonogramy.xaml.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

/// <summary>
/// Interaction logic for Harmonogramy.xaml
/// </summary>
public partial class Harmonogramy : UserControl
{
    #region fields
    /// <summary>
    /// Referencja do głównego okna
    /// </summary>
    public MainWindow m_mainWnd;

    /// <summary>
    /// Serwer wybrany z listy rozwijalnej
    /// </summary>
    public FtpEndpoint m_selectedEndpoint;

    /// <summary>
    /// Lista harmonogramów zdefiniowanych dla bieżącego serwera
    /// </summary>
    public ObservableCollection<FtpSchedule> m_schedules;

    /// <summary>
    /// Klient bazy danych
    /// </summary>
    private IFtpDiligentDatabaseClient m_database { get; set; }
    #endregion

    #region constructors
    public Harmonogramy(MainWindow wnd, IFtpDiligentDatabaseClient database)
    {
        InitializeComponent();
        m_mainWnd = wnd;
        m_database = database;
    }
    #endregion

    #region UI handlers
    private void OnAdd(object sender, RoutedEventArgs e)
    {
        var details = m_mainWnd.m_tbHarmonogramyDetails;
        details.m_schedules = lvHarmonogramy.Items as IEditableCollectionView;
        details.m_mode = eDbOperation.Insert;
        details.DataContext = details.m_schedules.AddNew();

        SwitchTabControl();
    }

    private void OnChange(object sender, RoutedEventArgs e)
    {
        var details = m_mainWnd.m_tbHarmonogramyDetails;
        details.m_schedules = lvHarmonogramy.Items as IEditableCollectionView;
        details.m_schedules.EditItem(lvHarmonogramy.SelectedItem);
        details.m_mode = eDbOperation.Update;
        details.DataContext = lvHarmonogramy.SelectedItem;

        SwitchTabControl();
    }

    private void OnRemove(object sender, RoutedEventArgs e)
    {
        var schedule = lvHarmonogramy.SelectedItem as FtpSchedule;
        var collection = lvHarmonogramy.Items as IEditableCollectionView;
        if (MessageBoxResult.Yes == MessageBox.Show("Czy usunąć harmonogram " + schedule.Name, "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Question)) {
            var errmsg = m_database.ModifySchedule(schedule.GetModel(), eDbOperation.Delete);
            if (string.IsNullOrEmpty(errmsg))
                collection.Remove(schedule);
            else
                FtpDispatcherGlobals.ShowError(eSeverityCode.Error, errmsg);
        }
    }

    /// <summary>
    /// Odświeża listę serwerów
    /// </summary>
    private void OnRelo(object sender, RoutedEventArgs e)
    {
        m_mainWnd.m_tbSerwery.LoadEndpoints();
        LoadSchedules(0);
    }

    /// <summary>
    /// Obsługa zdarzenia zmiany wybranego z listy serwera
    /// </summary>
    private void OnSerwerChanged(object sender, SelectionChangedEventArgs e)
    {
        var cb = sender as ComboBox;
        if (cb != null) {
            m_selectedEndpoint = cb.SelectedItem as FtpEndpoint;
            if (m_selectedEndpoint == null)
                LoadSchedules(0);
            else
                LoadSchedules(m_selectedEndpoint.XX);
        }
    }

    /// <summary>
    /// Switch to edit mode on double click
    /// </summary>
    private void OnDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (lvHarmonogramy.SelectedIndex >= 0)
            OnChange(sender, null);
    }
    #endregion

    #region public
    public void LoadSchedules(int endpoint)
    {
        if (endpoint == 0) {
            var enp = m_mainWnd.m_tbSerwery.m_endpoints;
            cbSerwery.DataContext = enp;
            if (enp.Count > 0) {
                cbSerwery.SelectedItem = enp[0];
                return;
            }
        }

        //m_schedules = FtpDiligentDesignTimeClient.GetSchedules(endpoint);
        var (tab, errmsg) = m_database.GetSchedules(endpoint);
        if (!string.IsNullOrEmpty(errmsg))
            FtpDispatcherGlobals.ShowError(eSeverityCode.Error, errmsg);
        else
            m_schedules = m_database.GetSchedulesCollection(tab);

        lvHarmonogramy.DataContext = m_schedules;
    }
    #endregion

    #region private
    private void SwitchTabControl()
    {
        m_mainWnd.tabHarmonogramy.Visibility = Visibility.Collapsed;
        m_mainWnd.tabHarmonogramyDetails.Visibility = Visibility.Visible;
        m_mainWnd.tcMain.SelectedIndex = 4;
    }
    #endregion
}
