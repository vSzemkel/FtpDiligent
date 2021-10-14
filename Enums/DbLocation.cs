
// -----------------------------------------------------------------------
// <copyright file="DbLocation.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    /// <summary>
    /// Typ wyliczeniowy służący do określania charakteru lokalizacji bazy danych
    /// </summary>
    struct eDbLocation
    {
        /// <summary>
        /// Lokalna dla instalacji baza developerska
        /// </summary>
        public static readonly string Local = "LOCAL";

        /// <summary>
        /// Baza danych w firmowej serwerowni
        /// </summary>
        public static readonly string OnPrem = "ONPREM";

        /// <summary>
        /// Baza danych w chmurze publicznej
        /// </summary>
        public static readonly string Cloud = "AZURE";
    }
}
