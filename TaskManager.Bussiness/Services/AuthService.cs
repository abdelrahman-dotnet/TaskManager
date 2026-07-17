using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using TaskManager.Data.Entities;

namespace TaskManager.Bussiness.Services
{
    public class AuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }

        public async Task<IdentityResult> RegisterAsync(ApplicationUser user, string password)
        {
            // FIX: CreatedAt existed on the entity but was never actually set.
            user.CreatedAt = DateTime.UtcNow;

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                // Assign a default role so every new user has a well-defined
                // permission set from day one.
                await _userManager.AddToRoleAsync(user, "User");
            }
            return result;
        }

        // FIX: return shape now carries an "error" so the caller (controller)
        // can tell "wrong credentials" apart from "account locked" /
        // "account deactivated" and respond accordingly (still without
        // leaking whether the username or the password was wrong).
        public async Task<(string? Token, DateTime Expiry, string? Error)> LoginAsync(
            string userNameOrEmail, string password)
        {
            var user = await _userManager.FindByNameAsync(userNameOrEmail)
                       ?? await _userManager.FindByEmailAsync(userNameOrEmail);

            if (user is null)
                return (null, DateTime.MinValue, "Invalid username/email or password.");

            // FIX: IsActive existed on ApplicationUser but was never checked,
            // meaning deactivated users could still log in.
            if (!user.IsActive)
                return (null, DateTime.MinValue, "This account has been deactivated.");

            // FIX: previously used _userManager.CheckPasswordAsync directly,
            // which bypasses ASP.NET Identity's account-lockout / brute-force
            // protection entirely. SignInManager.CheckPasswordSignInAsync
            // tracks failed attempts and enforces lockout automatically
            // (lockout must be enabled on ApplicationUser/IdentityOptions).
            var signInResult = await _signInManager.CheckPasswordSignInAsync(
                user, password, lockoutOnFailure: true);

            if (signInResult.IsLockedOut)
                return (null, DateTime.MinValue,
                    "Account locked due to multiple failed attempts. Please try again later.");

            if (!signInResult.Succeeded)
                return (null, DateTime.MinValue, "Invalid username/email or password.");

            // FIX: LastLoginAt existed on the entity but was never updated.
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var token = await _tokenService.GenerateTokenAsync(user, roles);
            var jwtToken = new JwtSecurityTokenHandler().ReadToken(token);
            var expiry = jwtToken.ValidTo;

            return (token, expiry, null);
        }
    }
}
