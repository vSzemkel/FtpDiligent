
// -----------------------------------------------------------------------
// <copyright file="FtpSyncModel.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;

public struct FtpSyncFileModel
{
    public string Name { get; set; }
    public long Size { get; set; }
    public DateTime Modified { get; set; }
    public byte[] MD5 { get; set; }
}

/// <summary>
/// Wynik operacji synchronizacji plików FTP
/// </summary>
public struct FtpSyncModel
{
    public int xx; // identyfikator schematu uruchomiania
    public DateTime syncTime; // na kiedy planowano to uruchomienie
    public eFtpDirection direction; // kierunek transferu (GET lub PUT)
    public FtpSyncFileModel[] files; // lista przetransferowanych plików
}
