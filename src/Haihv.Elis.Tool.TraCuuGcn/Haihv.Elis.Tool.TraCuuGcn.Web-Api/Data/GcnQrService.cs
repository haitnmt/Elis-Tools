using Haihv.Elis.Tool.TraCuuGcn.Models;
using Haihv.Elis.Tool.TraCuuGcn.Models.Extensions;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Settings;
using LanguageExt;
using LanguageExt.Common;
using InterpolatedSql.Dapper;
using ZiggyCreatures.Caching.Fusion;
using ILogger = Serilog.ILogger;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public interface IGcnQrService
{
    /// <summary>
    /// Lấy thông tin Mã QR không đồng bộ.
    /// </summary>
    /// <param name="maQr">Mã QR cần truy vấn.</param>
    /// <param name="hashQr">Mã QR đã được băm.</param>
    /// <param name="maGcnInDataBase">Mã GCN cần truy vấn.</param>
    /// <param name="cancellationToken">Token để hủy bỏ thao tác không đồng bộ.</param>
    /// <returns>Kết quả chứa thông tin Mã QR nếu tìm thấy, ngược lại trả về ngoại lệ.</returns>
    ValueTask<Result<MaQrInfo>> GetResultAsync(string? maQr = null, string? hashQr = null, long maGcnInDataBase = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin Mã QR không đồng bộ.
    /// </summary>
    /// <param name="maQr">Mã QR cần truy vấn.</param>
    /// <param name="hashQr">Mã QR đã được băm.</param>
    /// <param name="maGcnInDataBase">Mã GCN cần truy vấn.</param>
    /// <param name="cancellationToken">Token để hủy bỏ thao tác không đồng bộ.</param>
    /// <returns>Kết quả chứa thông tin Mã QR nếu tìm thấy</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn cơ sở dữ liệu.</exception>
    ValueTask<MaQrInfo?> GetAsync(string? maQr = null, string? hashQr = null, long maGcnInDataBase = 0,
        CancellationToken cancellationToken = default);
}

public sealed class GcnQrService(IConnectionElisData connectionElisData, ILogger logger, IFusionCache fusionCache) : IGcnQrService
{
    private readonly List<ConnectionElis> _connectionElis = connectionElisData.ConnectionElis;

    /// <summary>
    /// Lấy thông tin Mã QR không đồng bộ.
    /// </summary>
    /// <param name="maQr">Mã QR cần truy vấn.</param>
    /// <param name="hashQr">Mã QR đã được băm.</param>
    /// <param name="maGcnInDataBase">Mã GCN cần truy vấn.</param>
    /// <param name="cancellationToken">Token để hủy bỏ thao tác không đồng bộ.</param>
    /// <returns>Kết quả chứa thông tin Mã QR nếu tìm thấy, ngược lại trả về ngoại lệ.</returns>
    public async ValueTask<Result<MaQrInfo>> GetResultAsync(string? maQr= null, string? hashQr = null, long maGcnInDataBase = 0, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetAsync(maQr, hashQr, maGcnInDataBase, cancellationToken) ?? 
                   new Result<MaQrInfo>(new ValueIsNullException("Không tìm thấy thông tin Mã QR!"));
        }
        catch (Exception e)
        {
            logger.Error(e, "Lỗi khi lấy thông tin Mã QR: {MaQr}", maQr);
            return new Result<MaQrInfo>(e);
        }

    }
    
    /// <summary>
    /// Lấy thông tin Mã QR từ cơ sở dữ liệu.
    /// </summary>
    /// <param name="maQr">Mã QR cần truy vấn.</param>
    /// <param name="hashQr">Mã QR đã được băm.</param>
    /// <param name="maGcn">Mã GCN cần truy vấn.</param>
    /// <param name="cancellationToken">Token để hủy bỏ thao tác không đồng bộ.</param>
    /// <returns>Tuple chứa mã QR đã băm và thông tin Mã QR nếu tìm thấy, ngược lại trả về null.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn cơ sở dữ liệu.</exception>
    public async ValueTask<MaQrInfo?> GetAsync(string? maQr = null, string? hashQr = null, long maGcn = 0, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(maQr) && string.IsNullOrWhiteSpace(hashQr) && maGcn <= 0) return null;
        try
        {
            var maQrInfo = await fusionCache.GetOrDefaultAsync<MaQrInfo>(CacheSettings.KeyMaQr(maGcn), token: cancellationToken);
            if (maQrInfo is not null) return maQrInfo;
            var connectionElis = await connectionElisData.GetConnectionElis(maGcn);
            foreach (var connection in connectionElis)
            {
                await using var dbConnection = connection.ConnectionString.GetConnection();
                var query = dbConnection.SqlBuilder(
                    $"""
                     SELECT GuidID AS Id,
                            MaGCN AS MaGcn,
                            MaQR AS MaQr,
                            MaHoaQR AS HashQr
                     FROM GCNQR
                     WHERE (LOWER(MaQR) = LOWER({maQr}) OR LOWER(MaHoaQR) = LOWER({hashQr}) OR MaGCN = {maGcn}) AND HieuLuc = 1
                     """);
                var qrInData = await query.QueryFirstOrDefaultAsync<dynamic?>(cancellationToken: cancellationToken);
                if (qrInData is null) continue;
                maQr = qrInData.MaQr;
                maQrInfo = maQr.ToMaQr();
                if (!string.IsNullOrWhiteSpace(maQrInfo.MaDonVi))
                {
                    maQrInfo.TenDonVi = await fusionCache.GetOrSetAsync(CacheSettings.KeyDonViInGcn(maQrInfo.MaDonVi),
                        cancel => GetTenDonViInDataBaseAsync(maQrInfo.MaDonVi, cancel),
                        token: cancellationToken);
                }
                maQrInfo.MaGcnInDatabase = qrInData.MaGcn;
                _ = fusionCache.SetAsync(CacheSettings.KeyMaQr(maQrInfo.MaGcnInDatabase), maQrInfo, TimeSpan.FromDays(60), token: cancellationToken).AsTask();
                _ = fusionCache.SetAsync(CacheSettings.ConnectionName(maQrInfo.MaGcnInDatabase), connection.Name,
                    TimeSpan.FromDays(60),
                    token: cancellationToken).AsTask();
                return maQrInfo;
            }
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Lỗi khi lấy thông tin Mã QR: {MaQr}", maQr);
            throw;
        }
        return null;
    }
    
    /// <summary>
    /// Lấy thông tin tên đơn vị dựa trên mã đơn vị từ cơ sở dữ liệu.
    /// </summary>
    /// <param name="maDonVi">Mã định danh của đơn vị.</param>
    /// <param name="cancellationToken">Token để hủy bỏ thao tác không đồng bộ.</param>
    /// <returns>Tên đơn vị nếu tìm thấy, ngược lại trả về null.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn cơ sở dữ liệu.</exception>
    private async Task<string?> GetTenDonViInDataBaseAsync(string? maDonVi, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(maDonVi)) return null;
        try
        {
            foreach (var connectionString in _connectionElis.Select(x => x.ConnectionString))
            {
                await using var dbConnection = connectionString.GetConnection();
                var query = dbConnection.SqlBuilder($"SELECT Ten FROM DonViInGCN WHERE MaDinhDanh = {maDonVi}");
                return await query.QueryFirstOrDefaultAsync<string?>(cancellationToken: cancellationToken);
            }
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Lỗi khi lấy thông tin Tên đơn vị: {MaDonVi}", maDonVi);
            throw;
        }

        return null;
    }
}