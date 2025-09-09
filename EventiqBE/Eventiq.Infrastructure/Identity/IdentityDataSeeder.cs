using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Eventiq.Infrastructure.Identity;

public class IdentityDataSeeder
{
    public static async Task SeedRolesAndPermissionsAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        var rolesWithPermissions = new Dictionary<string, string[]>
        {
            {
                AppRoles.Admin, new[]
                {
                    "Event.Delete", 
                    "User.Manage",
                    "Event.Accept",
                    "Event.AcceptCancel"
                }
            },
            {
                AppRoles.Org, new[]
                {
                    "Event.Create",
                    "Event.Update",
                    "Event.View",
                    "Event.Submit",
                    "Event.Cancel",
                    "Event.RequestCancel",
                    "Event.Assign",
                    "Event.AssignCancel"
                }
            },
            {
                AppRoles.Staff, new[]
                {
                    "Event.View", "Ticket.Scan"
                }
            },
            {
                AppRoles.User, new[]
                {
                    "Event.View", "Ticket.Buy"
                }
            }
        };

        foreach (var role in rolesWithPermissions)
        {
            if (!await roleManager.RoleExistsAsync(role.Key))
            {
                await roleManager.CreateAsync(new IdentityRole(role.Key));
            }

            var identityRole = await roleManager.FindByNameAsync(role.Key);
            var existingClaims = await roleManager.GetClaimsAsync(identityRole);

            foreach (var permission in role.Value)
            {
                if (!existingClaims.Any(c => c.Type == "Permission" && c.Value == permission))
                {
                    await roleManager.AddClaimAsync(identityRole, new Claim("Permission", permission));
                }
            }
        }
    }

}