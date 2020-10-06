using API.Constants;
using API.Models;
using API.Settings;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace API.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMapper _mapper;
        private readonly JwtSettings _jwt;

        public UserService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IMapper mapper, IOptions<JwtSettings> jwt)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
            _jwt = jwt.Value;
        }

        public async Task<AuthenticationModel> GetTokenAsync(TokenRequestModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return new AuthenticationModel
                {
                    IsAuthenticated = false,
                    Message = $"No Accounts Registered with {model.Email}"
                };
            }

            var isCorrectPassword = await _userManager.CheckPasswordAsync(user, model.Password);

            if (isCorrectPassword)
            {
                JwtSecurityToken jwtSecurityToken = await CreateJwtToken(user);
                //todo

                return new AuthenticationModel
                {
                    IsAuthenticated = true,
                    //todo
                };
            }

            return new AuthenticationModel
            {
                //todo
            };
        }

        public async Task<string> RegisterAsync(RegisterModel model)
        {
            //var userRestration = new ApplicationUser
            //{
            //    UserName = model.Username,
            //    Email = model.Email,
            //    FirstName = model.FirstName,
            //    LastName = model.LastName
            //};

            var userRestration = _mapper.Map<ApplicationUser>(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
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

        private async Task<JwtSecurityToken> CreateJwtToken(ApplicationUser user)
        {
            return new JwtSecurityToken
            {

            };
        }
    }
}
