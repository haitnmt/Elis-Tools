using Microsoft.Extensions.Caching.Memory;

namespace Haihv.Elis.Tools.ChuyenDvhc.Data;

public static class CacheManager
{
    private static readonly MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

    public static void SetConnectionString(string connectionString)
    {
        Cache.Set("ConnectionString", connectionString);
    }

    public static string? GetConnectionString()
    {
        Cache.TryGetValue("ConnectionString", out string? connectionString);
        return connectionString;
    }
}