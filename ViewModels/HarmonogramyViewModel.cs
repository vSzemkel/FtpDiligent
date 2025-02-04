// -----------------------------------------------------------------------
// <copyright file="HarmonogramyViewModel.cs" company="private project">
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
    private IFtpRepository m_repository;
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
        set
        {
            SetProperty(ref m_selectedSchedule, value);
            RaisePropertyChanged(nameof(DetailsAvailable));
        }
    }

    private bool DetailsAvailable
    {
        get => m_selectedSchedule != null;
    }
    #endregion

    #region commands
    public DelegateCommand AddScheduleCommand { get; private set; }
    public DelegateCommand ModifyScheduleCommand { get; private set; }
    public DelegateCommand DeleteScheduleCommand { get; private set; }
    public DelegateCommand ReloadScheduleCommand { get; private set; }
    #endregion

    #region constructors
    public HarmonogramyViewModel(MainWindow wnd, IFtpRepository repository)
    {
        wnd.m_tbHarmonogramy = this;

        m_mainWnd = wnd;
        m_repository = repository;
        SelectedFtpEndpoint = FtpEndpoints.FirstOrDefault();
        AddScheduleCommand = new DelegateCommand(OnAdd);
        ModifyScheduleCommand = new DelegateCommand(OnChange).ObservesCanExecute(() => DetailsAvailable);
        DeleteScheduleCommand = new DelegateCommand(OnRemove).ObservesCanExecute(() => DetailsAvailable);
        ReloadScheduleCommand = new DelegateCommand(OnRelo);
    }
    #endregion

    #region public
    #endregion

    #region private
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
        if (MessageBoxResult.Yes == MessageBox.Show($"Czy usunąć harmonogram {m_selectedSchedule.Name}?", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Question))
        {
            var errmsg = m_repository.ModifySchedule(m_selectedSchedule.GetModel(), eDbOperation.Delete);
            if (string.IsNullOrEmpty(errmsg))
                m_schedules.Remove(m_selectedSchedule);
            else
                m_mainWnd.ShowErrorInfo(eSeverityCode.Error, errmsg);
        }
    }

    private void OnRelo()
    {
        m_mainWnd.m_tbSerwery.LoadEndpoints();
        LoadSchedules();
    }

    private void LoadSchedules()
    {
        if (SelectedFtpEndpoint == null)
            return;

        var (tab, errmsg) = m_repository.GetSchedules(SelectedFtpEndpoint.XX);
        if (!string.IsNullOrEmpty(errmsg))
            m_mainWnd.ShowErrorInfo(eSeverityCode.Error, errmsg);
        else
            FtpSchedules = m_repository.GetSchedulesCollection(tab.Rows.Cast<System.Data.DataRow>());
    }

    private void SwitchTabControl()
    {
        m_mainWnd.tabHarmonogramy.Visibility = Visibility.Collapsed;
        m_mainWnd.tabHarmonogramyDetails.Visibility = Visibility.Visible;
        m_mainWnd.tcMain.SelectedIndex = 4;
    }
    #endregion
}
