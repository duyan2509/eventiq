
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Eventiq.Application;
using Eventiq.Application.Interfaces.Services;
using Eventiq.Infrastructure;
using Eventiq.Infrastructure.Cloudinary;
using Eventiq.Infrastructure.Identity;
using Eventiq.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Eventiq.Api.Extensions;


public static class Extensions
{
    public static void AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("Eventiq.Migrations")));
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        var jwtKey = builder.Configuration["Jwt:Key"] ?? "super_secret_key";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

        builder
            .Services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;            
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],

                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero 
                };
                opt.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        Console.WriteLine("JWT failed: " + ctx.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnChallenge = ctx =>
                    {
                        Console.WriteLine("JWT challenge: " + ctx.ErrorDescription);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var userManager = context.HttpContext.RequestServices
                            .GetRequiredService<UserManager<ApplicationUser>>();
                        var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;

                        var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var tokenStamp = claimsIdentity?.FindFirst("SecurityStamp")?.Value;

                        if (userId != null && tokenStamp != null)
                        {
                            var user = await userManager.FindByIdAsync(userId);
                            var currentStamp = await userManager.GetSecurityStampAsync(user);

                            if (tokenStamp != currentStamp)
                            {
                                context.Fail("Token has been revoked");
                                Console.WriteLine("JWT invalid: " );

                            }
                        }
                    }
                };
                
            });
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Events.OnRedirectToLogin = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };

        });

        builder.Services.AddControllers();
        builder.Services.AddHealthChecks();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(opt =>
        {
            var scheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Reference = new()
                {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme,
                },
            };
            opt.AddSecurityDefinition(scheme.Reference.Id, scheme);
            opt.AddSecurityRequirement(
                new OpenApiSecurityRequirement { { scheme, Array.Empty<string>() } }
            );
        });

        builder.Services
            .InjectServices()
            .AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies())
            .AddInfrastructure(builder.Configuration)
            .AddCloudinaryInfrastructure(builder.Configuration)
            .AddPersistence(builder.Configuration)
            .AddOrgAuthorize(builder.Configuration);
        builder.Services.AddScoped<IJwtService,JwtService>();
    }
    public static IServiceCollection AddOrgAuthorize(this IServiceCollection services,IConfiguration config)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Event.Create", policy =>
                policy.RequireClaim("Permission", "Event.Create"));
        });
        return services;
    }
 
}
