using Eventiq.Application.Interfaces.Services;
using Eventiq.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Eventiq.Api.Tests;

public abstract class RedisIntegrationTestBase : IAsyncLifetime
{
    protected readonly IConnectionMultiplexer Redis;
    protected readonly IRedisService RedisService;

    protected RedisIntegrationTestBase()
    {
        Redis = ConnectionMultiplexer.Connect("localhost:6379");

        var services = new ServiceCollection();
        services.AddSingleton<IConnectionMultiplexer>(Redis);
        services.AddSingleton<IRedisService, RedisService>();

        var provider = services.BuildServiceProvider();
        RedisService = provider.GetRequiredService<IRedisService>();
    }

    public async Task InitializeAsync()
    {
        var db = Redis.GetDatabase();
        await db.ExecuteAsync("FLUSHDB");
    }

    public async Task DisposeAsync()
    {
        var db = Redis.GetDatabase();
        await db.ExecuteAsync("FLUSHDB");
        Redis.Dispose();
    }
}
