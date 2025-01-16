using Dapper;
using InterpolatedSql.Dapper;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Caching.Hybrid;
using ILogger = Serilog.ILogger;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public interface IChuSuDungService
{
    ValueTask<Result<ChuSuDung>> GetBySoDinhDanhAsync(GiayChungNhan giayChungNhan, string soDinhDanh);
}

public sealed class ChuSuDungService(
    IConnectionElisData connectionElisData,
    ILogger logger,
    HybridCache hybridCache) : IChuSuDungService
{
    private readonly List<string> _connectionStrings = connectionElisData.ConnectionStrings;
    private const string CacheKey = "ChuSuDung";

    public async ValueTask<Result<ChuSuDung>> GetBySoDinhDanhAsync(GiayChungNhan? giayChungNhan = null, string? soDinhDanh = null)
    {
        var cacheKey = $"{CacheKey}:{soDinhDanh}";
        try
        {
            var chuSuDung = await hybridCache.GetOrCreateAsync(cacheKey,
                cancel => GetBySoDinhDanhInDataAsync(giayChungNhan, soDinhDanh, cancel));
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
            foreach (var connectionString in _connectionStrings)
            {
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
                var chuSuDungData = await query.QueryFirstOrDefaultAsync<ChuSuDungData?>(cancellationToken: cancellationToken);
                if (chuSuDungData is null) continue;
                return new ChuSuDung(
                    chuSuDungData.SoDinhDanh,
                    chuSuDungData.HoVaTen,
                    chuSuDungData.GioiTinh == 1 ? "Nam" : "Nữ",
                    chuSuDungData.NamSinh,
                    string.IsNullOrWhiteSpace(chuSuDungData.DiaChi2) ? chuSuDungData.DiaChi1 : chuSuDungData.DiaChi2);
            }
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Lỗi khi truy vấn dữ liệu chủ sử dụng từ cơ sở dữ liệu, {SoDDinhDanh}", soDinhDanh);
            throw;
        }

        return null;
    }

    private record ChuSuDungData(string SoDinhDanh, string HoVaTen, int GioiTinh, int NamSinh, string DiaChi1, string DiaChi2);
}

public record ChuSuDung(string SoDinhDanh, string HoVaTen, string GioiTinh, int NamSinh, string DiaChi);