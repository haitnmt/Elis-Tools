using Haihv.Elis.Tool.TraCuuGcn.Models;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Settings;
using LanguageExt;
using LanguageExt.Common;
using ZiggyCreatures.Caching.Fusion;
using ILogger = Serilog.ILogger;
namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public class SearchService(IGcnQrService gcnQrService, IGiayChungNhanService giayChungNhanService,
    ILogger logger, IFusionCache fusionCache) : ISearchService
{
    public async Task<Result<GiayChungNhanWithMaQrInfo>> GetResultAsync(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new Result<GiayChungNhanWithMaQrInfo>(new ArgumentException("Tham số truy vấn không hợp lệ!"));
        }
        var giayChungNhanWithMaQrInfo = await GetInDatabaseAsync(query);
        if (giayChungNhanWithMaQrInfo is null)
        {
            return new Result<GiayChungNhanWithMaQrInfo>(new ValueIsNullException("Không tìm thấy thông tin!"));
        }
        var maGcn = giayChungNhanWithMaQrInfo.GiayChungNhan?.MaGcn ?? giayChungNhanWithMaQrInfo.MaQrInfo?.MaGcnInDatabase ?? 0;
        if (maGcn <= 0) return giayChungNhanWithMaQrInfo;
        var cacheKey = CacheSettings.KeySearch(query);
        await fusionCache.SetAsync(cacheKey, maGcn);
        return giayChungNhanWithMaQrInfo;

    }

    private async Task<GiayChungNhanWithMaQrInfo?> GetInDatabaseAsync(string query, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheSettings.KeySearch(query);
        var maGcn = await fusionCache.GetOrDefaultAsync<long>(cacheKey, token: cancellationToken);
        GiayChungNhan? giayChungNhan = null;
        MaQrInfo? maQrInfo = null;
        if (maGcn > 0)
        {
            giayChungNhan = await giayChungNhanService.GetAsync(maGcn: maGcn, cancellationToken: cancellationToken);
            if (giayChungNhan is not null)
            {
                maQrInfo = await gcnQrService.GetAsync(maGcnInDataBase: maGcn, cancellationToken: cancellationToken);
            }
        }
        if (giayChungNhan is null && maQrInfo is null)
        {
            if (long.TryParse(query, out var maHsGoc))
            {
                // Tìm kiếm theo mã hồ sơ gốc
                throw new NotImplementedException("Chức năng tìm kiếm theo mã hồ sơ gốc chưa được hỗ trợ!");
            }
            if (query.Length > 15)
            {
                // Tim kiếm theo mã QR
                maQrInfo = await gcnQrService.GetAsync(query, query, cancellationToken: cancellationToken);
                if (maQrInfo is null)
                {
                    logger.Warning("Không tìm thấy thông tin Mã QR: {Query}", query);
                    return null;
                }

                maGcn = maQrInfo.MaGcnInDatabase;
                giayChungNhan = await giayChungNhanService.GetAsync(maGcn: maGcn, cancellationToken: cancellationToken);
            }
            else
            {
                // Tìm kiếm theo số serial
                giayChungNhan = await giayChungNhanService.GetAsync(query, cancellationToken: cancellationToken);
                if (giayChungNhan is null)
                {
                    logger.Warning("Không tìm thấy thông tin Giấy chứng nhận: {Query}", query);
                    return null;
                }
                maGcn = giayChungNhan.MaGcn;
                maQrInfo = await gcnQrService.GetAsync(query, query, cancellationToken: cancellationToken);
            }
            if (maGcn <= 0)
            {
                return null;
            }
        }
        
        _ = fusionCache.SetAsync(cacheKey, cacheKey, token: cancellationToken).AsTask();
        return new GiayChungNhanWithMaQrInfo(giayChungNhan, maQrInfo);
    }
}