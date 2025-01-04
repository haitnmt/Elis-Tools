using Dapper;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Serilog;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;

public class GiayChungNhanRepository(string connectionString, ILogger? logger = null)
{
    public async Task<bool> UpdateGhiChuGiayChungNhan(List<ThuaDatCapNhat> thuaDatCapNhats, string? formatGhiChu = null,
        string? ngaySapNhap = null)
    {
        if (thuaDatCapNhats.Count == 0)
            return false;

        if (string.IsNullOrWhiteSpace(formatGhiChu))
            formatGhiChu = ThamSoThayThe.DefaultGhiChuGiayChungNhan;

        if (string.IsNullOrWhiteSpace(ngaySapNhap))
            ngaySapNhap = DateTime.Now.ToString(ThamSoThayThe.DinhDangNgaySapNhap);
        await using var connection = connectionString.GetConnection();
        try
        {
            const string sql = """
                               UPDATE GCNQSDD
                               SET GhiChu = @GhiChu
                               FROM GCNQSDD
                               INNER JOIN DangKyQSDD ON GCNQSDD.MaDangKy = DangKyQSDD.MaDangKy
                               INNER JOIN ThuaDat ON DangKyQSDD.MaThuaDat = ThuaDat.MaThuaDat
                               WHERE ThuaDat.MaThuaDat = @MaThuaDat;
                               """;


            foreach (var thuaDatCapNhat in thuaDatCapNhats)
            {
                var ghiChu = formatGhiChu
                    .Replace(ThamSoThayThe.ToBanDo, thuaDatCapNhat.ToBanDo)
                    .Replace(ThamSoThayThe.DonViHanhChinh, thuaDatCapNhat.TenDonViHanhChinh)
                    .Replace(ThamSoThayThe.NgaySapNhap, ngaySapNhap);
                await connection.ExecuteAsync(sql, new { thuaDatCapNhat.MaThuaDat, GhiChu = ghiChu });
            }

            return true;
        }
        catch (Exception ex)
        {
            if (logger == null) throw;
            logger.Error(ex, "Lỗi khi cập nhật Ghi Chú Giấy Chứng Nhận.");
            return false;
        }
    }

    public async Task RenewMaGiayChungNhanAsync(DvhcRecord capXaSau, int limit = 100)
    {
        throw new NotImplementedException();
    }
}