
// -----------------------------------------------------------------------
// <copyright file="FtpDispatcherGlobals.cs" company="Private Project">
// <legal>Copyright (c) Marcin Buchwald, marzec 2023</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;
using Prism.Ioc;

public static class FtpDispatcherGlobals
{
    /// <summary>
    /// Identyfikator instancji workera
    /// </summary>
    public static int Instance;

    /// <summary>
    /// Algotytm synchronizacji
    /// </summary>
    public static eSyncFileMode SyncMode;

    /// <summary>
    /// Czy po transferowniu pliku zweryfikowaæ jego rozmiar
    /// </summary>
    public static bool CheckTransferedStorage;

    /// <summary>
    /// Co ile sekund sprawdzamy, czy pliki w hotfolderze s¹ w pe³ni zapisane
    /// </summary>
    public static int HotfolderInterval;

    /// <summary>
    /// Poziom logowania komunikatów
    /// </summary>
    public static eSeverityCode TraceLevel;

    /// <summary>
    /// Nazwa aplikacyjnego EventLogu
    /// </summary>
    public static readonly string EventLog = "FtpDiligent";

    /// <summary>
    /// Umo¿liwia programowe uruchomienie transferu danych
    /// </summary>
    public static Action StartProcessing;

    /// <summary>
    /// Interfejs do kontenera DI
    /// </summary>
    public static IContainerProvider IoC;
}
