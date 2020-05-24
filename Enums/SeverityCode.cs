
// -----------------------------------------------------------------------
// <copyright file="SeverityCode.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Rodzaje komunikatów diagnostycznych
    /// </summary>
    [Flags]
    public enum eSeverityCode : byte
    {
        NextSync      = 0,                              // always on
        Error         = EventLogEntryType.Error,        // 1
        Warning       = EventLogEntryType.Warning,      // 2
        Message       = EventLogEntryType.Information,  // 4
        FileInfo      = EventLogEntryType.SuccessAudit, // 8
        TransferError = 16,
    }
}
