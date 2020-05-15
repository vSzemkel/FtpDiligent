// -----------------------------------------------------------------------
// <copyright file="SendEmails.cs" company="Agora SA">
//     Copyright (c) TDE - Development IT , sierpieñ 2019
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent
{
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
        /// Klucz us³ugi SendGrid
        /// </summary>
        public string m_sendGridKey;

        /// <summary>
        /// Referencja do g³ównego okna
        /// </summary>
        public MainWindow m_mainWnd;

        /// <summary>
        /// Na jaki adres wys³aæ mailowe powiadomienia o b³êdach
        /// </summary>
        public string m_errorsMailTo;
        #endregion

        #region constructor
        public SendEmails(string errorsMailTo, string apiKey)
        {
            m_sendGridKey = apiKey;
            m_errorsMailTo = errorsMailTo;
        }
        #endregion

        /// <summary>
        /// Uruchamia wysy³kê maili przez us³ugê SendGrid
        /// </summary>
        /// <param name="error">B³êd do wys³ania</param>
        public void Run(string error)
        {
            if (!string.IsNullOrEmpty(m_errorsMailTo)) {
                var message = PrepareMimeMessage(error);
                SendEmail(message);
            }
        }

        /// <summary>
        /// Buduje wiadomoœæ na podstawie danych wyci¹gniêtych z bazy
        /// </summary>
        /// <param name="sdr">Rekord z bazy danych</param>
        /// <returns>Wiadomoœæ do wys³ania</returns>
        private MimeMessage PrepareMimeMessage(string error) {
            try { 
                var msg = new MimeMessage();
                msg.Subject = "Powiadomienie o b³êdzie transferu plików";
                msg.To.AddRange(m_errorsMailTo.Split(';').Where(s => !string.IsNullOrEmpty(s)).Select(s => new MailboxAddress(s)));
                var senderMailbox = new MailboxAddress("FtpDiligent", "no_replay@sendgrid.net");
                msg.Sender = senderMailbox;
                msg.From.Add(senderMailbox);

                var body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = error };
                msg.Body = body;

                return msg;
            } catch (Exception exc) {
                m_mainWnd.m_showError(eSeverityCode.Error, $"PrepareMimeMessage error: {exc.Message}");
                return null;
            }
        }

        /// <summary>
        /// Wysy³anie wiadomoœci protoko³em SMTP
        /// </summary>
        /// <param name="msg">Wiadomoœæ</param>
        /// <returns>Status wysy³ki</returns>
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

                return true;
            } catch (Exception exc) {
                m_mainWnd.m_showError(eSeverityCode.Error, $"SendEmail to {m_mailServer} error: {exc.Message}");
                return false;
            }
        }
    }
}
