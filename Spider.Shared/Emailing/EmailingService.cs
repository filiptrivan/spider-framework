using Serilog;
using Spider.Shared.Exceptions;
using Spider.Shared.Helpers;
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
            _smtpClient = Helper.GetSmtpClient();
        }

        public async Task SendVerificationEmailAsync(string toEmail, string verificationCode)
        {
            string body = GetVerificationEmailBody(verificationCode);

            using (MailMessage mailMessage = new MailMessage(SettingsProvider.Current.EmailSender, toEmail)
            {
                Subject = SharedTerms.EmailAccountVerificationTitle,
                Body = body,
                BodyEncoding = Encoding.UTF8, // FT: Without this, the email is not sent, and don't throw the exception
                IsBodyHtml = true
            })
            {
                await _smtpClient.SendMailAsync(mailMessage); // https://stackoverflow.com/questions/11120350/how-to-check-programmatically-if-an-email-is-existing-or-not
            }
        }

        public async Task SendEmailAsync(string recipient, string subject, string body, string from = null)
        {
            using (MailMessage mailMessage = new MailMessage(from ?? SettingsProvider.Current.EmailSender, recipient)
            {
                Subject = subject,
                Body = body,
                BodyEncoding = Encoding.UTF8, // FT: Without this, the email is not sent, and don't throw the exception
                IsBodyHtml = true,
            })
            {
                await _smtpClient.SendMailAsync(mailMessage);
            }
        }

        public async Task SendEmailAsync(List<string> recipients, string subject, string body)
        {
            foreach (string recipient in recipients)
            {
                using (MailMessage mailMessage = new MailMessage(SettingsProvider.Current.EmailSender, recipient)
                {
                    Subject = subject,
                    Body = body,
                    BodyEncoding = Encoding.UTF8, // FT: Without this, the email is not sent, and don't throw the exception
                    IsBodyHtml = true,
                })
                {
                    await _smtpClient.SendMailAsync(mailMessage);
                }
            }
        }

        public async Task SendEmailFromBackgroundJobAsync(string recipient, string subject, string body)
        {
            using (MailMessage mailMessage = new MailMessage(SettingsProvider.Current.EmailSender, recipient)
            {
                Subject = subject,
                Body = body,
                BodyEncoding = Encoding.UTF8, // FT: Without this, the email is not sent, and don't throw the exception
                IsBodyHtml = true,
            })
            {
                try
                {
                    await _smtpClient.SendMailAsync(mailMessage);
                }
                catch (Exception ex)
                {
                    // FT: We need to log because exception will not get into api global error handler from the background job
                    Log.Error(
                        ex,
                        "We failed to send an email to the recipient: {recipient};",
                        recipient
                    );

                    throw;
                }
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
