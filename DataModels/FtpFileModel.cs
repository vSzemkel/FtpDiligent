
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
    /// <summary>
    /// Identyfikator instancji workera, uzywany tez do przekazywania rodzaju operacji
    /// </summary>
    public int Instance { get; set; }
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public DateTime FileDate { get; set; }
}
