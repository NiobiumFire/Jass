using BelotWebApp.EmailTemplates;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace BelotWebApp.Services.EmailService
{
    public class EmailService : IEmailSender
    {
        private readonly IConfiguration _config;

        public record EmailTemplateInfo(string Subject, string EmailTemplateFileName);

        private static readonly Dictionary<EmailTemplate, EmailTemplateInfo> EmailTemplateMap = new()
        {
            { EmailTemplate.ConfirmEmail, new EmailTemplateInfo("Confirm your email", "ConfirmEmail.html") },
            { EmailTemplate.ConfirmEmailChange, new EmailTemplateInfo("Confirm email change", "ConfirmEmailChange.html") }
        };

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toAddress, EmailTemplate emailTemplate, Dictionary<string, string> placeholders)
        {
            if (!EmailTemplateMap.TryGetValue(emailTemplate, out var info))
            {
                throw new ArgumentOutOfRangeException(nameof(emailTemplate));
            }

            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", info.EmailTemplateFileName);
            string message = await File.ReadAllTextAsync(templatePath);

            foreach (var placeholder in placeholders)
            {
                message = message.Replace("{{" + placeholder.Key + "}}", placeholder.Value);
            }

            await SendEmailAsync(toAddress, info.Subject, message);
        }

        public async Task SendEmailAsync(string toAddress, string subject, string message)
        {
            MailMessage mailMessage = new()
            {
                Subject = subject,
                Body = message,
                From = new MailAddress(_config["SMTPConfiguration:SenderAddress"]!, _config["SMTPConfiguration:SenderDisplayName"]),
                IsBodyHtml = true
            };
            mailMessage.To.Add(toAddress);

            try
            {
                using (SmtpClient client = new(_config["SMTPConfiguration:Host"], int.Parse(_config["SMTPConfiguration:Port"]!))
                {
                    Credentials = new NetworkCredential(_config["SMTPConfiguration:SenderAddress"], _config["SMTPConfiguration:EmailAppPassword"]),
                    EnableSsl = true
                })
                {
                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Email send failure", ex);
            }
        }
    }
}
