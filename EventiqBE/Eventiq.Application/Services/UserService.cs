using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Services;

namespace Eventiq.Application.Services;

public class UserService(IIdentityService identityService) : IUserService
{
    private readonly IIdentityService _identityService = identityService;

    public async Task<UserDto> GetByIdAsync(Guid id)
    {
        return await _identityService.GetByIdAsync(id);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto createUserDto)
    {
        var exists = await _identityService.CheckEmailExists(createUserDto.Email);
        if(exists)
            throw new Exception("Email already exists");
        return await _identityService.CreateAsync(createUserDto);
    }

    public Task<UserDto> UpdateAsync(Guid id, UpdateUserDto updateUserDto)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string id)
    {
        throw new NotImplementedException();
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
    {
        var result = await  _identityService.LoginAsync(loginDto);
        return result;
    }
}