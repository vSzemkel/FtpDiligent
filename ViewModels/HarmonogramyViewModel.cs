
// -----------------------------------------------------------------------
// <copyright file="HarmonogramyViewModel.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Prism.Ioc;
using Prism.Mvvm;
using FtpDiligent;
using FtpDiligent.Views;

public class HarmonogramyViewModel : BindableBase
{
    #region fields
    private MainWindow m_mainWnd;
    private IFtpDiligentDatabaseClient m_database;
    private FtpEndpoint m_selectedEndpoint;
    private ObservableCollection<FtpSchedule> m_schedules;
    #endregion

    #region properties
    public ObservableCollection<FtpEndpoint> FtpEndpoints => m_mainWnd.m_tbSerwery.m_endpoints;

    public FtpEndpoint SelectedFtpEndpoint
    {
        get => m_selectedEndpoint;
        set
        {
            SetProperty(ref m_selectedEndpoint, value);
            LoadSchedules();
        }
    }

    public ObservableCollection<FtpSchedule> Schedules
    {
        get => m_schedules;
        set
        {
            SetProperty(ref m_schedules, value);
        }
    }
    #endregion

    #region constructors
    public HarmonogramyViewModel()
    {
        m_database = FtpDispatcherGlobals.IoC.Resolve<IFtpDiligentDatabaseClient>();
        m_mainWnd = FtpDispatcherGlobals.IoC.Resolve<MainWindow>();
    }
    public HarmonogramyViewModel(MainWindow wnd, IFtpDiligentDatabaseClient database)
    {
        m_mainWnd = wnd;
        m_database = database;
    }
    #endregion

    #region commands
    //public ICommand AddCommand => new Command(OnAdd);
    //public ICommand ChangeCommand => new RelayCommand(OnChange);
    //public ICommand RemoveCommand => new RelayCommand(OnRemove);
    //public ICommand ReloadCommand => new RelayCommand(OnRelo);
    //public ICommand ServerChangedCommand => new RelayCommand(OnSerwerChanged);
    //public ICommand DoubleClickCommand => new RelayCommand(OnDoubleClick);
    #endregion

    #region methods
    private void OnAdd(object parameter)
    {
        var details = m_mainWnd.m_tbHarmonogramyDetails;
        details.m_schedules = m_schedules as IEditableCollectionView;
        details.m_mode = eDbOperation.Insert;
        details.DataContext = details.m_schedules.AddNew();

        SwitchTabControl();
    }

    private void OnChange(object parameter)
    {
        var details = m_mainWnd.m_tbHarmonogramyDetails;
        details.m_schedules = m_schedules as IEditableCollectionView;
        details.m_schedules.EditItem(parameter);
        details.m_mode = eDbOperation.Update;
        details.DataContext = parameter;

        SwitchTabControl();
    }

    private void OnRemove(object parameter)
    {
        var schedule = parameter as FtpSchedule;
        var collection = m_schedules as IEditableCollectionView;
        if (MessageBoxResult.Yes == MessageBox.Show("Czy usunąć harmonogram " + schedule.Name, "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Question))
        {
            var errmsg = m_database.ModifySchedule(schedule.GetModel(), eDbOperation.Delete);
            if (string.IsNullOrEmpty(errmsg))
                collection.Remove(schedule);
            else
                m_mainWnd.ShowErrorInfo(eSeverityCode.Error, errmsg);
        }
    }

    private void OnRelo(object parameter)
    {
        m_mainWnd.m_tbSerwery.LoadEndpoints();
        LoadSchedules();
    }

    private void OnDoubleClick(object parameter)
    {
        if (parameter != null)
            OnChange(parameter);
    }

    public void LoadSchedules()
    {
        if (FtpEndpoints== null || FtpEndpoints.Count == 0)
            return;
        if (SelectedFtpEndpoint == null)
            SelectedFtpEndpoint = FtpEndpoints[0];

        var (tab, errmsg) = m_database.GetSchedules(SelectedFtpEndpoint.XX);
        if (!string.IsNullOrEmpty(errmsg))
            m_mainWnd.ShowErrorInfo(eSeverityCode.Error, errmsg);
        else
            Schedules = m_database.GetSchedulesCollection(tab.Rows.Cast<System.Data.DataRow>());
    }

    private void SwitchTabControl()
    {
        m_mainWnd.tabHarmonogramy.Visibility = Visibility.Collapsed;
        m_mainWnd.tabHarmonogramyDetails.Visibility = Visibility.Visible;
        m_mainWnd.tcMain.SelectedIndex = 4;
    }
    #endregion
}
