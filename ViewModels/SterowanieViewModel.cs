// -----------------------------------------------------------------------
// <copyright file="SterowanieViewModel.cs" company="private project">
// <legal>Copyright (c) MB, February 2025</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;

using Prism.Commands;
using Prism.Mvvm;
using FtpDiligent;
using FtpDiligent.Views;

public sealed class SterowanieViewModel : BindableBase
{
    #region fields
    /// <summary>
    /// Referencja do głównego okna
    /// </summary>
    private MainWindow m_mainWnd;

    /// <summary>
    /// Zarządza wątkami roboczymi
    /// </summary>
    private IFtpDispatcher m_dispatcher;

    /// <summary>
    /// Lista ostatnio transferowanych plików
    /// </summary>
    private ObservableCollection<FtpFileModel> m_fileInfo = new();

    /// <summary>
    /// Log przetwarzania
    /// </summary>
    private ObservableCollection<string> m_msgLog = new();

    /// <summary>
    /// Lista ostatnio zarejestrowanych błędów i ostrzeżeń
    /// </summary>
    private ObservableCollection<FtpErrorModel> m_errLog = new();

    /// <summary>
    /// Komunikat o czasie najbliższego transferu plików
    /// </summary>
    private string m_nextSync;

    /// <summary>
    /// Czy trwa przetwarzanie
    /// </summary>
    private bool m_processing;
    #endregion

    #region properties
    public Array SynchronizationModes => Enum.GetValues(typeof(eSyncFileMode));

    public eSyncFileMode SelectedSyncMode
    {
        get => FtpDispatcherGlobals.SyncMode;
        set { SetProperty(ref FtpDispatcherGlobals.SyncMode, value); }
    }

    public ObservableCollection<FtpFileModel> FtpFileLog
    {
        get => m_fileInfo;
        set { SetProperty(ref m_fileInfo, value); }
    }

    public ObservableCollection<string> MessageLog
    {
        get => m_msgLog;
        set { SetProperty(ref m_msgLog, value); }
    }

    public ObservableCollection<FtpErrorModel> ErrorLog
    {
        get => m_errLog;
        set { SetProperty(ref m_errLog, value); }
    }

    public string NextSyncDateTime
    {
        get => m_nextSync;
        set { SetProperty(ref m_nextSync, value); }
    }

    public int FilesCount
    {
        get => m_dispatcher.GetNumberOfFilesTransferred();
    }

    public bool Processing {
        get => m_processing;
        set {
            SetProperty(ref m_processing, value);
            RaisePropertyChanged(nameof(NotProcessing));
        }
    }

    public bool NotProcessing { get => !m_processing; }
    #endregion

    #region commands
    public DelegateCommand StartProcessingCommand { get; private set; }
    public DelegateCommand StopProcessingCommand { get; private set; }
    public DelegateCommand ClearLogsCommand { get; private set; }
    #endregion

    #region constructors
    public SterowanieViewModel(MainWindow wnd, IFtpDispatcher dispatcher)
    {
        wnd.m_tbSterowanie = this;

        m_mainWnd = wnd;
        m_dispatcher = dispatcher;
        Processing = false;
        StartProcessingCommand = new DelegateCommand(OnStartSync).ObservesCanExecute(() => NotProcessing);
        StopProcessingCommand = new DelegateCommand(OnStopSync).ObservesCanExecute(() => Processing);
        ClearLogsCommand = new DelegateCommand(OnClearLog);
    }
    #endregion

    #region public
    /// <summary>
    /// Po wystapieniu eSeverityCode.TransferError restartuje scheduler
    /// </summary>
    public void RestartScheduler()
    {
        if (m_dispatcher.GetNumberOfFilesTransferred() > 0)
        {
            m_dispatcher.Stop();
            m_mainWnd.ShowErrorInfo(eSeverityCode.Message, "Restarting dispatcher");
            Thread.Sleep(5000);
            m_dispatcher.Start();
        }
    }

    /// <summary>
    /// Aktualizuje statystytkę przekazanych plików
    /// </summary>
    public void NotifyFileTransfer()
    {
        m_dispatcher.NotifyFileTransfer();
        RaisePropertyChanged(nameof(FilesCount));
    }
    #endregion

    #region private
    /// <summary>
    /// Uruchamia realizację zaplanowanych transferów
    /// </summary>
    private void OnStartSync()
    {
        string hostWithBadDir = CheckLocDirs();
        if (string.IsNullOrEmpty(hostWithBadDir))
        {
            Processing = true;
            m_dispatcher.Start();
            m_mainWnd.m_tbSerwery.StartHotfolders();
        } else
            MessageBox.Show($"Katalog lokalny {hostWithBadDir} jest niepoprawny", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    /// <summary>
    /// Zatrzymuje realizację zaplanowanych transferów
    /// </summary>
    private void OnStopSync()
    {
        Processing = false;
        m_dispatcher.Stop();
        m_mainWnd.m_tbSerwery.StopHotfolders();
    }

    /// <summary>
    /// Usuwa bieżącą zawartość listboxów prezentujących informacje o ostatnich operacjacg
    /// </summary>
    private void OnClearLog()
    {
        MessageBoxResult drQuest = MessageBox.Show("Czy chcesz wyczyścić okienka logów?", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (drQuest == MessageBoxResult.Yes)
        {
            FtpFileLog.Clear();
            ErrorLog.Clear();
            MessageLog.Clear();
        }
    }

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
