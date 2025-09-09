using System.Security.Claims;
using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Services;
using Microsoft.AspNetCore.Identity;

namespace Eventiq.Infrastructure.Identity;

public class IdentityService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    RoleManager<IdentityRole> roleManager,
    IJwtService jwtService)
    : IIdentityService
{
    public async Task<UserDto> GetByIdAsync(Guid id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user == null) 
            throw new Exception("User not found");

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            Username = user.UserName,
        };
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        var user = new ApplicationUser
        {
            UserName = $"{dto.FirstName} {dto.LastName}",
            Email = dto.Email,
        };

        var result = await userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            Username = user.UserName,
        };
    }

    public Task<UserDto> UpdateAsync(Guid id, UpdateUserDto dto)
    {
        throw new NotImplementedException();
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            throw new Exception($"User not found with email {dto.Email}");

        var result = await signInManager.CheckPasswordSignInAsync(user, dto.Password, false);

        if (!result.Succeeded)
            throw new Exception("Invalid login attempt");

        var roles = await userManager.GetRolesAsync(user);
        var roleClaims = new List<Claim>();
        foreach (var role in roles)
        {
            var r = await roleManager.FindByNameAsync(role);
            var claims = await roleManager.GetClaimsAsync(r!);
            roleClaims.AddRange(claims);
        }

        var permissions = roleClaims
            .Where(c => c.Type == "Permission")
            .Select(c => c.Value)
            .Distinct()
            .ToList();

        var token = jwtService
                .GenerateToken(Guid.Parse(user.Id), 
                user.Email!,
                roles.ToList(),
                permissions);

        return new LoginResponseDto { Token = token };
    }

    public async Task<bool> CheckEmailExists(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user != null;
    }
}
