
// -----------------------------------------------------------------------
// <copyright file="IFtpDiligentDatabaseClient.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    using System;
    using System.Collections.ObjectModel;
    using System.Data;
    using System.Threading.Tasks;

    public interface IFtpDiligentDatabaseClient
    {
        #region public STA
        /// <summary>
        /// Inicjalizuje instancję dla konkretnego hosta
        /// Identyfikator instancji zapamiętuje w <see cref="m_lastInsertedKey"/>
        /// </summary>
        /// <param name="hostname">Nazwa hosta</param>
        /// <returns></returns>
        (int, string) InitInstance(string hostname) => throw new NotImplementedException();

        /// <summary>
        /// Pobiera endpointy dla bieżącej instancji
        /// </summary>
        /// <param name="instance">Identyfikator instancji</param>
        /// <returns>Tabela z harmonogramem lub komunikat o błędzie</returns>
        (DataTable, string) GetEndpoints(int instance) => throw new NotImplementedException();

        /// <summary>
        /// Konwertuje obiekt typu DataTable na bindowalną w WPF kolekcję
        /// </summary>
        /// <param name="tab">Tabela z endpointami</param>
        /// <returns>Bindowalna w WPF kolekcja endpointów</returns>
        ObservableCollection<FtpEndpoint> GetEndpointsCollection(DataTable tab) => throw new NotImplementedException();

        /// <summary>
        /// Pobiera bieżący harmonogram dla wskazanej instancji FtpGetWorkera
        /// </summary>
        /// <param name="endpoint">Identyfikator endpointu FTP skonfigurowanego dla tej instancji</param>
        /// <returns>Tabela z harmonogramem lub komunikat o błędzie</returns>
        (DataTable, string) GetSchedules(int endpoint) => throw new NotImplementedException();

        /// <summary>
        /// Konwertuje obiekt typu DataTable na bindowalną w WPF kolekcję
        /// </summary>
        /// <param name="tab">Tabela z endpointami</param>
        /// <returns>Bindowalna w WPF kolekcja endpointów</returns>
        ObservableCollection<FtpSchedule> GetSchedulesCollection(DataTable tab) => throw new NotImplementedException();

        /// <summary>
        /// Tworzenie, modyfikacja, usunięcie endpointu FTP
        /// </summary>
        /// <param name="endpoint">Definicja endpointu</param>
        /// <param name="mode">Rodzaj operacji</param>
        /// <returns>Komunikat o ewentualnym błędzie</returns>
        string ModifyEndpoint(FtpEndpointModel endpoint, eDbOperation mode) => throw new NotImplementedException();

        /// <summary>
        /// Tworzenie, modyfikacja, usunięcie harmonogramu transferów FTP
        /// </summary>
        /// <param name="schedule">Definicja harmonogramu</param>
        /// <param name="mode">Rodzaj operacji</param>
        /// <returns>Komunikat o ewentualnym błędzie</returns>
        string ModifySchedule(FtpScheduleModel schedule, eDbOperation mode) => throw new NotImplementedException();
        #endregion

        #region public MTA
        /// <summary>
        /// Udostępnia identyfikator ostatnio utworzonego obiektu
        /// </summary>
        /// <returns>Wartość ostatnio wygenerowanego identyfikatora</returns>
        public int GetLastInsertedKey();

        /// <summary>
        /// Pobiera informację o najbliższym zadaniu do wykonania
        /// </summary>
        /// <param name="instance">Identyfikator instancji workera</param>
        /// <returns>
        /// Informacje o zadaniu, napis "0", gdy nic nie zaplanowano
        /// lub komunikat o błędzie z bazy danych
        /// </returns>
        (FtpScheduleModel, string) GetNextSync(int instance) => throw new NotImplementedException();

        /// <summary>
        /// Pobiera informację o parametrach serwera do bieżącego zadania do wykonania
        /// </summary>
        /// <param name="schedule">Identyfikator pozycji harmonogramu</param>
        /// <returns>
        /// Informacje o endpoincie, napis "0", gdy nie ma endpointu dla harmonogramu
        /// lub komunikat o błędzie z bazy danych
        /// </returns>
        Task<(FtpEndpointModel, string)> SelectEndpoint(int schedule) => throw new NotImplementedException();

        /// <summary>
        /// Loguje wywołanie synchronizacji, która nie wykonała żadnych transferów
        /// </summary>
        /// <param name="sync">Informacja o uruchomieniu workera i skopiowanych plikach</param>
        /// <returns>Komunikat o ewentualnym błędzie</returns>
        string LogActivation(FtpSyncModel sync) => throw new NotImplementedException();

        /// <summary>
        /// Loguje wywołanie synchronizacji, która wykonała transfery plików
        /// </summary>
        /// <param name="sync">Informacja o uruchomieniu workera i skopiowanych plikach</param>
        /// <returns>Komunikat o ewentualnym błędzie</returns>
        string LogSync(FtpSyncModel sync) => throw new NotImplementedException();

        /// <summary>
        /// Sprawdza, czy z danej instancji FtpGetWorkera pobrano już dany plik
        /// </summary>
        /// <param name="file">Dane pliku</param>
        /// <returns>Komunikat o ewentualnym błędzie</returns>
        (bool,string) VerifyFile(FtpFileModel file) => throw new NotImplementedException();

        /// <summary>
        /// Wykonuje zapytanie w bazie danych
        /// </summary>
        /// <param name="cmd">Polecenie zapytania</param>
        /// <returns>Tabela z wynikiem lub komunikat o błędzie</returns>
        (DataTable, string) ExecuteReader(IDbCommand cmd) => throw new NotImplementedException();
        (DataTable, string) ExecuteReaderAsync(IDbCommand cmd) => throw new NotImplementedException();

        /// <summary>
        /// Wykonuje zapytanie w bazie danych i zapamiętuje w zmiennej 
        /// statycznej wartość parametru OUT, pobraną z sekwencji przy insercie
        /// </summary>
        /// <param name="cmd">Polecenie zapytania</param>
        /// <param name="index">Numer porządkowy parametru typu OUT</param>
        /// <returns>Komunikat o ewentualnym błędzie</returns>
        string ExecuteNonQueryStoreKey(IDbCommand cmd, int index) => throw new NotImplementedException();
        string ExecuteNonQueryStoreKeyAsync(IDbCommand cmd, int index) => throw new NotImplementedException();

        /// <summary>
        /// Wykonuje zapytanie w bazie danych
        /// </summary>
        /// <param name="cmd">Polecenie zapytania</param>
        /// <returns>Komunikat o ewentualnym błędzie</returns>
        string ExecuteNonQuery(IDbCommand cmd) => throw new NotImplementedException();
        string ExecuteNonQueryAsync(IDbCommand cmd) => throw new NotImplementedException();

        /// <summary>
        /// Wykonuje zapytanie w bazie danych zwracające pojedynczą wartość typu T
        /// </summary>
        /// <param name="cmd">Polecenie zapytania</param>
        /// <returns>Wartość typu T pobraną z bazy lub komunikat o ewentualnym błędzie</returns>
        (T, string) ExecuteScalar<T>(IDbCommand cmd) => throw new NotImplementedException();
        (T, string) ExecuteScalarAsync<T>(IDbCommand cmd) => throw new NotImplementedException();
        #endregion
    }
}
