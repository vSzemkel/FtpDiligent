
// -----------------------------------------------------------------------
// <copyright file="MD5ForPath.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, pażdziernik 2021</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
    using System.IO;
    using System.Security.Cryptography;

    /// <summary>
    /// MD5 file hash generator
    /// </summary>
    public static class MD5ForPath
    {
        /// <summary>
        /// Checksum static generator
        /// </summary>
        static private MD5 generator = MD5.Create();

        /// <summary>
        /// Zwraca tekst komunikatu ostatniego wyjątku z pominięciem numeru błędu
        /// </summary>
        /// <param name="msg">Napis postaci SQLERRM do analizy</param>
        /// <returns></returns>
        public static byte[] ComputeMD5(this string path)
        {
            try {
                using (var stream = File.OpenRead(path)) {
                    return generator.ComputeHash(stream);
                }
            } catch {
                return null;
            }
        }
    }
}
