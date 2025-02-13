
// -----------------------------------------------------------------------
// <copyright file="SterowanieViewModel.cs">
// <legal>Copyright (c) Marcin Buchwald, February 2025</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;

using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;

using FtpDiligent;
using FtpDiligent.Events;
using FtpDiligent.Views;

public sealed class SterowanieViewModel : BindableBase
{
    #region fields
    /// <summary>
    /// Długość logu transferowanych plików
    /// </summary>
    private const int m_fileLogSize = 100;

    /// <summary>
    /// Referencja do głównego okna
    /// </summary>
    private MainWindow m_mainWnd;

    /// <summary>
    /// Zarządza wątkami roboczymi
    /// </summary>
    private IFtpDispatcher m_dispatcher;

    /// <summary>
    /// Konfiguracja aplikacji
    /// </summary>
    private IFtpDiligentConfig m_config;

    /// <summary>
    /// Mechanizm do przekazywania powiadomień
    /// </summary>
    private IEventAggregator m_eventAggr;

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
    public eSyncFileMode SelectedSyncMode
    {
        get => FtpDiligentGlobals.SyncMode;
        set { SetProperty(ref FtpDiligentGlobals.SyncMode, value); }
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

    public bool Processing {
        get => m_processing;
        set {
            SetProperty(ref m_processing, value);
            RaisePropertyChanged(nameof(NotProcessing));
        }
    }

    public bool NotProcessing => !m_processing;

    public int FilesCount => m_dispatcher.GetNumberOfFilesTransferred();

    public Array SynchronizationModes => Enum.GetValues(typeof(eSyncFileMode));
    #endregion

    #region commands
    public DelegateCommand StartProcessingCommand { get; private set; }
    public DelegateCommand StopProcessingCommand { get; private set; }
    public DelegateCommand ClearLogsCommand { get; private set; }
    #endregion

    #region constructor
    public SterowanieViewModel(MainWindow wnd, IFtpDiligentConfig config, IFtpDispatcher dispatcher, IEventAggregator eventAggr)
    {
        wnd.m_tbSterowanie = this;

        m_mainWnd = wnd;
        m_config = config;
        m_eventAggr = eventAggr;
        m_dispatcher = dispatcher;
        Processing = false;
        wnd.Closed += OnClosed;
        StartProcessingCommand = new DelegateCommand(OnStartSync).ObservesCanExecute(() => NotProcessing);
        StopProcessingCommand = new DelegateCommand(OnStopSync).ObservesCanExecute(() => Processing);
        ClearLogsCommand = new DelegateCommand(OnClearLog);

        m_eventAggr.GetEvent<StatusEvent>().Subscribe(GuiShowInfo, ThreadOption.UIThread);
        m_eventAggr.GetEvent<FileTransferredEvent>().Subscribe(GuiShowTransferDetails, ThreadOption.UIThread);
        m_config.LoadConfig();
    }
    #endregion

    #region handlers
    /// <summary>
    /// Gdy okno zostało zamknięte
    /// </summary>
    private void OnClosed(object sender, EventArgs e)
    {
        m_config.SaveConfig();
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
            GuiShowInfo(new StatusEventArgs(eSeverityCode.Message, "Restarting dispatcher"));
            Thread.Sleep(5000);
            m_dispatcher.Start();
        }
    }
    #endregion

    #region private
    /// <summary>
    /// Aktualizuje kontrolki na zakładce Sterowanie
    /// </summary>
    /// <param name="status">Kategoria powiadomienia oraz treść wiadomości</param>
    private void GuiShowInfo(StatusEventArgs status)
    {
        switch (status.severity)
        {
            case eSeverityCode.NextSync:
                NextSyncDateTime = status.message;
                break;
            case eSeverityCode.Message:
                MessageLog.Insert(0, $"{DateTime.Now:dd/MM/yyyy HH:mm} {status.message}");
                if (FtpDiligentGlobals.TraceLevel.HasFlag(eSeverityCode.Message))
                    EventLog.WriteEntry(FtpDiligentGlobals.EventLog, status.message, EventLogEntryType.Information);
                break;
            case eSeverityCode.Warning:
                ErrorLog.Insert(0, new FtpErrorModel() { Category = status.severity, Message = status.message });
                if (FtpDiligentGlobals.TraceLevel.HasFlag(eSeverityCode.Warning))
                    EventLog.WriteEntry(FtpDiligentGlobals.EventLog, status.message, EventLogEntryType.Warning);
                break;
            case eSeverityCode.TransferError:
                RestartScheduler();
                FtpDiligentGlobals.Mailer.Run(status.message);
                goto case eSeverityCode.Error;
            case eSeverityCode.Error:
                ErrorLog.Insert(0, new FtpErrorModel() { Category = status.severity, Message = status.message });
                if (FtpDiligentGlobals.TraceLevel.HasFlag(eSeverityCode.Error))
                    EventLog.WriteEntry(FtpDiligentGlobals.EventLog, status.message, EventLogEntryType.Error);
                break;
            default:
                ErrorLog.Insert(0, new FtpErrorModel() { Category = status.severity, Message = status.message });
                if (FtpDiligentGlobals.TraceLevel.HasFlag(eSeverityCode.Warning))
                    EventLog.WriteEntry(FtpDiligentGlobals.EventLog, status.message, EventLogEntryType.Warning);
                break;
        }
    }

    /// <summary>
    /// Aktualizuje informacje o przesyłaniu plików na zakładce Sterowanie
    /// </summary>
    /// <param name="arg">Szczegóły operacji</param>
    private void GuiShowTransferDetails(FileTransferredEventArgs arg)
    {
        FtpFileLog.Insert(0, new FtpFileModel()
        {
            Instance = (byte)arg.direction,
            FileName = arg.file.FullName,
            FileSize = arg.file.Length,
            FileDate = arg.file.LastWriteTime
        });

        if (FtpFileLog.Count > m_fileLogSize)
            FtpFileLog.RemoveAt(m_fileLogSize);

        m_dispatcher.NotifyFileTransfer();
        RaisePropertyChanged(nameof(FilesCount));

        if (FtpDiligentGlobals.TraceLevel.HasFlag(eSeverityCode.FileInfo))
            EventLog.WriteEntry(FtpDiligentGlobals.EventLog, arg.file.FullName + " transferred.", EventLogEntryType.SuccessAudit);
    }

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
            GuiShowInfo(new StatusEventArgs(eSeverityCode.Error, $"Katalog lokalny {hostWithBadDir} jest niepoprawny"));
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
