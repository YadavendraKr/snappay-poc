using StackExchange.Redis;
using System.Text.Json;

namespace Shared.Infrastructure;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
}

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _database = connectionMultiplexer.GetDatabase();
        _jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true,
            WriteIndented = false // Compact JSON for better performance
        };
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            if (!_connectionMultiplexer.IsConnected)
                return default;

            var value = await _database.StringGetAsync(key);
            if (!value.HasValue)
                return default;

            return JsonSerializer.Deserialize<T>(value.ToString(), _jsonOptions);
        }
        catch (RedisConnectionException)
        {
            return default; // Redis down → cache miss
        }
        catch (RedisTimeoutException)
        {
            return default; // Redis slow → cache miss
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value, _jsonOptions);
            if (expiration.HasValue)
                await _database.StringSetAsync(key, serialized, expiration.Value);
            else
                await _database.StringSetAsync(key, serialized);
        }
        catch (RedisConnectionException)
        {
            return; // Redis down → cache miss
        }
        catch (RedisTimeoutException)
        {
            return; // Redis slow → cache miss
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (Exception)
        {
            // Silently fail for resilience
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            if (!_connectionMultiplexer.IsConnected)
                return;

            // Use SCAN to avoid blocking the server (async pattern scanning)
            var endpoints = _connectionMultiplexer.GetEndPoints();
            if (endpoints.Length == 0)
                return;

            var server = _connectionMultiplexer.GetServer(endpoints[0]);
            var cursor = 0L;
            var batchSize = 0;
            const int maxIterations = 1000;
            var iterations = 0;

            do
            {
                // Use SCAN with MATCH to find keys asynchronously
                var result = server.Execute("SCAN", cursor, "MATCH", pattern, "COUNT", "1000");

                // result.IsNull is sufficient to determine if the SCAN command failed or returned no data
                if (result.IsNull)
                    break;

                // Use dynamic to resolve the ambiguous explicit conversion error between RedisResult and array types
                var scanResult = (RedisResult[])(dynamic)result;
                if (scanResult.Length != 2)
                    break;

                // Get cursor for next iteration
                cursor = long.Parse(scanResult[0].ToString() ?? "0");
                
                // Get keys from this iteration
                // Cast via dynamic to avoid the same conversion ambiguity on the inner keys array
                var keys = ((RedisKey[])(dynamic)scanResult[1])
                    .ToArray();

                if (keys.Length > 0)
                {
                    await _database.KeyDeleteAsync(keys);
                    batchSize += keys.Length;
                }

                iterations++;
            } while (cursor != 0 && iterations < maxIterations);
        }
        catch (Exception)
        {
            // Silently fail for resilience
        }
    }
}
