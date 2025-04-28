using Identity.Domain.Entities;
using Identity.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Application.Contracts
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterUserDto model);
        Task<AuthResponseDto> LoginAsync(LoginDto model);
        Task<List<ApplicationUser>> GetAllUsersAsync();
        Task<ApplicationUser> FindUserAsync(string? email, string? phoneNumber);
        Task<AuthResponseDto> UpdateUserAsync(string id, UpdateUserDto model);
        Task<AuthResponseDto> DeleteUserAsync(string id);
    }
}
