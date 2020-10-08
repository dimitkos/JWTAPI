using API.Constants;
using API.Contexts;
using API.Entities;
using API.Models;
using API.Settings;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace API.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMapper _mapper;
        private readonly JwtSettings _jwt;

        public UserService(UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IMapper mapper,
            ApplicationDbContext context,
            IOptions<JwtSettings> jwt)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
            _context = context;
            _jwt = jwt.Value;
        }

        public async Task<string> AddRoleAsync(AddRoleModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
                return $"No Accounts Registered with {model.Email}.";

            var isCorrectPassword = await _userManager.CheckPasswordAsync(user, model.Password);

            if (!isCorrectPassword)
                return $"Incorrect Credentials for user {user.Email}.";

            var roleExists = Enum.GetNames(typeof(Authorization.Roles)).Any(x => x.ToLower() == model.Role.ToLower());

            if (!roleExists)
                return $"Role {model.Role} not found.";

            var validRole = Enum.GetValues(typeof(Authorization.Roles)).Cast<Authorization.Roles>()
                    .Where(x => x.ToString().ToLower() == model.Role.ToLower())
                    .FirstOrDefault();

            await _userManager.AddToRoleAsync(user, validRole.ToString());

            return $"Added {model.Role} to user {model.Email}.";
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
                var token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
                var roles = await _userManager.GetRolesAsync(user);//.ConfigureAwait(false);

                var (refreshToken, refreshTokenExpiration) = await GetRefreshToken(user);

                return new AuthenticationModel
                {
                    IsAuthenticated = true,
                    Token = token,
                    Email = user.Email,
                    UserName = user.UserName,
                    Roles = roles.ToList(),
                    RefreshToken = refreshToken,
                    RefreshTokenExpiration = refreshTokenExpiration
                };
            }

            return new AuthenticationModel
            {
                IsAuthenticated = false,
                Message = $"Incorrect Credentials for user {user.Email}."
            };
        }

        public async Task<string> RegisterAsync(RegisterModel model)
        {
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

        public async Task<AuthenticationModel> RefreshTokenAsync(string token)
        {
            var user = await _context.Users.SingleOrDefaultAsync(x => x.RefreshTokens.Any(t => t.Token == token));

            if (user == null)
                return new AuthenticationModel
                {
                    IsAuthenticated = false,
                    Message = $"Token did not match any users.",
                };

            var refreshToken = user.RefreshTokens.Single(t => t.Token == token);

            if (!refreshToken.IsActive)
                return new AuthenticationModel
                {
                    IsAuthenticated = false,
                    Message = $"Token Not Active.",
                };

            //Revoke Current Refresh Token
            refreshToken.Revoked = DateTime.UtcNow;

            //Generate new Refresh Token and save to Database
            var newRefreshToken = CreateRefreshToken();
            user.RefreshTokens.Add(newRefreshToken);
            _context.Update(user);
            await _context.SaveChangesAsync();

            JwtSecurityToken jwtSecurityToken = await CreateJwtToken(user);
            var newToken = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);

            var roles = await _userManager.GetRolesAsync(user);

            return new AuthenticationModel
            {
                IsAuthenticated = true,
                UserName = user.UserName,
                Email = user.Email,
                Token = newToken,
                Roles = roles.ToList(),
                RefreshToken = newRefreshToken.Token,
                RefreshTokenExpiration = newRefreshToken.Expires
            };
        }


        public async Task<IEnumerable<RefreshToken>> GetById(string id)
        {
            var user = await _context.Users.FindAsync(id);

            return user.RefreshTokens;
        }

        private async Task<JwtSecurityToken> CreateJwtToken(ApplicationUser user)
        {
            var claims = await GetClaims(user);

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            return new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.DurationInMinutes),
                signingCredentials: signingCredentials);
        }

        private async Task<IEnumerable<Claim>> GetClaims(ApplicationUser user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            var roleClaims = roles.Select(role => new Claim("roles", role)).ToList();

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("uid", user.Id)
            }
            .Union(userClaims)
            .Union(roleClaims);

            return claims;
        }

        private async Task<(string refreshToken, DateTime refreshTokenExpiration)> GetRefreshToken(ApplicationUser user)
        {
            string refreshToken = string.Empty;
            DateTime refreshTokenExpiration = default;

            if (user.RefreshTokens.Any(a => a.IsActive))
            {
                var activeRefreshToken = user.RefreshTokens.Where(a => a.IsActive == true).FirstOrDefault();
                refreshToken = activeRefreshToken.Token;
                refreshTokenExpiration = activeRefreshToken.Expires;
            }
            else
            {
                var refreshTokenCreated = CreateRefreshToken();

                refreshToken = refreshTokenCreated.Token;
                refreshTokenExpiration = refreshTokenCreated.Expires;

                user.RefreshTokens.Add(refreshTokenCreated);
                _context.Update(user);
                await _context.SaveChangesAsync();
            }

            return (refreshToken, refreshTokenExpiration);
        }

        private RefreshToken CreateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var generator = new RNGCryptoServiceProvider();
            generator.GetBytes(randomNumber);
            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomNumber),
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow
            };
        }
    }
}
