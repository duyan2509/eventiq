﻿using Eventiq.Application.Interfaces;
using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Application.Interfaces.Services;
using Eventiq.Infrastructure.Identity;
using Eventiq.Infrastructure.Persistence;
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

        services.AddScoped<ISeatService>(seatService => new SeatsIoService
        (
            config["SeatsIo:SecretKey"]));
        return services;
    }
    public static IServiceCollection AddPersistence(this IServiceCollection services,IConfiguration config)
    {
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEventAddressRepository, EventAddressRepository>();
        services.AddScoped<ITicketClassRepository, TicketClassRepository>();
        services.AddScoped<IEventItemRepository, EventItemRepository>();
        services.AddScoped<IChartRepository, ChartRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}