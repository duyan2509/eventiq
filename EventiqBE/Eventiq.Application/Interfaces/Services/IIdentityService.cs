using Eventiq.Application.Dtos;

namespace Eventiq.Application.Interfaces.Services;

public interface IIdentityService
{
    Task<UserDto> GetByIdAsync(Guid id);
    Task<UserDto> CreateAsync(CreateUserDto dto);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserDto dto);
    Task<LoginResponseDto> LoginAsync(LoginDto dto);
    Task<bool> CheckEmailExists(string email);

}