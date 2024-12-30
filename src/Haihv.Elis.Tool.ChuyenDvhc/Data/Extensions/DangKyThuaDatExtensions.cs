using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;

public static class DangKyThuaDatExtensions
{
    private const long DefaultTempMaThuaDat = ThuaDatExtensions.DefaultTempMaThuaDat;
    private static readonly long DefaultTempMaDangKy = long.MaxValue;

    /// <summary>
    /// Cập nhật mã thửa đất trên đăng ký thửa đất.
    /// </summary>
    /// <param name="dbContext">Ngữ cảnh dữ liệu Elis.</param>
    /// <param name="maThuaDat">Mã thửa đất hiện tại.</param>
    /// <param name="newMaThuaDat">
    /// Mã thửa đất mới (tùy chọn).
    /// Nếu không được cung cấp, sẽ sử dụng mã thửa đất tạm thời [DefaultTempMaThuaDat].
    /// </param>
    /// <returns>Task bất đồng bộ.</returns>
    public static async Task UpdateMaThuaDatOnDangKyThuaDatAsync(this ElisDataContext dbContext, long maThuaDat,
        long? newMaThuaDat = null)
    {
        var dangKyThuaDats = await dbContext.DangKys
            .Where(d => d.MaThuaDat == maThuaDat)
            .ToListAsync();
        foreach (var dangKyThuaDat in dangKyThuaDats)
        {
            dangKyThuaDat.MaThuaDat = newMaThuaDat ?? DefaultTempMaThuaDat;
        }

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Cập nhật mã thửa đất lịch sử trên đăng ký thửa đất lịch sử.
    /// </summary>
    /// <param name="dbContext">Ngữ cảnh dữ liệu Elis.</param>
    /// <param name="maThuaDatLs">Mã thửa đất lịch sử hiện tại.</param>
    /// <param name="newMaThuaDatLs">
    /// Mã thửa đất lịch sử mới (tùy chọn).
    /// Nếu không được cung cấp, sẽ sử dụng mã thửa đất tạm thời [DefaultTempMaThuaDat].
    /// </param>
    /// <returns>Task bất đồng bộ.</returns>
    public static async Task UpdateMaThuaDatLsOnDangKyThuaDatLsAsync(this ElisDataContext dbContext, long maThuaDatLs,
        long? newMaThuaDatLs = null)
    {
        var dangKyThuaDats = await dbContext.DangKyLichSus
            .Where(d => d.MaThuaDatLs == maThuaDatLs)
            .ToListAsync();
        foreach (var dangKyThuaDat in dangKyThuaDats)
        {
            dangKyThuaDat.MaThuaDatLs = newMaThuaDatLs ?? DefaultTempMaThuaDat;
        }

        await dbContext.SaveChangesAsync();
    }
}