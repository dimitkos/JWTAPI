using API.Constants;
using API.Models;
using API.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace API.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly JwtSettings _jwt;

        public UserService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IOptions<JwtSettings> jwt)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwt = jwt.Value;
        }

        public async Task<string> RegisterAsync(RegisterModel model)
        {
            var userRestration = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            var user = await _userManager.FindByEmailAsync(model.Email);

            if(user == null)
            {
                var result = await _userManager.CreateAsync(userRestration, model.Password);

                if (result.Succeeded)
                    await _userManager.AddToRoleAsync(userRestration, Authorization.defaultRole.ToString());
                return $"User Registered with username {userRestration.UserName}";
            }
            else
            {
                return $"Email {userRestration.Email } is already registered.";
            }
        }
    }
}
