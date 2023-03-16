
// -----------------------------------------------------------------------
// <copyright file="FtpsUtility.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, maj 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FluentFTP;

/// <summary>
/// Umożliwia przeglądanie zasobów serwera FTP i pobieranie plików
/// protokołem FTP poprzez kanał szyfrowany na poziomie TSL
/// </summary>
public sealed class FtpsUtility : FtpUtilityBase, IFtpUtility
{
    #region fields
    /// <summary>
    /// Rozmiar buforu używanego przy kopiowaniu plik�w
    /// </summary>
    private const int m_bufferSize = 1 << 12;

    /// <summary>
    /// Klient usługi FTPS z biblioteki FluentFTP
    /// </summary>
    private FtpClient m_ftpsClient;

    /// <summary>
    /// Czy użył połączenia szyfrowanego
    /// </summary>
    private bool m_secure;
    #endregion

    #region constructor/destructor
    /// <summary>
    /// Konstruktor FtpUtility sterowanego przez <see>FtpDispatcher</see>
    /// </summary>
    /// <param name="endpoint">Parametry serwera</param>
    /// <param name="dispatcher">Obiekt sterujący wątkami</param>
    /// <param name="mode">Algorytm kwalifikacji plików do transferu</param>
    /// <param name="secure">Czy użył połączenia szyfrowanego</param>
    public FtpsUtility(FtpEndpointModel endpoint, FtpDispatcher dispatcher, eSyncFileMode mode, bool secure)
        : base(endpoint, dispatcher, mode) {
        m_secure = secure;
    }

    /// <summary>
    /// Konstruktor FtpUtility dla pojedynczych usług
    /// </summary>
    /// <param name="endpoint">Parametry serwera</param>
    public FtpsUtility(FtpEndpointModel endpoint, bool secure)
        : base(endpoint) {
        m_secure = secure;
    }

    /// <summary>
    /// Destruktor zwalniający zasoby
    /// </summary>
    ~FtpsUtility()
    {
        if (m_ftpsClient != null)
            m_ftpsClient.Dispose();
    }
    #endregion

    #region public methods
    /// <summary>
    /// Łączy się z endpointem i pobiera wszystkie pliki późniejsze niż data ostatniego pobrania
    /// </summary>
    /// <returns>Informacja o skopiowanych plikach</returns>
    public FtpSyncFileModel[] Download()
    {
        if (!CheckLocalDirectory() || !CheckDispatcher())
            return null;

        Connect();

        var ret = new List<FtpSyncFileModel>();
        var files = m_ftpsClient.GetListing().Where(f => f.Type == FtpObjectType.File).ToArray();
        foreach (FtpListItem f in files)
            if (GetFile(f)) {
                ret.Add(new FtpSyncFileModel() {
                    Name = f.Name,
                    Size = f.Size,
                    Modified = f.Modified,
                    MD5 = (m_sLocalDir + f.Name).ComputeMD5()
                });
                if (FtpDispatcherGlobals.ShowError != null)
                    FtpDispatcherGlobals.ShowError(eSeverityCode.FileInfo, $"1|{f.Name}|{f.Size}|{f.Modified.ToBinary()}");
            }

        if (m_Disp != null && !m_Disp.InProgress && FtpDispatcherGlobals.ShowError != null)
            FtpDispatcherGlobals.ShowError(eSeverityCode.Message, $"Pobieranie z serwera {m_sHost}{m_sRemoteDir} zostało przerwane przez użytkownika");

        m_ftpsClient.Disconnect();

        return ret.ToArray();
    }

    /// <summary>
    /// Łączy się z endpointem i wstawia wszystkie pliki z lokalnego katalogu
    /// </summary>
    /// <returns>Informacja o skopiowanych plikach</returns>
    public FtpSyncFileModel[] Upload()
    {
        if (!CheckLocalDirectory() || !CheckDispatcher())
            return null;

        Connect();

        var ret = new List<FtpSyncFileModel>();
        foreach (string sFileName in Directory.GetFiles(m_sLocalDir)) {
            var fi = new FileInfo(sFileName);
            if (m_Disp.InProgress && PutFile(fi)) {
                ret.Add(new FtpSyncFileModel() {
                    Name = fi.Name,
                    Size = fi.Length,
                    Modified = fi.LastWriteTime,
                    MD5 = fi.FullName.ComputeMD5()
                });
                if (FtpDispatcherGlobals.ShowError != null)
                    FtpDispatcherGlobals.ShowError(eSeverityCode.FileInfo, $"2|{fi.Name}|{fi.Length}|{fi.LastWriteTime.ToBinary()}");
            }
        }

        if (m_Disp != null && !m_Disp.InProgress && FtpDispatcherGlobals.ShowError != null)
            FtpDispatcherGlobals.ShowError(eSeverityCode.Message, $"Wstawianie na serwer {m_sHost}{m_sRemoteDir} zostało przerwane przez użytkownika");

        m_ftpsClient.Disconnect();

        return ret.ToArray();
    }

    /// <summary>
    /// Łączy się z endpointem i wstawia jeden pliki z lokalnego hot folderu
    /// </summary>
    /// <returns>Status powodzenia operacji</returns>
    public bool UploadHotFile(FileInfo file)
    {
        if (!CheckLocalDirectory())
            return false;

        Connect();

        bool status = PutFile(file);
        if (status)
            FtpDispatcherGlobals.ShowError(eSeverityCode.FileInfo, $"4|{file.Name}|{file.Length}|{file.LastWriteTime.ToBinary()}");

        m_ftpsClient.Disconnect();

        return status;
    }
    #endregion

    #region protected override methods
    /// <summary>
    /// Otwiera połączenie z serwerem FTP, autoryzuje się i nawiązuje sesję
    /// </summary>
    /// <returns>True, a jeśli się nie uda, rzuca wyjątek</returns>
    protected override bool Connect()
    {
        m_ftpsClient = new FtpClient(m_sHost, m_sUser, m_sPass);
        var config = m_ftpsClient.Config;
        config.DataConnectionType = FtpDataConnectionType.PASV;
        config.EncryptionMode = m_secure ? FtpEncryptionMode.Explicit : FtpEncryptionMode.None;
        config.DataConnectionEncryption = true;
        config.ValidateAnyCertificate = true;

        var transferMode = m_TransferMode == eFtpTransferMode.Binary 
                ? FtpDataType.Binary : FtpDataType.ASCII;
        config.DownloadDataType = transferMode;
        config.UploadDataType = transferMode;

        try {
            m_ftpsClient.Connect();
            if (!m_ftpsClient.DirectoryExists(m_sRemoteDir))
                throw new FtpUtilityException($"Remote directory {m_sHost}{m_sRemoteDir} does not exist");
            m_ftpsClient.SetWorkingDirectory(m_sRemoteDir);
        } catch (Exception ex) {
            throw new FtpUtilityException($"InternetConnect to {m_sHost} failed. {ex.Message}");
        }

        return true;
    }
    #endregion

    #region private methods
    /// <summary>
    /// Pobiera plik zmodyfikowany po dacie ostatniej synchronizacji endpointu
    /// </summary>
    /// <param name="file">struktura opisuj�ca plik lub katalog</param>
    /// <returns>Czy dosz�o do pobrania pliku</returns>
    private bool GetFile(FtpListItem file)
    {
        if (file.Size == 0)
            return false;

        DateTime lastWrite = file.Modified;
        switch (m_SyncMode) {
            case eSyncFileMode.NewerThenRefreshDate:
                if (lastWrite < m_dtLastRefresh)
                    return false;
                break;
            case eSyncFileMode.UniqueDateAndSizeOnDisk:
                if (CheckLocalStorage(file.Name, file.Size))
                    return false;
                break;
            case eSyncFileMode.UniqueDateAndSizeInDatabase:
                if (m_Disp.CheckDatabase(file.Name, file.Size, lastWrite))
                    return false;
                break;
            case eSyncFileMode.AllFiles:
                break;
        }

        string localPath = m_sLocalDir + file.Name;

        try {
            using (var stream = File.Create(localPath, m_bufferSize))
                m_ftpsClient.DownloadStream(stream, file.Name);
        } catch(Exception ex) {
            var dirsep = m_sRemoteDir.EndsWith('/') ? string.Empty : "/";
            throw new FtpUtilityException($"Kopiowanie {m_sHost}{m_sRemoteDir}{dirsep}{file.Name} do {m_sLocalDir} nie powiodło się. {ex.Message}");
        }

        if (FtpDispatcherGlobals.CheckTransferedStorage) {
            bool bStatus = CheckLocalStorage(file.Name, file.Size);
            if (!bStatus && File.Exists(localPath))
                File.Delete(localPath);
            return bStatus;
        }

        m_Disp?.NotifyFileTransfer();

        return true;
    }

    /// <summary>
    /// Wstawia plik zmodyfikowany po dacie ostatniej synchronizacji endpointu
    /// </summary>
    /// <param name="pFI">struktura opisująca plik lub katalog</param>
    /// <returns>Czy doszło do wstawienia pliku</returns>
    private bool PutFile(FileInfo pFI)
    {
        if (pFI.Length == 0)
            return false;

        switch (m_SyncMode) {
            case eSyncFileMode.NewerThenRefreshDate:
                if (pFI.LastWriteTime < m_dtLastRefresh)
                    return false;
                break;
            case eSyncFileMode.UniqueDateAndSizeOnDisk:
                if (CheckRemoteStorage(pFI.Name, pFI.Length))
                    return false;
                break;
            case eSyncFileMode.UniqueDateAndSizeInDatabase:
                if (m_Disp.CheckDatabase(pFI.Name, pFI.Length, pFI.LastWriteTime))
                    return false;
                break;
            case eSyncFileMode.AllFiles:
                break;
        }

        string remoteFilename = m_sRemoteDir;
        if (!remoteFilename.EndsWith('/')) remoteFilename += '/';
        remoteFilename += pFI.Name;

        try {
            var stream = File.OpenRead(pFI.FullName);
            m_ftpsClient.UploadStream(stream, remoteFilename, FtpRemoteExists.Overwrite);
            if (FtpDispatcherGlobals.CheckTransferedStorage)
                return CheckRemoteStorage(remoteFilename, pFI.Length);
        } catch (Exception ex) {
            throw new FtpUtilityException($"Kopiowanie {pFI.FullName} do {m_sHost}{m_sRemoteDir} nie powiodło się. {ex.Message}");
        }

        m_Disp?.NotifyFileTransfer();

        return true;
    }

    /// <summary>
    /// Sprawdza, czy w zasobie zdalnym istnieje już plik o zadanej nazwie i rozmiarze
    /// </summary>
    /// <param name="remoteName">Nazwa liku</param>
    /// <param name="length">Długość pliku</param>
    /// <returns>Czy istnieje plik o zadanych cechach w katalogu zdalnym</returns>
    private bool CheckRemoteStorage(string remoteName, long length)
    {
        return m_ftpsClient.GetFileSize(remoteName) == length;
    }
    #endregion
}
