using InterpolatedSql.Dapper;
using Microsoft.Extensions.Caching.Hybrid;
using ILogger = Serilog.ILogger;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public sealed class ChuSuDungService(
    IConnectionElisData connectionElisData,
    ILogger logger,
    HybridCache hybridCache)
{
    private readonly List<string> _connectionStrings = connectionElisData.ConnectionStrings;
    private readonly string _cacheKey = "ChuSuDung";

    private async ValueTask<ChuSuDung?> GetBySoDinhDanhInDataAsync(GiayChungNhan giayChungNhan, string soDinhDanh)
    {
        try
        {
            foreach (var connectionString in _connectionStrings)
            {
                await using var dbConnection = connectionString.GetConnection();
                var query = dbConnection.SqlBuilder(
                    $"""
                     SELECT MaDangKy
                     FROM (SELECT DISTINCT CSD.SoDinhDanh1 AS SoDinhDanh, 
                                           CSD.HoVaTen1 AS HoVaTen,
                                           CSD.GioiTinh1 AS GioiTinh, 
                                           CSD.NamSinh1 AS NamSinh, 
                                           CSD.DiaChi1 AS DiaChi
                           FROM ChuSuDung CSD
                                INNER JOIN GCNQSDD GCN ON CSD.MaChuSuDung = GCN.MaChuSuDung
                            WHERE CSD.SoDinhDanh1 = {soDinhDanh} AND GCN.MaGCN = {giayChungNhan.MaGcn}
                           UNION
                           SELECT DISTINCT CSD.SoDinhDanh2 AS SoDinhDanh, 
                                           CSD.HoVaTen2 AS HoVaTen, 
                                           CSD.GioiTinh2 AS GioiTinh, 
                                           CSD.NamSinh2 AS NamSinh, 
                                           CSD.DiaChi2 AS DiaChi
                           FROM ChuSuDung CSD
                                INNER JOIN GCNQSDD GCN ON CSD.MaChuSuDung = GCN.MaChuSuDung
                            WHERE CSD.SoDinhDanh2 = {soDinhDanh} AND GCN.MaGCN = {giayChungNhan.MaGcn}
                     ORDER BY MaDangKy DESC
                     """);
            }
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Lỗi khi truy vấn dữ liệu chủ sử dụng từ cơ sở dữ liệu, {SoDDinhDanh}", soDinhDanh);
            throw;
        }

        return null;
    }
}

public record ChuSuDung(string SoDinhDanh, string HoVaTen, string GioiTinh, int NamSinh, string DiaChi);