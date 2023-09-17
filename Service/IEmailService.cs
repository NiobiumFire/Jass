using BelotWebApp.Models;
using System.Threading.Tasks;

namespace BelotWebApp.Service
{
    public interface IEmailService
    {
        Task SendTestEmail(UserEmailOptions userEmailOptions);
    }
}