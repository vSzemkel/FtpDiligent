﻿
// -----------------------------------------------------------------------
// <copyright file="SerweryDetails.xaml.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent.Views;

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

using FtpDiligent.Events;

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

    /// <summary>
    /// Repozytorium danych
    /// </summary>
    private IFtpRepository m_repository;
    #endregion

    #region events
    private StatusEvent ShowStatus;
    #endregion

    #region constructors
    public SerweryDetails(MainWindow wnd, IFtpRepository repository)
    {
        InitializeComponent();

        m_mainWnd = wnd;
        m_repository = repository;
        ShowStatus = FtpDiligentGlobals.EventAggregator.GetEvent<StatusEvent>();
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
            endpoint.Instance = FtpDiligentGlobals.Instance;
            SanitizeDirectories(ref endpoint);
            errmsg = m_repository.ModifyEndpoint(endpoint.GetModel(), m_mode);
            if (string.IsNullOrEmpty(errmsg)) {
                endpoint.XX = m_repository.GetLastInsertedKey();
                m_endpoints.CommitNew();
            }
        } else {
            var endpoint = m_endpoints.CurrentEditItem as FtpEndpoint;
            SanitizeDirectories(ref endpoint);
            errmsg = m_repository.ModifyEndpoint(endpoint.GetModel(), m_mode);
            if (string.IsNullOrEmpty(errmsg))
                m_endpoints.CommitEdit();
        }

        if (string.IsNullOrEmpty(errmsg))
            RestoreTabControl();
        else
            ShowStatus.Publish(new StatusEventArgs(eSeverityCode.Error, errmsg));
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
        if (enp.Protocol != eFtpProtocol.FileCopy && (string.IsNullOrEmpty(enp.RemoteDirectory) || !enp.RemoteDirectory.StartsWith('/')))
            enp.RemoteDirectory = '/' + (enp.RemoteDirectory ?? string.Empty);
        if (string.IsNullOrEmpty(enp.LocalDirectory))
            enp.LocalDirectory = "\\";
        if (!enp.LocalDirectory.EndsWith('\\'))
            enp.LocalDirectory += '\\';
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
