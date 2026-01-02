// See https://aka.ms/new-console-template for more information

using System.Reflection;
using Eventiq.Application.Interfaces.Services;
using Eventiq.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Eventiq.Application;

public static class Main
{
    public static IServiceCollection InjectServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped<ICheckinService, CheckinService>();
        services.AddScoped<ICheckoutService, CheckoutService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IRevenueService, RevenueService>();
        return services;
    }
    public static IServiceCollection AddAutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        return services;
    }
}