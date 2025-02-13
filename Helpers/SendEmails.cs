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
using Prism.Events;

using FtpDiligent.Events;

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
    /// Na jaki adres wysłać mailowe powiadomienia o błędach
    /// </summary>
    public string m_errorsMailTo;
    #endregion

    #region events
    /// <summary>
    /// Rozgłasza status powiadomienia email
    /// </summary>
    private StatusEvent MailNotificationStatus;
    #endregion

    #region constructor
    /// <summary>
    /// Klasa pomocnicza do wysyłania maili
    /// </summary>
    /// <param name="eventAggr">Mechanizm do przekazywania powiadomień</param>
    /// <param name="errorsMailTo">Lista adresów odbiorców, rozdzialona średnikami</param>
    /// <param name="apiKey">Klucz prywatny do usługi SendGrid</param>
    public SendEmails(IEventAggregator eventAggr, string errorsMailTo, string apiKey)
    {
        m_sendGridKey = apiKey;
        m_errorsMailTo = errorsMailTo;
        MailNotificationStatus = eventAggr.GetEvent<StatusEvent>();
    }
    #endregion

    #region public
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
    #endregion

    #region private
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
            NotifyMailNotificatioStatus(eSeverityCode.Error, $"PrepareMimeMessage error: {exc.Message}");
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

            NotifyMailNotificatioStatus(eSeverityCode.Message, $"Wysłano {msg.To.Count} powiadomienie/a mailowe.");
            return true;
        } catch (Exception exc) {
            NotifyMailNotificatioStatus(eSeverityCode.Error, $"SendEmail to {m_mailServer} error: {exc.Message}");
            return false;
        }
    }

    /// <summary>
    /// Triggers an FileTransferred event with provided arguments
    /// </summary>
    /// <param name="severity">Severity code</param>
    /// <param name="message">Description</param>
    protected void NotifyMailNotificatioStatus(eSeverityCode severity, string message) => MailNotificationStatus.Publish(new StatusEventArgs(severity, message));
    #endregion
}
