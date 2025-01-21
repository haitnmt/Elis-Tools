using InterpolatedSql.Dapper;
using ILogger = Serilog.ILogger;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public class MucDichAndHinhThucService(string connectionString, ILogger logger)
{
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
    public async ValueTask<(string LoaiDat, string ThoiHan, string HinhThuc)> GetMucDichSuDungAsync(long maGcn,
        CancellationToken cancellationToken = default)
    {
        if (maGcn <= 0) return (string.Empty, string.Empty, string.Empty);
        try
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
            return mucDichSuDungs.Count == 0
                ? (string.Empty, string.Empty, string.Empty)
                : (GetLoaiDat(mucDichSuDungs), GetThoiHan(mucDichSuDungs), GetHinhThuc(mucDichSuDungs));
        }
        catch (Exception e)
        {
            logger.Error(e, "Lỗi khi lấy thông tin Mục đích sử dụng theo Mã GCN: {MaGcn}", maGcn);
            throw;
        }
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

    private static string GetHinhThuc(List<MucDichSuDung> mucDichSuDungs)
    {
        switch (mucDichSuDungs.Count)
        {
            case 0:
                return string.Empty;
            case 1:
            {
                var dienTichChung = mucDichSuDungs[0].DienTichChung;
                var dienTichRieng = mucDichSuDungs[0].DienTichRieng;
                return dienTichChung switch
                {
                    > 0 when dienTichRieng > 0 => $"{dienTichChung} m² sử dụng chung, {dienTichRieng} m² sử dụng riêng",
                    > 0 => "Sử dụng chung",
                    _ => dienTichRieng > 0 ? "Sử dụng riêng" : string.Empty
                };
            }
            default:
            {
                List<string> suDungChung = [];
                List<string> suDungRieng = [];
                suDungChung.AddRange(mucDichSuDungs.Where(x => x.DienTichChung > 0)
                    .Select(mucDichSuDung => $"{mucDichSuDung.Ten} {mucDichSuDung.DienTichChung} m²"));
                suDungRieng.AddRange(mucDichSuDungs.Where(x => x.DienTichRieng > 0)
                    .Select(mucDichSuDung => $"{mucDichSuDung.Ten} {mucDichSuDung.DienTichRieng} m²"));
                var stringSuDungChung = string.Join(", ", suDungChung);
                var stringSuDungRieng = string.Join(", ", suDungRieng);
                var result =
                    $"{(string.IsNullOrWhiteSpace(stringSuDungRieng) ? "" : $"Sử dụng riêng: {stringSuDungRieng}")};" +
                    $"{(string.IsNullOrWhiteSpace(stringSuDungChung) ? "" : $"Sử dụng chung: {stringSuDungChung}")};";
                return result.Trim();
            }
        }
    }
}