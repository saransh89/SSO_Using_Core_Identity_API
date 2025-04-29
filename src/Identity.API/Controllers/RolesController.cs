using Identity.Domain.Entities;
using Identity.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ITAdminOnly")]
    public class RolesController : ControllerBase
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public RolesController(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        /// <summary>
        /// Create
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return BadRequest("Role name must not be empty.");

            if (await _roleManager.RoleExistsAsync(roleName))
                return Conflict($"Role '{roleName}' already exists.");

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

            return result.Succeeded ? Ok($"Role '{roleName}' created.") : BadRequest(result.Errors);
        }

        // 🔹 Get All Roles
        [HttpGet]
        public IActionResult GetAll()
        {
            var roles = _roleManager.Roles.Select(r => r.Name).ToList();
            return Ok(roles);
        }

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        /// <returns></returns>
        [HttpPut("{oldName}")]
        public async Task<IActionResult> Rename(string oldName, [FromBody] string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                return BadRequest("New role name must not be empty.");

            var role = await _roleManager.FindByNameAsync(oldName);
            if (role == null) return NotFound($"Role '{oldName}' not found.");

            role.Name = newName;
            var result = await _roleManager.UpdateAsync(role);

            return result.Succeeded ? Ok($"Role renamed to '{newName}'.") : BadRequest(result.Errors);
        }

        
        /// <summary>
        /// Delete
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        [HttpDelete("{roleName}")]
        public async Task<IActionResult> Delete(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null) return NotFound($"Role '{roleName}' not found.");

            var result = await _roleManager.DeleteAsync(role);

            return result.Succeeded ? Ok($"Role '{roleName}' deleted.") : BadRequest(result.Errors);
        }

        [HttpPost("assign-role-to-user")]
        public async Task<IActionResult> AssignRole([FromBody] RoleAssignmentDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.UserId) || string.IsNullOrEmpty(dto.RoleName))
            {
                return BadRequest("User ID and Role Name are required.");
            }

            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
            {
                return NotFound($"User with ID {dto.UserId} not found.");
            }

            var roleExists = await _roleManager.RoleExistsAsync(dto.RoleName);
            if (!roleExists)
            {
                return BadRequest($"Role {dto.RoleName} does not exist.");
            }

            var result = await _userManager.AddToRoleAsync(user, dto.RoleName);
            if (result.Succeeded)
            {
                return Ok(new { message = $"User {user.UserName} has been successfully assigned to {dto.RoleName} role." });
            }

            // Log each error in the result for better debugging
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { message = "Failed to assign role.", errors });
        }
    }
}
