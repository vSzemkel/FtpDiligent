
// -----------------------------------------------------------------------
// <copyright file="FtpDiligentOracleClient.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Oracle.ManagedDataAccess.Client;

class FtpDiligentOracleClient : FtpDiligentDatabaseClientBase, IFtpDiligentDatabaseClient
{
    #region fields
    /// <summary>
    /// Połączenie do bazy danych wykorzystywane tylko w jednym wątku GUI
    /// </summary>
    private readonly OracleConnection guiConn;
    #endregion

    #region constructor
    public FtpDiligentOracleClient(string connStr)
    {
        try {
            guiConn = new OracleConnection(connStr);
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
        OracleCommand cmd = guiConn.CreateCommand();
        cmd.CommandText = "begin init_instance(:name,:xx); end;";
        cmd.Parameters.Add("name", OracleDbType.Varchar2, 128).Value = hostname;
        cmd.Parameters.Add("xx", OracleDbType.Int32).Direction = ParameterDirection.Output;

        return (m_lastInsertedKey, ExecuteNonQueryStoreKeyAsync(cmd, 1).Result);
    }

    /// <summary>
    /// Pobiera endpointy dla bieżącej instancji
    /// </summary>
    /// <param name="instance">Identyfikator instancji</param>
    /// <returns>Tabela z harmonogramem lub komunikat o błędzie</returns>
    public (DataTable, string) GetEndpoints(int instance)
    {
        OracleCommand cmd = guiConn.CreateCommand();
        cmd.CommandText = "select xx,ins_xx,host,userid,passwd,remote_dir,local_dir,refresh_date,protocol,direction,transfer_mode from ftp_endpoint where ins_xx=:ins and usuniety is null order by host";
        cmd.Parameters.Add("ins", OracleDbType.Int32).Value = instance;

        return ExecuteReaderAsync(cmd).Result;
    }

    /// <summary>
    /// Konwertuje obiekt typu DataTable na bindowalną w WPF kolekcję
    /// </summary>
    /// <param name="tab">Tabela z endpointami</param>
    /// <returns>Bindowalna w WPF kolekcja endpointów</returns>
    public ObservableCollection<FtpEndpoint> GetEndpointsCollection(DataTable tab)
    {
        var ret = new ObservableCollection<FtpEndpoint>();
        foreach (DataRow dr in tab.Rows)
            ret.Add(new (
                xx: (int)(decimal)dr[0],
                insXX: (int)(decimal)dr[1],
                host: dr[2].ToString(),
                uid: dr[3].ToString(),
                pwd: dr[4].ToString(),
                remDir: dr[5].ToString(),
                locDir: dr[6].ToString(),
                lastSync: (DateTime)dr[7],
                prot: (eFtpProtocol)(short)dr[8],
                dir: (eFtpDirection)(short)dr[9],
                mode: (eFtpTransferMode)(short)dr[10])
            );

        return ret;
    }

    /// <summary>
    /// Pobiera bieżący harmonogram dla wskazanej instancji FtpGetWorkera
    /// </summary>
    /// <param name="endpoint">Identyfikator endpointu FTP skonfigurowanego dla tej instancji</param>
    /// <returns>Tabela z harmonogramem lub komunikat o błędzie</returns>
    public (DataTable, string) GetSchedules(int endpoint)
    {
        OracleCommand cmd = guiConn.CreateCommand();
        cmd.CommandText = "select xx,end_xx,nazwa,job_start,job_stop,job_step,disabled from ftp_schedule where end_xx=:edp and usuniety is null order by job_start";
        cmd.Parameters.Add("edp", OracleDbType.Int32).Value = endpoint;

        return ExecuteReaderAsync(cmd).Result;
    }

    /// <summary>
    /// Konwertuje obiekt typu DataTable na bindowalną w WPF kolekcję
    /// </summary>
    /// <param name="tab">Tabela z endpointami</param>
    /// <returns>Bindowalna w WPF kolekcja endpointów</returns>
    public ObservableCollection<FtpSchedule> GetSchedulesCollection(DataTable tab)
    {
        var ret = new ObservableCollection<FtpSchedule>();
        foreach (DataRow dr in tab.Rows) {
            ret.Add(new FtpSchedule(new FtpScheduleModel() {
                xx = (int)(decimal)dr[0],
                endXX = (int)(decimal)dr[1],
                name = dr[2].ToString(),
                startSpan = new TimeSpan(0, minutes: (short)(decimal)dr[3], 0),
                stopSpan = new TimeSpan(0, minutes: (short)(decimal)dr[4], 0),
                stride = (short)(decimal)dr[5],
                enabled = dr[6] == DBNull.Value,
            }));
        }

        return ret;
    }

    /// <summary>
    /// Tworzenie, modyfikacja, usunięcie endpointu FTP
    /// </summary>
    /// <param name="endpoint">Definicja endpointu</param>
    /// <param name="mode">Rodzaj operacji</param>
    /// <returns>Komunikat o ewentualnym błędzie</returns>
    public string ModifyEndpoint(FtpEndpointModel endpoint, eDbOperation mode)
    {
        OracleCommand cmd = guiConn.CreateCommand();
        cmd.CommandText = "begin modify_endpoint(:mode,:xx,:ins_xx,:host,:userid,:passwd,:remdir,:locdir,:transprot,:transdir,:transmode); end;";
        var par = cmd.Parameters;
        par.Add("mode", OracleDbType.Int32).Value = (int)mode;
        par.Add("xx", OracleDbType.Int32).Value = endpoint.xx;
        par.Add("ins_xx", OracleDbType.Int32).Value = endpoint.insXX;
        par.Add("host", OracleDbType.Varchar2, 64).Value = endpoint.host;
        par.Add("userid", OracleDbType.Varchar2, 32).Value = endpoint.uid;
        par.Add("passwd", OracleDbType.Varchar2, 32).Value = endpoint.pwd;
        par.Add("remdir", OracleDbType.Varchar2, 256).Value = endpoint.remDir;
        par.Add("locdir", OracleDbType.Varchar2, 256).Value = endpoint.locDir;
        par.Add("transprot", OracleDbType.Byte).Value = endpoint.protocol;
        par.Add("transdir", OracleDbType.Byte).Value = endpoint.direction;
        par.Add("transmode", OracleDbType.Byte).Value = endpoint.mode;

        if (mode == eDbOperation.Insert)
            return ExecuteNonQueryStoreKeyAsync(cmd, 1).Result;
        else
            return ExecuteNonQueryAsync(cmd).Result;
    }

    /// <summary>
    /// Tworzenie, modyfikacja, usunięcie harmonogramu transferów FTP
    /// </summary>
    /// <param name="schedule">Definicja harmonogramu</param>
    /// <param name="mode">Rodzaj operacji</param>
    /// <returns>Komunikat o ewentualnym błędzie</returns>
    public string ModifySchedule(FtpScheduleModel schedule, eDbOperation mode)
    {
        OracleCommand cmd = guiConn.CreateCommand();
        cmd.CommandText = "begin modify_schedule2(:mode,:xx,:end_xx,:nazwa,:start,:stop,:stride,:disabled); end;";
        var par = cmd.Parameters;
        par.Add("mode", OracleDbType.Int32).Value = (int)mode;
        par.Add("xx", OracleDbType.Int32).Value = schedule.xx;
        par.Add("end_xx", OracleDbType.Int32).Value = schedule.endXX;
        par.Add("nazwa", OracleDbType.Varchar2, 128).Value = schedule.name;
        par.Add("start", OracleDbType.Int16).Value = (short)schedule.startSpan.TotalMinutes;
        par.Add("stop", OracleDbType.Int16).Value = (short)schedule.stopSpan.TotalMinutes;
        par.Add("stride", OracleDbType.Int16).Value = schedule.stride;
        par.Add("disabled", OracleDbType.Byte).Value = !schedule.enabled;

        if (mode == eDbOperation.Insert)
            return ExecuteNonQueryStoreKeyAsync(cmd, 1).Result;
        else
            return ExecuteNonQueryAsync(cmd).Result;
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
        var conn = new OracleConnection(m_connStr);
        OracleCommand cmd = conn.CreateCommand();
        cmd.CommandText = "begin select_next_sync(:ins_xx,:refCur); end;";
        cmd.Parameters.Add("ins_xx", OracleDbType.Int32).Value = instance;
        cmd.Parameters.Add("refCur", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

        try {
            conn.Open();
            var odr = cmd.ExecuteReader(CommandBehavior.SingleRow);
            if (!odr.Read())
                return (ret, "0");

            ret.xx = (int)odr.GetDecimal(0);
            ret.name = odr.GetString(1);
            ret.nextSyncTime = odr.GetDateTime(2);
            odr.Close();
        } catch (OracleException oex) {
            return (ret, OracleMessageFormatter.FirstLine(oex.Message));
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
        var conn = new OracleConnection(m_connStr);
        OracleCommand cmd = conn.CreateCommand();
        cmd.Parameters.Add("sch_xx", OracleDbType.Int32).Value = schedule;
        cmd.Parameters.Add("refCur", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
        cmd.CommandText = "begin endpoint_for_schedule(:sch_xx,:refCur); end;";

        try {
            await conn.OpenAsync();
            var odr = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (!odr.Read())
                return (ret, "0");

            ret.host = odr.GetString(0);
            ret.uid = odr.GetString(1);
            ret.pwd = odr.GetString(2);
            ret.remDir = odr.GetString(3);
            ret.locDir = odr.GetString(4);
            ret.lastSync = odr.GetDateTime(5);
            ret.nextSync = odr.GetDateTime(6);
            ret.protocol = (eFtpProtocol)odr.GetByte(7);
            ret.direction = (eFtpDirection)odr.GetByte(8);
            ret.mode = (eFtpTransferMode)odr.GetByte(9);
            odr.Close();
        } catch (OracleException oex) {
            return (ret, OracleMessageFormatter.FirstLine(oex.Message));
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
    public async Task<string> LogActivation(FtpSyncModel sync)
    {
        var conn = new OracleConnection(m_connStr);
        OracleCommand cmd = conn.CreateCommand();
        cmd.CommandText = "begin log_nodownload(:sch_xx,:sync_time); end;";
        cmd.Parameters.Add("sch_xx", OracleDbType.Int32).Value = sync.xx;
        cmd.Parameters.Add("sync_time", OracleDbType.Date).Value = sync.syncTime;

        return await ExecuteNonQueryAsync(cmd);
    }

    /// <summary>
    /// Loguje wywołanie synchronizacji, która wykonała transfery plików
    /// </summary>
    /// <param name="sync">Informacja o uruchomieniu workera i skopiowanych plikach</param>
    /// <returns>Komunikat o ewentualnym błędzie</returns>
    public async Task<string> LogSync(FtpSyncModel sync)
    {
        var conn = new OracleConnection(m_connStr);
        OracleCommand cmd = conn.CreateCommand();
        cmd.CommandText = "begin log_download(:transdir,:sch_xx,:sync_fime,:file_names,:file_sizes,:file_dates); end;";
        var par = cmd.Parameters;
        par.Add("transdir", OracleDbType.Byte).Value = sync.direction;
        par.Add("sch_xx", OracleDbType.Int32).Value = sync.xx;
        par.Add("sync_time", OracleDbType.Date).Value = sync.syncTime;
        par.Add("file_names", OracleDbType.Varchar2, 256).CollectionType = OracleCollectionType.PLSQLAssociativeArray;
        par.Add("file_sizes", OracleDbType.Int64).CollectionType = OracleCollectionType.PLSQLAssociativeArray;
        par.Add("file_dates", OracleDbType.Date).CollectionType = OracleCollectionType.PLSQLAssociativeArray;

        var ind = 0;
        var size = sync.files.Length;
        var fileNames = new string[size];
        var fileSizes = new long[size];
        var fileDates = new DateTime[size];
        foreach (var fi in sync.files) {
            fileNames[ind] = fi.Name;
            fileSizes[ind] = fi.Size;
            fileDates[ind] = fi.Modified;
            ++ind;
        }

        par[3].Value = fileNames;
        par[4].Value = fileSizes;
        par[5].Value = fileDates;
        par[3].Size = par[4].Size = par[5].Size = size;

        return await ExecuteNonQueryAsync(cmd);
    }

    /// <summary>
    /// Sprawdza, czy z danej instancji FtpGetWorkera pobrano już dany plik
    /// </summary>
    /// <param name="file">Dane pliku</param>
    /// <returns>Komunikat o ewentualnym błędzie</returns>
    public (bool,string) VerifyFile(FtpFileModel file)
    {
        var conn = new OracleConnection(m_connStr);
        OracleCommand cmd = conn.CreateCommand();
        cmd.CommandText = "select check_file(:ins_xx,:file_name,:file_size,:file_date) from dual";
        var par = cmd.Parameters;
        par.Add("ins_xx", OracleDbType.Int32).Value = file.Instance;
        par.Add("file_name", OracleDbType.Varchar2, 256).Value = file.FileName;
        par.Add("file_size", OracleDbType.Int64).Value = file.FileSize;
        par.Add("file_date", OracleDbType.Date).Value = file.FileDate;

        var (status,msg) = ExecuteScalarAsync<decimal>(cmd).Result;
        return (status > 0, msg);
    }
    #endregion

    #region private static
    /// <summary>
    /// Wykonuje zapytanie w bazie danych
    /// </summary>
    /// <param name="cmd">Polecenie zapytania</param>
    /// <returns>Tabela z wynikiem lub komunikat o błędzie</returns>
    public async Task<(DataTable, string)> ExecuteReaderAsync(OracleCommand cmd)
    {
        var ret = new DataTable();

        try {
            await cmd.Connection.OpenAsync();
            var odr = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult);
            ret.Load(odr);
        } catch (OracleException oex) {
            return (null, OracleMessageFormatter.FirstLine(oex.Message));
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
    /// <param name="index">Numer porządkowy parametru typu OUT</param>
    /// <returns>Komunikat o ewentualnym błędzie</returns>
    public async Task<string> ExecuteNonQueryStoreKeyAsync(OracleCommand cmd, int index)
    {
        var par = cmd.Parameters[index];
        par.Direction = ParameterDirection.InputOutput;

        string errmsg = await ExecuteNonQueryAsync(cmd);
        if (string.IsNullOrEmpty(errmsg))
            m_lastInsertedKey = (int)(Oracle.ManagedDataAccess.Types.OracleDecimal)par.Value;
        else
            m_lastInsertedKey = 0;

        return errmsg;
    }

    /// <summary>
    /// Wykonuje zapytanie w bazie danych
    /// </summary>
    /// <param name="cmd">Polecenie zapytania</param>
    /// <returns>Komunikat o ewentualnym błędzie</returns>
    public async Task<string> ExecuteNonQueryAsync(OracleCommand cmd)
    {
        var errmsg = string.Empty;

        try {
            await cmd.Connection.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        } catch (OracleException oex) {
            errmsg = OracleMessageFormatter.FirstLine(oex.Message);
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
    public async Task<(T, string)> ExecuteScalarAsync<T>(OracleCommand cmd)
    {
        var errmsg = string.Empty;

        try {
            await cmd.Connection.OpenAsync();
            var ret = await cmd.ExecuteScalarAsync();
            return ((T)ret, string.Empty);
        } catch (OracleException oex) {
            errmsg = OracleMessageFormatter.FirstLine(oex.Message);
        } catch (Exception sex) {
            errmsg = sex.Message;
        } finally {
            cmd.Connection.Close();
        }

        return (default(T), errmsg);
    }
    #endregion
}
