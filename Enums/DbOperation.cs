
// -----------------------------------------------------------------------
// <copyright file="DbOperation.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    /// <summary>
    /// Typ wyliczeniowy służący do określania trybu wywołań procedur modyfikujących dane w bazie
    /// </summary>
    public enum eDbOperation : byte
    {
        /// <summary>
        /// Wstawienie rekordu
        /// </summary>
        Insert = 0,
        /// <summary>
        /// Modyfikacja rekordu
        /// </summary>
        Update,
        /// <summary>
        /// Usunięcie rekordu
        /// </summary>
        Delete
    }
}
