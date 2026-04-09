using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Data.Entities;

namespace TaskManager.Bussiness.Services
{
    public class AuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;

        public AuthService(UserManager<ApplicationUser> userManager,ITokenService tokenService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
        }
        public async Task<IdentityResult> RegisterAsync(ApplicationUser user,string password)
            => await _userManager.CreateAsync(user, password);

        public async Task<(string? token,DateTime expiry)> LoginAsync(string userNameOrEmail,string password)
        {
            var user = await _userManager.FindByNameAsync(userNameOrEmail)??(await _userManager.FindByEmailAsync(userNameOrEmail));
            if (user is null) return (null, DateTime.MinValue);

            var valid = await _userManager.CheckPasswordAsync(user,password);
            if(!valid) return (null, DateTime.MinValue);

            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenService.GenerateToken(user, roles);
            var jwtToken = new JwtSecurityTokenHandler().ReadToken(token);
            var expiry = jwtToken.ValidTo;
            return (token,expiry);
        }
    }
}
