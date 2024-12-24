using System.Data;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;

public static class ThuaDatExtensions
{
    /// <summary>
    /// Lấy số lượng Thửa Đất dựa trên danh sách Mã Tờ Bản Đồ.
    /// </summary>
    /// <param name="dbContext">
    /// Ngữ cảnh cơ sở dữ liệu.
    /// </param>
    /// <param name="maDvhcBiSapNhap">
    /// Danh sách Mã Đơn Vị Hành Chính đang bị sập nhập.
    /// </param>
    /// <returns>Số lượng Thửa Đất.</returns>
    public static async Task<int> GetCountThuaDatAsync(this DbContext dbContext, List<int> maDvhcBiSapNhap)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        var parameters = new List<string>();

        for (var i = 0; i < maDvhcBiSapNhap.Count; i++)
        {
            var parameterName = $"@MaToBanDo{i}";
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = maDvhcBiSapNhap[i];
            parameter.DbType = DbType.Int64;
            command.Parameters.Add(parameter);
            parameters.Add(parameterName);
        }

        var sql = $"""
                   SELECT COUNT(ThuaDat.MaThuaDat) 
                   FROM   ThuaDat INNER JOIN ToBanDo ON ThuaDat.MaToBanDo = ToBanDo.MaToBanDo
                   WHERE (ToBanDo.MaDVHC IN ({string.Join(", ", parameters)}))
                   """;
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync();
        return int.TryParse(result?.ToString(), out var count) ? count : 0;
    }

    /// <summary>
    /// Lấy danh sách Mã Thửa Đất dựa trên danh sách Mã Tờ Bản Đồ.
    /// </summary>
    /// <param name="dbContext">
    /// Ngữ cảnh cơ sở dữ liệu.
    /// </param>
    /// <param name="dvhcBiSapNhap">
    /// Bản ghi Đơn Vị Hành Chính đang bị sập nhập.
    /// </param>
    /// <param name="minMaThuaDat">
    /// Giá trị tối thiểu của Mã Thửa Đất.
    /// </param>
    /// <param name="limit">
    /// Số lượng giới hạn kết quả trả về.
    /// </param>
    /// <param name="formatGhiChuThuaDat">
    /// Định dạng Ghi Chú Thửa Đất.
    /// </param>
    /// <param name="ngaySapNhap">
    /// Ngày sáp nhập.
    /// </param>
    /// <returns>Danh sách Mã Thửa Đất.</returns>
    public static async Task<List<ThuaDatCapNhat>> UpdateAndGetThuaDatToBanDoAsync(this ElisDataContext dbContext,
        DvhcRecord dvhcBiSapNhap,
        long minMaThuaDat = long.MinValue, int limit = 100, string? formatGhiChuThuaDat = null, string ngaySapNhap = "")
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        if (string.IsNullOrWhiteSpace(formatGhiChuThuaDat))
            formatGhiChuThuaDat = ThamSoThayThe.DefaultGhiChuThuaDat;
        if (string.IsNullOrWhiteSpace(ngaySapNhap))
            ngaySapNhap = DateTime.Now.ToString(ThamSoThayThe.DinhDangNgaySapNhap);
        // Tạo câu lệnh SQL
        const string sql = $"""
                            SELECT DISTINCT TOP (@Limit) ThuaDat.MaThuaDat, ThuaDat.ThuaDatSo, ToBanDo.SoTo
                            FROM   ThuaDat INNER JOIN ToBanDo ON ThuaDat.MaToBanDo = ToBanDo.MaToBanDo
                            WHERE (ToBanDo.MaDVHC = @MaDvhc AND ThuaDat.MaThuaDat > @MinMaThuaDat)
                            ORDER BY ThuaDat.MaThuaDat
                            OPTION (RECOMPILE)
                            """;
        command.CommandText = sql;

        // Tạo tham số cho câu lệnh SQL 

        // Tham số @MaDvhc
        var parameterMaDvhc = command.CreateParameter();
        parameterMaDvhc.ParameterName = "@MaDvhc";
        parameterMaDvhc.Value = dvhcBiSapNhap.MaDvhc;
        command.Parameters.Add(parameterMaDvhc);

        // Tham số @Limit
        var parameterLimit = command.CreateParameter();
        parameterLimit.ParameterName = "@Limit";
        parameterLimit.Value = limit;
        command.Parameters.Add(parameterLimit);

        // Tham số @MinMaThuaDat
        var parameterMinMaThuaDat = command.CreateParameter();
        parameterMinMaThuaDat.ParameterName = "@MinMaThuaDat";
        parameterMinMaThuaDat.Value = minMaThuaDat;
        command.Parameters.Add(parameterMinMaThuaDat);

        // Thực thi câu lệnh SQL
        var result = await command.ExecuteReaderAsync();

        // Đọc kết quả trả về
        List<ThuaDatCapNhat> thuaDatCapNhats = [];
        while (await result.ReadAsync())
        {
            var maThuaDat = result.GetInt64(0);
            // Thêm vào danh sách kết quả
            thuaDatCapNhats.Add(new ThuaDatCapNhat(
                maThuaDat,
                result.GetString(1).Trim(),
                result.GetString(2).Trim(),
                dvhcBiSapNhap.Ten));
        }

        await result.CloseAsync();

        // Cập nhật thông tin Thửa Đất
        foreach (var thuaDatCapNhat in thuaDatCapNhats)
        {
            var thuaDat = await dbContext.ThuaDats.FindAsync(thuaDatCapNhat.MaThuaDat);
            if (thuaDat == null) continue;
            thuaDat.GhiChu = formatGhiChuThuaDat
                .Replace(ThamSoThayThe.NgaySapNhap, ngaySapNhap)
                .Replace(ThamSoThayThe.ToBanDo, thuaDatCapNhat.ToBanDo)
                .Replace(ThamSoThayThe.DonViHanhChinh, dvhcBiSapNhap.Ten);
            dbContext.ThuaDats.Update(thuaDat);
        }

        // Lưu thay đổi vào cơ sở dữ liệu
        await dbContext.SaveChangesAsync();
        return thuaDatCapNhats;
    }
}