using Microsoft.Extensions.Caching.Distributed;

namespace Haihv.Elis.Tool.ChuyenDvhc.Services;

public class FileDistributedCache : IDistributedCache
{
    private readonly string _cacheDirectory;

    public FileDistributedCache(string cacheDirectory)
    {
        _cacheDirectory = cacheDirectory;
        Directory.CreateDirectory(_cacheDirectory);
    }

    public byte[]? Get(string key)
    {
        var path = ConvertKey(key);
        if (!File.Exists(path)) return null;

        var content = File.ReadAllBytes(path);
        return content;
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        return Task.FromResult(Get(key));
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        
        var path = ConvertKey(key);
        File.WriteAllBytes(path, value);
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        var path = ConvertKey(key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }

    public void Refresh(string key)
    {
        // Không cần làm gì
    }

    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        Refresh(key);
        return Task.CompletedTask;
    }

    private string ConvertKey(string key)
    {
        key = key.Replace(':', '_');
        return Path.Combine(_cacheDirectory, key + ".cache");
    }
}
