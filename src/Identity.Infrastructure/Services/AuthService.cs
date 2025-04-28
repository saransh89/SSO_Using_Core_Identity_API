using Identity.Application.Contracts;
using Identity.Domain.Entities;
using Identity.Shared.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public AuthService(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto model)
    {
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Phone = model.Phone,
            Address = model.Address,
            Department = model.Department
        };
        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            return new AuthResponseDto { IsSuccessful = false, Errors = result.Errors.Select(e => e.Description) };
        }

        var token = await _tokenService.GenerateTokenAsync(user);
        return new AuthResponseDto { IsSuccessful = true, Token = token };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
        {
            return new AuthResponseDto { IsSuccessful = false, Errors = new[] { "Invalid credentials." } };
        }

        var token = await _tokenService.GenerateTokenAsync(user);
        return new AuthResponseDto { IsSuccessful = true, Token = token };
    }

    public async Task<List<ApplicationUser>> GetAllUsersAsync()
    {
        // Retrieve all users that are not marked as deleted
        var users = await _userManager.Users.ToListAsync();
        return users;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="email"></param>
    /// <param name="phoneNumber"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<ApplicationUser> FindUserAsync(string? email, string? phoneNumber)
    {
        if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phoneNumber))
        {
            throw new ArgumentException("Either email or phone number must be provided.");
        }

      var  query = _userManager.Users.Where(u =>
            (string.IsNullOrEmpty(email) || u.Email == email) &&
            (string.IsNullOrEmpty(phoneNumber) || u.PhoneNumber == phoneNumber || u.Phone == phoneNumber)
        );

        var user = await query.FirstOrDefaultAsync();
        return user!;
    }



    public async Task<AuthResponseDto> UpdateUserAsync(string id, UpdateUserDto model)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

        if (user == null)
        {
            return new AuthResponseDto
            {
                IsSuccessful = false,
                Message = ["User not found."]
            };
        }

        // Update the user fields
        if (!string.IsNullOrEmpty(model.FirstName))
            user.FirstName = model.FirstName;

        if (!string.IsNullOrEmpty(model.LastName))
            user.LastName = model.LastName;

        if (!string.IsNullOrEmpty(model.Phone))
            user.Phone = model.Phone;

        if (!string.IsNullOrEmpty(model.Address))
            user.Address = model.Address;

        if (model.IsActive.HasValue)
            user.IsActive = model.IsActive.Value;

        // Update the user
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return new AuthResponseDto
            {
                IsSuccessful = false,
                Message = [$"Failed to update user: {errors}"]
            };
        }

        return new AuthResponseDto
        {
            IsSuccessful = true,
            Message = ["User updated successfully."]
        };
    }

    public async Task<AuthResponseDto> DeleteUserAsync(string id)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

        if (user == null)
        {
            return new AuthResponseDto
            {
                IsSuccessful = false,
                Message = new[] { "User not found." }
            };
        }

        // Mark the user as soft deleted
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        // user.DeletedByUserId = currentlyLoggedInUserId; // Optional if you have context info

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);

            return new AuthResponseDto
            {
                IsSuccessful = false,
                Message = errors
            };
        }

        return new AuthResponseDto
        {
            IsSuccessful = true,
            Message = new[] { "User deleted successfully." }
        };
    }

}
