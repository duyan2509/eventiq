using System.Security.Claims;
using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Application.Interfaces.Services;
using Eventiq.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Eventiq.Infrastructure.Identity;

public class IdentityService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    RoleManager<IdentityRole> roleManager,
    IJwtService jwtService,
    IStaffRepository staffRepository,
    IEventItemRepository eventItemRepository,
    ITransferRequestRepository transferRequestRepository)
    : IIdentityService
{
    private readonly IJwtService _jwtService = jwtService;
    private readonly IStaffRepository _staffRepository = staffRepository;
    private readonly IEventItemRepository _eventItemRepository = eventItemRepository;
    private readonly ITransferRequestRepository _transferRequestRepository = transferRequestRepository;

    public async Task<UserDto> GetByIdAsync(Guid id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user == null) 
            throw new Exception("User not found");
        var roles = await userManager.GetRolesAsync(user);
        if(roles==null)
            throw new Exception("Role not found");
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            Username = user.UserName,
            Roles =   roles.ToList(),
            IsBanned = user.IsBanned
        };
    }

    public async Task<UserDto> GetByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null) 
            throw new KeyNotFoundException($"User not found with email: {email}");
        var roles = await userManager.GetRolesAsync(user);
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            Username = user.UserName,
            Roles = roles?.ToList() ?? new List<string>(),
            IsBanned = user.IsBanned
        };
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
        };

        var result = await userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        var roleResult = await userManager.AddToRolesAsync(user, [AppRoles.User]);
        if (!roleResult.Succeeded)
            throw new Exception(string.Join(", ", roleResult.Errors.Select(e => e.Description)));

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            Username = user.UserName,
            IsBanned = user.IsBanned
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

        if (user.IsBanned)
            throw new Exception("Your account has been banned. Please contact support for more information.");

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
        var securityStamp = await userManager.GetSecurityStampAsync(user);

        var rolesList = roles.ToList();
        var now = DateTime.UtcNow;
        var staff = await _staffRepository.GetCurrentShiftAsync(Guid.Parse(user.Id), now);
        if (staff != null && !rolesList.Contains(AppRoles.Staff))
        {
            var evnt = staff.Event;
            if (evnt != null)
            {
                var eventItems = await _eventItemRepository.GetAllByEventIdAsync(evnt.Id);
                var activeEventItem = eventItems
                    .Where(ei => ei.Start <= now && ei.End >= now)
                    .OrderByDescending(ei => ei.Start)
                    .FirstOrDefault();
                if (activeEventItem != null)
                {
                    rolesList.Add(AppRoles.Staff);
                }
            }
        }

        var token = _jwtService
                .GenerateToken(Guid.Parse(user.Id), 
                user.Email!,
                rolesList,
                permissions,
                securityStamp);

        return new LoginResponseDto
        {
            User = await GetByIdAsync(Guid.Parse(user.Id)),
            Token = token
        };
    }

    public async Task<bool> CheckEmailExists(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user != null;
    }

    public async Task<string> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
            throw new Exception($"User not found with email ${email}");

        return await userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            throw new UnauthorizedAccessException("User not found");
        await userManager.ChangePasswordAsync(user,oldPassword, newPassword);
    }

    public async Task ResetPasswordAsync(string email, string code, string newPassword)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
            throw new Exception($"User not found with email: {email}");
        var result = await userManager.ResetPasswordAsync(user, code, newPassword);
        if(!result.Succeeded)
            throw new Exception($"{result.Errors.First().Description}");
    }

    public async Task<bool> VerifyPasswordAsync(Guid userId, string password)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return false;
        return await userManager.CheckPasswordAsync(user, password);
    }

    public async Task AssignOrgRole(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user != null)
        {
            if (!await userManager.IsInRoleAsync(user, AppRoles.Org))
                await userManager.AddToRoleAsync(user, AppRoles.Org);
        }

        else throw new Exception($"User not found with email: {userId}");
    }

    public async Task RemoveOrgRole(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user != null)
        {
            await userManager.RemoveFromRoleAsync(user, AppRoles.Org);

            await userManager.UpdateSecurityStampAsync(user);  
        }
        else throw new Exception($"User not found with email: {userId}");
    }

    public async Task<string> GenerateNewJwt(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user != null)
        {
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

            var securityStamp = await userManager.GetSecurityStampAsync(user);

            var rolesList = roles.ToList();
            var now = DateTime.UtcNow;
            var staff = await _staffRepository.GetCurrentShiftAsync(userId, now);
            if (staff != null && !rolesList.Contains(AppRoles.Staff))
            {
                var evnt = staff.Event;
                if (evnt != null)
                {
                    var eventItems = await _eventItemRepository.GetAllByEventIdAsync(evnt.Id);
                    var activeEventItem = eventItems
                        .Where(ei => ei.Start <= now && ei.End >= now)
                        .OrderByDescending(ei => ei.Start)
                        .FirstOrDefault();
                    if (activeEventItem != null)
                    {
                        rolesList.Add(AppRoles.Staff);
                    }
                }
            }

            var token = _jwtService
                .GenerateToken(Guid.Parse(user.Id), 
                    user.Email!,
                    rolesList,
                    permissions,
                    securityStamp);
            return token;
        }
        throw new Exception($"User not found with userId: {userId}");
    }

    public async Task<List<Guid>> GetUserOrgsAsync(Guid userId)
    {
        var orgIds = await userManager.Users
            .Where(u => u.Id == userId.ToString())
            .SelectMany(u => u.Organizations.Select(o => o.Id))
            .ToListAsync();
        return orgIds;
    }

    public async Task BanUserAsync(Guid userId, Guid adminUserId, string? banReason)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            throw new KeyNotFoundException("User not found");

        if (user.IsBanned)
            throw new InvalidOperationException("User is already banned");

        user.IsBanned = true;
        user.BannedAt = DateTime.UtcNow;
        user.BanReason = banReason;
        user.BannedByUserId = adminUserId.ToString();

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        var pendingTransfers = await _transferRequestRepository.GetAllAsync();
        var userPendingTransfers = pendingTransfers
            .Where(tr => tr.ToUserId == userId.ToString() && tr.Status == TransferRequestStatus.PENDING)
            .ToList();

        foreach (var transfer in userPendingTransfers)
        {
            transfer.Status = TransferRequestStatus.REJECTED;
            await _transferRequestRepository.UpdateAsync(transfer);
        }
    }

    public async Task UnbanUserAsync(Guid userId, Guid adminUserId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            throw new KeyNotFoundException("User not found");

        if (!user.IsBanned)
            throw new InvalidOperationException("User is not banned");

        user.IsBanned = false;
        user.UnbannedAt = DateTime.UtcNow;
        user.UnbannedByUserId = adminUserId.ToString();

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<PaginatedResult<AdminUserDto>> GetUsersAsync(int page, int size, string? emailSearch)
    {
        var query = userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(emailSearch))
        {
            query = query.Where(u => u.Email != null && u.Email.Contains(emailSearch));
        }

        var total = await query.CountAsync();
        var users = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        var userDtos = new List<AdminUserDto>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            userDtos.Add(new AdminUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList(),
                IsBanned = user.IsBanned,
                BannedAt = user.BannedAt,
                BanReason = user.BanReason
            });
        }

        return new PaginatedResult<AdminUserDto>
        {
            Data = userDtos,
            Total = total,
            Page = page,
            Size = size
        };
    }

    public async Task<bool> IsUserBannedAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return false;
        return user.IsBanned;
    }
}
