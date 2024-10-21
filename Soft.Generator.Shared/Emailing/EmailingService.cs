using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.Emailing
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
                Subject = "Account verification",
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
            MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress(SettingsProvider.Current.EmailSender),
                Subject = subject,
                Body = body,
                BodyEncoding = Encoding.UTF8, // FT: Without this, the email is not sent, and don't throw the exception
                IsBodyHtml = true,
            };

            foreach (var recipient in recipients)
            {
                mailMessage.To.Add(recipient);
            }

            try
            {
                await _smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // don't throw, log
                throw;
            }
        }

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
