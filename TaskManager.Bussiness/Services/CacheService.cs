using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using System.Text.Json;

namespace TaskManager.Bussiness.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memory;
        private readonly IDatabase _redis;

        private static readonly TimeSpan MemoryCacheDuration = TimeSpan.FromMinutes(1);

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };

        public CacheService(IMemoryCache memory,IConnectionMultiplexer redis)
        {
            _memory = memory;
            _redis = redis.GetDatabase();
        }
        #region Versioning
        public async Task<long> GetVersionAsync(string key)
        {
            var versionKey = $"{key}:version";
            var version = await _redis.StringGetAsync(versionKey);
            if (!version.HasValue)
            {
                await _redis.StringSetAsync(versionKey,1,when : When.NotExists);
                return 1;
            }
            return (long)version;
        }
        public async Task<long> IncrementVersionAsync(string key)
        {
            return await _redis.StringIncrementAsync($"{key}:version");
        }
        #endregion
        #region Cache
        public async Task<T?> GetAsync<T>(string key)
        {
            // Level 1 Cache (Memory)
            if (_memory.TryGetValue(key, out T? memoryValue))
            {
                return memoryValue;
            }
            // Level 2 Cache (Redis)
            var redisValue = await _redis.StringGetAsync(key);
            if (!redisValue.HasValue)
            {
                return default;
            }
            var value = JsonSerializer.Deserialize<T>(redisValue!,JsonOptions);
            if (value is not null)
            {
                _memory.Set(key,value,MemoryCacheDuration);
            }
            return (value);
        }

        public async Task SetAsync<T>(string key, T value,TimeSpan expiration)
        {
            var json = JsonSerializer.Serialize(value,JsonOptions);
            // Redis
            await _redis.StringSetAsync(key, json, expiration);
            // Memory
            _memory.Set(key,value,MemoryCacheDuration);
        }
        public async Task RemoveAsync(string key)
        {
            _memory.Remove(key);
            await _redis.KeyDeleteAsync(key);
        }
        #endregion
    }
}