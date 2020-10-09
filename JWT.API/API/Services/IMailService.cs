using API.Models;
using System.Threading.Tasks;

namespace API.Services
{
    public interface IMailService
    {
        Task SendEmailAsync(MailRequest mailRequest);
        Task SendCustomEmailAsync(RegisterModel request, string subject);
    }
}
