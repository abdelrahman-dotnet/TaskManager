using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.DTOs.Account;
using TaskManager.Bussiness.Services;
using TaskManager.Data.Entities;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        // ValidationFilter already rejects an invalid ModelState before the action runs,
        // so the manual "if (!ModelState.IsValid)" checks were dead code and are removed here.

        [HttpPost("Register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var user = new ApplicationUser
            {
                UserName = dto.UserName,
                Email = dto.Email,
            };

            var result = await _authService.RegisterAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Register failed. UserName: {UserName}, Email: {Email}", dto.UserName, dto.Email);
                return BadRequest(result.Errors.Select(e => e.Description));
            }

            _logger.LogInformation("User registered successfully. UserName: {UserName}, UserId: {UserId}", dto.UserName, user.Id);
            return Ok("User Registered");
        }

        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var (token, expiry, error) = await _authService.LoginAsync(dto.Email, dto.Password);

            if (token == null)
            {
                _logger.LogWarning("Login failed. Email: {Email}", dto.Email);
                return Unauthorized(error);
            }

            _logger.LogInformation("Login successful. Email: {Email}", dto.Email);
            return Ok(new AuthResponseDto { AccessToken = token, ExpiresAt = expiry });
        }

        //will add refresh token end point here
    }
}
