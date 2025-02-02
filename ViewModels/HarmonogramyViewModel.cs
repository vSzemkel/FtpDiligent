// -----------------------------------------------------------------------
// <copyright file="App.cs" company="private project">
// <legal>Copyright (c) MB, February 2025</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

using Prism.Commands;
using Prism.Mvvm;
using FtpDiligent;
using FtpDiligent.Views;

public sealed class HarmonogramyViewModel : BindableBase
{
    #region fields
    private MainWindow m_mainWnd;
    private IFtpDiligentDatabaseClient m_database;
    private ObservableCollection<FtpSchedule> m_schedules;
    private FtpEndpoint m_selectedEndpoint;
    private FtpSchedule m_selectedSchedule;
    #endregion

    #region properties
    public ObservableCollection<FtpEndpoint> FtpEndpoints => m_mainWnd.m_tbSerwery.m_endpoints;

    public ObservableCollection<FtpSchedule> FtpSchedules
    {
        get => m_schedules;
        set { SetProperty(ref m_schedules, value); }
    }

    public FtpEndpoint SelectedFtpEndpoint
    {
        get => m_selectedEndpoint;
        set
        {
            SetProperty(ref m_selectedEndpoint, value);
            LoadSchedules();
        }
    }

    public FtpSchedule SelectedFtpSchedule
    {
        get => m_selectedSchedule;
        set { SetProperty(ref m_selectedSchedule, value); }
    }
    #endregion

    #region commandes
    public DelegateCommand AddScheduleCommand { get; private set; }
    public DelegateCommand ModifyScheduleCommand { get; private set; }
    public DelegateCommand DeleteScheduleCommand { get; private set; }
    public DelegateCommand ReloadScheduleCommand { get; private set; }
    #endregion

    #region constructors
    public HarmonogramyViewModel(MainWindow wnd, IFtpDiligentDatabaseClient database)
    {
        m_mainWnd = wnd;
        m_database = database;
        SelectedFtpEndpoint = FtpEndpoints.FirstOrDefault();
        AddScheduleCommand = new DelegateCommand(OnAdd, CanExecute);
        ModifyScheduleCommand = new DelegateCommand(OnChange, CanExecute);
        DeleteScheduleCommand = new DelegateCommand(OnRemove, CanExecute);
        ReloadScheduleCommand = new DelegateCommand(OnRelo, () => true);
    }
    #endregion

    #region methods
    private void OnAdd()
    {
        var details = m_mainWnd.m_tbHarmonogramyDetails;
        details.m_schedules = CollectionViewSource.GetDefaultView(m_schedules) as IEditableCollectionView;
        details.m_mode = eDbOperation.Insert;
        details.DataContext = details.m_schedules.AddNew();

        SwitchTabControl();
    }

    public void OnChange()
    {
        var details = m_mainWnd.m_tbHarmonogramyDetails;
        details.m_schedules = CollectionViewSource.GetDefaultView(m_schedules) as IEditableCollectionView;
        details.m_schedules.EditItem(m_selectedSchedule);
        details.m_mode = eDbOperation.Update;
        details.DataContext = m_selectedSchedule;

        SwitchTabControl();
    }

    private void OnRemove()
    {
        var collection = CollectionViewSource.GetDefaultView(m_schedules) as IEditableCollectionView;
        if (MessageBoxResult.Yes == MessageBox.Show($"Czy usunąć harmonogram {m_selectedSchedule.Name}?", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Question))
        {
            var errmsg = m_database.ModifySchedule(m_selectedSchedule.GetModel(), eDbOperation.Delete);
            if (string.IsNullOrEmpty(errmsg))
                collection.Remove(m_selectedSchedule);
            else
                m_mainWnd.ShowErrorInfo(eSeverityCode.Error, errmsg);
        }
    }

    private void OnRelo()
    {
        m_mainWnd.m_tbSerwery.LoadEndpoints();
        LoadSchedules();
    }

    public void StoreViewInShell(Views.Harmonogramy vh)
    {
        m_mainWnd.m_tbHarmonogramy = vh;
    }
    #endregion

    #region private
    private bool CanExecute()
    {
        return m_selectedEndpoint != null;
    }

    private void LoadSchedules()
    {
        if (FtpEndpoints == null || FtpEndpoints.Count == 0)
            return;
        if (SelectedFtpEndpoint == null)
            SelectedFtpEndpoint = FtpEndpoints[0];

        var (tab, errmsg) = m_database.GetSchedules(SelectedFtpEndpoint.XX);
        if (!string.IsNullOrEmpty(errmsg))
            m_mainWnd.ShowErrorInfo(eSeverityCode.Error, errmsg);
        else
            FtpSchedules = m_database.GetSchedulesCollection(tab.Rows.Cast<System.Data.DataRow>());
    }

    private void SwitchTabControl()
    {
        m_mainWnd.tabHarmonogramy.Visibility = Visibility.Collapsed;
        m_mainWnd.tabHarmonogramyDetails.Visibility = Visibility.Visible;
        m_mainWnd.tcMain.SelectedIndex = 4;
    }
    #endregion
}
