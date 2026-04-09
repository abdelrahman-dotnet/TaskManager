using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Bussiness.Config;
using TaskManager.Data.Entities;

namespace TaskManager.Bussiness.Services
{
    public class TokenServices : ITokenService
    {
        private readonly JwtSettings _jwt;

        public TokenServices(JwtSettings jwt)
        {
            _jwt = jwt;
        }
        public string GenerateToken(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                new Claim(ClaimTypes.Email,user.Email??string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
            };
            foreach (var r in roles)
                claims.Add(new Claim(ClaimTypes.Role, r));
            var authSignInkey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var cred = new SigningCredentials(authSignInkey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer : _jwt.Issuer,
                audience : _jwt.Audience,
                expires : DateTime.UtcNow.AddMinutes(_jwt.DurationInMinutes),
                claims : claims,
                signingCredentials : cred);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
