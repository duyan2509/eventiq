using Eventiq.Infrastructure;
using Eventiq.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Eventiq.Api.Extensions;

public static class Seed
{
    public static async void SeedAdminAndRole(this WebApplication app)
    {
        var config = app.Services.GetRequiredService<IConfiguration>();
        using var scope = app.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Seed Roles and Permission
        await IdentityDataSeeder.SeedRolesAndPermissionsAsync(roleManager, userManager);
        

        // Seed Admin User
        string? adminEmail = config["SeedAdmin:Email"];
        string? adminPassword = config["SeedAdmin:Password"];
        if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
            throw new Exception("Admin Password and Admin Email are required in settings.");
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
            }
        }
    }
}