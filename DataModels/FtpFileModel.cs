
// -----------------------------------------------------------------------
// <copyright file="FtpSyncModel.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;

/// <summary>
/// Dane transferowanego pliku
/// </summary>
public struct FtpFileModel
{
    public int Instance { get; set; } // identyfikator instancji workera
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public DateTime FileDate { get; set; }
}
