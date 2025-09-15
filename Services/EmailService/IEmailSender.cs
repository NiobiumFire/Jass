using BelotWebApp.EmailTemplates;

namespace BelotWebApp.Services.EmailService
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toAddress, EmailTemplate emailTemplate, Dictionary<string, string> placeholders);
        Task SendEmailAsync(string toAddress, string subject, string message);
    }
}
