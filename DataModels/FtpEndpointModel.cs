
// -----------------------------------------------------------------------
// <copyright file="FtpEndpointModel.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    using System;

    /// <summary>
    /// Endpoint FTP
    /// </summary>
    public struct FtpEndpointModel
    {
        public string host;
        public string uid;
        public string pwd;
        public string remDir;
        public string locDir;
        public int xx;                  // identyfikator endpointu
        public int insXX;               // numer instancji workera
        public eFtpDirection direction; // kierunek transferu (GET lub PUT)
        public eFtpTransferMode mode;   // ASCII lub BIN
        public DateTime lastSync;
        public DateTime nextSync;
    }
}
