using InterpolatedSql.Dapper;
using ILogger = Serilog.ILogger;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public class MucDichService(
    IConnectionElisData connectionElisData,
    ILogger logger)
{
    private readonly List<string> _connectionStrings = connectionElisData.ConnectionStrings;

    private record MucDichSuDung(
        string Ten,
        double DienTichRieng,
        double DienTichChung,
        string ThoiHan
    );

    /// <summary>
    /// Lấy thông tin mục đích sử dụng và thời hạn từ bộ nhớ đệm hoặc cơ sở dữ liệu.
    /// </summary>
    /// <param name="maGcn">Mã GCN cần truy vấn.</param>
    /// <param name="cancellationToken">Token hủy bỏ để hủy bỏ thao tác không đồng bộ.</param>
    /// <returns>Thông tin mục đích sử dụng và thời hạn.</returns>
    public async ValueTask<(string LoaiDat, string ThoiHan)> GetMucDichSuDungAsync(long maGcn,
        CancellationToken cancellationToken = default)
    {
        if (maGcn <= 0) return (string.Empty, string.Empty);
        try
        {
            foreach (var connectionString in _connectionStrings)
            {
                await using var dbConnection = connectionString.GetConnection();
                var query = dbConnection.SqlBuilder(
                    $"""
                     SELECT DISTINCT MDSD.FullName AS Ten,
                                     DKMD.DienTichRieng,
                                     DKMD.DienTichChung, 
                                     DKMD.ThoiHan
                     FROM DangKyMDSDD DKMD
                         INNER JOIN MucDichSDD MDSD ON DKMD.MaMDSDD = MDSD.MaMDSDD
                     WHERE DKMD.MaGCN = {maGcn}
                     """);
                var mucDichSuDungs =
                    (await query.QueryAsync<MucDichSuDung>(cancellationToken: cancellationToken)).ToList();
                if (mucDichSuDungs.Count == 0) continue;
                return (GetLoaiDat(mucDichSuDungs), GetThoiHan(mucDichSuDungs));
            }
        }
        catch (Exception e)
        {
            logger.Error(e, "Lỗi khi lấy thông tin Mục đích sử dụng theo Mã GCN: {MaGcn}", maGcn);
            throw;
        }

        return (string.Empty, string.Empty);
    }

    private static string GetThoiHan(List<MucDichSuDung> mucDichSuDungs)
        => mucDichSuDungs.Count == 0
            ? string.Empty
            : string.Join(", ", mucDichSuDungs.Select(mucDichSuDung =>
                $"{mucDichSuDung.ThoiHan}" +
                $"{(mucDichSuDungs.Count > 1 ? $"{mucDichSuDung.DienTichChung + mucDichSuDung.DienTichRieng} m²" : "")}"));

    private static string GetLoaiDat(List<MucDichSuDung> mucDichSuDungs)
        => mucDichSuDungs.Count == 0
            ? string.Empty
            : string.Join(", ", mucDichSuDungs.Select(mucDichSuDung =>
                $"{mucDichSuDung.Ten}" +
                $"{(mucDichSuDungs.Count > 1 ? $"{mucDichSuDung.DienTichChung + mucDichSuDung.DienTichRieng} m²" : "")}"));
}