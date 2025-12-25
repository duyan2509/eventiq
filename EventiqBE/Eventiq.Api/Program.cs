using Eventiq.Api.Extensions;
using Eventiq.Api.Hubs;
using Eventiq.Infrastructure;
using Eventiq.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration) 
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.AddApplicationServices();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.SeedAdminAndRole();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Eventiq API V1");
        c.RoutePrefix = string.Empty; 
    });
    // Disable HTTPS redirection in development for SignalR
    // app.UseHttpsRedirection();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health");

app.Run();

