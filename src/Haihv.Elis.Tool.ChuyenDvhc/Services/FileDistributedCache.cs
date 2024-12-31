using Microsoft.Extensions.Caching.Distributed;

namespace Haihv.Elis.Tool.ChuyenDvhc.Services;

public class FileDistributedCache : IDistributedCache
{
    private readonly string _cacheDirectory;
    private readonly IFileService _fileService;

    public FileDistributedCache(IFileService fileService, string cacheDirectory)
    {
        _cacheDirectory = cacheDirectory;
        _fileService = fileService;
        Directory.CreateDirectory(_cacheDirectory);
    }

    public byte[]? Get(string key)
        => _fileService.ReadAllBytes(ConvertKey(key));
    
    public Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default)
        => _fileService.ReadAllBytesAsync(ConvertKey(key), cancellationToken);

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        => _fileService.WriteAllBytes(ConvertKey(key), value);

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken cancellationToken = default)
        => _fileService.WriteAllBytesAsync(ConvertKey(key),value, cancellationToken);

    public void Remove(string key)
        => _fileService.Delete(ConvertKey(key));

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => _fileService.DeleteAsync(ConvertKey(key), cancellationToken);

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
        return Path.Combine(_cacheDirectory, key + ".cache");
    }
}
