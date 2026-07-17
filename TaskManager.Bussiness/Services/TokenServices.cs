using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskManager.Bussiness.Config;
using TaskManager.Data.Context;
using TaskManager.Data.Entities;

namespace TaskManager.Bussiness.Services
{
    public class TokenServices : ITokenService
    {
        private readonly JwtSettings _jwt;
        private readonly AppDbContext _context;

        public TokenServices(JwtSettings jwt, AppDbContext context)
        {
            _jwt = jwt;
            _context = context;
        }

        public async Task<string> GenerateTokenAsync(ApplicationUser user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var r in roles)
                claims.Add(new Claim(ClaimTypes.Role, r));

            // Load the permissions granted to the user's roles and add them as
            // "permission" claims so the Permission-based Authorization Policies
            // (registered in Program.cs against Permissions.All) actually work.
            //
            // FIX: this ran as a synchronous ".ToList()" DB call inside a
            // synchronous method invoked from an async caller (AuthService) ->
            // blocking a thread-pool thread on every login. Now awaited
            // properly with ToListAsync().
            var permissionNames = await _context.Roles
                .Where(role => roles.Contains(role.Name))
                .SelectMany(role => role.RolePermissions)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToListAsync();

            foreach (var permission in permissionNames)
                claims.Add(new Claim("permission", permission));

            var authSignInkey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var cred = new SigningCredentials(authSignInkey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                expires: DateTime.UtcNow.AddMinutes(_jwt.DurationInMinutes),
                claims: claims,
                signingCredentials: cred);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
