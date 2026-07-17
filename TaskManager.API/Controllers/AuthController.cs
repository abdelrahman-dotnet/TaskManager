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

        public AuthController(AuthService authService)
        {
            _authService = authService;
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
                return BadRequest(result.Errors.Select(e => e.Description));

            return Ok("User Registered");
        }

        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var (token, expiry, error) = await _authService.LoginAsync(dto.Email, dto.Password);

            if (token == null)
                return Unauthorized(error);

            return Ok(new AuthResponseDto { AccessToken = token, ExpiresAt = expiry });
        }

        //will add refresh token end point here
    }
}