
// -----------------------------------------------------------------------
// <copyright file="HarmonogramyDetails.xaml.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

/// <summary>
/// Interaction logic for Sterowanie.xaml
/// </summary>
public partial class HarmonogramyDetails : UserControl
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
    /// Lista harmonogramów zdefiniowanych dla bieżącego serwera
    /// </summary>
    public IEditableCollectionView m_schedules;

    /// <summary>
    /// Klient bazy danych
    /// </summary>
    private IFtpDiligentDatabaseClient m_database { get; set; }
    #endregion

    #region constructors
    public HarmonogramyDetails(MainWindow wnd, IFtpDiligentDatabaseClient database)
    {
        InitializeComponent();

        m_mainWnd = wnd;
        m_database = database;
        var dayNames = CultureInfo.CurrentUICulture.DateTimeFormat.DayNames;
        cbStartDay.ItemsSource = dayNames;
        cbStopDay.ItemsSource = dayNames;
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
            var schedule = m_schedules.CurrentAddItem as FtpSchedule;
            if (!ValidateSchedule(schedule))
                return;

            schedule.Endpoint = m_mainWnd.m_tbHarmonogramy.m_selectedEndpoint.XX;
            errmsg = m_database.ModifySchedule(schedule.GetModel(), m_mode);
            if (string.IsNullOrEmpty(errmsg)) {
                schedule.XX = m_database.GetLastInsertedKey();
                m_schedules.CommitNew();
            }
        } else {
            var schedule = m_schedules.CurrentEditItem as FtpSchedule;
            if (!ValidateSchedule(schedule))
                return;
            errmsg = m_database.ModifySchedule(schedule.GetModel(), m_mode);
            if (string.IsNullOrEmpty(errmsg))
                m_schedules.CommitEdit();
        }

        if (string.IsNullOrEmpty(errmsg))
            RestoreTabControl();
        else
            FtpDispatcherGlobals.ShowError(eSeverityCode.Error, errmsg);
    }

    /// <summary>
    /// Porzucenie zmian
    /// </summary>
    private void OnCancel(object sender, RoutedEventArgs e)
    {
        if (m_mode == eDbOperation.Insert)
            m_schedules.CancelNew();
        else
            m_schedules.CancelEdit();

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
    /// Przywraca zakładkę do trybu nieedycyjnego
    /// </summary>
    private void RestoreTabControl()
    {
        m_mainWnd.tabHarmonogramyDetails.Visibility = Visibility.Collapsed;
        m_mainWnd.tabHarmonogramy.Visibility = Visibility.Visible;
        m_mainWnd.tcMain.SelectedIndex = 3;
    }

    /// <summary>
    /// Sygnalizuje niepoprawny przedział dat
    /// </summary>
    /// <param name="schedule">Harmonogram</param>
    /// <returns>Status poprawności</returns>
    private bool ValidateSchedule(FtpSchedule schedule)
    {
        if (schedule.StartTime > schedule.StopTime) {
            MessageBox.Show("Popraw daty", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        
        if (schedule.Stride < 5) {
            MessageBox.Show("Trzeba zachować co najmniej 5 minut odstępu", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        return true;
    }
    #endregion
}
