using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace BelotWebApp.Services.EmailService
{
    public class EmailService : IEmailSender
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
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
