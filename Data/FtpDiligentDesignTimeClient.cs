
// -----------------------------------------------------------------------
// <copyright file="FtpDiligentDesignTimeClient.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;
using System.Collections.ObjectModel;
using System.Data;

class FtpDiligentDesignTimeClient
{
    #region public static
    /// <summary>
    /// Pobiera endpointy dla bieżącej instancji
    /// </summary>
    /// <param name="instance">Identyfikator instancji</param>
    /// <returns>Tabela z harmonogramem lub komunikat o błędzie</returns>
    public static ObservableCollection<FtpEndpoint> GetEndpoints(int instance)
    {
        var ret = new ObservableCollection<FtpEndpoint> {
            new (
                xx: 1,
                insXX: instance,
                host: "gwmanam5.agora.pl",
                uid: "ftp",
                pwd: "ftp",
                remDir: "/",
                locDir: @"C:\Tmp\Ftp",
                lastSync: DateTime.MinValue,
                prot: eFtpProtocol.FTP,
                dir: eFtpDirection.Put,
                mode: eFtpTransferMode.Ascii
            ) { NextSyncTime = DateTime.Now.AddMinutes(15) },
            new (
                xx: 2,
                insXX: instance,
                host: "ftp.ftpdiligentdesigntimeclient.pl",
                uid: "ftp",
                pwd: "pa$$word76",
                locDir:  @"C:\Tmp\Ftp",
                remDir: "/",
                lastSync: DateTime.MinValue,
                prot: eFtpProtocol.SFTP,
                dir: eFtpDirection.Get | eFtpDirection.Put,
                mode: eFtpTransferMode.Ascii
            ) { NextSyncTime = DateTime.Now.AddMinutes(1) },
        };

        return ret;
    }

    /// <summary>
    /// Pobiera bieżący harmonogram dla wskazanej instancji FtpGetWorkera
    /// </summary>
    /// <param name="endpoint">Identyfikator endpointu FTP skonfigurowanego dla tej instancji</param>
    /// <returns>Tabela z harmonogramem lub komunikat o błędzie</returns>
    public static ObservableCollection<FtpSchedule> GetSchedules(int endpoint)
    {
        var ret = new ObservableCollection<FtpSchedule>() {
            new FtpSchedule() {
                XX = 1,
                Endpoint = endpoint,
                Name = "DesignTimeSchedule 1",
                StartDow = DayOfWeek.Tuesday,
                StartTime = new TimeSpan(7, 30, 0),
                StopDow = DayOfWeek.Saturday,
                StopTime = new TimeSpan(21, 40, 0),
                Stride = 10,
                Enabled = true
            },
            new FtpSchedule() {
                XX = 2,
                Endpoint = endpoint,
                Name = "DesignTimeSchedule 2",
                StartDow = DayOfWeek.Wednesday,
                StartTime = new TimeSpan(17, 0, 0),
                StopDow = DayOfWeek.Friday,
                StopTime = new TimeSpan(5, 30, 0),
                Stride = 120,
                Enabled = false
            },
            new FtpSchedule() {
                XX = 3,
                Endpoint = endpoint,
                Name = "DesignTimeSchedule 3",
                StartDow = DayOfWeek.Sunday,
                StartTime = new TimeSpan(12, 0, 0),
                StopDow = DayOfWeek.Sunday,
                StopTime = new TimeSpan(13, 0, 0),
                Stride = 60,
                Enabled = true
            },
            new FtpSchedule() {
                XX = 4,
                Endpoint = endpoint,
                Name = "DesignTimeSchedule 4",
                StartDow = DayOfWeek.Saturday,
                StartTime = new TimeSpan(8, 45, 0),
                StopDow = DayOfWeek.Saturday,
                StopTime = new TimeSpan(9, 45, 0),
                Stride = 90,
                Enabled = true
            },
            new FtpSchedule() {
                XX = 5,
                Endpoint = endpoint,
                Name = "DesignTimeSchedule 5",
                StartDow = DayOfWeek.Thursday,
                StartTime = new TimeSpan(8, 0, 0),
                StopDow = DayOfWeek.Thursday,
                StopTime = new TimeSpan(20, 30, 0),
                Stride = 60,
                Enabled = true
            },
        };

        return ret;
    }
    #endregion
}
