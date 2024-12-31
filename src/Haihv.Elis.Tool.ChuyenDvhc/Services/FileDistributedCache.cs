using Microsoft.Extensions.Caching.Distributed;

namespace Haihv.Elis.Tool.ChuyenDvhc.Services;

public class FileDistributedCache(IFileService fileService, string cacheDirectory) : IDistributedCache
{
    public byte[]? Get(string key)
        => fileService.ReadAllBytes(ConvertKey(key));
    
    public Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default)
        => fileService.ReadAllBytesAsync(ConvertKey(key), cancellationToken);

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        => fileService.WriteAllBytes(ConvertKey(key), value);

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken cancellationToken = default)
        => fileService.WriteAllBytesAsync(ConvertKey(key),value, cancellationToken);

    public void Remove(string key)
        => fileService.Delete(ConvertKey(key));

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => fileService.DeleteAsync(ConvertKey(key), cancellationToken);

    public void Refresh(string key)
    {
        // Không cần làm gì
    }

    public Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        Refresh(key);
        return Task.CompletedTask;
    }

    private string ConvertKey(string key)
    {
        key = key.Replace(':', '_');
        return Path.Combine(cacheDirectory, key + ".cache");
    }
}
