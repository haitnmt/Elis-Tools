using Dapper;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Microsoft.Data.SqlClient;
using Serilog;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;

public class GiayChungNhanRepository(ILogger? logger = null, string? connectionString = null, SqlConnection? dbConnection = null) : DataRepository(logger, connectionString, dbConnection)
{
    private readonly ILogger? _logger = logger;
    public async Task<bool> UpdateGhiChuGiayChungNhan(List<ThuaDatCapNhat> thuaDatCapNhats, string? formatGhiChu = null, string? ngaySapNhap = null)
    {
        if (thuaDatCapNhats.Count == 0)
            return false;

        if (string.IsNullOrWhiteSpace(formatGhiChu))
            formatGhiChu = ThamSoThayThe.DefaultGhiChuGiayChungNhan;

        if (string.IsNullOrWhiteSpace(ngaySapNhap))
            ngaySapNhap = DateTime.Now.ToString(ThamSoThayThe.DinhDangNgaySapNhap);
        await using var connection = await GetAndOpenConnectionAsync();
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
                await connection.ExecuteAsync(sql, new {thuaDatCapNhat.MaThuaDat, GhiChu = ghiChu});
            }

            return true;
        }
        catch (Exception ex)
        {
            if (_logger == null) throw;
            _logger.Error(ex, "Lỗi khi cập nhật Ghi Chú Giấy Chứng Nhận.");
            return false;
        }
    }
}