using Dapper;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;
using Serilog;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;

public class DangKyThuaDatRepository(string connectionString, ILogger? logger = null)
{
    private const long DefaultTempMaThuaDat = ThuaDatRepository.DefaultTempMaThuaDat;
    //private static readonly long DefaultTempMaDangKy = long.MaxValue;

    /// <summary>
    /// Cập nhật mã thửa đất trên đăng ký thửa đất.
    /// </summary>
    /// <param name="maThuaDat">Mã thửa đất hiện tại.</param>
    /// <param name="newMaThuaDat">
    /// Mã thửa đất mới (tùy chọn).
    /// Nếu không được cung cấp, sẽ sử dụng mã thửa đất tạm thời [DefaultTempMaThuaDat].
    /// </param>
    /// <param name="isLichSu">Có phải là đăng ký thửa đất lịch sử hay không.</param>
    /// <returns>Task bất đồng bộ.</returns>
    public async Task UpdateMaThuaDatOnDangKyThuaDatAsync(long maThuaDat, long? newMaThuaDat = null,
        bool isLichSu = false)
    {
        await using var connection = connectionString.GetConnection();
        try
        {
            var sql = isLichSu
                    ? """
                         UPDATE DangKyQSDD
                         SET MaThuaDat = @NewMaThuaDat
                         WHERE MaThuaDat = @MaThuaDat
                      """
                    : """
                         UPDATE DangKyQSDDLS
                         SET MaThuaDatLS = @NewMaThuaDat
                         WHERE MaThuaDatLS = @MaThuaDat
                      """
                ;
            await connection.ExecuteAsync(sql,
                new { NewMaThuaDat = newMaThuaDat ?? DefaultTempMaThuaDat, MaThuaDat = maThuaDat });
        }
        catch (Exception e)
        {
            if (logger == null) throw;
            logger.Error(e,
                "Lỗi khi cập nhật mã thửa đất trên đăng ký thửa đất. MaThuaDat: {MaThuaDat}, NewMaThuaDat: {NewMaThuaDat}, IsLichSu: {IsLichSu}",
                maThuaDat, newMaThuaDat, isLichSu);
        }
    }

    public async Task RenewMaDangKyAsync(DvhcRecord capXaSau, int limit = 100)
    {
        throw new NotImplementedException();
    }
}