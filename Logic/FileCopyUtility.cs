
// -----------------------------------------------------------------------
// <copyright file="FileCopyUtility.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// <retired>Not used, for demonstration purposes only</retired>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Azure.Identity;

/// <summary>
/// Umożliwia przeglądanie zasob,ow serwera FTP i pobieranie plików
/// </summary>
public sealed class FileCopyUtility : FtpUtilityBase, IFtpUtility
{
    #region fields
    #endregion

    #region constructor
    /// <summary>
    /// Konstruktor FtpUtility sterowanego przez <see>FtpDispatcher</see>
    /// </summary>
    /// <param name="endpoint">Parametry serwera</param>
    /// <param name="dispatcher">Obiekt sterujący wątkami</param>
    /// <param name="mode">Algorytm kwalifikacji plików do transferu</param>
    public FileCopyUtility(FtpEndpointModel endpoint, FtpDispatcher dispatcher, eSyncFileMode mode)
        : base(endpoint, dispatcher, mode) {
    }

    /// <summary>
    /// Konstruktor FtpUtility dla pojedynczych us�ug
    /// </summary>
    /// <param name="endpoint">Parametry serwera</param>
    public FileCopyUtility(FtpEndpointModel endpoint)
        : base(endpoint) { 
    }
    #endregion

    #region public methods
    /// <summary>
    /// Łączy si z endpointem i pobiera wszystkie pliki p�niejsze ni� data ostatniego pobrania
    /// </summary>
    /// <returns>Informacja o skopiowanych plikach</returns>
    public FtpSyncFileModel[] Download()
    {
        if (!CheckLocalDirectory() || !CheckDispatcher())
            return null;

        Connect();

        var ret = new List<FtpSyncFileModel>();
        foreach (var path in Directory.GetFiles(m_sRemoteDir)) {
            var fi = new FileInfo(path);
            if (GetFile(fi)) {  // TODO: CancellationToken check
                ret.Add(new FtpSyncFileModel() {
                    Name = fi.Name,
                    Size = fi.Length,
                    Modified = fi.LastWriteTime,
                    MD5 = fi.FullName.ComputeMD5()
                });

                NotifyFileTransferred(eFtpDirection.Get, fi);
            }
        }

        if (m_Disp != null && !m_Disp.InProgress)
            NotifyFileTransferred(eSeverityCode.Message, eFtpDirection.Get, $"Kopiowanie plików z katalogu {m_sRemoteDir} zostało przerwane przez użytkownika");

        return ret.ToArray();
    }

    /// <summary>
    /// Łączy się z endpointem i wstawia wszystkie pliki z lokalnego katalogu
    /// </summary>
    /// <returns>Informacja o skopiowanych plikach</returns>
    public FtpSyncFileModel[] Upload()
    {
        NotifyFileTransferred(eSeverityCode.Warning, eFtpDirection.Put, $"Kopiowanie plików jest możliwe tylko przy użyciu operacji GET");
        return null;
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
            NotifyFileTransferred(eFtpDirection.HotfolderPut, file);

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
        return Directory.Exists(m_sRemoteDir);
    }
    #endregion

    #region private methods
    /// <summary>
    /// Pobiera plik zmodyfikowany po dacie ostatniej synchronizacji endpointu
    /// </summary>
    /// <param name="pFD">struktura opisująca plik lub katalog</param>
    /// <returns>Czy doszło do pobrania pliku</returns>
    private bool GetFile(FileInfo file)
    {
        if (!file.Exists || file.Length == 0)
            return false;

        switch (m_SyncMode) {
            case eSyncFileMode.NewerThenRefreshDate:
                if (file.LastWriteTime < m_dtLastRefresh)
                    return false;
                break;
            case eSyncFileMode.UniqueDateAndSizeOnDisk:
                if (CheckLocalStorage(file.Name, file.Length))
                    return false;
                break;
            case eSyncFileMode.UniqueDateAndSizeInDatabase:
                if (m_Disp.CheckDatabase(file.Name, file.Length, file.LastWriteTime))
                    return false;
                break;
            case eSyncFileMode.AllFiles:
                break;
        }

        string localPath = m_sLocalDir + file.Name;
        File.Copy(file.FullName, localPath);
            //throw new FtpUtilityException($"Kopiowanie {file.FullName} do {m_sLocalDir} nie powiodło się");

        m_Disp?.NotifyFileTransfer();

        if (FtpDispatcherGlobals.CheckTransferedStorage) {
            bool bStatus = CheckLocalStorage(file.Name, file.Length);
            if (!bStatus && File.Exists(localPath))
                File.Delete(localPath);
            return bStatus;
        }

        return true;
    }

    /// <summary>
    /// Wstawia plik zmodyfikowany po dacie ostatniej synchronizacji endpointu
    /// </summary>
    /// <param name="pFD">struktura opisuj�ca plik lub katalog</param>
    /// <returns>Czy dosz�o do wstawienia pliku</returns>
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

        //FtpPutFile(hFtpSess, pFI.FullName, pFI.Name, (uint)m_TransferMode, iContext);

        int iWin32Error = 111;// Marshal.GetLastWin32Error();
        if (iWin32Error > 0 && iWin32Error != 512 && iWin32Error != 12003)
            throw new FtpUtilityException($"Kopiowanie {pFI.FullName} do {m_sHost}{m_sRemoteDir} nie powiodło się", iWin32Error);

        m_Disp?.NotifyFileTransfer();

        if (FtpDispatcherGlobals.CheckTransferedStorage)
            return CheckRemoteStorage(pFI.Name, pFI.Length);

        return true;
    }

    /// <summary>
    /// Sprawdza, czy w zasobie zdalnym istnieje już plik o zadanej nazwie i rozmiarze
    /// </summary>
    /// <param name="remoteName">Nazwa liku</param>
    /// <param name="length">Długość pliku</param>
    /// <returns>Czy istnieje plik o zadanych cechach w katalogu zdalnym</returns>
    private bool CheckRemoteStorage(string sFileName, long sLength)
    {
        var fi = new FileInfo(sFileName);
        return fi.Exists && fi.Length == sLength;
    }
    #endregion
}
