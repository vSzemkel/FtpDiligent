
// -----------------------------------------------------------------------
// <copyright file="FtpProtocol.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, maj 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

/// <summary>
/// Protokol FTP uzywany do transportu
/// </summary>
public enum eFtpProtocol : byte
{
    FTP  = 1,
    FTPS = 2,
    SFTP = 3
}
