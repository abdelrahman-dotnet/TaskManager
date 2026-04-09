using Microsoft.AspNetCore.Http;
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

        [HttpPost]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if(!ModelState.IsValid)
                return BadRequest(ModelState.Values.SelectMany(v=>v.Errors.Select(e=>e.ErrorMessage)).ToList());
            var user = new ApplicationUser
            {
                UserName = dto.UserName,
                Email = dto.Email,
            };
            var result = await _authService.RegisterAsync(user,dto.Password);
            return Ok("User Registered");
        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            if(!ModelState.IsValid) 
                return BadRequest(ModelState.Values.SelectMany(v=>v.Errors.Select(e=>e.ErrorMessage)).ToList());
            var(token,expiry) = await _authService.LoginAsync(dto.UserName,dto.Password);
            if (token == null)
                return Unauthorized();
            return Ok(new AuthResponseDto { AccessToken = token , ExpiresAt = expiry });
        }
    }
}
