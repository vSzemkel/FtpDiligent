
// -----------------------------------------------------------------------
// <copyright file="FtpDispatcherGlobals.cs">
// <legal>Copyright (c) Marcin Buchwald, September 2024</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------
namespace FtpDiligent;

using System;

using Prism.Events;

public static class FtpDiligentGlobals
{
    /// <summary>
    /// Identyfikator instancji workera
    /// </summary>
    public static int Instance;

    /// <summary>
    /// Algorytm synchronizacji
    /// </summary>
    public static eSyncFileMode SyncMode;

    /// <summary>
    /// Czy po transferowniu pliku zweryfikować jego rozmiar
    /// </summary>
    public static bool CheckTransferedStorage;

    /// <summary>
    /// Co ile sekund sprawdzamy, czy pliki w hotfolderze są w pełni zapisane
    /// </summary>
    public static int HotfolderInterval;

    /// <summary>
    /// Poziom logowania komunikatów
    /// </summary>
    public static eSeverityCode TraceLevel;

    /// <summary>
    /// Nazwa aplikacyjnego EventLogu
    /// </summary>
    public static readonly string EventLog = typeof(App).Namespace;

    /// <summary>
    /// Umożliwia programowe uruchomienie transferu danych
    /// </summary>
    public static Action StartProcessing;

    /// <summary>
    /// Wysyła mailem powiadomienia o błędach
    /// </summary>
    public static SendEmails Mailer;

    /// <summary>
    /// Mechanizm do przekazywania powiadomień
    /// </summary>
    public static IEventAggregator EventAggregator;
}
