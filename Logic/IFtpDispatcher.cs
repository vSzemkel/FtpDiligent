
// -----------------------------------------------------------------------
// <copyright file="IFtpDispatcher.cs" company="Private Project">
// <legal>Copyright (c) Marcin Buchwald, marzec 2023</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;

public interface IFtpDispatcher
{
    /// <summary>
    /// Inicjuje w wątku z puli pętlą przetwarzania żądań pobranie plików z endpointów ftp
    /// </summary>
    void Start();

    /// <summary>
    /// Inicjuje w wątku z puli pętlą przetwarzania żądań
    /// niezwłoczne pobranie plików z konkretnego endpointu Ftp
    /// </summary>
    /// <param name="endpoint">Endpoint, dla którego symulujemy wywołanie z harmonogramu</param>
    public void StartNow(FtpEndpoint endpoint);

    /// <summary>
    /// Przerywa oczekujący wątek i pozwala na zakończenie pracy dispatchera
    /// </summary>
    public void Stop();

    /// <summary>
    /// Używana w trybie: UniqueDateAndSizeInDatabase. Sprawdza, czy z danej instancji FtpGetWorkera pobrano ju� dany plik
    /// </summary>
    /// <param name="sFileName">Nazwa liku</param>
    /// <param name="lLength">Długość pliku</param>
    /// <param name="dtDate">Data ostatniej modyfikacji pliku</param>
    /// <returns>Czy z danej instancji FtpGetWorkera pobrano już dany plik</returns>
    public bool CheckDatabase(string sFileName, long lLength, DateTime dtDate);

    /// <summary>
    /// Udostępnia liczbę przesłanych plików
    /// </summary>
    public int GetNumberOfFilesTransferred();

    /// <summary>
    /// Zlicza przesłane pliki
    /// </summary>
    public void NotifyFileTransfer();
}
