
// -----------------------------------------------------------------------
// <copyright file="SeverityCode.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    /// <summary>
    /// Rodzaje komunikatów diagnostycznych
    /// </summary>
    public enum eSeverityCode : byte
    {
        NextSync,
        FileInfo,
        Error,
        TransferError,
        Warning,
        Message,
    }
}
