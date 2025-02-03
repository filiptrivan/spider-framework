using Spider.Shared.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Emailing
{
    public class EmailingService
    {
        private readonly SmtpClient _smtpClient;

        public EmailingService()
        {
            _smtpClient = new SmtpClient(SettingsProvider.Current.SmtpHost, SettingsProvider.Current.SmtpPort)
            {
                Credentials = new NetworkCredential(SettingsProvider.Current.SmtpUser, SettingsProvider.Current.SmtpPass),
                EnableSsl = true
            };

            _smtpClient.SendCompleted += new SendCompletedEventHandler(SmtpSendCompleted);
        }

        public async Task SendVerificationEmailAsync(string toEmail, string verificationCode)
        {
            string body = GetVerificationEmailBody(verificationCode);

            MailMessage mailMessage = new MailMessage(SettingsProvider.Current.EmailSender, toEmail)
            {
                Subject = SharedTerms.EmailAccountVerificationTitle,
                Body = body,
                BodyEncoding = Encoding.UTF8, // FT: Without this, the email is not sent, and don't throw the exception
                IsBodyHtml = true
            };

            try
            {
                await _smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // log
                throw;
            }
        }

        public async Task SendEmailAsync(List<string> recipients, string subject, string body)
        {
            try
            {
                foreach (string recipient in recipients)
                {
                    MailMessage mailMessage = new MailMessage(SettingsProvider.Current.EmailSender, recipient)
                    {
                        Subject = subject,
                        Body = body,
                        BodyEncoding = Encoding.UTF8, // FT: Without this, the email is not sent, and don't throw the exception
                        IsBodyHtml = true,
                    };

                    await _smtpClient.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                // don't throw, log
                throw;
            }
        }

        public async Task SendEmailAsync(string recipient, string subject, string body)
        {
            try
            {
                MailMessage mailMessage = new MailMessage(SettingsProvider.Current.EmailSender, recipient)
                {
                    Subject = subject,
                    Body = body,
                    BodyEncoding = Encoding.UTF8, // FT: Without this, the email is not sent, and don't throw the exception
                    IsBodyHtml = true,
                };

                await _smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // don't throw, log
                throw;
            }
        }

        /// <summary>
        /// TODO FT: Test if this is working
        /// </summary>
        private void SmtpSendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled == true || e.Error != null)
            {
                throw new Exception(e.Cancelled ? "Email sedning was canceled." : "Error: " + e.Error.ToString());
            }
        }

        private string GetVerificationEmailBody(string verificationCode)
        {
            string body = $$"""
{{verificationCode}}
""";

            return body;
        }
    }
}
