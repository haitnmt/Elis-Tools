using Dapper;
using Microsoft.Data.SqlClient;
using Serilog;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;

public class DangKyThuaDatRepository(
    ILogger? logger = null,
    string? connectionString = null,
    SqlConnection? dbConnection = null) :
    DataRepository(logger, connectionString, dbConnection)
{
    private readonly ILogger? _logger = logger;
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
    /// <param name="cancellationToken">Token hủy bỏ để hủy tác vụ không đồng bộ.</param>
    /// <returns>Task bất đồng bộ.</returns>
    public async Task UpdateMaThuaDatOnDangKyThuaDatAsync(long maThuaDat, long? newMaThuaDat = null,
        bool isLichSu = false, CancellationToken cancellationToken = default)
    {
        await using var connection = await GetAndOpenConnectionAsync(cancellationToken);
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
            if (_logger == null) throw;
            _logger.Error(e,
                "Lỗi khi cập nhật mã thửa đất trên đăng ký thửa đất. MaThuaDat: {MaThuaDat}, NewMaThuaDat: {NewMaThuaDat}, IsLichSu: {IsLichSu}",
                maThuaDat, newMaThuaDat, isLichSu);
        }
    }
}