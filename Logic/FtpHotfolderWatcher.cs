
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

using Prism.Events;

using FtpDiligent.Events;

/// <summary>
/// Klasa monitorujace hotfolder i transmitująca nowe pliki
/// </summary>
public class FtpHotfolderWatcher
{
    #region fields
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
    private Dictionary<int, FileInfo> m_stagedFiles = new();

    /// <summary>
    /// Umożliwia zatrzymanie pętli przeglądania <see cref="m_stagedFiles"/>
    /// </summary>
    private CancellationTokenSource m_cts;

    /// <summary>
    /// Dane o transferze zachowywane w bazie danych
    /// </summary>
    private FtpSyncModel m_log = new();

    /// <summary>
    /// Repozytorium danych
    /// </summary>
    private IFtpRepository m_repository;
    #endregion

    #region events
    /// <summary>
    /// Rozgłasza status monitoringu
    /// </summary>
    private StatusEvent HotfolderStatusNotification;
    #endregion

    #region constructor
    /// <summary>
    /// Konstruktor FtpUtility dla pojedynczych usług
    /// </summary>
    /// <param name="endpoint">Parametry serwera</param>
    /// <param name="repository">Repozytorium danych</param>
    public FtpHotfolderWatcher(FtpEndpointModel endpoint, IEventAggregator eventAggr, IFtpRepository repository)
    {
        m_repository = repository;
        m_ftpUtility = IFtpUtility.Create(endpoint);
        m_log.xx = -endpoint.xx;
        m_log.syncTime = DateTime.Now;
        m_log.direction = eFtpDirection.HotfolderPut;
        HotfolderStatusNotification = eventAggr.GetEvent<StatusEvent>();
        RegisterWatcher();
    }
    #endregion

    #region public members
    /// <summary>
    /// Włącza monitoring
    /// </summary>
    public void StartWatching()
    {
        m_cts = new CancellationTokenSource();
        m_hotfolderWatcher.EnableRaisingEvents = true;
        ThreadPool.QueueUserWorkItem(PumpStaged, m_cts.Token);
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
            var file = new FileInfo(e.FullPath);
            if (!file.Attributes.HasFlag(FileAttributes.Directory))
                m_stagedFiles[file.FullName.GetHashCode()] = file; // potencialnie nadpisuje rekordy plików wciąż modyfikowanych
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
    /// Wątek roboczy przegląda pliki i przekazuje do transferu gotowe do przesłania
    /// </summary>
    private void PumpStaged(object o)
    {
        var ct = (CancellationToken)o;
        var good2go = new List<FileInfo>();
        var staged = new Dictionary<int, FileInfo>();

        while (!ct.IsCancellationRequested) {
            lock (m_stagedFiles) {
                // select local files ready to upload
                foreach (var stagedInfo in m_stagedFiles) {
                    var file = stagedInfo.Value;
                    try {
                        using (var fs = file.OpenWrite()) 
                            if (fs.CanWrite) {
                                good2go.Add(file);
                                continue;
                            }
                    } catch (UnauthorizedAccessException) {
                        HotfolderStatusNotification.Publish(new StatusEventArgs(eSeverityCode.Message, $"Odczyt pliku {file.FullName} zostanie ponowiony"));
                    } catch (Exception e) {
                        HotfolderStatusNotification.Publish(new StatusEventArgs(eSeverityCode.Warning, $"Nie udało się odczytać pliku {file.FullName} {e.Message}"));
                    }

                    staged.Add(stagedInfo.Key, file);
                }

                m_stagedFiles = staged;
                staged.Clear();
            }
            // transfer (deep copy of good2go)
            if (good2go.Any())
                ThreadPool.QueueUserWorkItem(UploadFiles, good2go.ToArray());
            // wait for the next run
            Thread.Sleep(1000 * FtpDiligentGlobals.HotfolderInterval);
            // clear
            good2go.Clear();
        }

        m_cts.Dispose();
    }

    /// <summary>
    /// Czasochłonne kopiowanie plików w osobnym wątku
    /// </summary>
    /// <param name="o">Lista plików do transferu</param>
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
            HotfolderStatusNotification.Publish(new StatusEventArgs(eSeverityCode.TransferError, fex.Message));
        } catch (System.Exception se) {
            HotfolderStatusNotification.Publish(new StatusEventArgs(eSeverityCode.TransferError, se.Message));
        }

        m_log.files = log.ToArray();

        m_repository.LogSync(m_log);
    }
    #endregion
}
