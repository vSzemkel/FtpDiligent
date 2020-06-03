
// -----------------------------------------------------------------------
// <copyright file="FtpScheduleModel.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    using System;

    /// <summary>
    /// Harmonogram transferów FTP
    /// </summary>
    public struct FtpScheduleModel
    {
        public int xx;         // identyfikator schematu uruchomiania
        public int endXX;      // identyfikator endpointu
        public short stride;   // co ile minut ponowić
        public TimeSpan startSpan;
        public TimeSpan stopSpan;
        public DateTime nextSyncTime;
        public string name;
        public bool enabled;

        /// <summary>
        /// 32 bitowy hash pary [xx, nextSyncTime]
        /// </summary>
        public int Hash => (int)nextSyncTime.ToBinary() + (xx << 16);
    }
}
