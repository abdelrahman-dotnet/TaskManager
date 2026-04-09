using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.API.DTOs.User;
using TaskManager.Bussiness.Interfaces;
using TaskManager.Data.Entities;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
           _unitOfWork = unitOfWork;
           _userManager = userManager;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDto = users.Select(u => new UserReadDto
            {
                UserName = u.UserName,
                Email = u.Email,
                Id = u.Id,
                DateOfCreation = u.CreatedAt,
                IsActive = u.IsActive,
            });
            return Ok(userDto);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound("User Not Found");
            var userDto = new UserReadDto
            {
                UserName = user.UserName,
                Email = user.Email,
                Id = user.Id,
                DateOfCreation = user.CreatedAt,
                IsActive = user.IsActive,
            };
            return Ok(userDto);
        }
        // will add get my profile endpoint here
        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] UserCreateDto dto)
        {
            if(!ModelState.IsValid) 
                return BadRequest(ModelState.Values.SelectMany(e=>e.Errors).Select(m=>m.ErrorMessage));

            var existingByUserName = await _userManager.FindByNameAsync(dto.UserName);
            if (existingByUserName != null)
                return BadRequest("User Name Already Exists");
            var existingByEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (existingByEmail != null)
                return BadRequest("Email Already Exist");

            var user = new ApplicationUser
            {
                UserName = dto.UserName,
                Email = dto.Email,
                IsActive = dto.IsActive,
            };
            var result = await _userManager.CreateAsync(user);
            if(!result.Succeeded)
                return BadRequest(result.Errors.Select(e=>e.Description));
            var userRead = new UserReadDto
            {
                Id = user.Id,
                DateOfCreation = user.CreatedAt,
                Email = user.Email,
                IsActive = user.IsActive,
            };

            return CreatedAtAction(nameof(GetById),new {id = user.Id},userRead);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUser(string id ,UserUpdateDto dto)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return BadRequest("User Not Found");
            user.UserName = dto.UserName;
            user.Email = dto.Email;
            user.IsActive = dto.IsActive;
            await _userManager.UpdateAsync(user);
            return Ok("user updated succesfullt");
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return BadRequest("user not found");
            await _userManager.DeleteAsync(user);
            return Ok("deleted successfully");
        }
    }
}
