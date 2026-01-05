using Eventiq.Application.Dtos;

namespace Eventiq.Application.Interfaces.Services;

public interface IUserService
{
    Task<UserDto> GetByIdAsync(Guid id);

    Task<UserDto> CreateAsync(CreateUserDto createUserDto);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserDto updateUserDto);
    Task<LoginResponseDto> LoginAsync(LoginDto loginDto);
    Task RequestResetPasswordAsync(string email);
    Task ChangePasswordAsync(Guid userId, string oldPassword,  string newPassword);
    Task ConfirmResetPasswordAsync(string email, string code, string newPassword);
    Task BanUserAsync(Guid userId, Guid adminUserId, string? banReason);
    Task UnbanUserAsync(Guid userId, Guid adminUserId);
    Task<PaginatedResult<AdminUserDto>> GetUsersAsync(int page, int size, string? emailSearch);
}