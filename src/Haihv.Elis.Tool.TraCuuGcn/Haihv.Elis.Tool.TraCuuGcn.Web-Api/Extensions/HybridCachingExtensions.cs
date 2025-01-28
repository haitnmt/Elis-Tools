using Microsoft.Extensions.Caching.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Extensions;

public static class HybridCachingExtensions
{
    public static void AddCache(this IServiceCollection services, string? redisConnectionString = null)
    {
        //Add MemoryCache
        services.AddMemoryCache();
        var fusionOptions = services.AddFusionCache()
            .WithDefaultEntryOptions(options =>
            {
                options.DistributedCacheDuration = TimeSpan.FromDays(1);
                options.Duration = TimeSpan.FromMinutes(5);
                options.Duration = TimeSpan.FromMinutes(5);
                options.IsFailSafeEnabled = true;
                options.FailSafeThrottleDuration = TimeSpan.FromSeconds(15);
                options.FailSafeThrottleDuration = TimeSpan.FromDays(1);
            })
            .WithSerializer(new FusionCacheSystemTextJsonSerializer());
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            fusionOptions.WithDistributedCache(
                new RedisCache(
                    new RedisCacheOptions
                    {
                        Configuration = redisConnectionString,
                        InstanceName = "TraCuuGcn"
                    }
                ));
        }
        
        fusionOptions.AsHybridCache();
    }
}