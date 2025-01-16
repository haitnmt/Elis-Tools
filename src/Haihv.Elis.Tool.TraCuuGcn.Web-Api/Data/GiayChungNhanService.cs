using InterpolatedSql.Dapper;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Caching.Hybrid;
using ILogger = Serilog.ILogger;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public interface IGiayChungNhanService
{
    ValueTask<Result<GiayChungNhan>> GetBySerialAsync(string serial);
}

public sealed class GiayChungNhanService(
    IConnectionElisData connectionElisData,
    ILogger logger,
    HybridCache hybridCache) : IGiayChungNhanService
{
    private readonly List<string> _connectionStrings = connectionElisData.ConnectionStrings;
    private static string CacheKey(string serial) => $"GCN:{serial}";

    public async ValueTask<Result<GiayChungNhan>> GetBySerialAsync(string serial)
    {
        var cacheKey = CacheKey(serial);
        try
        {
            var giayChungNhan = await hybridCache.GetOrCreateAsync(cacheKey,
                cancel => GetBySerialInDataBaseAsync(serial, cancel));
            if (giayChungNhan is null)
                return new Result<GiayChungNhan>(new ValueIsNullException("Không tìm thấy giấy chứng nhận!"));
            if (giayChungNhan.NgayKy < DateTime.Now.AddYears(-10) ||
                giayChungNhan.NgayKy > DateTime.Now ||
                string.IsNullOrWhiteSpace(giayChungNhan.SoVaoSo))
            {
                return new Result<GiayChungNhan>(new ValueIsNullException("Giấy chứng nhận chưa có hiệu lực!"));
            }

            return giayChungNhan;
        }
        catch (Exception exception)
        {
            return new Result<GiayChungNhan>(exception);
        }
    }

    private async ValueTask<GiayChungNhan?> GetBySerialInDataBaseAsync(string serial,
        CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var connectionString in _connectionStrings)
            {
                await using var dbConnection = connectionString.GetConnection();
                var query = dbConnection.SqlBuilder(
                    $"""
                     SELECT MaGCN AS MaGcn, MaDangKy AS MaDangKy, SoSerial AS Serial, NgayKy, NguoiKy, SoVaoSo
                     FROM GCNQSDD
                     WHERE SoSerial = {serial}
                     """);
                var giayChungNhan =
                    await query.QueryFirstOrDefaultAsync<GiayChungNhan?>(cancellationToken: cancellationToken);
                if (giayChungNhan is null) continue;

                return giayChungNhan;
            }
        }
        catch (Exception e)
        {
            logger.Error(e, "Lỗi khi lấy thông tin Giấy chứng nhận theo Serial: {Serial}", serial);
            throw;
        }

        return null;
    }
}

public record GiayChungNhan(
    long MaGcn,
    long MaDangKy,
    string Serial,
    DateTime NgayKy,
    string NguoiKy,
    string SoVaoSo
);