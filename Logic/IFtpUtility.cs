
// -----------------------------------------------------------------------
// <copyright file="IFtpUtility.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    using System.IO;

    interface IFtpUtility
    {
        #region factory methods
        /// <summary>
        /// Konstruktor FtpUtility sterowanego przez <see>FtpDispatcher</see>
        /// </summary>
        /// <param name="endpoint">Parametry serwera</param>
        /// <param name="dispatcher">Obiekt sterujący wątkami</param>
        /// <param name="mode">Algorytm kwalifikacji plików do transferu</param>
        public static IFtpUtility Create(FtpEndpointModel endpoint, FtpDispatcher dispatcher, eSyncFileMode mode)
        {
            switch (endpoint.protocol) {
                case eFtpProtocol.FTP:
                    return new FtpUtility(endpoint, dispatcher, mode);
                case eFtpProtocol.SFTP:
                    return new SFtpUtility(endpoint, dispatcher, mode);
                case eFtpProtocol.FTPS:
                    return new FtpsUtility(endpoint, dispatcher, mode);
            }

            return null;
        }

        /// <summary>
        /// Konstruktor FtpUtility dla pojedynczych usług
        /// </summary>
        /// <param name="endpoint">Parametry serwera</param>
        /// <param name="window">Główne okno aplikacji</param>
        public static IFtpUtility Create(FtpEndpointModel endpoint, MainWindow window)
        {
            switch (endpoint.protocol) {
                case eFtpProtocol.FTP:
                    return new FtpUtility(endpoint, window);
                case eFtpProtocol.SFTP:
                    return new SFtpUtility(endpoint, window);
                case eFtpProtocol.FTPS:
                    return new FtpsUtility(endpoint, window);
            }

            return null;
        }
        #endregion

        FtpSyncFileModel[] Download();
        FtpSyncFileModel[] Upload();
        bool UploadHotFile(FileInfo file);
        bool CheckConnection(ref string sErrInfo);
        bool CheckLocalDirectory();
        string GetLocalDirectory();
    }
}
