﻿
// -----------------------------------------------------------------------
// <copyright file="SerweryViewModel.cs" company="private project">
// <legal>Copyright (c) MB, February 2025</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent.ViewModels;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

using Prism.Commands;
using Prism.Mvvm;
using FtpDiligent;
using FtpDiligent.Views;

public sealed class SerweryViewModel : BindableBase
{
    #region fields
    /// <summary>
    /// Referencja do głównego okna
    /// </summary>
    public MainWindow m_mainWnd;

    /// <summary>
    /// Lista monitorowanych hotfolderów
    /// </summary>
    private List<FtpHotfolderWatcher> m_hotfolders = new();

    /// <summary>
    /// Lista serwerów zdefiniowanych dla bieżącej instancji
    /// </summary>
    public ObservableCollection<FtpEndpoint> m_endpoints;

    /// <summary>
    /// Endpoint wybrany na liście
    /// </summary>
    private FtpEndpoint m_selectedEndpoint;

    /// <summary>
    /// Klient bazy danych
    /// </summary>
    private IFtpDiligentDatabaseClient m_database { get; set; }
    #endregion

    #region properties
    /// <summary>
    /// Lista serwerów zdefiniowanych dla bieżącej instancji
    /// </summary>
    public ObservableCollection<FtpEndpoint> FtpEndpoints
    {
        get => m_endpoints;
        set { SetProperty(ref m_endpoints, value); }
    }
    /// <summary>
    /// Serwer wskazany w tabeli lub null
    /// </summary>
    public FtpEndpoint SelectedFtpEndpoint {
        get => m_selectedEndpoint;
        set 
        { 
            SetProperty(ref m_selectedEndpoint, value);
            RaisePropertyChanged(nameof(DetailsAvailable));
        }
    }

    private bool DetailsAvailable
    {
        get => m_selectedEndpoint != null;
    }
    #endregion

    #region commands
    public DelegateCommand AddEndpointCommand { get; private set; }
    public DelegateCommand ModifyEndpointCommand { get; private set; }
    public DelegateCommand DeleteEndpointCommand { get; private set; }
    public DelegateCommand ReloadEndpointCommand { get; private set; }
    public DelegateCommand TestEndpointCommand { get; private set; }
    public DelegateCommand SynchronizeEndpointCommand { get; private set; }
    #endregion

    #region constructors
    public SerweryViewModel(MainWindow wnd, IFtpDiligentDatabaseClient database)
    {
        wnd.m_tbSerwery = this;

        m_mainWnd = wnd;
        m_database = database;
        AddEndpointCommand = new DelegateCommand(OnAdd);
        ModifyEndpointCommand = new DelegateCommand(OnChange).ObservesCanExecute(() => DetailsAvailable);
        DeleteEndpointCommand = new DelegateCommand(OnRemove).ObservesCanExecute(() => DetailsAvailable);
        ReloadEndpointCommand = new DelegateCommand(LoadEndpoints);
        TestEndpointCommand = new DelegateCommand(OnTest).ObservesCanExecute(() => DetailsAvailable);
        SynchronizeEndpointCommand = new DelegateCommand(OnSync).ObservesCanExecute(() => DetailsAvailable);
        LoadEndpoints();
    }
    #endregion

    #region public
    public void LoadEndpoints()
    {
        //m_endpoints = FtpDiligentDesignTimeClient.GetEndpoints(m_mainWnd.m_instance);
        var (tab, errmsg) = m_database.GetEndpoints(FtpDispatcherGlobals.Instance);
        if (!string.IsNullOrEmpty(errmsg))
        {
            m_mainWnd.ShowErrorInfo(eSeverityCode.Error, errmsg);
            m_endpoints = new ObservableCollection<FtpEndpoint>();
        }
        else
            m_endpoints = m_database.GetEndpointsCollection(tab.Rows.Cast<System.Data.DataRow>());
    }

    /// <summary>
    /// Uruchamia monitorowanie i transfer PUT
    /// </summary>
    public void StartHotfolders()
    {
        foreach (FtpEndpoint enp in m_endpoints)
            if (enp.Direction.HasFlag(eFtpDirection.HotfolderPut))
            {
                var fhw = new FtpHotfolderWatcher(enp.GetModel(), m_database);
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
    private void OnAdd()
    {
        var details = m_mainWnd.m_tbSerweryDetails;
        details.m_endpoints = CollectionViewSource.GetDefaultView(m_endpoints) as IEditableCollectionView;
        details.m_mode = eDbOperation.Insert;
        details.DataContext = details.m_endpoints.AddNew();

        SwitchTabControl();
    }

    public void OnChange()
    {
        var details = m_mainWnd.m_tbSerweryDetails;
        details.m_endpoints = CollectionViewSource.GetDefaultView(m_endpoints) as IEditableCollectionView;
        details.m_endpoints.EditItem(m_selectedEndpoint);
        details.m_mode = eDbOperation.Update;
        details.DataContext = m_selectedEndpoint;

        SwitchTabControl();
    }

    private void OnRemove()
    {
        if (MessageBoxResult.Yes == MessageBox.Show($"Czy usunąć serwer {m_selectedEndpoint.Host}{m_selectedEndpoint.RemoteDirectory} ?", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Question)) {
            var errmsg = m_database.ModifyEndpoint(m_selectedEndpoint.GetModel(), eDbOperation.Delete);
            if (string.IsNullOrEmpty(errmsg))
                m_endpoints.Remove(m_selectedEndpoint);
            else
                m_mainWnd.ShowErrorInfo(eSeverityCode.Error, errmsg);
        }
    }

    private void OnTest()
    {
        if (m_selectedEndpoint != null) {
            Mouse.OverrideCursor = Cursors.Wait;

            string errmsg = string.Empty;
            var fu = IFtpUtility.Create(m_selectedEndpoint.GetModel());

            bool isErr = !fu.CheckConnection(ref errmsg);

            if (!fu.CheckLocalDirectory()) {
                isErr = true;
                errmsg += "\nKatalog lokalny nie istnieje"; 
            }

            Mouse.OverrideCursor = null;
            MessageBox.Show(errmsg, isErr ? "Ostrzeżenie" : "Info", MessageBoxButton.OK, isErr ? MessageBoxImage.Error : MessageBoxImage.Information);
        } else
            MessageBox.Show("Nie wybrano serwera do sprawdzenia.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnSync()
    {
        if (m_selectedEndpoint != null) {
            var disp = new FtpDispatcher(this.m_database);
            disp.StartNow(m_selectedEndpoint);
            m_mainWnd.tcMain.SelectedIndex = 0;
        }
    }

    private void SwitchTabControl()
    {
        m_mainWnd.tabSerwery.Visibility = Visibility.Collapsed;
        m_mainWnd.tabSerweryDetails.Visibility = Visibility.Visible;
        m_mainWnd.tcMain.SelectedIndex = 2;
    }
    #endregion
}
