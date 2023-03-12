
// -----------------------------------------------------------------------
// <copyright file="SFtpUtility.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, maj 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;

/// <summary>
/// Umo�liwia przegl�danie zasob�w serwera FTP i pobieranie plik�w
/// poprzez transport oparty na protokole SSH (port 22)
/// </summary>
public sealed class SFtpUtility : FtpUtilityBase, IFtpUtility
{
    #region fields
    /// <summary>
    /// Rozmiar buforu u�ywanego przy kopiowaniu plik�w
    /// </summary>
    private const int m_bufferSize = 1 << 12;

    /// <summary>
    /// Klient us�ugi SFTP z biblioteki Renci.SshNet
    /// </summary>
    private SftpClient m_sftpClient;

    /// <summary>
    /// RSA primary key u�ywany do komunikacji po SSH
    /// </summary>
    private string m_privateKey = System.Configuration.ConfigurationManager.AppSettings["SftpKey"];
    #endregion

    #region constructor/destructor
    /// <summary>
    /// Konstruktor FtpUtility sterowanego przez <see>FtpDispatcher</see>
    /// </summary>
    /// <param name="endpoint">Parametry serwera</param>
    /// <param name="dispatcher">Obiekt steruj�cy w�tkami</param>
    /// <param name="mode">Algorytm kwalifikacji plik�w do transferu</param>
    public SFtpUtility(FtpEndpointModel endpoint, FtpDispatcher dispatcher, eSyncFileMode mode)
        : base(endpoint, dispatcher, mode) {
    }

    /// <summary>
    /// Konstruktor FtpUtility dla pojedynczych us�ug
    /// </summary>
    /// <param name="endpoint">Parametry serwera</param>
    public SFtpUtility(FtpEndpointModel endpoint)
        : base(endpoint) {
    }

    /// <summary>
    /// Destruktor zwalniaj�cy zasoby
    /// </summary>
    ~SFtpUtility()
    {
        if (m_sftpClient != null)
            m_sftpClient.Dispose();
    }
    #endregion

    #region public methods
    /// <summary>
    /// Łączy się z endpointem i pobiera wszystkie pliki póniejsze niż data ostatniego pobrania
    /// </summary>
    /// <returns>Informacja o skopiowanych plikach</returns>
    public FtpSyncFileModel[] Download()
    {
        if (!CheckLocalDirectory() || !CheckDispatcher())
            return null;

        Connect();

        var ret = new List<FtpSyncFileModel>();
        var files = m_sftpClient.ListDirectory(m_sRemoteDir).Where(f => f.IsRegularFile).ToArray();
        foreach (SftpFile f in files)
            if (GetFile(f)) {
                ret.Add(new FtpSyncFileModel() {
                    Name = f.Name,
                    Size = f.Length,
                    Modified = f.LastWriteTime,
                    MD5 = (m_sLocalDir + f.Name).ComputeMD5()
                });
                if (FtpDispatcherGlobals.ShowError != null)
                    FtpDispatcherGlobals.ShowError(eSeverityCode.FileInfo, $"1|{f.Name}|{f.Length}|{f.LastWriteTime.ToBinary()}");
            }

        if (m_Disp != null && !m_Disp.InProgress && FtpDispatcherGlobals.ShowError != null)
            FtpDispatcherGlobals.ShowError(eSeverityCode.Message, $"Pobieranie z serwera {m_sHost}{m_sRemoteDir} zosta�o przerwane przez u�ytkownika");

        m_sftpClient.Disconnect();

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

        m_sftpClient.Disconnect();

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

        m_sftpClient.Disconnect();

        return status;
    }
    #endregion

    #region protected override methods
    /// <summary>
    /// Otwiera po��czenie z serwerem FTP, autoryzuje si� i nawi�zuje sesj�
    /// </summary>
    /// <returns>True, a je�li si� nie uda, rzuca wyj�tek</returns>
    protected override bool Connect()
    {
        if (string.IsNullOrEmpty(m_sPass)) {
            var gzip = GetSSHPrivateKey();
            m_sftpClient = new SftpClient(m_sHost, m_sUser, new PrivateKeyFile(gzip));
        } else
            m_sftpClient = new SftpClient(m_sHost, m_sUser, m_sPass);

        try {
            m_sftpClient.Connect();
            m_sftpClient.ChangeDirectory(m_sRemoteDir);
        } catch (SftpPathNotFoundException) {
            throw new FtpUtilityException($"Remote directory {m_sHost}{m_sRemoteDir} does not exist");
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
    private bool GetFile(SftpFile file)
    {
        if (file.Length == 0)
            return false;

        DateTime lastWrite = file.LastWriteTime;
        switch (m_SyncMode) {
            case eSyncFileMode.NewerThenRefreshDate:
                if (lastWrite < m_dtLastRefresh)
                    return false;
                break;
            case eSyncFileMode.UniqueDateAndSizeOnDisk:
                if (CheckLocalStorage(file.Name, file.Length))
                    return false;
                break;
            case eSyncFileMode.UniqueDateAndSizeInDatabase:
                if (m_Disp.CheckDatabase(file.Name, file.Length, lastWrite))
                    return false;
                break;
            case eSyncFileMode.AllFiles:
                break;
        }

        string localPath = m_sLocalDir + file.Name;

        try {
            using (var stream = File.Create(localPath, m_bufferSize))
                m_sftpClient.DownloadFile(file.Name, stream);
        } catch(Exception ex) {
            var dirsep = m_sRemoteDir.EndsWith('/') ? string.Empty : "/";
            throw new FtpUtilityException($"Kopiowanie {m_sHost}{m_sRemoteDir}{dirsep}{file.Name} do {m_sLocalDir} nie powiodło się. {ex.Message}");
        }

        if (FtpDispatcherGlobals.CheckTransferedStorage) {
            bool bStatus = CheckLocalStorage(file.Name, file.Length);
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
            m_sftpClient.UploadFile(stream, remoteFilename);
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
        SftpFile f = m_sftpClient.Get(remoteName);
        return f.Length == length;
    }

    /// <summary>
    /// Stream z kluczem prywatnym
    /// </summary>
    /// <returns>Otwarty stream, kt�ry costanie zamkni�ty w konstruktorze SftpClient</returns>
    private GZipStream GetSSHPrivateKey()
    {
        // scrumble
        byte[] compressedKey = Convert.FromBase64String(m_privateKey);
        byte[] xorKey = System.Text.ASCIIEncoding.ASCII.GetBytes("Grizzli");
        var keylen = xorKey.Length;
        for (int i = 0; i < compressedKey.Length; ++i)
            compressedKey[i] ^= xorKey[i % keylen];
        // compress
        var buff = new MemoryStream(compressedKey);
        return new GZipStream(buff, CompressionMode.Decompress);
    }

    /// <summary>
    /// Genrates one liner, recoverable private kay
    /// </summary>
    /// <returns>Single line string</returns>
    private string GenerateScrumbledSSHPrivateKey()
    {
        // compress
        var compressed = new MemoryStream();
        var ds = new GZipStream(compressed, CompressionLevel.Optimal);
        var stream = File.OpenRead(@"c:\Code\FtpDiligent\FtpDiligent_RSA");
        stream.CopyTo(ds);
        ds.Close();
        // scrumble
        var compressedKey = compressed.GetBuffer();
        byte[] xorKey = System.Text.ASCIIEncoding.ASCII.GetBytes("Grizzli");
        var xorKeyLen = xorKey.Length;
        for (int i = 0; i < compressedKey.Length; ++i)
            compressedKey[i] ^= xorKey[i % xorKeyLen];
        return Convert.ToBase64String(compressedKey);
    }
    #endregion
}
