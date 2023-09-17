namespace BelotWebApp.Services.EmailService
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toAddress, string subject, string message);
    }
}
