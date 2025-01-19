using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Settings;
using InterpolatedSql.Dapper;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Caching.Hybrid;
using ILogger = Serilog.ILogger;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public interface IChuSuDungService
{
    ValueTask<Result<ChuSuDung>> GetBySoDinhDanhAsync(GiayChungNhan giayChungNhan, string soDinhDanh, 
        CancellationToken cancellationToken = default);
}

public sealed class ChuSuDungService(
    IConnectionElisData connectionElisData,
    ILogger logger,
    HybridCache hybridCache) : IChuSuDungService
{
    public async ValueTask<Result<ChuSuDung>> GetBySoDinhDanhAsync(GiayChungNhan? giayChungNhan = null,
        string? soDinhDanh = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(soDinhDanh) ||
            giayChungNhan is null ||
            string.IsNullOrWhiteSpace(giayChungNhan.Serial))
            return new Result<ChuSuDung>(new ValueIsNullException("Số định danh không hợp lệ!"));
        var cacheKey = CacheSettings.KeyChuSuDung(soDinhDanh, giayChungNhan.Serial);
        try
        {
            var chuSuDung = await hybridCache.GetOrCreateAsync(cacheKey, 
                cancel => GetBySoDinhDanhInDataAsync(giayChungNhan, soDinhDanh, cancel), cancellationToken: cancellationToken);
            return chuSuDung ?? new Result<ChuSuDung>(new ValueIsNullException("Không tìm thấy chủ sử dụng!"));
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Lỗi khi truy vấn dữ liệu chủ sử dụng từ cơ sở dữ liệu, {SoDDinhDanh}", soDinhDanh);
            return new Result<ChuSuDung>(exception);
        }
    }

    private async ValueTask<ChuSuDung?> GetBySoDinhDanhInDataAsync(
        GiayChungNhan? giayChungNhan = null,
        string? soDinhDanh = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(soDinhDanh) || giayChungNhan is null)
            return null;
        try
        {
            var connectionName = await hybridCache.GetOrCreateAsync<string?>(CacheSettings.ConnectionName(giayChungNhan.Serial),
                _ => ValueTask.FromResult<string?>(null), 
                cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(connectionName)) return null;
            var connectionString = connectionElisData.GetConnectionString(connectionName);
            await using var dbConnection = connectionString.GetConnection();
            var query = dbConnection.SqlBuilder(
                $"""
                 SELECT TOP(1) SoDinhDanh, HoVaTen, GioiTinh, NamSinh, DiaChi1, DiaChi2
                 FROM (SELECT DISTINCT CSD.SoDinhDanh1 AS SoDinhDanh, 
                                       CSD.Ten1 AS HoVaTen,
                                       CSD.GioiTinh1 AS GioiTinh, 
                                       CSD.NamSinh1 AS NamSinh, 
                                       CSD.DiaChi1 AS DiaChi1,
                                       CSD.DiaChi2 AS DiaChi2
                       FROM ChuSuDung CSD
                            INNER JOIN GCNQSDD GCN ON CSD.MaChuSuDung = GCN.MaChuSuDung
                        WHERE CSD.SoDinhDanh1 = {soDinhDanh} AND GCN.MaGCN = {giayChungNhan.MaGcn}
                       UNION
                       SELECT DISTINCT CSD.SoDinhDanh2 AS SoDinhDanh, 
                                       CSD.Ten2 AS HoVaTen, 
                                       CSD.GioiTinh2 AS GioiTinh, 
                                       CSD.NamSinh2 AS NamSinh, 
                                       CSD.DiaChi1 AS DiaChi1,
                                       CSD.DiaChi2 AS DiaChi2
                       FROM ChuSuDung CSD
                            INNER JOIN GCNQSDD GCN ON CSD.MaChuSuDung = GCN.MaChuSuDung
                        WHERE CSD.SoDinhDanh2 = {soDinhDanh} AND GCN.MaGCN = {giayChungNhan.MaGcn}
                        ) AS CSD
                 """);
            var chuSuDungData =
                await query.QueryFirstOrDefaultAsync<dynamic?>(cancellationToken: cancellationToken);
            if (chuSuDungData is null) return null;
            return new ChuSuDung(
                chuSuDungData.SoDinhDanh,
                chuSuDungData.HoVaTen,
                chuSuDungData.GioiTinh == 1 ? "Nam" : "Nữ",
                chuSuDungData.NamSinh,
                string.IsNullOrWhiteSpace(chuSuDungData.DiaChi2) ? chuSuDungData.DiaChi1 : chuSuDungData.DiaChi2);
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Lỗi khi truy vấn dữ liệu chủ sử dụng từ cơ sở dữ liệu, {SoDDinhDanh}", soDinhDanh);
            throw;
        }
    }
}

public record ChuSuDung(string SoDinhDanh, string HoVaTen, string GioiTinh, int NamSinh, string DiaChi);