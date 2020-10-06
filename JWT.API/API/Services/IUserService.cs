using API.Models;
using System.Threading.Tasks;

namespace API.Services
{
    public interface IUserService
    {
        Task<string> RegisterAsync(RegisterModel model);
    }
}
