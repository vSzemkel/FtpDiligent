
// -----------------------------------------------------------------------
// <copyright file="FtpUtilityBase.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;
using System.IO;

public abstract class FtpUtilityBase
{
    #region fields
    protected string m_sHost;
    protected string m_sUser;
    protected string m_sPass;
    protected string m_sRemoteDir;
    protected string m_sLocalDir;
    protected DateTime m_dtLastRefresh;
    protected eSyncFileMode m_SyncMode;
    protected eFtpTransferMode m_TransferMode;

    protected FtpDispatcher m_Disp;
    #endregion

    #region constructor
    /// <summary>
    /// Konstruktor FtpUtility sterowanego przez <see>FtpDispatcher</see>
    /// </summary>
    /// <param name="endpoint">Parametry serwera</param>
    /// <param name="dispatcher">Obiekt sterujący wątkami</param>
    /// <param name="mode">Algorytm kwalifikacji plików do transferu</param>
    public FtpUtilityBase(FtpEndpointModel endpoint, FtpDispatcher dispatcher, eSyncFileMode mode)
    {
        m_SyncMode = mode;
        m_Disp = dispatcher;
        FromFtpEndpoint(endpoint);
    }

    /// <summary>
    /// Konstruktor FtpUtility dla pojedynczych usług
    /// </summary>
    /// <param name="endpoint">Parametry serwera</param>
    public FtpUtilityBase(FtpEndpointModel endpoint)
    {
        m_SyncMode = eSyncFileMode.AllFiles;
        FromFtpEndpoint(endpoint);
    }
    #endregion

    #region events
    /// <summary>
    /// Rozgłasza informację o przetransportowanym pliku
    /// </summary>
    public static event EventHandler<FileTransferredEventArgs> FileTransferred;

    /// <summary>
    /// Rozgłasza status operacji transferu pliku
    /// </summary>
    public static event EventHandler<TransferNotificationEventArgs> TransferStatusNotification;
    #endregion

    #region public methods
    /// <summary>
    /// Podaje ścieżkę do lokalnego katalogu roboczego
    /// </summary>
    public string GetLocalDirectory() => m_sLocalDir;

    /// <summary>
    /// Sprawdza istnienie lokalnego katalogu
    /// </summary>
    /// <returns>Czy istnieje</returns>
    public bool CheckLocalDirectory()
    {
        if (!Directory.Exists(m_sLocalDir)) {
            string sMsg = "Nie odnaleziono katalogu lokalnego: " + m_sLocalDir;
            if (FtpDispatcherGlobals.ShowError != null) {
                FtpDispatcherGlobals.ShowError(eSeverityCode.Error, sMsg);
                return false;
            } else
                throw new FtpUtilityException(sMsg);
        }

        return true;
    }

    /// <summary>
    /// Nawiązuje połączenie z endpointem i natychmiast je kończy
    /// </summary>
    /// <returns>Stwierdza, czy dane używane do nawiązania połączenia są prawidłowe</returns>
    public bool CheckConnection(ref string sErrInfo)
    {
        try {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            Connect();

            sw.Stop();
            sErrInfo = "Połączenie zostało nawiązane";
            if (sw.ElapsedMilliseconds >= 100)
                sErrInfo += $" w ciągu {(decimal)sw.ElapsedMilliseconds / 1000:##0.##} [s]";

            return true;
        } catch (FtpUtilityException fue) {
            sErrInfo = fue.Message;
            return false;
        }
    }
    #endregion

    #region protected methods
    /// <summary>
    /// Sprawdza, czy w zasobie lokalnym istnieje już plik o zadanej nazwie i rozmiarze
    /// </summary>
    /// <param name="sFileName">Nazwa liku</param>
    /// <param name="sLength">Długość pliku</param>
    /// <returns>Czy istnieje plik o zadanych cechach w katalogu lokalnym</returns>
    protected bool CheckLocalStorage(string sFileName, long sLength)
    {
        var fi = new FileInfo(m_sLocalDir + sFileName);
        if (!fi.Exists) return false;

        return fi.Length == sLength;
    }

    /// <summary>
    /// Sprawdza, czy wywołanie metody transferującej pliki nastąpiło w prawidłowym kontekście
    /// </summary>
    protected bool CheckDispatcher()
    {
        if (m_Disp == null && m_SyncMode == eSyncFileMode.UniqueDateAndSizeInDatabase) {
            string sMsg = "Pobieranie plików w tym trybie wymaga dispatchera";
            if (FtpDispatcherGlobals.ShowError != null) {
                FtpDispatcherGlobals.ShowError(eSeverityCode.Error, sMsg);
                return false;
            } else
                throw new FtpUtilityException(sMsg);
        }

        return true;
    }

    /// <summary>
    /// Triggers an FileTransferred event with provided arguments
    /// </summary>
    /// <param name="code">Severity code</param>
    /// <param name="operation">Operation type</param>
    /// <param name="message">Description</param>
    protected void NotifyFileTransferred(eSeverityCode code, eFtpDirection operation, string message)
    {
        var eventArgs = new FileTransferredEventArgs(code, operation, null, message);
        if (FileTransferred != null)
            FileTransferred(this, eventArgs);
    }

    /// <summary>
    /// Triggers an FileTransferred event with provided arguments
    /// </summary>
    /// <param name="code">Severity code</param>
    /// <param name="operation">Operation type</param>
    /// <param name="file">File details</param>
    protected void NotifyFileTransferred(eFtpDirection operation, FileInfo file)
    {
        var eventArgs = new FileTransferredEventArgs(eSeverityCode.FileInfo, operation, file, string.Empty);
        if (FileTransferred != null)
            FileTransferred(this, eventArgs);
    }
    #endregion

    #region protected abstract methods
    protected abstract bool Connect();
    #endregion

    #region private methods
    /// <summary>
    /// Inicjalizuje własności zależne od endpointu
    /// </summary>
    /// <param name="endpoint">Dane endpointu</param>
    private void FromFtpEndpoint(FtpEndpointModel endpoint)
    {
        m_sHost = endpoint.host;
        m_sUser = endpoint.uid;
        m_sPass = endpoint.pwd;
        m_sRemoteDir = endpoint.remDir;
        m_sLocalDir = endpoint.locDir;
        m_dtLastRefresh = endpoint.lastSync;
        m_TransferMode = endpoint.mode;
    }
    #endregion
}
