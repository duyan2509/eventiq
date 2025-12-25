
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
using Microsoft.AspNetCore.Mvc.ApplicationModels;
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
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend",
                policy => policy.WithOrigins("http://localhost:3000") 
                                .AllowAnyHeader()
                                .AllowAnyMethod()
                                .AllowCredentials());
        });

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
                
                // Configure SignalR to accept token from query string
                opt.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
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
                                Console.WriteLine("JWT invalid: ");

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

        builder.Services.AddControllers(options =>
        {
            options.Conventions.Add(new RouteTokenTransformerConvention(
                new LowercaseControllerTransformer()));
        });

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
        builder.Services.AddScoped<IJwtService, JwtService>();
        
        // Đăng ký background worker để xử lý event processing
        builder.Services.AddHostedService<EventProcessingWorker>();
    }
    public static IServiceCollection AddOrgAuthorize(this IServiceCollection services, IConfiguration config)
    {
        services.AddAuthorization(options =>
        {
            // Admin policy - require Admin role
            // Support both ClaimTypes.Role and the full claim type name
            options.AddPolicy("Admin", policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole("Admin") ||
                    context.User.HasClaim(ClaimTypes.Role, "Admin") ||
                    context.User.HasClaim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "Admin") ||
                    context.User.HasClaim("role", "Admin")));
            
            options.AddPolicy("Event.Create", policy =>
                policy.RequireClaim("Permission", "Event.Create"));
            options.AddPolicy("Event.Submit", policy =>
                policy.RequireClaim("Permission", "Event.Submit"));
            options.AddPolicy("Event.View", policy =>
                policy.RequireClaim("Permission", "Event.View"));
            options.AddPolicy("Event.Update", policy =>
                policy.RequireClaim("Permission", "Event.Update"));
            options.AddPolicy("Event.Cancel", policy =>
                policy.RequireClaim("Permission", "Event.Cancel"));
            options.AddPolicy("Event.RequestCancel", policy =>
                policy.RequireClaim("Permission", "Event.RequestCancel"));
            options.AddPolicy("Event.Assign", policy =>
                policy.RequireClaim("Permission", "Event.Assign"));
            options.AddPolicy("Event.AssignCancel", policy =>
                policy.RequireClaim("Permission", "Event.AssignCancel"));
        });
        return services;
    }
    public class LowercaseControllerTransformer : IOutboundParameterTransformer
    {
        public string TransformOutbound(object value)
        {
            return value?.ToString().ToLowerInvariant();
        }
    }

}
