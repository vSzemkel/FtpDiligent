
// -----------------------------------------------------------------------
// <copyright file="FtpDirection.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, maj 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    /// <summary>
    /// Protokol FTP uzywany do transportu
    /// </summary>
    public enum eFtpProtocol : byte
    {
        FTP  = 1,
        SFTP = 2,
        FTPS = 3
    }
}
