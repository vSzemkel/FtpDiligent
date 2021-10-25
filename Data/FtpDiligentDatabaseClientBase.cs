
// -----------------------------------------------------------------------
// <copyright file="FtpDiligentDatabaseClientBase.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    public class FtpDiligentDatabaseClientBase
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
        #endregion
    }
}
