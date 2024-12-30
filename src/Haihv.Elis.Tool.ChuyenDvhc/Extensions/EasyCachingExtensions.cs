using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Hybrid;


namespace Haihv.Elis.Tool.ChuyenDvhc.Extensions;

public static class EasyCachingExtensions
{
    [Experimental("EXTEXP0018")]
    public static void AddHybridCaching(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024;
            options.MaximumKeyLength = 1024;
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(60),
                LocalCacheExpiration = TimeSpan.FromMinutes(5)
            };
        });
    }
}