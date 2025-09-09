using Eventiq.Application.Interfaces.Services;
using Eventiq.Infrastructure.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Eventiq.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Identity
        services.AddScoped<IIdentityService, IdentityService>();


        return services;
    }
}