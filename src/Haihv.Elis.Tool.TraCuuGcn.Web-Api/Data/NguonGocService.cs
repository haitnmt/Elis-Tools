using InterpolatedSql.Dapper;
using ILogger = Serilog.ILogger;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public class NguonGocService(
    IConnectionElisData connectionElisData,
    ILogger logger)
{
    private readonly List<string> _connectionStrings = connectionElisData.ConnectionStrings;

    private record NguonGocSuDung(
        string Ten,
        double DienTich
    );

    /// <summary>
    /// Lấy thông tin nguồn gốc sử dụng theo Mã GCN.
    /// </summary>
    /// <param name="maGcn">Mã GCN cần tra cứu.</param>
    /// <param name="cancellationToken">Token hủy bỏ tác vụ không bắt buộc.</param>
    /// <returns>Chuỗi thông tin nguồn gốc sử dụng.</returns>
    public async ValueTask<string> GetNguonGocSuDungAsync(long maGcn,
        CancellationToken cancellationToken = default)
    {
        if (maGcn <= 0) return string.Empty;
        try
        {
            foreach (var connectionString in _connectionStrings)
            {
                await using var dbConnection = connectionString.GetConnection();
                var query = dbConnection.SqlBuilder(
                    $"""
                     SELECT DISTINCT NGSQ.NguonGocGCN AS Ten,
                                     DKNG.DienTich
                     FROM DangKyNGSDD DKNG
                         INNER JOIN NguonGocSDD NGSQ ON DKNG.MaNGSDD = NGSQ.MaNGSDD
                     WHERE DKNG.MaGCN = {maGcn}
                     """);
                var nguonGocSuDungs = (await query.QueryAsync<NguonGocSuDung>(cancellationToken: cancellationToken))
                    .ToList();
                if (nguonGocSuDungs.Count == 0) continue;
                return GetNguonGoc(nguonGocSuDungs);
            }
        }
        catch (Exception e)
        {
            logger.Error(e, "Lỗi khi lấy thông tin Nguồn gốc sử dụng theo Mã GCN: {MaGcn}", maGcn);
            throw;
        }

        return string.Empty;
    }

    private static string GetNguonGoc(List<NguonGocSuDung> nguonGocSuDungs)
        => nguonGocSuDungs.Count == 0
            ? string.Empty
            : string.Join(", ", nguonGocSuDungs.Select(nguonGocSuDung =>
                $"{nguonGocSuDung.Ten}" +
                $"{(nguonGocSuDungs.Count > 1 ? $"{nguonGocSuDung.DienTich} m²" : "")}"));
}