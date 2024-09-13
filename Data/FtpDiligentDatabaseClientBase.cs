
// -----------------------------------------------------------------------
// <copyright file="FtpDiligentDatabaseClientBase.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;

namespace FtpDiligent;

public abstract class FtpDiligentDatabaseClientBase
{
    #region fields
    /// <summary>
    /// Opis połączenia do bazy danych
    /// </summary>
    public string m_connStr;

    /// <summary>
    /// Ostatnia wartość pobrana z sekwencji do natychmiastowego, jednowątkowego użycia
    /// </summary>
    protected int m_lastInsertedKey = 0;
    #endregion

    #region public
    /// <summary>
    /// Udostępnia identyfikator ostatnio utworzonego obiektu
    /// </summary>
    /// <returns>Wartość ostatnio wygenerowanego identyfikatora</returns>
    public int GetLastInsertedKey() => m_lastInsertedKey;

    /// <summary>
    /// Konwertuje kolekcję DataRow na bindowalną w WPF kolekcję
    /// </summary>
    /// <param name="rows">Kolekcja obiektów pobrana z bazy danych</param>
    /// <returns>Bindowalna w WPF kolekcja endpointów</returns>
    public ObservableCollection<FtpEndpoint> GetEndpointsCollection(IEnumerable<DataRow> rows) => new (rows.Select(dr => CreateFtpEndpoint(dr)));

    /// <summary>
    /// Konwertuje kolekcję DataRow na bindowalną w WPF kolekcję
    /// </summary>
    /// <param name="rows">Kolekcja obiektów pobrana z bazy danych</param>
    /// <returns>Bindowalna w WPF kolekcja endpointów</returns>
    public ObservableCollection<FtpSchedule> GetSchedulesCollection(IEnumerable<DataRow> rows) => new (rows.Select(dr => CreateFtpSchedule(dr)));
    #endregion

    #region protected
    /// <summary>
    /// Konwertuje wiersz z tabeli bazodanowej na konkretny typ
    /// </summary>
    /// <param name="row">Wiersz danych</param>
    protected abstract FtpEndpoint CreateFtpEndpoint(DataRow row);

    /// <summary>
    /// Konwertuje wiersz z tabeli bazodanowej na konkretny typ
    /// </summary>
    /// <param name="row">Wiersz danych</param>
    protected abstract FtpSchedule CreateFtpSchedule(DataRow row);
    #endregion
}
