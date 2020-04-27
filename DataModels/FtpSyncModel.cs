
// -----------------------------------------------------------------------
// <copyright file="FtpSyncModel.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    using System;

    /// <summary>
    /// Wynik operacji synchronizacji plików FTP
    /// </summary>
    public struct FtpSyncModel
    {
        public int xx; // identyfikator schematu uruchomiania
        public DateTime syncTime; // na kiedy planowano to uruchomienie
        public string[] fileNames;
        public long[] fileSizes;
        public DateTime[] fileDates;
        public eFtpDirection direction; // kierunek transferu (GET lub PUT)
    }
}
