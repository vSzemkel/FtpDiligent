
// -----------------------------------------------------------------------
// <copyright file="TransferNotificationEventArgs.cs">
// <legal>Copyright (c) Marcin Buchwald, September 2024</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

/// <summary>
/// Informacje o zakończonym transferze pliku
/// </summary>
public record struct TransferNotificationEventArgs
(
    /// <summary>
    /// Podtyp powiadomienia
    /// </summary>
    eSeverityCode severity,

    /// <summary>
    /// Treść wiadomości
    /// </summary>
    string message
);
