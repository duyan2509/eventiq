using Eventiq.Application.Interfaces.Services;
using Eventiq.Infrastructure.Identity;
using Eventiq.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Eventiq.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,IConfiguration config)
    {
        // Identity
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IEmailService>(sp =>
            new SmtpEmailService(
                config["SeedAdmin:Email"],
                "sandbox.smtp.mailtrap.io",
                2525,
                config["Mailtrap:Username"],
                config["Mailtrap:Password"]
            ));


        return services;
    }
}