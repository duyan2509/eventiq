using System.Security.Claims;

namespace Eventiq.Application.Interfaces.Services;

public interface IJwtService
{
    string GenerateToken(Guid userId, string email, List<string> roles, List<string> permissions);
    ClaimsPrincipal? ValidateToken(string token);
}