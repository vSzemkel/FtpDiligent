
// -----------------------------------------------------------------------
// <copyright file="FtpErrorModel.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, maj 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;

/// <summary>
/// Dane transferowanego pliku
/// </summary>
public class FtpErrorModel
{
    public eSeverityCode Category { get; set; }
    public DateTime Time { get; }
    public string Message { get; set; }

    public FtpErrorModel()
    {
        Time = DateTime.Now;
    }
}
