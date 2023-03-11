
// -----------------------------------------------------------------------
// <copyright file="SyncFileMode.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

/// <summary>
/// Tryb kwalifikacji pliku do pobrania
/// </summary>
public enum eSyncFileMode : byte
{
    NewerThenRefreshDate = 0,
    UniqueDateAndSizeOnDisk,
    UniqueDateAndSizeInDatabase,
    UniqueMD5Checksum,
    AllFiles
}
