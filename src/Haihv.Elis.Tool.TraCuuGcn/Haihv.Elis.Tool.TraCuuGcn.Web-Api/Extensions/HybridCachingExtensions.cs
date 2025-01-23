using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Extensions;

public static class HybridCachingExtensions
{
    [Experimental("EXTEXP0018")]
    public static void AddHybridCaching(this IServiceCollection services, string? redisConnectionString = null)
    {
        // Thêm Redis Cache
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "TraCuuGcn:";
            });
        }

        services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024;
            options.MaximumKeyLength = 1024;
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromDays(1),
                LocalCacheExpiration = TimeSpan.FromMinutes(30)
            };
        });
    }
}