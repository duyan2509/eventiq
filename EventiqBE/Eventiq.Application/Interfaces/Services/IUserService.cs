using Eventiq.Application.Dtos;

namespace Eventiq.Application.Interfaces.Services;

public interface IUserService
{
    Task<UserDto> GetByIdAsync(Guid id);

    Task<UserDto> CreateAsync(CreateUserDto createUserDto);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserDto updateUserDto);
    Task<LoginResponseDto> LoginAsync(LoginDto loginDto);
}