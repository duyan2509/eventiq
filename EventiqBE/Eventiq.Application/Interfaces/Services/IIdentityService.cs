using Eventiq.Application.Dtos;

namespace Eventiq.Application.Interfaces.Services;

public interface IIdentityService
{
    Task<UserDto> GetByIdAsync(Guid id);
    Task<UserDto> GetByEmailAsync(string email);
    Task<UserDto> CreateAsync(CreateUserDto dto);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserDto dto);
    Task<LoginResponseDto> LoginAsync(LoginDto dto);
    
    Task<bool> CheckEmailExists(string email);
    Task<string> GeneratePasswordResetTokenAsync(string email);
    Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);
    Task ResetPasswordAsync(string email, string code, string newPassword);
    Task<bool> VerifyPasswordAsync(Guid userId, string password);
    /// <summary>
    /// assign organization role and return new jwt
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task AssignOrgRole(Guid userId);
    Task RemoveOrgRole(Guid userId);
    Task<string> GenerateNewJwt(Guid userId);
    Task<List<Guid>> GetUserOrgsAsync(Guid userId);
    Task BanUserAsync(Guid userId, Guid adminUserId, string? banReason);
    Task UnbanUserAsync(Guid userId, Guid adminUserId);
    Task<PaginatedResult<AdminUserDto>> GetUsersAsync(int page, int size, string? emailSearch);
    Task<bool> IsUserBannedAsync(Guid userId);
}