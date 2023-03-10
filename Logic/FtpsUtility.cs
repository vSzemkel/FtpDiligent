
// -----------------------------------------------------------------------
// <copyright file="FtpsUtility.cs" company="Agora SA">
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

    using FluentFTP;

    /// <summary>
    /// Umo¿liwia przegl¹danie zasobów serwera FTP i pobieranie plików
    /// protoko³em FTP poprzez kana³ szyfrowany na poziomie TSL
    /// </summary>
    public sealed class FtpsUtility : FtpUtilityBase, IFtpUtility
    {
        #region fields
        /// <summary>
        /// Rozmiar buforu u¿ywanego przy kopiowaniu plików
        /// </summary>
        private const int m_bufferSize = 1 << 12;

        /// <summary>
        /// Klient us³ugi FTPS z biblioteki FluentFTP
        /// </summary>
        private FtpClient m_ftpsClient;

        /// <summary>
        /// Czy u¿yæ po³¹czenia szyfrowanego
        /// </summary>
        private bool m_secure;
        #endregion

        #region constructor/destructor
        /// <summary>
        /// Konstruktor FtpUtility sterowanego przez <see>FtpDispatcher</see>
        /// </summary>
        /// <param name="endpoint">Parametry serwera</param>
        /// <param name="dispatcher">Obiekt steruj¹cy w¹tkami</param>
        /// <param name="mode">Algorytm kwalifikacji plików do transferu</param>
        /// <param name="secure">Czy u¿yæ po³¹czenia szyfrowanego</param>
        public FtpsUtility(FtpEndpointModel endpoint, FtpDispatcher dispatcher, eSyncFileMode mode, bool secure)
            : base(endpoint, dispatcher, mode) {
            m_secure = secure;
        }

        /// <summary>
        /// Konstruktor FtpUtility dla pojedynczych us³ug
        /// </summary>
        /// <param name="endpoint">Parametry serwera</param>
        /// <param name="window">G³ówne okno aplikacji</param>
        public FtpsUtility(FtpEndpointModel endpoint, MainWindow window, bool secure)
            : base(endpoint, window) {
            m_secure = secure;
        }

        /// <summary>
        /// Destruktor zwalniaj¹cy zasoby
        /// </summary>
        ~FtpsUtility()
        {
            if (m_ftpsClient != null)
                m_ftpsClient.Dispose();
        }
        #endregion

        #region public methods
        /// <summary>
        /// £¹czy siê z endpointem i pobiera wszystkie pliki póŸniejsze ni¿ data ostatniego pobrania
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
                    if (m_showError != null)
                        m_showError(eSeverityCode.FileInfo, $"1|{f.Name}|{f.Size}|{f.Modified.ToBinary()}");
                }

            if (m_Disp != null && !m_Disp.InProgress && m_showError != null)
                m_showError(eSeverityCode.Message, $"Pobieranie z serwera {m_sHost}{m_sRemoteDir} zosta³o przerwane przez u¿ytkownika");

            m_ftpsClient.Disconnect();

            return ret.ToArray();
        }

        /// <summary>
        /// £¹czy siê z endpointem i wstawia wszystkie pliki z lokalnego katalogu
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
                    if (m_showError != null)
                        m_showError(eSeverityCode.FileInfo, $"2|{fi.Name}|{fi.Length}|{fi.LastWriteTime.ToBinary()}");
                }
            }

            if (m_Disp != null && !m_Disp.InProgress && m_showError != null)
                m_showError(eSeverityCode.Message, $"Wstawianie na serwer {m_sHost}{m_sRemoteDir} zosta³o przerwane przez u¿ytkownika");

            m_ftpsClient.Disconnect();

            return ret.ToArray();
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

            m_ftpsClient.Disconnect();

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
            m_ftpsClient = new FtpClient(m_sHost, m_sUser, m_sPass);
            m_ftpsClient.Config.DataConnectionType = FtpDataConnectionType.PASV;
            m_ftpsClient.Config.EncryptionMode = m_secure ? FtpEncryptionMode.Explicit : FtpEncryptionMode.None;
            m_ftpsClient.Config.DataConnectionEncryption = true;
            m_ftpsClient.Config.ValidateAnyCertificate = true;

            var transferMode = m_TransferMode == eFtpTransferMode.Binary 
                    ? FtpDataType.Binary : FtpDataType.ASCII;
            m_ftpsClient.Config.DownloadDataType = transferMode;
            m_ftpsClient.Config.UploadDataType = transferMode;

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
        /// <param name="file">struktura opisuj¹ca plik lub katalog</param>
        /// <returns>Czy dosz³o do pobrania pliku</returns>
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
                throw new FtpUtilityException($"Kopiowanie {m_sHost}{m_sRemoteDir}{dirsep}{file.Name} do {m_sLocalDir} nie powiod³o siê. {ex.Message}");
            }

            if (m_mainWnd.m_checkTransferedStorage) {
                bool bStatus = CheckLocalStorage(file.Name, file.Size);
                if (!bStatus && File.Exists(localPath))
                    File.Delete(localPath);
                return bStatus;
            }

            if (m_Disp != null)
                m_Disp.m_filesTransfered++;

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
                m_ftpsClient.UploadStream(stream, remoteFilename, FtpRemoteExists.Overwrite);
                if (m_mainWnd.m_checkTransferedStorage)
                    return CheckRemoteStorage(remoteFilename, pFI.Length);
            } catch (Exception ex) {
                throw new FtpUtilityException($"Kopiowanie {pFI.FullName} do {m_sHost}{m_sRemoteDir} nie powiod³o siê. {ex.Message}");
            }

            if (m_Disp != null)
                m_Disp.m_filesTransfered++;

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
            return m_ftpsClient.GetFileSize(remoteName) == length;
        }
        #endregion
    }
}
