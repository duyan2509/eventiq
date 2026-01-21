using Eventiq.Application.Interfaces.Services;
using StackExchange.Redis;
using System.Text.Json;

namespace Eventiq.Infrastructure.Services;

public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    
    // Lua script for atomic seat locking (all-or-nothing)
    private const string LockSeatsScript = @"
        local eventItemId = ARGV[1]
        local ttl = tonumber(ARGV[2])
        local lockedCount = 0
        local failedKeys = {}
        
        -- Check all seats first
        for i = 3, #ARGV do
            local seatId = ARGV[i]
            local key = 'lock:seat:' .. eventItemId .. ':' .. seatId
            if redis.call('EXISTS', key) == 1 then
                table.insert(failedKeys, seatId)
            end
        end
        
        -- If any seat is locked, fail all
        if #failedKeys > 0 then
            return {0, failedKeys}
        end
        
        -- Lock all seats atomically
        for i = 3, #ARGV do
            local seatId = ARGV[i]
            local key = 'lock:seat:' .. eventItemId .. ':' .. seatId
            redis.call('SET', key, '1', 'EX', ttl)
            lockedCount = lockedCount + 1
        end
        
        return {lockedCount, {}}
    ";
    
    private readonly LuaScript _lockSeatsLuaScript;
    private readonly LoadedLuaScript _loadedLuaScript;

    public RedisService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _lockSeatsLuaScript = LuaScript.Prepare(LockSeatsScript);
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        _loadedLuaScript = _lockSeatsLuaScript.Load(server);
    }

    public async Task<bool> LockSeatsAsync(Guid eventItemId, List<string> seatIds, TimeSpan ttl)
    {
        if (seatIds == null || seatIds.Count == 0)
            return false;

        var keys = Array.Empty<RedisKey>();
        var values = new List<RedisValue>
        {
            eventItemId.ToString(),
            ((int)ttl.TotalSeconds).ToString()
        };
        values.AddRange(seatIds.Select(s => (RedisValue)s));


        var result = await _database.ScriptEvaluateAsync(
            _loadedLuaScript.Hash, 
            keys, 
            values.ToArray());
        
        if (result.Type == ResultType.Array && result.Length >= 2)
        {
            var lockedCount = (int)result[0];
            
            return lockedCount == seatIds.Count;
        }
        
        return false;
    }

    public async Task ExtendSeatLockAsync(Guid eventItemId, List<string> seatIds, TimeSpan additionalTtl)
    {
        if (seatIds == null || seatIds.Count == 0)
            return;

        var tasks = seatIds.Select(async seatId =>
        {
            var key = $"lock:seat:{eventItemId}:{seatId}";
            if (await _database.KeyExistsAsync(key))
            {
                await _database.KeyExpireAsync(key, additionalTtl);
            }
        });

        await Task.WhenAll(tasks);
    }

    public async Task ReleaseSeatsAsync(Guid eventItemId, List<string> seatIds)
    {
        if (seatIds == null || seatIds.Count == 0)
            return;

        var keys = seatIds.Select(seatId => 
            new RedisKey($"lock:seat:{eventItemId}:{seatId}")).ToArray();
        
        await _database.KeyDeleteAsync(keys);
    }

    public async Task<bool> IsSeatLockedAsync(Guid eventItemId, string seatId)
    {
        var key = $"lock:seat:{eventItemId}:{seatId}";
        return await _database.KeyExistsAsync(key);
    }

    public async Task SetCheckoutSessionAsync(string checkoutId, string data, TimeSpan ttl)
    {
        var key = $"checkout:{checkoutId}";
        await _database.StringSetAsync(key, data, ttl);
    }

    public async Task ExtendCheckoutSessionAsync(string checkoutId, TimeSpan additionalTtl)
    {
        var key = $"checkout:{checkoutId}";
        if (await _database.KeyExistsAsync(key))
        {
            await _database.KeyExpireAsync(key, additionalTtl);
        }
    }

    public async Task<string?> GetCheckoutSessionAsync(string checkoutId)
    {
        var key = $"checkout:{checkoutId}";
        var value = await _database.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task DeleteCheckoutSessionAsync(string checkoutId)
    {
        var key = $"checkout:{checkoutId}";
        await _database.KeyDeleteAsync(key);
    }
}

