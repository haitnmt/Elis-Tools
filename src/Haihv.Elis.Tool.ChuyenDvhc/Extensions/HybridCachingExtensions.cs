using System.Diagnostics.CodeAnalysis;
using Haihv.Elis.Tool.ChuyenDvhc.Services;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Hosting;


namespace Haihv.Elis.Tool.ChuyenDvhc.Extensions;

public static class HybridCachingExtensions
{
    [Experimental("EXTEXP0018")]
    public static void AddHybridCaching(this IServiceCollection services)
    {
        services.AddSingleton<IDistributedCache>(sp =>
            new FileDistributedCache(
                sp.GetRequiredService<IFileService>(),
                Settings.FilePath.CacheOnDisk));
        services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024;
            options.MaximumKeyLength = 1024;
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(60),
                LocalCacheExpiration = TimeSpan.FromDays(1)
            };
        });
        // Xoá Cache cũ quá 1 ngày:
        services.AddHostedService<ClearCacheService>();
    }
}

public class ClearCacheService(HybridCache hybridCache) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!stoppingToken.IsCancellationRequested)
        {
           await hybridCache.RemoveAsync(CacheData.CapTinh, stoppingToken);
        }
    }
}