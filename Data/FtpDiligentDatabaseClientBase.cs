
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
        public static readonly string connStr = System.Configuration.ConfigurationManager.ConnectionStrings[eDbLocation.Cloud].ConnectionString;

        /// <summary>
        /// Ostatnia wartość pobrana z sekwencji do natychmiastowego, jednowątkowego użycia
        /// </summary>
        public static int m_lastInsertedKey = 0;
        #endregion
    }
}
