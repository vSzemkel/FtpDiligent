
// -----------------------------------------------------------------------
// <copyright file="FtpHotfolderWatcher.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

/// <summary>
/// Klasa monitorujace hotfolder i transmitująca nowe pliki
/// </summary>
public class FtpHotfolderWatcher
{
    #region fields
    /// <summary>
    /// Referencja do głównego okna
    /// </summary>
    private MainWindow m_mainWnd;

    /// <summary>
    /// Klient usługi FTP
    /// </summary>
    private IFtpUtility m_ftpUtility;

    /// <summary>
    /// Instancja watchera dostarczana przez framework
    /// </summary>
    private FileSystemWatcher m_hotfolderWatcher;

    /// <summary>
    /// Pliki teoretycznie nadal podlegające modyfikacji
    /// </summary>
    private List<FileInfo> m_stagedFiles = new List<FileInfo>();

    /// <summary>
    /// Umożliwia zatrzymanie pętli przeglądania <see cref="m_stagedFiles"/>
    /// </summary>
    private CancellationTokenSource m_cts;

    /// <summary>
    /// Dane o transferze zachowywane w bazie danych
    /// </summary>
    private FtpSyncModel m_log = new FtpSyncModel();

    /// <summary>
    /// Klient bazy danych
    /// </summary>
    private IFtpDiligentDatabaseClient m_database { get; set; }
    #endregion

    #region constructor
    /// <summary>
    /// Konstruktor FtpUtility dla pojedynczych usług
    /// </summary>
    /// <param name="endpoint">Parametry serwera</param>
    /// <param name="window">Główne okno aplikacji</param>
    public FtpHotfolderWatcher(FtpEndpointModel endpoint, MainWindow wnd, IFtpDiligentDatabaseClient database)
    {
        m_mainWnd = wnd;
        m_database = database;
        m_ftpUtility = IFtpUtility.Create(endpoint, wnd);
        m_log.xx = -endpoint.xx;
        m_log.syncTime = DateTime.Now;
        m_log.direction = eFtpDirection.HotfolderPut;
        RegisterWatcher();
    }
    #endregion

    #region public members
    /// <summary>
    /// Włącza monitoring
    /// </summary>
    public void StartWatching()
    {
        m_hotfolderWatcher.EnableRaisingEvents = true;
        m_cts = new CancellationTokenSource();
        ThreadPool.QueueUserWorkItem(PumpStage, m_cts.Token);
    }

    /// <summary>
    /// Wyłącza monitoring
    /// </summary>
    public void StopWatching()
    {
        m_cts.Cancel();
        m_hotfolderWatcher.EnableRaisingEvents = false;
    }
    #endregion

    #region private members
    /// <summary>
    /// Przechwytuja zdarzenia utworzenia, modyfikacji i usunięcia pliku lub katalogu
    /// </summary>
    protected void OnStage(object source, FileSystemEventArgs e)
    {
        lock (m_stagedFiles) {
            if (m_stagedFiles.All(fi => fi.FullName != e.FullPath)) {
                var fi = new FileInfo(e.FullPath);
                if (!fi.Attributes.HasFlag(FileAttributes.Directory))
                    m_stagedFiles.Add(fi);
            }
        }
    }

    /// <summary>
    /// Inicjalizacja watchera na podstawie parametrów konfiguracyjnych: ścieżki i rozmiaru bufora
    /// </summary>
    // [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    private void RegisterWatcher()
    {
        m_hotfolderWatcher = new FileSystemWatcher();
        m_hotfolderWatcher.Path = m_ftpUtility.GetLocalDirectory();
        m_hotfolderWatcher.IncludeSubdirectories = false;
        m_hotfolderWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;

        m_hotfolderWatcher.Changed += new FileSystemEventHandler(OnStage);
        m_hotfolderWatcher.Created += new FileSystemEventHandler(OnStage);
        m_hotfolderWatcher.Renamed += new RenamedEventHandler(OnStage);
    }

    /// <summary>
    /// Wątek roboczy przegląda
    /// </summary>
    private void PumpStage(object o)
    {
        var staged = new List<FileInfo>();
        var good2go = new List<FileInfo>();
        CancellationToken ct = (CancellationToken)o;

        while (!ct.IsCancellationRequested) {
            // select
            lock (m_stagedFiles) {
                foreach (FileInfo fi in m_stagedFiles.ToArray()) {
                    try {
                        var f = fi.OpenWrite();
                        if (!f.CanWrite)
                            staged.Add(fi);
                        else
                            good2go.Add(fi);
                        f.Close();
                    } catch (UnauthorizedAccessException) {
                        staged.Add(fi);
                        FtpDispatcherGlobals.ShowError(eSeverityCode.Message, $"Odczyt pliku {fi.FullName} zostanie ponowiony");
                    } catch (Exception e) {
                        staged.Add(fi);
                        FtpDispatcherGlobals.ShowError(eSeverityCode.Warning, $"Nie udało się odczytać pliku {fi.FullName} {e.Message}");
                    }

                }

                m_stagedFiles.Clear();
                m_stagedFiles.AddRange(staged);
            }
            // transfer (deep copy od good2go)
            if (good2go.Any())
                ThreadPool.QueueUserWorkItem(UploadFiles, good2go.ToArray());
            // wait for the next run
            Thread.Sleep(1000 * m_mainWnd.m_hotfolderInterval);
            // clear
            staged.Clear();
            good2go.Clear();
        }

        m_cts.Dispose();
    }

    /// <summary>
    /// Czasochłonne kopiowanie plików w osobnym wątku
    /// </summary>
    /// <param name="o">Lista plików do transferu</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Await.Warning", "CS4014:Await.Warning")]
    private void UploadFiles(object o)
    {
        var files = o as FileInfo[];
        var log = new List<FtpSyncFileModel>();
        try {
            foreach (FileInfo fi in files)
                if (m_ftpUtility.UploadHotFile(fi))
                    log.Add(new FtpSyncFileModel() {
                        Name = fi.Name,
                        Size = fi.Length,
                        Modified = fi.LastWriteTime,
                        MD5 = fi.FullName.ComputeMD5()
                    });
        } catch (FtpUtilityException fex) {
            m_mainWnd.ShowErrorInfo(eSeverityCode.TransferError, fex.Message);
        } catch (System.Exception se) {
            m_mainWnd.ShowErrorInfo(eSeverityCode.TransferError, se.Message);
        }

        m_log.files = log.ToArray();

        // not wait CS4014
        m_database.LogSync(m_log);
    }
    #endregion
}
