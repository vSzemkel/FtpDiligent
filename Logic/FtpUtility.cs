
// -----------------------------------------------------------------------
// <copyright file="FtpUtility.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

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
    /// Umo¿liwia przegl¹danie zasobów serwera FTP i pobieranie plików
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
        /// <param name="dispatcher">Obiekt steruj¹cy w¹tkami</param>
        /// <param name="mode">Algorytm kwalifikacji plików do transferu</param>
        public FtpUtility(FtpEndpointModel endpoint, FtpDispatcher dispatcher, eSyncFileMode mode)
            : base(endpoint, dispatcher, mode) {
        }

        /// <summary>
        /// Konstruktor FtpUtility dla pojedynczych us³ug
        /// </summary>
        /// <param name="endpoint">Parametry serwera</param>
        /// <param name="window">G³ówne okno aplikacji</param>
        public FtpUtility(FtpEndpointModel endpoint, MainWindow window)
            : base(endpoint, window) { 
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
        /// £¹czy siê z endpointem i pobiera wszystkie pliki póŸniejsze ni¿ data ostatniego pobrania
        /// </summary>
        /// <param name="log">Informacja o skopiowanych plikach</param>
        /// <returns>Tablice nazw pobranych plików oraz ich rozmiarów</returns>
        public bool Download(ref FtpSyncModel log)
        {
            if (!CheckLocalDirectory() || !CheckDispatcher())
                return false;

            Connect();

            var lsFileNames = new List<string>();
            var llFileSizes = new List<long>();
            var ldFileDates = new List<DateTime>();
            var pFD = new WIN32_FIND_DATA();
            IntPtr hFind = FtpFindFirstFile(hFtpSess, IntPtr.Zero, pFD, INTERNET_FLAG_RELOAD, iContext);
            if (hFind == IntPtr.Zero) {
                if (Marshal.GetLastWin32Error() != ERROR_NO_MORE_FILES)
                    throw new FtpUtilityException("B³¹d przegl¹dania zasobu " + m_sHost + m_sRemoteDir);
                else
                    goto noFilesFound;
            }

            if (GetFile(pFD)) {
                var size = FoundSize2Long(pFD);
                var last = FoundTime2DateTime(pFD.ftLastWriteTime);
                lsFileNames.Add(pFD.cFileName);
                llFileSizes.Add(size);
                ldFileDates.Add(last);
                if (m_showError != null)
                    m_showError(eSeverityCode.FileInfo, $"1|{pFD.cFileName}|{size}|{last.ToBinary()}");
            }
            while (InternetFindNextFile(hFind, pFD) && m_Disp.InProgress)
                if (GetFile(pFD)) {
                    var size = FoundSize2Long(pFD);
                    var last = FoundTime2DateTime(pFD.ftLastWriteTime);
                    lsFileNames.Add(pFD.cFileName);
                    llFileSizes.Add(size);
                    ldFileDates.Add(last);
                    if (m_showError != null)
                        m_showError(eSeverityCode.FileInfo, $"1|{pFD.cFileName}|{size}|{last.ToBinary()}");
                }

            if (m_Disp != null && !m_Disp.InProgress && m_showError != null)
                m_showError(eSeverityCode.Message, $"{DateTime.Now:dd/MM/yyyy HH:mm} Pobieranie z serwera {m_sHost}{m_sRemoteDir} zosta³o przerwane przez u¿ytkownika");

            if (Marshal.GetLastWin32Error() != ERROR_NO_MORE_FILES)
                throw new FtpUtilityException("B³¹d pobierania z zasobu " + m_sHost + m_sRemoteDir);

noFilesFound:
            InternetCloseHandle(hFind);
            Dispose();

            log.fileNames = lsFileNames.ToArray();
            log.fileSizes = llFileSizes.ToArray();
            log.fileDates = ldFileDates.ToArray();

            return true;
        }

        /// <summary>
        /// £¹czy siê z endpointem i wstawia wszystkie pliki z lokalnego katalogu
        /// </summary>
        /// <param name="log">Informacja o skopiowanych plikach</param>
        /// <returns>Status powodzenia operacji</returns>
        public bool Upload(ref FtpSyncModel log)
        {
            if (!CheckLocalDirectory() || !CheckDispatcher())
                return false;

            Connect();

            var lsFileNames = new List<string>();
            var llFileSizes = new List<long>();
            var ldFileDates = new List<DateTime>();
            foreach (string sFileName in Directory.GetFiles(m_sLocalDir)) {
                var fi = new FileInfo(sFileName);
                if (m_Disp.InProgress && PutFile(fi)) {
                    lsFileNames.Add(fi.Name);
                    llFileSizes.Add(fi.Length);
                    ldFileDates.Add(fi.LastWriteTime);
                    if (m_showError != null)
                        m_showError(eSeverityCode.FileInfo, $"2|{fi.Name}|{fi.Length}|{fi.LastWriteTime.ToBinary()}");
                }
            }

            if (m_Disp != null && !m_Disp.InProgress && m_showError != null)
                m_showError(eSeverityCode.Message, $"{DateTime.Now:dd/MM/yyyy HH:mm} Wstawianie na serwer {m_sHost}{m_sRemoteDir} zosta³o przerwane przez u¿ytkownika");

            Dispose();

            log.fileNames = lsFileNames.ToArray();
            log.fileSizes = llFileSizes.ToArray();
            log.fileDates = ldFileDates.ToArray();

            return true;
        }

        /// <summary>
        /// £¹czy siê z endpointem i wstawia jeden pliki z lokalnego hot folderu
        /// </summary>
        /// <returns>Status powodzenia operacji</returns>
        public bool UploadHotFile(FileInfo file)
        {
            if (!CheckLocalDirectory())
                return false;

            Connect();

            bool status = PutFile(file);
            if (status)
                m_showError.Invoke(eSeverityCode.FileInfo, $"4|{file.Name}|{file.Length}|{file.LastWriteTime.ToBinary()}");

            Dispose();

            return status;
        }
        #endregion

        #region protected override methods
        /// <summary>
        /// Otwiera po³¹czenie z serwerem FTP, autoryzuje siê i nawi¹zuje sesjê
        /// </summary>
        /// <returns>True, a jeœli siê nie uda, rzuca wyj¹tek</returns>
        protected override bool Connect()
        {
            iContext = IntPtr.Zero;

            hFtpConn = InternetOpen($"{MainWindow.m_eventLog} {m_mainWnd.m_instance}", INTERNET_OPEN_TYPE_DIRECT, "", "", 0);
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
        /// <param name="pFD">struktura opisuj¹ca plik lub katalog</param>
        /// <returns>Czy dosz³o do pobrania pliku</returns>
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

            if (!FtpGetFile(hFtpSess, pFD.cFileName, m_sLocalDir + pFD.cFileName, false, FILE_ATTRIBUTE_NORMAL, (uint)m_TransferMode, iContext)) {
                var dirsep = m_sRemoteDir.EndsWith('/')? string.Empty : "/";
                throw new FtpUtilityException($"Kopiowanie {m_sHost}{m_sRemoteDir}{dirsep}{pFD.cFileName} do {m_sLocalDir} nie powiod³o siê");
            }

            if (m_mainWnd.m_checkTransferedStorage) {
                bool bStatus = CheckLocalStorage(pFD.cFileName, size);
                if (!bStatus && File.Exists(m_sLocalDir + pFD.cFileName))
                    File.Delete(m_sLocalDir + pFD.cFileName);
                return bStatus;
            }

            return true;
        }

        /// <summary>
        /// Wstawia plik zmodyfikowany po dacie ostatniej synchronizacji endpointu
        /// </summary>
        /// <param name="pFD">struktura opisuj¹ca plik lub katalog</param>
        /// <returns>Czy dosz³o do wstawienia pliku</returns>
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
                throw new FtpUtilityException($"Kopiowanie {pFI.FullName} do {m_sHost}{m_sRemoteDir} nie powiod³o siê", iWin32Error);

            if (m_mainWnd.m_checkTransferedStorage)
                return CheckRemoteStorage(pFI.Name, pFI.Length);

            return true;
        }

        /// <summary>
        /// Sprawdza, czy w zasobie zdalnym istnieje ju¿ plik o zadanej nazwie i rozmiarze
        /// </summary>
        /// <param name="sFileName">Nazwa liku</param>
        /// <param name="sLength">D³ugoœæ pliku</param>
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
        /// <param name="ft">Wartoœæ typu System.Runtime.InteropServices.ComTypes.FILETIME</param>
        /// <returns>Równowa¿na wartoœæ typu DateTime</returns>
        DateTime FoundTime2DateTime(FILETIME ft) => DateTime.FromFileTime(Convert.ToInt64(ft.dwHighDateTime) << 32 + ft.dwLowDateTime);

        /// <summary>
        /// Konwertuje rozmiar pliku
        /// </summary>
        /// <param name="fd">Struktura z WIN32 API</param>
        /// <returns>Rozmiar pliku</returns>
        Int64 FoundSize2Long(WIN32_FIND_DATA fd) => (Convert.ToInt64(fd.nFileSizeHigh) << 32) + fd.nFileSizeLow;
        #endregion
    }
}
