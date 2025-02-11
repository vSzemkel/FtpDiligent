// -----------------------------------------------------------------------
// <copyright file="StatusEvent.cs">
// <legal>Copyright (c) Marcin Buchwald, September 2024</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent.Events;

using Prism.Events;

/// <summary>
/// Informacje o zdarzeniu w systemie
/// </summary>
public record struct StatusEventArgs
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

public class StatusEvent : PubSubEvent<StatusEventArgs>
{
}
