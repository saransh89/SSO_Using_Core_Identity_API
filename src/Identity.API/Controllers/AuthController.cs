using Identity.Application.Contracts;
using Identity.Domain.Entities;
using Identity.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto model)
    {
        if (model.Password != model.ConfirmPassword)
        {
            return BadRequest(new { message = "Passwords do not match." });
        }

        var result = await _authService.RegisterAsync(model);
        if (!result.IsSuccessful)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        var result = await _authService.LoginAsync(model);
        if (!result.IsSuccessful)
            return Unauthorized(result);

        return Ok(result);
    }

    // GET: api/auth/users
    [HttpGet("GetAllUsers")]
    [Authorize(Roles = "Admin")]  // Only allow Admin to access
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _authService.GetAllUsersAsync();
        return Ok(users);
    }


    // Find a user by email or phone
    [HttpGet("GetUserByEmailOrPhoneNumber")]
    [Authorize(Roles = "Admin")]  // Only allow Admin to access
    public async Task<IActionResult> FindUser([FromQuery] string? email, [FromQuery] string? phoneNumber)
    {
        var user = await _authService.FindUserAsync(email, phoneNumber);
        if (user == null)
            return NotFound("User not found.");

        return Ok(user);
    }

    // Update a user
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]  // Only allow Admin to access
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto model)
    {
        var response = await _authService.UpdateUserAsync(id, model);
        if (!response.IsSuccessful) // Assuming your AuthResponseDto has an IsSuccessful flag
            return BadRequest(response.Message);

        return Ok(response);
    }

    // Delete a user
    [HttpDelete("{id}")]
    [Authorize(Policy = "ITAdminOnly")]
    //[Authorize(Roles = "Admin")]  // Only allow Admin to access
    public async Task<IActionResult> DeleteUser(string id)
    {
        var response = await _authService.DeleteUserAsync(id);
        if (!response.IsSuccessful)
            return BadRequest(response.Message);

        return Ok(response);
    }
}
