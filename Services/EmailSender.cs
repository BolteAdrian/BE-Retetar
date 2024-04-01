using Microsoft.Extensions.Options;
using static Retetar.Utils.Constants.ResponseConstants;
using Retetar.DataModels;
using MailKit.Net.Smtp;
using MimeKit;

namespace Retetar.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailConfigurationDto _emailConfig;

        public EmailSender(IOptions<EmailConfigurationDto> emailConfig)
        {
            _emailConfig = emailConfig?.Value ?? throw new ArgumentNullException(nameof(emailConfig), EMAIL.CONFIG_NULL);
        }

        /// <summary>
        /// Asynchronously sends an email to the specified recipient.
        /// </summary>
        /// <param name="email">The email address of the recipient.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="message">The body of the email.</param>
        /// <exception cref="ArgumentNullException">Thrown when the 'email' parameter is null or empty.</exception>
        /// <exception cref="Exception">Thrown when there is an error sending the email.</exception>
        public async Task SendEmailAsync(string email, string subject, string message)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentNullException(nameof(email), EMAIL.EMAIL_NULL_OR_EMPTY);
            }

            try
            {
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress(_emailConfig.FromName, _emailConfig.FromAddress));
                emailMessage.To.Add(new MailboxAddress("Recipient Name", email)); // Provide the recipient name here
                emailMessage.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = message;
                emailMessage.Body = bodyBuilder.ToMessageBody();

                using var smtpClient = new SmtpClient();
                smtpClient.Connect(_emailConfig.SmtpServer, _emailConfig.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                smtpClient.Authenticate(_emailConfig.SmtpUsername, _emailConfig.SmtpPassword);
                await smtpClient.SendAsync(emailMessage);
                smtpClient.Disconnect(true);
            }
            catch (Exception ex)
            {
                throw new Exception(EMAIL.ERROR_SENDING, ex);
            }
        }
    }
}