using Eventiq.Application.Dtos;

namespace Eventiq.Application.Interfaces.Services;

public interface IIdentityService
{
    Task<UserDto> GetByIdAsync(Guid id);
    Task<UserDto> CreateAsync(CreateUserDto dto);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserDto dto);
    Task<LoginResponseDto> LoginAsync(LoginDto dto);
    
    Task<bool> CheckEmailExists(string email);
    Task<string> GeneratePasswordResetTokenAsync(string email);
    Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);
    Task ResetPasswordAsync(string email, string code, string newPassword);
    /// <summary>
    /// assign organization role and return new jwt
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task AssignOrgRole(Guid userId);
    Task RemoveOrgRole(Guid userId);
    Task<string> GenerateNewJwt(Guid userId);
    Task<List<Guid>> GetUserOrgsAsync(Guid userId);
}