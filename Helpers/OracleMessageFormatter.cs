
// -----------------------------------------------------------------------
// <copyright file="FtpScheduleModel.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;
using System.Xml;

/// <summary>
/// Klasa udostępnia metody formatujące komunikaty przekazywane z bazy Oracle
/// </summary>
public class OracleMessageFormatter
{
    /// <summary>
    /// Zwraca tekst komunikatu ostatniego wyjątku z pominięciem numeru błędu
    /// </summary>
    /// <param name="msg">Napis postaci SQLERRM do analizy</param>
    /// <returns></returns>
    public static string FirstLine(string msg)
    {
        int p = msg.IndexOf("ORA", 11);
        return p == -1 ? msg : msg.Substring(11, p - 12);
    }

    /// <summary>
    /// Tworzy komunikat w formacie XML.
    /// </summary>
    /// <param name="oex">Wyjątek</param>
    /// <returns></returns>
    public static XmlDocument GetXmlErrMsg(Exception oex)
    {
        var xDoc = new XmlDocument();
        xDoc.LoadXml(String.Format("<Error Source=\"{0}\" Message=\"{1}\" />", oex.Source, FirstLine(oex.Message)));
        return xDoc;
    }
}
