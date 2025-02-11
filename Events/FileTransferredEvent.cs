// -----------------------------------------------------------------------
// <copyright file="FileTransferred.cs">
// <legal>Copyright (c) Marcin Buchwald, September 2024</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent.Events;

using System.IO;

using Prism.Events;

/// <summary>
/// Podstawowe dane pliku
/// </summary>
public record struct FileTransferredEventArgs
(
    /// <summary>
    /// Rodzaj transferu
    /// </summary>
    eFtpDirection direction,

    /// <summary>
    /// Dane pliku
    /// </summary>
    FileInfo file
);

/// <summary>
/// Informacje o zakończonym transferze pliku
/// </summary>
public class FileTransferredEvent : PubSubEvent<FileTransferredEventArgs>
{
}
