using Haihv.Elis.Tool.TraCuuGcn.Models;
using Haihv.Elis.Tool.TraCuuGcn.Models.Extensions;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Settings;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Caching.Hybrid;
using Haihv.Tool.Extensions.String;
using InterpolatedSql.Dapper;
using ILogger = Serilog.ILogger;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public interface IGcnQrService
{
    /// <summary>
    /// Lấy thông tin Mã QR không đồng bộ.
    /// </summary>
    /// <param name="maQr">Mã QR cần truy vấn.</param>
    /// <param name="cancellationToken">Token để hủy bỏ thao tác không đồng bộ.</param>
    /// <returns>Kết quả chứa thông tin Mã QR nếu tìm thấy, ngược lại trả về ngoại lệ.</returns>
    ValueTask<Result<MaQrInfo>> GetAsync(string? maQr, CancellationToken cancellationToken = default);
}

public sealed class GcnQrService(IConnectionElisData connectionElisData, ILogger logger, HybridCache hybridCache) : IGcnQrService
{
    private readonly List<ConnectionElis> _connectionElis = connectionElisData.ConnectionElis;

    /// <summary>
    /// Lấy thông tin Mã QR không đồng bộ.
    /// </summary>
    /// <param name="maQr">Mã QR cần truy vấn.</param>
    /// <param name="cancellationToken">Token để hủy bỏ thao tác không đồng bộ.</param>
    /// <returns>Kết quả chứa thông tin Mã QR nếu tìm thấy, ngược lại trả về ngoại lệ.</returns>
    public async ValueTask<Result<MaQrInfo>> GetAsync(string? maQr, CancellationToken cancellationToken = default)
    {
        var hashQr = maQr.ComputeHash();
        var cacheKey = CacheSettings.KeyMaQr(hashQr);
        try
        {
            var maQrInfo = await hybridCache.GetOrCreateAsync<MaQrInfo?>(cacheKey,
                _ => ValueTask.FromResult<MaQrInfo?>(null), cancellationToken: cancellationToken);
            if (maQrInfo is not null) return new Result<MaQrInfo>(maQrInfo);
            (hashQr, maQrInfo) = await GetFromDataBaseAsync(maQr, null, cancellationToken);
            if (maQrInfo is null)
                return new Result<MaQrInfo>(new ValueIsNullException("Không tìm thấy thông tin Mã QR!"));
            cacheKey = CacheSettings.KeyMaQr(hashQr);
            await hybridCache.SetAsync(cacheKey, maQrInfo, cancellationToken: cancellationToken);
            return maQrInfo;
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Lỗi khi lấy thông tin Mã QR: {MaQr}", maQr);
            return new Result<MaQrInfo>(new ValueIsNullException("Không tìm thấy thông tin Mã QR!"));
        }
    }

    /// <summary>
    /// Lấy thông tin Mã QR từ cơ sở dữ liệu.
    /// </summary>
    /// <param name="maQr">Mã QR cần truy vấn.</param>
    /// <param name="hashQr">Mã QR đã được băm.</param>
    /// <param name="cancellationToken">Token để hủy bỏ thao tác không đồng bộ.</param>
    /// <returns>Tuple chứa mã QR đã băm và thông tin Mã QR nếu tìm thấy, ngược lại trả về null.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn cơ sở dữ liệu.</exception>
    private async ValueTask<(string? HashQr, MaQrInfo? MaQrInfo)> GetFromDataBaseAsync(string? maQr, string? hashQr,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(maQr) && string.IsNullOrWhiteSpace(hashQr)) return (null, null);
        try
        {
            foreach (var connectionString in _connectionElis.Select(x => x.ConnectionString))
            {
                await using var dbConnection = connectionString.GetConnection();
                var query = dbConnection.SqlBuilder(
                    $"""
                     SELECT GuidID AS Id,
                            MaQR AS MaQr,
                            MaHoaQR AS HashQr
                     FROM GCNQR
                     WHERE (LOWER(MaQR) = LOWER({maQr}) OR LOWER(MaHoaQR) = LOWER({hashQr})) AND HieuLuc = 1
                     """);
                var qrInData = await query.QueryFirstOrDefaultAsync<dynamic?>(cancellationToken: cancellationToken);
                if (qrInData is null) continue;
                maQr = qrInData.MaQr;
                hashQr = qrInData.HashQr;
                var maQrInfo = maQr.ToMaQr();
                maQrInfo.TenDonVi = await GetTenDonViInAsync(maQrInfo.MaDonVi, cancellationToken);
                return (hashQr, maQrInfo);
            }
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Lỗi khi lấy thông tin Mã QR: {MaQr}", maQr);
            throw;
        }

        return (null, null);
    }
    
    /// <summary>
    /// Lấy thông tin tên đơn vị dựa trên mã đơn vị.
    /// </summary>
    /// <param name="maDonVi">Mã định danh của đơn vị.</param>
    /// <param name="cancellationToken">Token để hủy bỏ thao tác không đồng bộ.</param>
    /// <returns>Tên đơn vị nếu tìm thấy, ngược lại trả về null.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn cơ sở dữ liệu.</exception>
    private async ValueTask<string?> GetTenDonViInAsync(string? maDonVi, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(maDonVi)) return null;
        var cacheKey = CacheSettings.KeyDonViInGcn(maDonVi);
        try
        {
            var tenDonVi = await hybridCache.GetOrCreateAsync<string?>(cacheKey,
                cancel => GetTenDonViInDataBaseAsync(maDonVi, cancel),
                cancellationToken: cancellationToken);
            return tenDonVi;
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Lỗi khi lấy thông tin Tên đơn vị: {MaDonVi}", maDonVi);
            return null;
        }
    }
    
    /// <summary>
    /// Lấy thông tin tên đơn vị dựa trên mã đơn vị từ cơ sở dữ liệu.
    /// </summary>
    /// <param name="maDonVi">Mã định danh của đơn vị.</param>
    /// <param name="cancellationToken">Token để hủy bỏ thao tác không đồng bộ.</param>
    /// <returns>Tên đơn vị nếu tìm thấy, ngược lại trả về null.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn cơ sở dữ liệu.</exception>
    private async ValueTask<string?> GetTenDonViInDataBaseAsync(string? maDonVi, CancellationToken cancellationToken = default)
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