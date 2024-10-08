// -----------------------------------------------------------------------
// <copyright file="SendEmails.cs" company="Agora SA">
//     Copyright (c) TDE - Development IT , sierpie� 2019
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System;
using System.Linq;

using MailKit.Net.Smtp;
using MimeKit;

public class SendEmails
{
    #region fields
    /// <summary>
    /// Nazwa serwera mailowego
    /// </summary>
    private readonly string m_mailServer = "smtp.sendgrid.net";

    /// <summary>
    /// Klucz usługi SendGrid
    /// </summary>
    public string m_sendGridKey;

    /// <summary>
    /// Referencja do głównego okna
    /// </summary>
    public MainWindow m_mainWnd;

    /// <summary>
    /// Na jaki adres wysłać mailowe powiadomienia o błędach
    /// </summary>
    public string m_errorsMailTo;
    #endregion

    #region constructor
    /// <summary>
    /// Klasa pomocnicza do wysy�ania maili
    /// </summary>
    /// <param name="wnd">G��wne okno aplikacji WPF</param>
    /// <param name="errorsMailTo">Lista adres�w odbiorc�w, rozdzialona �rednikami</param>
    /// <param name="apiKey">Klucz prywatny do usługi SendGrid</param>
    public SendEmails(MainWindow wnd, string errorsMailTo, string apiKey)
    {
        m_mainWnd = wnd;
        m_sendGridKey = apiKey;
        m_errorsMailTo = errorsMailTo;
    }
    #endregion

    /// <summary>
    /// Uruchamia wysyłkę maili przez usługę SendGrid
    /// </summary>
    /// <param name="error">Błąd do wysłania</param>
    public void Run(string error)
    {
        if (!string.IsNullOrEmpty(m_errorsMailTo)) {
            var message = PrepareMimeMessage(error);
            SendEmail(message);
        }
    }

    /// <summary>
    /// Buduje wiadomość na podstawie danych wyciągniętych z bazy
    /// </summary>
    /// <param name="sdr">Rekord z bazy danych</param>
    /// <returns>Wiadomość do wysłania</returns>
    private MimeMessage PrepareMimeMessage(string error) {
        try { 
            var msg = new MimeMessage();
            msg.Subject = "Powiadomienie o błędzie transferu plików";
            msg.To.AddRange(m_errorsMailTo.Split(';').Where(s => !string.IsNullOrEmpty(s)).Select(s => MailboxAddress.Parse(s)));
            var senderMailbox = new MailboxAddress("FtpDiligent", "no_replay@sendgrid.net");
            msg.Sender = senderMailbox;
            msg.From.Add(senderMailbox);

            var body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = error };
            msg.Body = body;

            return msg;
        } catch (Exception exc) {
            FtpDispatcherGlobals.ShowError(eSeverityCode.Error, $"PrepareMimeMessage error: {exc.Message}");
            return null;
        }
    }

    /// <summary>
    /// Wysyłanie wiadomości protokołem SMTP
    /// </summary>
    /// <param name="msg">Wiadomość</param>
    /// <returns>Status wysyłki</returns>
    private bool SendEmail(MimeMessage msg) {
        try {
            using (var client = new SmtpClient()) {
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                client.Connect(m_mailServer, 587, false);
                client.Authenticate("apikey", m_sendGridKey);

                client.Send(msg);
                client.Disconnect(true);
            }

            FtpDispatcherGlobals.ShowError(eSeverityCode.Message, $"Wysłano {msg.To.Count} powiadomienie/a mailowe.");
            return true;
        } catch (Exception exc) {
            FtpDispatcherGlobals.ShowError(eSeverityCode.Error, $"SendEmail to {m_mailServer} error: {exc.Message}");
            return false;
        }
    }
}
