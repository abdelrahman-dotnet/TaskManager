using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.API.DTOs.Role;
using TaskManager.Bussiness.Interfaces;
using TaskManager.Data.Entities;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly RoleManager<Role> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public RoleController(RoleManager<Role> roleManager,UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var dto = roles.Select(r => new RoleReadDto
            {
                Description = r.Description,
                Name = r.Name,
            });
            return Ok(dto);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
             
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();
            var roleDto = new RoleReadDto
            {
                Description = role.Description,
                Name = role.Name,
            };
            return Ok(roleDto); 
        }
        [HttpPost]
        public async Task<IActionResult> CreateRole(RoleCreateAndUpdateDto dto)
        {
            if(!ModelState.IsValid)
                return BadRequest(ModelState.Values.SelectMany(e=>e.Errors).Select(e=>e.ErrorMessage));
            var existingRole = await _roleManager.FindByNameAsync(dto.Name);
            if (existingRole != null)
                return BadRequest("Role Name Already Exists");
            var role = new Role
            {
                Name = dto.Name,
                Description = dto.Description,
            };
            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e=>e.Description));
            var roleRead = new RoleCreateAndUpdateDto
            {
                Description = role.Description,
                Name = dto.Name,
            };
            return CreatedAtAction(nameof(GetById), new {id=role.Id},roleRead);
        }

        [HttpPut]
        public async Task<IActionResult> Update(string id,RoleCreateAndUpdateDto dto)
        {
            if(!ModelState.IsValid)
                return BadRequest(ModelState.Values.SelectMany(e=>e.Errors.Select(e=>e.ErrorMessage)));
            var existing = await _roleManager.FindByIdAsync(id);
            if (existing == null)
                return BadRequest("Role Not Found");
            existing.Name = dto.Name;
            existing.Description = dto.Description;
            var result = await _roleManager.UpdateAsync(existing);
            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));
            var roleDto = new RoleReadDto
            {
                Name = existing.Name,
                Description = existing.Description,
            };
            return Ok(roleDto);
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();
            await _roleManager.DeleteAsync(role);
            return Ok("Role Deleted Succesfully");
        }
        [HttpPost("{Assign}")]
        public async Task<IActionResult> AssignRoleToUser(AssignRoleDto dto)
        {
            if(!ModelState.IsValid)
                return BadRequest(ModelState.Values.SelectMany(m=>m.Errors.Select(e=>e.ErrorMessage)));
            // check user
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
                return NotFound();
            // check role
            var role = await _roleManager.FindByNameAsync(dto.RoleName);
            // or
            //var rolee = await _roleManager.RoleExistsAsync(dto.RoleName);
            if (role == null)
                return NotFound();
            // check if user in role
            var UserinRole = await _userManager.IsInRoleAsync(user, dto.RoleName);
            if (UserinRole)
                return BadRequest($"User {user.UserName} Already Has Role {role.Name}");
            var result = await _userManager.AddToRoleAsync(user,dto.RoleName);
            if(!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));
            //will edit it soon
            var resultDto = new AssignRoleDto
            {
                UserId = user.Id,
                RoleName = dto.RoleName,
            };
            return Ok(resultDto);
        }
        [HttpDelete("{RemoveRole}")]
        public async Task<IActionResult> RemoveUserRole(string userId,string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
                return NotFound();
            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description).ToList());
            return Ok($"Role {roleName} Removed from user {user.UserName}");
        }
        [HttpGet("{GetUserRoles}")]
        public async Task<IActionResult> GetUserRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)   
                return NotFound();
            var roles = await _userManager.GetRolesAsync(user);
            return Ok(roles);
        }
    }
}
