
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
    using System.IO.Compression;
    using System.Linq;

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

        /// <summary>
        /// RSA primary key u¿ywany do komunikacji po SSH
        /// </summary>
        private string m_privateKey = "WPlhenpsaUdyYxfvq6fkKnk8jVAsqCPiErgOf9D+WINoG7Ej8aCzNVOWMx3OzF/55wkPu42Bm6ZPwMMhxVB2uwBVR348lyK6oAXxs+HumZuSkG5pwXo1Y4qLfFIZN0W/dpuCUy0BxY6uVc0F3yFf/fB8hHGBpLD+h7+rTX8gzUEPRfjm7kuXnIvhxnvSYZHM5ldD2xh33EJF4VsUTcYusVRr3Cf78GQfBHE9lC/kXYoWxOPGI0i6yP6kS4411WXfOo0xUFk928zJOXYDVcx90gwpV72mvV1KNqCtg+F6bEDc4KVvlR7YUFHiceB6JP1V3Qcx1p77F2rdEbWuOh8GHAa5roVtNjkwQfGYlTmOC6kuTSG/lXHbI0680VTreSvGNxca38smOmwi07wouZqvPAAm/bIeM4RQ7BFFLiuyvQwcQPUt9eGi3zuAn8qsu54tMLFieeFnYvRXHY34BnH8yG0mWuyIAMRCU80+dqQ1lYZca+RP29i6EPrKsRhXZJreA/3dqwEEoov/8MEni+xwMZfUTL0tpEsYqLts0cRKNoWcepQsEZHayq622z10WH2cbeQ9/H7bz0CLxKyy01li9ABAw2cqPwUUl6I5SYPLCKG3b/vtIWLpb4A0ijT0LYdEpzb9YqJ7P1M1s+bB76TAPhsRX01K9nuflqlpsrbFzbUnu9lRqGHtKCDJ32F6lPbi/ZSIPV8DPVUwZyQ3MOLu+SD4d/6fc6M5ZcZUF8TsdqCtXYT7x1BtkBfiAaBIVFCnvV+3mKVMtwLQdjoz/mFvg3MVE+fMZibf+hAsK0EPkeW3Ah+A3QwIeQlPBtNyBlnvcBkyy/sxGoZOOcvjuYMQ0t74PICeUISYCo2u8srvlB9s9Quoo2fNaOCLEqmIytlNvrlWz50nv3WS3v0WW9JAHt0taFE3HPNi6YehInIeAEuSjKH3bPBMoncIvZ8PPkWLBbIhN/kPdzNOn/beJDMgLYT7x5N/MN4TjV9DAYqkBly9PaqGOPuSQQiPxdBJ+dwJo+zaCxZbuTufr0XViueKaHVbyET4itp5miLxIa7WMP3Mq1RDbe6MUORz/mRXvC+puCnQnNRvrdMQrKN27miVqgHcxG7BbdeTfm20c4MVCRiu9AP3yl0kAW0SZ41iuZ4uxlH2RLUaguAXuVwP6hE5qrpyhm2+YWxgKrszF+LsNiY7qYcsDJoZV4tso7CbbqKy3BCKvYmADk53kR3Z8xU2btC0/3I3L6YobyT9y98UdPuc2dzjk6Z5SnASfigs3Rv3dxeqoqAlzOd2a/S5g3QfMRYLHXg4g0RZjOPCnxDaklXtPAlJ9nBQAGC2S3DybZOx4W1UipiZz+YLUFyhlKTDzf930gYIt58gMwS2c32LhrjyUxf16jum2fdiZzAX5+9MqmGh5EIPN6XedQz0EU9H9oe5AXkPDa3/0Ik5R6j8WwavTJRRPOnBMnfqSrFj84VcWilKj/ID1lRCeuWq57s3G3eipR7lgPAQScGxmktdoyL3D2dVJkIH11nfNj0uURBi4mgE202hOumiswQX4AfolB8M+10qQK8v1IQFbZuWYJJvju1HQD+i7Np2zvntl2KHjFj1Q7TAqo13gkPC/zqb/1oTyfCvEJkkrHC4IApJG9ceTrQQu25HXJcKQysCaQP2g651pd9I1B9X46UvkMLKC5pTwCuFbi23uqQSaPNPDl9jTNl+E4EBtLaXdp41waHRhpYPiUOywG569XxsaQ==";
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

            if (m_mainWnd.m_checkTransferedStorage) {
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
                if (m_mainWnd.m_checkTransferedStorage)
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

        /// <summary>
        /// Stream z kluczem prywatnym
        /// </summary>
        /// <returns>Otwarty stream, który costanie zamkniêty w konstruktorze SftpClient</returns>
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
            GZipStream ds = new GZipStream(compressed, CompressionLevel.Optimal);
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
}
