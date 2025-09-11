using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Services;

namespace Eventiq.Application.Services;

public class UserService(IIdentityService identityService, IEmailService emailService) : IUserService
{
    private readonly IIdentityService _identityService = identityService;
    private readonly IEmailService _emailService = emailService;
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

    public async Task RequestResetPasswordAsync(string email)
    {
        var token = await _identityService.GeneratePasswordResetTokenAsync(email);
        await _emailService.SendEmailAsync(email,"Reset Password",$"Using this code when confirm new password reset email address: <b>{token}</b>");
    }

    public async Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
    {
        await _identityService.ChangePasswordAsync(userId, oldPassword, newPassword);
    }

    public async Task ConfirmResetPasswordAsync(string email, string code, string newPassword)
    {
        await _identityService.ResetPasswordAsync(email, code, newPassword);
    }
}