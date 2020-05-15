
// -----------------------------------------------------------------------
// <copyright file="SFtpUtility.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, maj 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;

    using Renci.SshNet;
    using Renci.SshNet.Common;
    using Renci.SshNet.Sftp;

    /// <summary>
    /// Umo¿liwia przegl¹danie zasobów serwera FTP i pobieranie plików
    /// poprzez transport oparty na protokole SSH (port 22)
    /// </summary>
    public sealed class SFtpUtility : FtpUtilityBase, IFtpUtility
    {
        #region fields
        /// <summary>
        /// Rozmiar buforu u¿ywanego przy kopiowaniu plików
        /// </summary>
        private const int m_bufferSize = 1 << 12;

        /// <summary>
        /// Klient us³ugi SFTP z biblioteki Renci.SshNet
        /// </summary>
        private SftpClient m_sftpClient;
        #endregion

        #region constructor/destructor
        /// <summary>
        /// Konstruktor FtpUtility sterowanego przez <see>FtpDispatcher</see>
        /// </summary>
        /// <param name="endpoint">Parametry serwera</param>
        /// <param name="dispatcher">Obiekt steruj¹cy w¹tkami</param>
        /// <param name="mode">Algorytm kwalifikacji plików do transferu</param>
        public SFtpUtility(FtpEndpointModel endpoint, FtpDispatcher dispatcher, eSyncFileMode mode)
            : base(endpoint, dispatcher, mode) {
        }

        /// <summary>
        /// Konstruktor FtpUtility dla pojedynczych us³ug
        /// </summary>
        /// <param name="endpoint">Parametry serwera</param>
        /// <param name="window">G³ówne okno aplikacji</param>
        public SFtpUtility(FtpEndpointModel endpoint, MainWindow window)
            : base(endpoint, window) { 
        }

        /// <summary>
        /// Destruktor zwalniaj¹cy zasoby
        /// </summary>
        ~SFtpUtility()
        {
            if (m_sftpClient != null)
                m_sftpClient.Dispose();
        }
        #endregion

        #region public methods
        /// <summary>
        /// £¹czy siê z endpointem i pobiera wszystkie pliki póŸniejsze ni¿ data ostatniego pobrania
        /// </summary>
        /// <param name="log">Informacja o skopiowanych plikach</param>
        /// <returns>Tablice nazw pobranych plików oraz ich rozmiarów</returns>
        public bool Download(ref FtpSyncModel log)
        {
            if (m_Disp == null && m_SyncMode == eSyncFileMode.UniqueDateAndSizeInDatabase) {
                string sMsg = "Pobieranie plików w tym trybie wymaga dispatchera";
                if (m_showError != null) {
                    m_showError(eSeverityCode.Error, sMsg);
                    return false;
                } else
                    throw new FtpUtilityException(sMsg);
            }

            if (!CheckLocalDirectory())
                return false;

            Connect();

            var lsFileNames = new List<string>();
            var llFileSizes = new List<long>();
            var ldFileDates = new List<DateTime>();
            var files = m_sftpClient.ListDirectory(m_sRemoteDir).Where(f => f.IsRegularFile).ToArray();
            foreach (SftpFile f in files)
                if (GetFile(f)) {
                    lsFileNames.Add(f.Name);
                    llFileSizes.Add(f.Length);
                    ldFileDates.Add(f.LastWriteTime);
                    if (m_showError != null)
                        m_showError(eSeverityCode.FileInfo, $"1|{f.Name}|{f.Length}|{f.LastWriteTime.ToBinary()}");
                }

            if (m_Disp != null && !m_Disp.InProgress && m_showError != null)
                m_showError(eSeverityCode.Message, $"{DateTime.Now:dd/MM/yyyy HH:mm} Pobieranie z serwera {m_sHost}{m_sRemoteDir} zosta³o przerwane przez u¿ytkownika");

            m_sftpClient.Disconnect();

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
            if (!CheckLocalDirectory())
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

            m_sftpClient.Disconnect();

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

            m_sftpClient.Disconnect();

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
        /// <param name="file">struktura opisuj¹ca plik lub katalog</param>
        /// <returns>Czy dosz³o do pobrania pliku</returns>
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
            
            try {
                var stream = File.Create(m_sLocalDir + file.Name, m_bufferSize);
                m_sftpClient.DownloadFile(file.Name, stream);
            } catch(Exception ex) {
                var dirsep = m_sRemoteDir.EndsWith('/') ? string.Empty : "/";
                throw new FtpUtilityException($"Kopiowanie {m_sHost}{m_sRemoteDir}{dirsep}{file.Name} do {m_sLocalDir} nie powiod³o siê. {ex.Message}");
            }

            if (m_mainWnd.m_checkLocalStorage) {
                bool bStatus = CheckLocalStorage(file.Name, file.Length);
                if (!bStatus && File.Exists(m_sLocalDir + file.Name))
                    File.Delete(m_sLocalDir + file.Name);
                return bStatus;
            }

            return true;
        }

        /// <summary>
        /// Wstawia plik zmodyfikowany po dacie ostatniej synchronizacji endpointu
        /// </summary>
        /// <param name="pFI">struktura opisuj¹ca plik lub katalog</param>
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

            string remoteFilename = m_sRemoteDir;
            if (!remoteFilename.EndsWith('/')) remoteFilename += '/';
            remoteFilename += pFI.Name;

            try {
                var stream = File.OpenRead(pFI.FullName);
                m_sftpClient.UploadFile(stream, remoteFilename);
                if (m_mainWnd.m_checkLocalStorage)
                    return CheckRemoteStorage(remoteFilename, pFI.Length);
            } catch (Exception ex) {
                throw new FtpUtilityException($"Kopiowanie {pFI.FullName} do {m_sHost}{m_sRemoteDir} nie powiod³o siê. {ex.Message}");
            }

            return true;
        }

        /// <summary>
        /// Sprawdza, czy w zasobie zdalnym istnieje ju¿ plik o zadanej nazwie i rozmiarze
        /// </summary>
        /// <param name="remoteName">Nazwa liku</param>
        /// <param name="length">D³ugoœæ pliku</param>
        /// <returns>Czy istnieje plik o zadanych cechach w katalogu zdalnym</returns>
        private bool CheckRemoteStorage(string remoteName, long length)
        {
            SftpFile f = m_sftpClient.Get(remoteName);
            return f.Length == length;
        }

        #endregion
    }
}
