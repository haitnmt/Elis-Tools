using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;

public static class GiayChungNhanExtensions
{
    public static async Task<bool> UpdateGhiChuGiayChungNhan(this ElisDataContext dataContext,
        List<ThuaDatCapNhat> thuaDatCapNhats, string? formatGhiChu = null, string? ngaySapNhap = null)
    {
        if (thuaDatCapNhats.Count == 0)
            return false;

        if (string.IsNullOrWhiteSpace(formatGhiChu))
            formatGhiChu = ThamSoThayThe.DefaultGhiChuGiayChungNhan;

        if (string.IsNullOrWhiteSpace(ngaySapNhap))
            ngaySapNhap = DateTime.Now.ToString(ThamSoThayThe.DinhDangNgaySapNhap);

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
            var ghiChuGiayChungNhan = formatGhiChu
                .Replace(ThamSoThayThe.NgaySapNhap, ngaySapNhap)
                .Replace(ThamSoThayThe.SoThua, thuaDatCapNhat.ThuaDatSo)
                .Replace(ThamSoThayThe.ToBanDo, thuaDatCapNhat.ToBanDo)
                .Replace(ThamSoThayThe.DonViHanhChinh, thuaDatCapNhat.TenDonViHanhChinh);
            await using var command = dataContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;

            // Tham số @GhiChu
            var parameterGhiChu = command.CreateParameter();
            parameterGhiChu.ParameterName = "@GhiChu";
            parameterGhiChu.Value = ghiChuGiayChungNhan;
            command.Parameters.Add(parameterGhiChu);

            // Tham số @MaThuaDat
            var parameterMaThuaDat = command.CreateParameter();
            parameterMaThuaDat.ParameterName = "@MaThuaDat";
            parameterMaThuaDat.Value = thuaDatCapNhat.MaThuaDat;
            command.Parameters.Add(parameterMaThuaDat);

            // Thực thi câu lệnh SQL
            await command.ExecuteNonQueryAsync();
        }

        return true;
    }
}