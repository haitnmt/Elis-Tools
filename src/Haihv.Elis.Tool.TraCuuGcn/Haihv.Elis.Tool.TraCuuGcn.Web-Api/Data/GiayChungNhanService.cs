using Haihv.Elis.Tool.TraCuuGcn.Models;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Extensions;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Settings;
using InterpolatedSql.Dapper;
using LanguageExt;
using LanguageExt.Common;
using ZiggyCreatures.Caching.Fusion;
using ILogger = Serilog.ILogger;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public sealed class GiayChungNhanService(
    IConnectionElisData connectionElisData,
    ILogger logger,
    IFusionCache fusionCache) :
    IGiayChungNhanService
{
    /// <summary>
    /// Lấy thông tin Giấy chứng nhận theo số serial.
    /// </summary>
    /// <param name="serial">Số serial của Giấy chứng nhận.</param>
    /// <param name="maGcn"></param>
    /// <param name="cancellationToken">Token hủy bỏ tác vụ không bắt buộc.</param>
    /// <returns>Kết quả chứa thông tin Giấy chứng nhận hoặc lỗi nếu không tìm thấy.</returns>
    /// <exception cref="ValueIsNullException">Ném ra ngoại lệ nếu không tìm thấy thông tin Giấy chứng nhận.</exception>
    public async Task<Result<GiayChungNhan>> GetResultAsync(string? serial = null, long maGcn = 0,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serial) && maGcn <= 0)
            return new Result<GiayChungNhan>(new ValueIsNullException("Không tìm thấy thông tin Giấy chứng nhận!"));
        try
        {
            var cache = CacheSettings.KeyGiayChungNhan(maGcn);
            return await fusionCache.GetOrSetAsync(cache,
                cancel => GetAsync(serial, maGcn, cancel), token: cancellationToken) ?? 
                   new Result<GiayChungNhan>(new ValueIsNullException("Không tìm thấy thông tin Giấy chứng nhận!"));
        }
        catch (Exception e)
        {
            logger.Error(e, "Lỗi khi lấy thông tin Giấy chứng nhận: {Serial} {MaGCN}", serial, maGcn);
            return new Result<GiayChungNhan>(e);
        }

    }
    
    /// <summary>
    /// Lấy thông tin Giấy chứng nhận theo số serial.
    /// </summary>
    /// <param name="serial">Số serial của Giấy chứng nhận.</param>
    /// <param name="maGcn"></param>
    /// <param name="cancellationToken">Token hủy bỏ tác vụ không bắt buộc.</param>
    /// <returns>Kết quả chứa thông tin Giấy chứng nhận</returns>
    public async Task<GiayChungNhan?> GetAsync(string? serial = null, long maGcn = 0,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serial) && maGcn <= 0) return null;
        try
        {
            var giayChungNhan = await fusionCache.GetOrDefaultAsync<GiayChungNhan>(CacheSettings.KeyGiayChungNhan(maGcn),
                token: cancellationToken);
            if (giayChungNhan is not null) return giayChungNhan;
            var connectionElis = await connectionElisData.GetConnectionElis(maGcn);
            foreach (var (connectionName, _, connectionString) in connectionElis)
            {
                await using var dbConnection = connectionString.GetConnection();
                var query = dbConnection.SqlBuilder(
                    $"""
                     SELECT MaGCN AS MaGcn, 
                            MaDangKy AS MaDangKy, 
                            MaHinhThucSH AS MaHinhThucSh,
                            DienTichRieng,
                            DienTichChung,
                            SoSerial AS Serial, 
                            CASE WHEN NgayKy < '1990-01-01' 
                                THEN NgayVaoSo 
                                ELSE NgayKy 
                            END AS NgayKy, 
                            NguoiKy, 
                            SoVaoSo
                     FROM GCNQSDD
                     WHERE SoSerial IS NOT NULL AND SoSerial <> N'' AND MaGcn > 0 AND 
                           (LOWER(SoSerial) = LOWER({serial}) OR MaGCN = {maGcn})
                     """);
                giayChungNhan =
                    await query.QueryFirstOrDefaultAsync<GiayChungNhan?>(cancellationToken: cancellationToken);
                if (giayChungNhan is null) continue;
                _ = fusionCache.SetAsync(CacheSettings.KeyGiayChungNhan(giayChungNhan.MaGcn), giayChungNhan, token: cancellationToken).AsTask();
                _ = fusionCache.SetCacheConnectionName(giayChungNhan.MaGcn, connectionName, cancellationToken);
                return giayChungNhan;
            }
        
        }
        catch (Exception e)
        {
            logger.Error(e, "Lỗi khi lấy thông tin Giấy chứng nhận: {Serial} {MaGCN}", serial, maGcn);
            throw;
        }

        return null;
    }
    
}