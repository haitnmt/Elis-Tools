using System.Data;
using Dapper;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Microsoft.Data.SqlClient;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;

public sealed class ThuaDatExtensions(string connectionString)
{
    public const long DefaultTempMaThuaDat = long.MaxValue;



    private static async Task<int> GetCountThuaDatAsync(SqlConnection dbConnection, List<int> maDvhcBiSapNhap)
    {
        const string sql = """
                           SELECT COUNT(ThuaDat.MaThuaDat) 
                           FROM   ThuaDat INNER JOIN ToBanDo ON ThuaDat.MaToBanDo = ToBanDo.MaToBanDo
                           WHERE (ToBanDo.MaDVHC IN @MaDVHC)
                           """;
        if (dbConnection.State != ConnectionState.Open)
            await dbConnection.OpenAsync();
        return await dbConnection.ExecuteAsync(sql, new {MaDVHC = maDvhcBiSapNhap});
    }

    private static async Task<int> GetCountThuaDatAsync(string connectionString, List<int> maDvhcBiSapNhap)
    {
        await using var dbConnection = new SqlConnection(connectionString);
        await dbConnection.OpenAsync();
        return await GetCountThuaDatAsync(dbConnection, maDvhcBiSapNhap);
    }
    /// <summary>
    /// Lấy số lượng Thửa Đất dựa trên danh sách Mã Tờ Bản Đồ.
    /// </summary>
    /// <param name="maDvhcBiSapNhap">
    /// Danh sách Mã Đơn Vị Hành Chính đang bị sập nhập.
    /// </param>
    /// <returns>Số lượng Thửa Đất.</returns>
    public async Task<int> GetCountThuaDatAsync(List<int> maDvhcBiSapNhap)
        => await GetCountThuaDatAsync(connectionString, maDvhcBiSapNhap);


    private static async Task<List<ThuaDatCapNhat>> UpdateAndGetThuaDatToBanDoAsync(SqlConnection dbConnection,
        DvhcRecord dvhcBiSapNhap,
        long minMaThuaDat = long.MinValue, int limit = 100, string? formatGhiChuThuaDat = null, string ngaySapNhap = "")
    {
        
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

    /// <summary>
    /// Tạo Thửa Đất tạm thời.
    /// </summary>
    /// <param name="dbContext">Ngữ cảnh cơ sở dữ liệu.</param>
    /// <param name="maToBanDo">Mã Tờ Bản Đồ.</param>
    /// <param name="maThuaDatTemp">Mã Thửa Đất tạm thời (tùy chọn).</param>
    /// <returns>Mã Thửa Đất tạm thời.</returns>
    public static async Task<long> CreateTempThuaDatAsync(this ElisDataContext dbContext,
        long? maToBanDo = null,
        long? maThuaDatTemp = null)
    {
        try
        {
            maToBanDo = await dbContext.CreateTempToBanDoAsync(maToBanDo);
            var tempThuaDat = new ThuaDat
            {
                MaThuaDat = maThuaDatTemp ?? DefaultTempMaThuaDat,
                MaToBanDo = maToBanDo.Value,
                ThuaDatSo = "Temp",
                GhiChu = "Thửa đất tạm thời"
            };

            // Kiểm tra xem Thửa Đất đã tồn tại chưa
            var thuaDat = await dbContext.ThuaDats.FindAsync(tempThuaDat.MaThuaDat);

            if (thuaDat != null)
                return await dbContext.CreateTempThuaDatLsAsync(maToBanDo, maThuaDatTemp);

            // Nếu Thửa Đất chưa tồn tại thì thêm mới
            dbContext.ThuaDats.Add(tempThuaDat);
            await dbContext.SaveChangesAsync();

            return await dbContext.CreateTempThuaDatLsAsync(maToBanDo, maThuaDatTemp);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return long.MinValue;
        }
    }

    /// <summary>
    /// Tạo Thửa Đất Lịch Sử tạm thời.
    /// </summary>
    /// <param name="dbContext">Ngữ cảnh cơ sở dữ liệu.</param>
    /// <param name="maToBanDo">Mã Tờ Bản Đồ.</param>
    /// <param name="maThuaDatTemp">Mã Thửa Đất tạm thời (tùy chọn).</param>
    private static async Task<long> CreateTempThuaDatLsAsync(this ElisDataContext dbContext,
        long? maToBanDo = null,
        long? maThuaDatTemp = null)
    {
        try
        {
            maToBanDo ??= await dbContext.CreateTempToBanDoAsync(maToBanDo);
            var tempThuaDatLs = new ThuaDatLichSu
            {
                MaThuaDatLs = maThuaDatTemp ?? DefaultTempMaThuaDat,
                MaToBanDo = maToBanDo.Value,
                ThuaDatSo = "Temp",
                GhiChu = "Thửa đất tạm thời"
            };

            // Kiểm tra xem Thửa Đất đã tồn tại chưa
            var thuaDatLs = await dbContext.ThuaDatLichSus.FindAsync(tempThuaDatLs.MaThuaDatLs);

            if (thuaDatLs != null)
                return tempThuaDatLs.MaThuaDatLs;

            // Nếu Thửa Đất chưa tồn tại thì thêm mới
            dbContext.ThuaDatLichSus.Add(tempThuaDatLs);
            await dbContext.SaveChangesAsync();

            return tempThuaDatLs.MaThuaDatLs;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return long.MinValue;
        }
    }

    /// <summary>
    /// Lấy danh sách Mã Thửa Đất dựa trên Mã Đơn Vị Hành Chính.
    /// </summary>
    /// <param name="dataContext">Ngữ cảnh cơ sở dữ liệu.</param>
    /// <param name="maDvhc">Mã Đơn Vị Hành Chính.</param>
    /// <returns>Danh sách Mã Thửa Đất.</returns>
    public static async Task<SortedSet<long>> GetMaThuaDatOrLichSuAsync(this ElisDataContext dataContext, int maDvhc)
    {
        if (maDvhc <= 0) return [];
        // Lấy danh sách Mã Thửa Đất
        var maThuaDats = await dataContext.GetMaThuaDatAsync(maDvhc);

        // Lấy danh sách Mã Thửa Đất Lịch sử
        maThuaDats.UnionWith(await dataContext.GetMaThuaDatLichSuAsync(maDvhc));

        // Trả về danh sách Mã Thửa Đất
        return maThuaDats;
    }

    /// <summary>
    /// Lấy danh sách Mã Thửa Đất dựa trên Mã Đơn Vị Hành Chính.
    /// </summary>
    /// <param name="dataContext">Ngữ cảnh cơ sở dữ liệu.</param>
    /// <param name="maDvhc">Mã Đơn Vị Hành Chính.</param>
    /// <returns>Danh sách Mã Thửa Đất.</returns>
    private static async Task<SortedSet<long>> GetMaThuaDatAsync(this ElisDataContext dataContext, int maDvhc)
    {
        if (maDvhc <= 0) return [];
        await using var command = dataContext.Database.GetDbConnection().CreateCommand();
        // Lấy danh sách Mã Thửa Đất dựa trên Mã Đơn Vị Hành Chính
        // Tạo câu lệnh SQL
        const string sql = """
                           SELECT ThuaDat.MaThuaDat
                           FROM   ThuaDat INNER JOIN ToBanDo ON ThuaDat.MaToBanDo = ToBanDo.MaToBanDo
                           WHERE ToBanDo.MaDVHC = @MaDvhc
                           ORDER BY ThuaDat.MaThuaDat
                           """;
        command.CommandText = sql;
        // Tham số @MaDvhc
        var parameterMaDvhc = command.CreateParameter();
        parameterMaDvhc.ParameterName = "@MaDvhc";
        parameterMaDvhc.Value = maDvhc;
        command.Parameters.Add(parameterMaDvhc);

        // Thực thi câu lệnh SQL
        var result = await command.ExecuteReaderAsync();

        // Đọc kết quả trả về
        SortedSet<long> maThuaDats = [];
        while (await result.ReadAsync())
        {
            maThuaDats.Add(result.GetInt64(0));
        }

        return maThuaDats;
    }

    /// <summary>
    /// Lấy danh sách Mã Thửa Đất Lịch sử dựa trên Mã Đơn Vị Hành Chính.
    /// </summary>
    /// <param name="dataContext">Ngữ cảnh cơ sở dữ liệu.</param>
    /// <param name="maDvhc">Mã Đơn Vị Hành Chính.</param>
    /// <returns>Danh sách Mã Thửa Đất.</returns>
    private static async Task<SortedSet<long>> GetMaThuaDatLichSuAsync(this ElisDataContext dataContext, int maDvhc)
    {
        if (maDvhc <= 0) return [];
        await using var command = dataContext.Database.GetDbConnection().CreateCommand();
        // Lấy danh sách Mã Thửa Đất dựa trên Mã Đơn Vị Hành Chính
        // Tạo câu lệnh SQL
        const string sql = """
                           SELECT ThuaDatLS.MaThuaDatLS
                           FROM   ThuaDatLS INNER JOIN ToBanDo ON ThuaDatLS.MaToBanDo = ToBanDo.MaToBanDo
                           WHERE ToBanDo.MaDVHC = @MaDvhc
                           ORDER BY ThuaDatLS.MaThuaDatLS
                           """;
        command.CommandText = sql;
        // Tham số @MaDvhc
        var parameterMaDvhc = command.CreateParameter();
        parameterMaDvhc.ParameterName = "@MaDvhc";
        parameterMaDvhc.Value = maDvhc;
        command.Parameters.Add(parameterMaDvhc);

        // Thực thi câu lệnh SQL
        var result = await command.ExecuteReaderAsync();

        // Đọc kết quả trả về
        SortedSet<long> maThuaDats = [];
        while (await result.ReadAsync())
        {
            maThuaDats.Add(result.GetInt64(0));
        }

        return maThuaDats;
    }


    public static async Task RenewMaThuaDatOrLichSuAsync(this ElisDataContext dbContext,
        long maThuaDat,
        long newMaThuaDat,
        long? maThuaDatTemp = null)
    {
        await dbContext.RenewMaThuaDatAsync(maThuaDat, newMaThuaDat, maThuaDatTemp);
        await dbContext.RenewMaThuaDatLsAsync(maThuaDat, newMaThuaDat, maThuaDatTemp);
    }

    /// <summary>
    /// Cập nhật mã Thửa Đất.
    /// </summary>
    /// <param name="dbContext">Ngữ cảnh cơ sở dữ liệu.</param>
    /// <param name="maThuaDat">Mã Thửa Đất hiện tại.</param>
    /// <param name="newMaThuaDat">Mã Thửa Đất mới.</param>
    /// <param name="maThuaDatTemp">Mã Thửa Đất tạm thời (tùy chọn).</param>
    private static async Task RenewMaThuaDatAsync(this ElisDataContext dbContext,
        long maThuaDat,
        long newMaThuaDat,
        long? maThuaDatTemp = null)
    {
        maThuaDatTemp ??= DefaultTempMaThuaDat;
        // Cập nhật mã đăng ký theo mã tạm thời:
        await dbContext.UpdateMaThuaDatOnDangKyThuaDatAsync(maThuaDat);
        // Cập nhật mã thửa đất mới:
        var thuaDat = await dbContext.ThuaDats.FindAsync(maThuaDat);
        if (thuaDat == null) return;
        thuaDat.MaThuaDat = newMaThuaDat;
        dbContext.ThuaDats.Update(thuaDat);
        await dbContext.SaveChangesAsync();

        // Cập nhật lại mã đăng ký theo mã thửa đất mới:
        await dbContext.UpdateMaThuaDatOnDangKyThuaDatAsync(maThuaDatTemp.Value, newMaThuaDat);

        // Cập nhật thông thửa đất cũ:
        await dbContext.UpdateMaThuaDatCuAsync(maThuaDat, newMaThuaDat);
    }

    private static async Task RenewMaThuaDatLsAsync(this ElisDataContext dbContext,
        long maThuaDatLs,
        long newMaThuaDatLs,
        long? maThuaDatTemp = null)
    {
        maThuaDatTemp ??= DefaultTempMaThuaDat;
        // Cập nhật mã đăng ký theo mã tạm thời:
        await dbContext.UpdateMaThuaDatLsOnDangKyThuaDatLsAsync(maThuaDatLs);
        // Cập nhật mã thửa đất mới:
        var thuaDatLs = await dbContext.ThuaDatLichSus.FindAsync(maThuaDatLs);
        if (thuaDatLs == null) return;
        thuaDatLs.MaThuaDatLs = newMaThuaDatLs;
        dbContext.ThuaDatLichSus.Update(thuaDatLs);
        await dbContext.SaveChangesAsync();

        // Cập nhật lại mã đăng ký theo mã thửa đất mới:
        await dbContext.UpdateMaThuaDatLsOnDangKyThuaDatLsAsync(maThuaDatTemp.Value, newMaThuaDatLs);
    }
}