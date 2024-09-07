﻿
// -----------------------------------------------------------------------
// <copyright file="FileTransferredEventArgs.cs">
// <legal>Copyright (c) Marcin Buchwald, September 2024</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

using System.IO;

namespace FtpDiligent;


/// <summary>
/// Informacje o zakończonym transferze pliku
/// </summary>
public record struct FileTransferredEventArgs
(
    /// <summary>
    /// Podtyp powiadomienia
    /// </summary>
    eSeverityCode severity,

    /// <summary>
    /// Rodzaj transferu
    /// </summary>
    eFtpDirection direction,

    /// <summary>
    /// Dane pliku
    /// </summary>
    FileInfo file,

    /// <summary>
    /// Treść wiadomości
    /// </summary>
    string message
);