// -----------------------------------------------------------------------
// <copyright file="SendEmails.cs" company="Agora SA">
//     Copyright (c) TDE - Development IT , sierpie� 2019
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
        /// Klucz us�ugi SendGrid
        /// </summary>
        public string m_sendGridKey;

        /// <summary>
        /// Referencja do g��wnego okna
        /// </summary>
        public MainWindow m_mainWnd;

        /// <summary>
        /// Na jaki adres wys�a� mailowe powiadomienia o b��dach
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
        /// Uruchamia wysy�k� maili przez us�ug� SendGrid
        /// </summary>
        /// <param name="error">B��d do wys�ania</param>
        public void Run(string error)
        {
            if (!string.IsNullOrEmpty(m_errorsMailTo)) {
                var message = PrepareMimeMessage(error);
                SendEmail(message);
            }
        }

        /// <summary>
        /// Buduje wiadomo�� na podstawie danych wyci�gni�tych z bazy
        /// </summary>
        /// <param name="sdr">Rekord z bazy danych</param>
        /// <returns>Wiadomo�� do wys�ania</returns>
        private MimeMessage PrepareMimeMessage(string error) {
            try { 
                var msg = new MimeMessage();
                msg.Subject = "Powiadomienie o b��dzie transferu plik�w";
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
        /// Wysy�anie wiadomo�ci protoko�em SMTP
        /// </summary>
        /// <param name="msg">Wiadomo��</param>
        /// <returns>Status wysy�ki</returns>
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
