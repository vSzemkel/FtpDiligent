
// -----------------------------------------------------------------------
// <copyright file="FtpUtility.cs" company="Agora SA">
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
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

using FtpDiligent.Events;

[StructLayout(LayoutKind.Sequential)]
public class WIN32_FIND_DATA
{
    public Int32 dwFileAttributes = 0;
    public FILETIME ftCreationTime;
    public FILETIME ftLastAccessTime;
    public FILETIME ftLastWriteTime;
    public Int32 nFileSizeHigh = 0;
    public Int32 nFileSizeLow = 0;
    public Int32 dwReserved0 = 0;
    public Int32 dwReserved1 = 0;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public string cFileName = null; //[MAX_PATH];
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
    public string cAlternateFileName = null;
    public Int32 dwFileType = 0;
    public Int32 dwCreatorType = 0;
    public Int16 wFinderFlags = 0;
}

/// <summary>
/// Umożliwia przeglądanie zasob,ow serwera FTP i pobieranie plików
/// </summary>
public sealed class FtpUtility : FtpUtilityBase, IFtpUtility, IDisposable
{
    #region fields
    private bool bDisposed = false;
    private IntPtr hFtpConn = IntPtr.Zero;
    private IntPtr hFtpSess = IntPtr.Zero;
    private IntPtr iContext;
    #endregion

    #region constructor
    /// <summary>
    /// Konstruktor FtpUtility sterowanego przez <see>FtpDispatcher</see>
    /// </summary>
    /// <param name="endpoint">Parametry serwera</param>
    /// <param name="dispatcher">Obiekt sterujący wątkami</param>
    /// <param name="mode">Algorytm kwalifikacji plików do transferu</param>
    public FtpUtility(FtpEndpointModel endpoint, FtpDispatcher dispatcher, eSyncFileMode mode)
        : base(endpoint, dispatcher, mode) {
    }

    /// <summary>
    /// Konstruktor FtpUtility dla pojedynczych us�ug
    /// </summary>
    /// <param name="endpoint">Parametry serwera</param>
    public FtpUtility(FtpEndpointModel endpoint)
        : base(endpoint) { 
    }
    #endregion

    #region dispose patern implementation
    ~FtpUtility()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool bDisposing)
    {
        if (!bDisposed) {
            InternetCloseHandle(hFtpSess);
            InternetCloseHandle(hFtpConn);
            hFtpSess = hFtpConn = IntPtr.Zero;
            bDisposed = true;
        }
    }
    #endregion

    #region SDK constants
    private static readonly int iPort = 21;                          // default ftp
    private static readonly int ERROR_NO_MORE_FILES = 18;
    private static readonly int INTERNET_SERVICE_FTP = 1;
    private static readonly int INTERNET_OPEN_TYPE_DIRECT = 1;
    private static readonly uint GENERIC_READ = 0x80000000;
    private static readonly uint FILE_ATTRIBUTE_NORMAL = 128;
    private static readonly uint FILE_ATTRIBUTE_DIRECTORY = 16;
    private static readonly uint INTERNET_FLAG_PASSIVE = 0x08000000; // for InternetConnect to non microsoft servers
    //private static readonly uint FTP_TRANSFER_TYPE_ASCII = 1;
    //private static readonly uint FTP_TRANSFER_TYPE_BINARY = 2;
    private static readonly uint INTERNET_FLAG_RELOAD = 0x80000000;
    #endregion

    #region DllImport from wininet.dll
    [DllImport("wininet.dll", SetLastError = true)]
    public static extern IntPtr InternetOpen(string lpszAgent, Int32 dwAccessType, string lpszProxyName, string lpszProxyBypass, UInt32 dwFlags);

    [DllImport("wininet.dll", SetLastError = true)]
    public static extern IntPtr InternetConnect(IntPtr hInternet, string lpszServerName, Int32 nServerPort, string lpszUsername, string lpszPassword, Int32 dwService, UInt32 dwFlags, IntPtr dwContext);

    [DllImport("wininet.dll", SetLastError = true)]
    public static extern bool InternetCloseHandle(IntPtr hInternet);

    [DllImport("wininet.dll", SetLastError = true)]
    public static extern bool FtpSetCurrentDirectory(IntPtr hConnect, string lpszDirectory);

    [DllImport("wininet.dll", SetLastError = true)]
    public static extern bool FtpGetCurrentDirectory(IntPtr hConnect, StringBuilder lpszCurrentDirectory, ref Int32 lpdwCurrentDirectory);

    [DllImport("wininet.dll", SetLastError = true)]
    public static extern IntPtr FtpFindFirstFile(IntPtr hConnect, IntPtr lpszSearchFile, [Out][In] WIN32_FIND_DATA lpFindFileData, UInt32 dwFlags, IntPtr dwContext);

    [DllImport("wininet.dll", SetLastError = true)]
    public static extern bool InternetFindNextFile(IntPtr hFind, [Out][In] WIN32_FIND_DATA lpFindFileData);

    [DllImport("wininet.dll", SetLastError = true)]
    public static extern bool FtpGetFile(IntPtr hConnect, string lpszRemoteFile, string lpszNewFile, bool fFailIfExists, UInt32 dwFlagsAndAttributes, UInt32 dwFlags, IntPtr dwContext);

    [DllImport("wininet.dll", SetLastError = true)]
    public static extern bool FtpPutFile(IntPtr hConnect, string lpszLocalFile, string lpszNewRemoteFile, UInt32 dwFlags, IntPtr dwContext);

    [DllImport("wininet.dll", SetLastError = true)]
    public static extern IntPtr FtpOpenFile(IntPtr hConnect, string lpszFileName, UInt32 dwAccess, UInt32 dwFlags, IntPtr dwContext);

    [DllImport("wininet.dll", SetLastError = true)]
    public static extern UInt32 FtpGetFileSize(IntPtr hFile, ref UInt32 lpdwFileSizeHigh);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);
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
        var pFD = new WIN32_FIND_DATA();
        IntPtr hFind = FtpFindFirstFile(hFtpSess, IntPtr.Zero, pFD, INTERNET_FLAG_RELOAD, iContext);
        if (hFind == IntPtr.Zero) {
            if (Marshal.GetLastWin32Error() != ERROR_NO_MORE_FILES)
                throw new FtpUtilityException("Błąd przeglądania zasobu " + m_sHost + m_sRemoteDir);
            else
                goto noFilesFound;
        }

        if (GetFile(pFD)) {
            var size = FoundSize2Long(pFD);
            var last = FoundTime2DateTime(pFD.ftLastWriteTime);
            ret.Add(new FtpSyncFileModel() {
                Name = pFD.cFileName,
                Size = size,
                Modified = last,
                MD5 = (m_sLocalDir + pFD.cFileName).ComputeMD5()
            });
            FileTransferred.Publish(new FileTransferredEventArgs(eFtpDirection.Get, new FileInfo(pFD.cFileName)));
        }
        while (InternetFindNextFile(hFind, pFD) && m_Disp.InProgress)
            if (GetFile(pFD)) {
                var size = FoundSize2Long(pFD);
                var last = FoundTime2DateTime(pFD.ftLastWriteTime);
                ret.Add(new FtpSyncFileModel() {
                    Name = pFD.cFileName,
                    Size = size,
                    Modified = last,
                    MD5 = (m_sLocalDir + pFD.cFileName).ComputeMD5()
                });
                FileTransferred.Publish(new FileTransferredEventArgs(eFtpDirection.Get, new FileInfo(pFD.cFileName)));
            }

        if (m_Disp != null && !m_Disp.InProgress)
            TransferStatusNotification.Publish(new StatusEventArgs(eSeverityCode.Message, $"Pobieranie z serwera {m_sHost}{m_sRemoteDir} zostało przerwane przez użytkownika"));

        if (Marshal.GetLastWin32Error() != ERROR_NO_MORE_FILES)
            throw new FtpUtilityException("Błąd pobierania z zasobu " + m_sHost + m_sRemoteDir);

noFilesFound:
        InternetCloseHandle(hFind);
        Dispose();

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
                FileTransferred.Publish(new FileTransferredEventArgs(eFtpDirection.Put, fi));
            }
        }

        if (m_Disp != null && !m_Disp.InProgress)
            TransferStatusNotification.Publish(new StatusEventArgs(eSeverityCode.Message, $"Wstawianie na serwer {m_sHost}{m_sRemoteDir} zostało przerwane przez użytkownika"));

        Dispose();

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
            FileTransferred.Publish(new FileTransferredEventArgs(eFtpDirection.HotfolderPut, file));

        Dispose();

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
        iContext = IntPtr.Zero;

        hFtpConn = InternetOpen($"{FtpDiligentGlobals.EventLog} {FtpDiligentGlobals.Instance}", INTERNET_OPEN_TYPE_DIRECT, "", "", 0);
        if (hFtpConn == IntPtr.Zero) throw new FtpUtilityException("InternetOpen failed");

        hFtpSess = InternetConnect(hFtpConn, m_sHost, iPort, m_sUser, m_sPass, INTERNET_SERVICE_FTP, INTERNET_FLAG_PASSIVE, iContext);
        if (hFtpSess == IntPtr.Zero) throw new FtpUtilityException($"InternetConnect to {m_sHost} failed");

        if (!FtpSetCurrentDirectory(hFtpSess, m_sRemoteDir))
            throw new FtpUtilityException($"Remote directory {m_sHost}{m_sRemoteDir} does not exist");

        return true;
    }
    #endregion

    #region private methods
    /// <summary>
    /// Pobiera plik zmodyfikowany po dacie ostatniej synchronizacji endpointu
    /// </summary>
    /// <param name="pFD">struktura opisująca plik lub katalog</param>
    /// <returns>Czy doszło do pobrania pliku</returns>
    private bool GetFile(WIN32_FIND_DATA pFD)
    {
        if ((pFD.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) > 0)
            return false;
        long size = FoundSize2Long(pFD);
        if (size == 0)
            return false;

        DateTime lastWrite = FoundTime2DateTime(pFD.ftLastWriteTime);
        switch (m_SyncMode) {
            case eSyncFileMode.NewerThenRefreshDate:
                if (lastWrite < m_dtLastRefresh)
                    return false;
                break;
            case eSyncFileMode.UniqueDateAndSizeOnDisk:
                if (CheckLocalStorage(pFD.cFileName, size))
                    return false;
                break;
            case eSyncFileMode.UniqueDateAndSizeInDatabase:
                if (m_Disp.CheckDatabase(pFD.cFileName, size, lastWrite))
                    return false;
                break;
            case eSyncFileMode.AllFiles:
                break;
        }

        string localPath = m_sLocalDir + pFD.cFileName;
        if (!FtpGetFile(hFtpSess, pFD.cFileName, localPath, false, FILE_ATTRIBUTE_NORMAL, (uint)m_TransferMode, iContext)) {
            var dirsep = m_sRemoteDir.EndsWith('/')? string.Empty : "/";
            throw new FtpUtilityException($"Kopiowanie {m_sHost}{m_sRemoteDir}{dirsep}{pFD.cFileName} do {m_sLocalDir} nie powiodło się");
        }

        if (FtpDiligentGlobals.CheckTransferedStorage) {
            bool bStatus = CheckLocalStorage(pFD.cFileName, size);
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

        FtpPutFile(hFtpSess, pFI.FullName, pFI.Name, (uint)m_TransferMode, iContext);

        int iWin32Error = Marshal.GetLastWin32Error();
        if (iWin32Error > 0 && iWin32Error != 512 && iWin32Error != 12003)
            throw new FtpUtilityException($"Kopiowanie {pFI.FullName} do {m_sHost}{m_sRemoteDir} nie powiodło się", iWin32Error);

        if (FtpDiligentGlobals.CheckTransferedStorage)
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
        IntPtr hFile = FtpOpenFile(hFtpSess, sFileName, GENERIC_READ, 0, iContext);
        if (hFile == IntPtr.Zero)
            return false;
        UInt32 lpdwFileSizeHigh = 0;
        Int64 uiFileSize = (Int64)FtpGetFileSize(hFile, ref lpdwFileSizeHigh);
        InternetCloseHandle(hFile);
        uiFileSize += lpdwFileSizeHigh << 32;
        return uiFileSize == sLength;
    }

    /// <summary>
    /// Konwertuje daty. Istotne tylko w trybie x64
    /// </summary>
    /// <param name="ft">Wartość typu System.Runtime.InteropServices.ComTypes.FILETIME</param>
    /// <returns>Równoważna wartość typu DateTime</returns>
    DateTime FoundTime2DateTime(FILETIME ft) => DateTime.FromFileTime(Convert.ToInt64(ft.dwHighDateTime) << 32 + ft.dwLowDateTime);

    /// <summary>
    /// Konwertuje rozmiar pliku
    /// </summary>
    /// <param name="fd">Struktura z WIN32 API</param>
    /// <returns>Rozmiar pliku</returns>
    Int64 FoundSize2Long(WIN32_FIND_DATA fd) => (Convert.ToInt64(fd.nFileSizeHigh) << 32) + fd.nFileSizeLow;
    #endregion
}
