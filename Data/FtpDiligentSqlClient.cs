﻿
// -----------------------------------------------------------------------
// <copyright file="FtpDiligentSqlClient.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;
using System.Data;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;

sealed class FtpDiligentSqlClient : FtpDiligentDatabaseClientBase, IFtpRepository
{
    #region fields
    /// <summary>
    /// Połączenie do bazy danych wykorzystywane tylko w jednym wątku GUI
    /// </summary>
    private readonly SqlConnection guiConn;
    #endregion

    #region constructor
    public FtpDiligentSqlClient(string connStr)
    {
        try {
            guiConn = new SqlConnection(connStr);
        } catch (ArgumentException ex) {
            System.Windows.MessageBox.Show(ex.Message, "FtpDiligentSqlClient", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Stop);
            Environment.Exit(1);
        }

        m_connStr = connStr;
    }
    #endregion

    #region public STA
    /// <summary>
    /// Inicjalizuje instancję dla konkretnego hosta
    /// Identyfikator instancji zapamiętuje w <see cref="m_lastInsertedKey"/>
    /// </summary>
    /// <param name="hostname">Nazwa hosta</param>
    /// <returns></returns>
    public (int, string) InitInstance(string hostname)
    {
        SqlCommand cmd = guiConn.CreateCommand();
        cmd.CommandText = "exec [ftp].[sp_init_instance] @name,@zone";
        cmd.Parameters.Add("name", SqlDbType.VarChar, 128).Value = hostname;
        cmd.Parameters.Add("zone", SqlDbType.VarChar, 128).Value = TimeZoneInfo.Local.StandardName;

        return ExecuteScalar<int>(cmd);
    }

    /// <summary>
    /// Pobiera endpointy dla bieżącej instancji
    /// </summary>
    /// <param name="instance">Identyfikator instancji</param>
    /// <returns>Tabela z harmonogramem lub komunikat o błędzie</returns>
    public (DataTable, string) GetEndpoints(int instance)
    {
        SqlCommand cmd = guiConn.CreateCommand();
        cmd.CommandText = "select xx,ins_xx,host,userid,passwd,remote_dir,local_dir,refresh_date,protocol,direction,transfer_mode from [ftp].[ftp_endpoint] where ins_xx=@ins and disabled is null order by host";
        cmd.Parameters.Add("ins", SqlDbType.Int).Value = instance;

        return ExecuteReader(cmd);
    }

    /// <summary>
    /// Pobiera bieżący harmonogram dla wskazanej instancji FtpGetWorkera
    /// </summary>
    /// <param name="endpoint">Identyfikator endpointu FTP skonfigurowanego dla tej instancji</param>
    /// <returns>Tabela z harmonogramem lub komunikat o błędzie</returns>
    public (DataTable, string) GetSchedules(int endpoint)
    {
        SqlCommand cmd = guiConn.CreateCommand();
        cmd.CommandText = "select xx,end_xx,name,job_start,job_stop,job_stride,disabled from [ftp].[ftp_schedule] where end_xx=@edp and deleted is null order by job_start";
        cmd.Parameters.Add("edp", SqlDbType.Int).Value = endpoint;

        return ExecuteReader(cmd);
    }

    /// <summary>
    /// Tworzenie, modyfikacja, usunięcie endpointu FTP
    /// </summary>
    /// <param name="endpoint">Definicja endpointu</param>
    /// <param name="mode">Rodzaj operacji</param>
    /// <returns>Komunikat o ewentualnym błędzie</returns>
    public string ModifyEndpoint(FtpEndpointModel endpoint, eDbOperation mode)
    {
        SqlCommand cmd = guiConn.CreateCommand();
        cmd.CommandText = "exec [ftp].[sp_modify_endpoint] @mode,@xx,@ins_xx,@host,@userid,@passwd,@remdir,@locdir,@transprot,@transdir,@transmode";
        var par = cmd.Parameters;
        par.Add("mode", SqlDbType.Int).Value = (int)mode;
        par.Add("xx", SqlDbType.Int).Value = endpoint.xx;
        par.Add("ins_xx", SqlDbType.Int).Value = endpoint.insXX;
        par.Add("host", SqlDbType.VarChar, 64).Value = endpoint.host;
        par.Add("userid", SqlDbType.VarChar, 32).Value = endpoint.uid;
        par.Add("passwd", SqlDbType.VarChar, 32).Value = endpoint.pwd;
        par.Add("remdir", SqlDbType.VarChar, 256).Value = endpoint.remDir;
        par.Add("locdir", SqlDbType.VarChar, 256).Value = endpoint.locDir;
        par.Add("transprot", SqlDbType.TinyInt).Value = endpoint.protocol;
        par.Add("transdir", SqlDbType.TinyInt).Value = endpoint.direction;
        par.Add("transmode", SqlDbType.TinyInt).Value = endpoint.mode;

        if (mode == eDbOperation.Insert)
            return ExecuteNonQueryStoreKey(cmd);
        else
            return ExecuteNonQuery(cmd);
    }

    /// <summary>
    /// Tworzenie, modyfikacja, usunięcie harmonogramu transferów FTP
    /// </summary>
    /// <param name="schedule">Definicja harmonogramu</param>
    /// <param name="mode">Rodzaj operacji</param>
    /// <returns>Komunikat o ewentualnym błędzie</returns>
    public string ModifySchedule(FtpScheduleModel schedule, eDbOperation mode)
    {
        SqlCommand cmd = guiConn.CreateCommand();
        cmd.CommandText = "exec [ftp].[sp_modify_schedule] @mode,@xx,@end_xx,@nazwa,@start,@stop,@stride,@disabled";
        var par = cmd.Parameters;
        par.Add("mode", SqlDbType.Int).Value = (int)mode;
        par.Add("xx", SqlDbType.Int).Value = schedule.xx;
        par.Add("end_xx", SqlDbType.Int).Value = schedule.endXX;
        par.Add("nazwa", SqlDbType.VarChar, 128).Value = schedule.name;
        par.Add("start", SqlDbType.SmallInt).Value = (short)schedule.startSpan.TotalMinutes;
        par.Add("stop", SqlDbType.SmallInt).Value = (short)schedule.stopSpan.TotalMinutes;
        par.Add("stride", SqlDbType.SmallInt).Value = schedule.stride;
        par.Add("disabled", SqlDbType.TinyInt).Value = !schedule.enabled;

        if (mode == eDbOperation.Insert)
            return ExecuteNonQueryStoreKey(cmd);
        else
            return ExecuteNonQuery(cmd);
    }
    #endregion

    #region public MTA
    /// <summary>
    /// Pobiera informację o najbliższym zadaniu do wykonania
    /// </summary>
    /// <param name="instance">Identyfikator instancji workera</param>
    /// <returns>
    /// Informacje o zadaniu, napis "0", gdy nic nie zaplanowano
    /// lub komunikat o błędzie z bazy danych
    /// </returns>
    public (FtpScheduleModel, string) GetNextSync(int instance)
    {
        var ret = new FtpScheduleModel();
        var conn = new SqlConnection(m_connStr);
        SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = "exec [ftp].[sp_select_next_sync] @ins_xx";
        cmd.Parameters.Add("ins_xx", SqlDbType.Int).Value = instance;

        try {
            conn.Open();
            var sdr = cmd.ExecuteReader(CommandBehavior.SingleRow);
            if (!sdr.Read())
                return (ret, "0");

            ret.xx = sdr.GetInt32(0);
            ret.name = sdr.GetString(1);
            if (ret.xx > 0)
                ret.nextSyncTime = sdr.GetDateTimeOffset(2).LocalDateTime;
            sdr.Close();
        } catch (SqlException oex) {
            return (ret, oex.Message);
        } catch (Exception sex) {
            return (ret, sex.Message);
        } finally {
            conn.Close();
        }

        return (ret, string.Empty);
    }

    /// <summary>
    /// Pobiera informację o parametrach serwera do bieżącego zadania do wykonania
    /// </summary>
    /// <param name="schedule">Identyfikator pozycji harmonogramu</param>
    /// <returns>
    /// Informacje o endpoincie, napis "0", gdy nie ma endpointu dla harmonogramu
    /// lub komunikat o błędzie z bazy danych
    /// </returns>
    public async Task<(FtpEndpointModel, string)> SelectEndpoint(int schedule)
    {
        var ret = new FtpEndpointModel();
        var conn = new SqlConnection(m_connStr);
        SqlCommand cmd = conn.CreateCommand();
        cmd.Parameters.Add("sch_xx", SqlDbType.Int).Value = schedule;
        cmd.CommandText = "exec [ftp].[sp_endpoint_for_schedule] @sch_xx";

        try {
            await conn.OpenAsync();
            var sdr = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (!sdr.Read())
                return (ret, "0");
            ret.host = sdr.GetString(0);
            ret.uid = sdr.GetString(1);
            ret.pwd = sdr.GetString(2);
            ret.remDir = sdr.GetString(3);
            ret.locDir = sdr.GetString(4);
            ret.lastSync = sdr.GetDateTime(5);
            ret.nextSync = DateTime.Now;
            ret.protocol = (eFtpProtocol)sdr.GetByte(6);
            ret.direction = (eFtpDirection)sdr.GetByte(7);
            ret.mode = (eFtpTransferMode)sdr.GetByte(8);
            sdr.Close();
        } catch (SqlException oex) {
            return (ret, oex.Message);
        } catch (Exception sex) {
            return (ret, sex.Message);
        } finally {
            conn.Close();
        }

        return (ret, string.Empty);
    }

    /// <summary>
    /// Loguje wywołanie synchronizacji, która nie wykonała żadnych transferów
    /// </summary>
    /// <param name="sync">Informacja o uruchomieniu workera i skopiowanych plikach</param>
    /// <returns>Komunikat o ewentualnym błędzie</returns>
    public string LogActivation(FtpSyncModel sync)
    {
        var conn = new SqlConnection(m_connStr);
        SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = "exec [ftp].[sp_log_activation] @xx,@sync_time";
        cmd.Parameters.Add("xx", SqlDbType.Int).Value = sync.xx;
        cmd.Parameters.Add("sync_time", SqlDbType.DateTime2).Value = sync.syncTime;

        return ExecuteNonQuery(cmd);
    }

    /// <summary>
    /// Loguje wywołanie synchronizacji, która wykonała transfery plików
    /// </summary>
    /// <param name="sync">Informacja o uruchomieniu workera i skopiowanych plikach</param>
    /// <returns>Komunikat o ewentualnym błędzie</returns>
    public string LogSync(FtpSyncModel sync)
    {
        var conn = new SqlConnection(m_connStr);
        SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = "exec [ftp].[sp_log_download] @transdir,@xx,@sync_time,@file_name,@file_size,@file_date,@md5";
        var par = cmd.Parameters;
        par.Add("transdir", SqlDbType.TinyInt).Value = sync.direction;
        par.Add("xx", SqlDbType.Int).Value = sync.xx;
        par.Add("sync_time", SqlDbType.DateTime).Value = sync.syncTime;
        par.Add("file_name", SqlDbType.VarChar, 256);
        par.Add("file_size", SqlDbType.BigInt);
        par.Add("file_date", SqlDbType.DateTime2);
        par.Add("md5", SqlDbType.Binary, 16);

        string ret = string.Empty;
        foreach (var fi in sync.files) {
            par[3].Value = fi.Name;
            par[4].Value = fi.Size;
            par[5].Value = fi.Modified;
            par[6].Value = fi.MD5;
            ret = ExecuteNonQuery(cmd);
            if (!string.IsNullOrEmpty(ret)) break;
        }

        return ret;
    }

    /// <summary>
    /// Sprawdza, czy z danej instancji FtpGetWorkera pobrano już dany plik
    /// </summary>
    /// <param name="file">Dane pliku</param>
    /// <returns>Komunikat o ewentualnym błędzie</returns>
    public (bool,string) VerifyFile(FtpFileModel file)
    {
        var conn = new SqlConnection(m_connStr);
        SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = "exec [ftp].[sp_check_file] @ins_xx,@file_name,@file_size,@file_date";
        var par = cmd.Parameters;
        par.Add("ins_xx", SqlDbType.Int).Value = file.Instance;
        par.Add("file_name", SqlDbType.VarChar, 256).Value = file.FileName;
        par.Add("file_size", SqlDbType.BigInt).Value = file.FileSize;
        par.Add("file_date", SqlDbType.DateTime2).Value = file.FileDate;

        var (status, msg) = ExecuteScalar<int>(cmd);
        return (status > 0, msg);
    }
    #endregion

    #region protected
    /// <summary>
    /// Konwertuje wiersz z tabeli bazodanowej na konkretny typ
    /// </summary>
    /// <param name="row">Wiersz danych</param>
    protected override FtpEndpoint CreateFtpEndpoint(DataRow row) => new (new FtpEndpointModel()
    {
        xx = (int)row[0],
        insXX = (int)row[1],
        host = row[2].ToString(),
        uid = row[3].ToString(),
        pwd = row[4].ToString(),
        remDir = row[5].ToString(),
        locDir = row[6].ToString(),
        lastSync = (DateTime)row[7],
        protocol = (eFtpProtocol)row[8],
        direction = (eFtpDirection)row[9],
        mode = (eFtpTransferMode)row[10]
    });

    /// <summary>
    /// Konwertuje wiersz z tabeli bazodanowej na konkretny typ
    /// </summary>
    /// <param name="row">Wiersz danych</param>
    protected override FtpSchedule CreateFtpSchedule(DataRow row) => new (new FtpScheduleModel()
    {
        xx = (int)row[0],
        endXX = (int)row[1],
        name = row[2].ToString(),
        startSpan = new TimeSpan(0, minutes: (short)row[3], 0),
        stopSpan = new TimeSpan(0, minutes: (short)row[4], 0),
        stride = (short)row[5],
        enabled = row[6] == DBNull.Value
    });
    #endregion

    #region private static
    /// <summary>
    /// Wykonuje zapytanie w bazie danych
    /// </summary>
    /// <param name="cmd">Polecenie zapytania</param>
    /// <returns>Tabela z wynikiem lub komunikat o błędzie</returns>
    public (DataTable, string) ExecuteReader(SqlCommand cmd)
    {
        var ret = new DataTable();

        try {
            cmd.Connection.Open();
            var sdr = cmd.ExecuteReader(CommandBehavior.SingleResult);
            ret.Load(sdr);
        } catch (SqlException oex) {
            return (null, oex.Message);
        } catch (Exception sex) {
            return (null, sex.Message);
        } finally {
            cmd.Connection.Close();
        }

        return (ret, string.Empty);
    }

    /// <summary>
    /// Wykonuje zapytanie w bazie danych i zapamiętuje w zmiennej 
    /// statycznej wartość parametru OUT, pobraną z sekwencji przy insercie
    /// </summary>
    /// <param name="cmd">Polecenie zapytania</param>
    /// <returns>Komunikat o ewentualnym błędzie</returns>
    public string ExecuteNonQueryStoreKey(SqlCommand cmd)
    {
        var (key, errmsg) = ExecuteScalar<int>(cmd);
        if (string.IsNullOrEmpty(errmsg))
            m_lastInsertedKey = key;
        else
            m_lastInsertedKey = 0;

        return errmsg;
    }

    /// <summary>
    /// Wykonuje zapytanie w bazie danych
    /// </summary>
    /// <param name="cmd">Polecenie zapytania</param>
    /// <returns>Komunikat o ewentualnym błędzie</returns>
    public string ExecuteNonQuery(SqlCommand cmd)
    {
        var errmsg = string.Empty;

        try {
            cmd.Connection.Open();
            cmd.ExecuteNonQuery();
        } catch (SqlException oex) {
            errmsg = oex.Message;
        } catch (Exception sex) {
            errmsg = sex.Message;
        } finally {
            cmd.Connection.Close();
        }

        return errmsg;
    }

    /// <summary>
    /// Wykonuje zapytanie w bazie danych zwracające pojedynczą wartość typu T
    /// </summary>
    /// <param name="cmd">Polecenie zapytania</param>
    /// <returns>Wartość typu T pobraną z bazy lub komunikat o ewentualnym błędzie</returns>
    public (T, string) ExecuteScalar<T>(SqlCommand cmd)
    {
        var errmsg = string.Empty;

        try {
            cmd.Connection.Open();
            var ret = cmd.ExecuteScalar();
            return ((T)ret, string.Empty);
        } catch (SqlException oex) {
            errmsg = oex.Message;
        } catch (Exception sex) {
            errmsg = sex.Message;
        } finally {
            cmd.Connection.Close();
        }

        return (default(T), errmsg);
    }
    #endregion
}
