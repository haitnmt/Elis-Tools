using Dapper;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Microsoft.Data.SqlClient;
using Serilog;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;

public sealed class ThuaDatRepository(string connectionString, ILogger? logger = null)
{
    public const long DefaultTempMaThuaDat = long.MaxValue;

    /// <summary>
    /// Lấy số lượng Thửa Đất dựa trên danh sách Mã Tờ Bản Đồ.
    /// </summary>
    /// <param name="maDvhcBiSapNhap">
    /// Danh sách Mã Đơn Vị Hành Chính đang bị sập nhập.
    /// </param>
    /// <returns>Số lượng Thửa Đất.</returns>
    public async Task<int> GetCountThuaDatAsync(List<int> maDvhcBiSapNhap)
    {
        if (maDvhcBiSapNhap.Count == 0) return 0;
        // Lấy kết nối cơ sở dữ liệu
        await using var connection = connectionString.GetConnection();
        try
        {
            const string sql = """
                               SELECT COUNT(ThuaDat.MaThuaDat) 
                               FROM   ThuaDat INNER JOIN ToBanDo ON ThuaDat.MaToBanDo = ToBanDo.MaToBanDo
                               WHERE (ToBanDo.MaDVHC IN @MaDVHC)
                               """;
            return await connection.ExecuteAsync(sql, new { MaDVHC = maDvhcBiSapNhap });
        }
        catch (Exception e)
        {
            if (logger == null) throw;
            logger.Error(e, "Lỗi khi lấy số lượng Thửa Đất theo mã Đơn Vị Hành Chính. [{MaDVHC}]", maDvhcBiSapNhap);
            return -1;
        }
    }

    /// <summary>
    /// Lấy danh sách Mã Thửa Đất dựa trên danh sách Mã Tờ Bản Đồ.
    /// </summary>
    /// <param name="dvhcBiSapNhap">Bản ghi Đơn Vị Hành Chính đang bị sập nhập. </param>
    /// <param name="minMaThuaDat">Giá trị tối thiểu của Mã Thửa Đất.</param>
    /// <param name="limit">Số lượng giới hạn kết quả trả về.</param>
    /// <param name="formatGhiChuThuaDat">Định dạng Ghi Chú Thửa Đất.</param>
    /// <param name="ngaySapNhap">Ngày sáp nhập.</param>
    /// <returns>Danh sách Mã Thửa Đất.</returns>
    public async Task<List<ThuaDatCapNhat>> UpdateAndGetThuaDatToBanDoAsync(DvhcRecord dvhcBiSapNhap,
        long minMaThuaDat = long.MinValue,
        int limit = 100, string? formatGhiChuThuaDat = null, string ngaySapNhap = "")
    {
        // Lấy kết nối cơ sở dữ liệu
        await using var connection = connectionString.GetConnection();

        // Khởi tạo giá trị mặc định cho các tham số
        if (string.IsNullOrWhiteSpace(formatGhiChuThuaDat))
            formatGhiChuThuaDat = ThamSoThayThe.DefaultGhiChuThuaDat;
        if (string.IsNullOrWhiteSpace(ngaySapNhap))
            ngaySapNhap = DateTime.Now.ToString(ThamSoThayThe.DinhDangNgaySapNhap);

        // Lấy danh sách Thửa Đất cần cập nhật
        // Tạo câu lệnh SQL
        const string sql = """
                           SELECT DISTINCT TOP (@Limit) ThuaDat.MaThuaDat, ThuaDat.ThuaDatSo, ToBanDo.SoTo
                           FROM   ThuaDat INNER JOIN ToBanDo ON ThuaDat.MaToBanDo = ToBanDo.MaToBanDo
                           WHERE (ToBanDo.MaDVHC = @MaDvhc AND ThuaDat.MaThuaDat > @MinMaThuaDat)
                           ORDER BY ThuaDat.MaThuaDat
                           OPTION (RECOMPILE)
                           """;
        // Tạo tham số cho câu lệnh SQL
        var parameters = new
        {
            dvhcBiSapNhap.MaDvhc,
            Limit = limit,
            MinMaThuaDat = minMaThuaDat
        };

        // Thực thi câu lệnh SQL
        var thuaDats = (await connection.QueryAsync<(long MaThuaDat, string ThuaDatSo, string SoTo)>(sql, parameters))
            .ToList();
        // Tạo danh sách Thửa Đất cập nhật
        var thuaDatCapNhats = thuaDats.Select(thuaDat =>
            new ThuaDatCapNhat(thuaDat.MaThuaDat, thuaDat.ThuaDatSo, thuaDat.SoTo, dvhcBiSapNhap.Ten)).ToList();

        // Cập nhật thông tin Thửa Đất
        // Tạo câu lệnh SQL
        const string updateQuery = """
                                   UPDATE ThuaDat
                                   SET GhiChu = @GhiChu
                                   WHERE MaThuaDat = @MaThuaDat
                                   """;
        // Tạo tham số cho câu lệnh SQL
        var parametersUpdate = thuaDatCapNhats.Select(thuaDatCapNhat => new
        {
            GhiChu = formatGhiChuThuaDat
                .Replace(ThamSoThayThe.NgaySapNhap, ngaySapNhap)
                .Replace(ThamSoThayThe.ToBanDo, thuaDatCapNhat.ToBanDo)
                .Replace(ThamSoThayThe.DonViHanhChinh, dvhcBiSapNhap.Ten),
            thuaDatCapNhat.MaThuaDat
        });
        // Thực thi câu lệnh SQL
        await connection.ExecuteAsync(updateQuery, parametersUpdate);

        // Trả về danh sách Thửa Đất cập nhật
        return thuaDatCapNhats;
    }

    /// <summary>
    /// Tạo hoặc cập nhật thông tin Thửa Đất Cũ.
    /// </summary>
    /// <param name="thuaDatCapNhats">Danh sách Thửa Đất cần cập nhật.</param>
    /// <param name="formatToBanDoCu">Định dạng tờ bản đồ cũ.</param>
    /// <param name="cancellationToken">Token hủy bỏ (tùy chọn).</param>
    /// <returns>Trả về true nếu có bản ghi được tạo hoặc cập nhật, ngược lại trả về false.</returns>
    public async Task CreateOrUpdateThuaDatCuAsync(List<ThuaDatCapNhat> thuaDatCapNhats,
        string? formatToBanDoCu = null, CancellationToken cancellationToken = default)
    {
        if (thuaDatCapNhats.Count == 0) return;
        if (string.IsNullOrWhiteSpace(formatToBanDoCu))
            formatToBanDoCu = ThamSoThayThe.DefaultToBanDoCu;
        await using var connection = connectionString.GetConnection();
        try
        {
            const string upsertQuery = """
                                       MERGE INTO ThuaDatCu AS Target
                                       USING (SELECT @MaThuaDat AS MaThuaDat) AS Source
                                       ON Target.MaThuaDat = Source.MaThuaDat
                                       WHEN MATCHED THEN
                                           UPDATE SET ToBanDoCu = CONCAT(Target.ToBanDoCu, ' - [', @ToBanDoCu, ']')
                                       WHEN NOT MATCHED THEN
                                           INSERT (MaThuaDat, ToBanDoCu)
                                           VALUES (@MaThuaDat, @ToBanDoCu);
                                       """;
            foreach (var thuaDatCapNhat in thuaDatCapNhats)
            {
                var toBanDoCu = formatToBanDoCu
                    .Replace(ThamSoThayThe.ToBanDo, thuaDatCapNhat.ToBanDo)
                    .Replace(ThamSoThayThe.DonViHanhChinh, thuaDatCapNhat.TenDonViHanhChinh);
                await connection.ExecuteAsync(upsertQuery, new { thuaDatCapNhat.MaThuaDat, ToBanDoCu = toBanDoCu });
            }
        }
        catch (Exception ex)
        {
            if (logger == null) throw;
            logger.Error(ex, "Lỗi khi tạo hoặc cập nhật thông tin Thửa Đất Cũ");
        }
    }

    private static async Task<long> CreateTempThuaDatAsync(SqlConnection connection,
        long? maToBanDo = null, long? maThuaDatTemp = null, bool isLichSu = false)
    {
        // Tạo Tờ Bản Đồ tạm thời
        maToBanDo = await ToBanDoRepository.CreateTempToBanDoAsync(connection, maToBanDo);

        // Tạo câu lệnh SQL để tạo hoặc cập nhật Thửa Đất tạm thời
        var sql = isLichSu
            ? """
                IF NOT EXISTS (SELECT 1 FROM ThuaDat WHERE MaThuaDat = @MaThuaDat)
                BEGIN
                    INSERT INTO ThuaDat (MaThuaDat, MaToBanDo, ThuaDatSo, GhiChu)
                    VALUES (@MaThuaDat, @MaToBanDo, @ThuaDatSo, @GhiChu)
                END
                ELSE
                BEGIN
                    UPDATE ThuaDat
                    SET MaToBanDo = @MaToBanDo, ThuaDatSo = @ThuaDatSo, GhiChu = @GhiChu
                    WHERE MaThuaDat = @MaThuaDat
                END
              """
            : """
                  IF NOT EXISTS (SELECT 1 FROM ThuaDatLS WHERE MaThuaDatLS = @MaThuaDat)
                  BEGIN
                      INSERT INTO ThuaDatLS (MaThuaDatLS, MaToBanDo, ThuaDatSo, GhiChu)
                      VALUES (@MaThuaDat, @MaToBanDo, @ThuaDatSo, @GhiChu)
                  END
                  ELSE
                  BEGIN
                      UPDATE ThuaDatLS
                      SET MaToBanDo = @MaToBanDo, ThuaDatSo = @ThuaDatSo, GhiChu = @GhiChu
                      WHERE MaThuaDatLS = @MaThuaDat
                  END
              """;
        // Tạo tham số cho câu lệnh SQL
        var parameters = new
        {
            MaThuaDat = maThuaDatTemp ?? DefaultTempMaThuaDat,
            MaToBanDo = maToBanDo,
            ThuaDatSo = "Temp",
            GhiChu = "Thửa đất tạm thời"
        };
        // Thực thi câu lệnh SQL
        await connection.ExecuteAsync(sql, parameters);
        return parameters.MaThuaDat;
    }

    /// <summary>
    /// Tạo Thửa Đất tạm thời.
    /// </summary>
    /// <param name="maToBanDo">Mã Tờ Bản Đồ.</param>
    /// <param name="maThuaDatTemp">Mã Thửa Đất tạm thời (tùy chọn).</param>
    /// <param name="cancellationToken">Token hủy bỏ.</param>
    /// <returns>Mã Thửa Đất tạm thời.</returns>
    public async Task<long> CreateTempThuaDatAsync(long? maToBanDo = null, long? maThuaDatTemp = null,
        CancellationToken cancellationToken = default)
    {
        // Lấy kết nối cơ sở dữ liệu
        await using var connection = connectionString.GetConnection();
        try
        {
            await CreateTempThuaDatAsync(connection, maToBanDo, maThuaDatTemp);
            return await CreateTempThuaDatAsync(connection, maToBanDo, maThuaDatTemp, true);
        }
        catch (Exception e)
        {
            if (logger == null) throw;
            logger.Error(e, "Lỗi khi tạo Thửa Đất tạm thời. [MaToBanDo: {MaToBanDo}, MaThuaDatTemp: {MaThuaDatTemp}]",
                maToBanDo, maThuaDatTemp);
            return long.MinValue;
        }
    }

    public async Task<bool> UpdateMaToBanDoAsync(long maToBanDo, long newMaToBanDo)
    {
        try
        {
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();

            // Tạo câu lệnh SQL
            const string sqlThuaDat = """
                                      UPDATE ThuaDat
                                      SET MaToBanDo = @NewMaToBanDo
                                      WHERE MaToBanDo = @MaToBanDo
                                      """;
            // Tạo tham số cho câu lệnh SQL
            var parameters = new { NewMaToBanDo = newMaToBanDo, MaToBanDo = maToBanDo };

            // Thực thi câu lệnh SQL cập nhật thưa đất
            await connection.ExecuteAsync(sqlThuaDat, parameters);

            // Thực thi câu lệnh SQL cập nhật thửa đất lịch sử
            await connection.ExecuteAsync(sqlThuaDat.Replace("ThuaDat", "ThuaDatLS"), parameters);
            return true;
        }
        catch (Exception exception)
        {
            if (logger == null) throw;
            logger.Error(exception,
                "Lỗi khi cập nhật Mã Tờ Bản Đồ. [MaToBanDo: {MaToBanDo}, NewMaToBanDo: {NewMaToBanDo}]",
                maToBanDo, newMaToBanDo);
            return false;
        }
    }
}