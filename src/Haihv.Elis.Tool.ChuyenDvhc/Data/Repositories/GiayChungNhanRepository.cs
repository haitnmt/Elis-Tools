using System.Data;
using Dapper;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Serilog;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;

public class GiayChungNhanRepository(string connectionString, ILogger? logger = null)
{
    #region Cập nhật ghi chú vào Giấy chứng nhận

    /// <summary>
    /// Cập nhật ghi chú vào Giấy chứng nhận
    /// </summary>
    /// <param name="thuaDatCapNhats">Danh sách Thửa đất cần cập nhật ghi chú.</param>
    /// <param name="formatGhiChu">Định dạng ghi chú.</param>
    /// <param name="ngaySapNhap">Ngày sáp nhập.</param>
    /// <returns></returns>
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

    #endregion

    #region Làm mới mã Giấy chứng nhận

    private const long DefaultTempGiayChungNhan = 0;

    /// <summary>
    /// Kiểm tra xem số lượng Giấy chứng nhận theo Đơn Vị Hành Chính có vượt quá giới hạn không.
    /// </summary>
    /// <param name="dvhc">Đơn vị hành chính.</param>
    /// <returns>Trả về true nếu không vượt quá giới hạn, ngược lại trả về false.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn.</exception>
    private async Task<bool> CheckOverflowAsync(DvhcRecord dvhc)
    {
        // Lấy tổng số lượng Giấy chứng nhận theo Đơn Vị Hành Chính
        const string query = """
                             SELECT COUNT(DISTINCT MaGCN) AS Total
                             FROM (
                                 SELECT DISTINCT gcn.MaGCN AS MaGCN
                                 FROM GCNQSDD gcn
                                          INNER JOIN DangKyQSDD dk ON gcn.MaDangKy = dk.MaDangKy
                                          INNER JOIN ThuaDat td ON dk.MaThuaDat = td.MaThuaDat
                                          INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                 WHERE MaDVHC = @MaDVHC
                                 UNION
                                 SELECT DISTINCT gcn.MaGCNLS AS MaGCN
                                 FROM GCNQSDDLS gcn
                                          INNER JOIN DangKyQSDD dk ON gcn.MaDangKyLS = dk.MaDangKy
                                          INNER JOIN ThuaDat td ON dk.MaThuaDat = td.MaThuaDat
                                          INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                 WHERE MaDVHC = @MaDVHC
                                 UNION
                                 SELECT DISTINCT gcn.MaGCNLS AS MaGCN
                                 FROM GCNQSDDLS gcn
                                          INNER JOIN DangKyQSDDLS dk ON gcn.MaDangKyLS = dk.MaDangKyLS
                                          INNER JOIN ThuaDatLS td ON dk.MaThuaDatLS = td.MaThuaDatLS
                                          INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                 WHERE MaDVHC = @MaDVHC
                                 UNION
                                 SELECT DISTINCT gcn.MaGCNLS AS MaGCN
                                 FROM GCNQSDDLS gcn
                                          INNER JOIN DangKyQSDDLS dk ON gcn.MaDangKyLS = dk.MaDangKyLS
                                          INNER JOIN ThuaDat td ON dk.MaThuaDatLS = td.MaThuaDat
                                          INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                 WHERE MaDVHC = @MaDVHC
                                 ) AS CombinedResult
                             """;
        try
        {
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();
            // Tạo tham số cho câu lệnh SQL
            var parameters = new { MaDVHC = dvhc.MaDvhc };
            // Thực thi câu lệnh SQL
            var count = await connection.ExecuteScalarAsync<int>(query, parameters);
            // Kiểm tra xem số lượng Đăng ký có vượt quá giới hạn không
            return count < PrimaryKeyExtensions.GetMaximumPrimaryKey();
        }
        catch (Exception e)
        {
            logger?.Error(e, "Lỗi khi kiểm tra vượt quá giới hạn Giấy chứng nhận. [MaDVHC: {MaDVHC}]", dvhc.MaDvhc);
            return false;
        }
    }

    /// <summary>
    /// Tạo Giấy chứng nhận tạm thời.
    /// </summary>
    /// <param name="maGiayChungNhanTemp">Mã Giấy chứng nhận tạm thời.
    /// Mặc định: <see cref="DefaultTempGiayChungNhan"/>
    /// </param>
    /// <param name="maDangKyTemp">Mã Đăng ký tạm thời.
    /// Mặc định: <see cref="DangKyThuaDatRepository.DefaultTempMaDangKy"/>
    /// </param>
    /// <param name="reCreateTempDangKy">Tạo lại Đăng ký tạm thời.</param>
    /// <returns>Trả về mã Giấy chứng nhận tạm thời.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn.</exception>
    private async Task<long> CreateTempGiayChungNhanAsync(long maGiayChungNhanTemp = DefaultTempGiayChungNhan,
        long maDangKyTemp = DangKyThuaDatRepository.DefaultTempMaDangKy, bool reCreateTempDangKy = false)
    {
        try
        {
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();

            // Tạo Đăng ký tạm thời nếu cần
            if (reCreateTempDangKy && maDangKyTemp == DangKyThuaDatRepository.DefaultTempMaDangKy)
                maDangKyTemp = await new DangKyThuaDatRepository(connectionString, logger)
                    .CreateTempDangKyAsync(reCreateTempThuaDat: true);

            // Tạo câu lệnh SQL để tạo hoặc cập nhật Giấy chứng nhận tạm thời
            const string queryGiayChungNhan = """
                                              IF NOT EXISTS (SELECT 1 FROM GCNQSDD WHERE MaGCN = @MaGCN)
                                              BEGIN
                                                  INSERT INTO GCNQSDD (MaGCN, MaDangKy, GhiChu)
                                                  VALUES (@MaGCN, @MaDangKy, @GhiChu)
                                              END
                                              ELSE
                                              BEGIN
                                                  UPDATE GCNQSDD
                                                  SET MaDangKy = @MaDangKy, GhiChu = @GhiChu
                                                  WHERE MaGCN = @MaGCN
                                              END;
                                              """;
            const string queryGiayChungNhanLichSu = """
                                                    IF NOT EXISTS (SELECT 1 FROM GCNQSDDLS WHERE MaGCNLS = @MaGCN)
                                                    BEGIN
                                                        INSERT INTO GCNQSDDLS (MaGCNLS, MaDangKyLS, GhiChu)
                                                        VALUES (@MaGCN, @MaDangKy, @GhiChu)
                                                    END
                                                    ELSE
                                                    BEGIN
                                                        UPDATE GCNQSDDLS
                                                        SET MaDangKyLS = @MaDangKy, GhiChu = @GhiChu
                                                        WHERE MaGCNLS = @MaGCN
                                                    END;
                                                    """;
            const string query = $"""
                                  {queryGiayChungNhan}
                                  {queryGiayChungNhanLichSu}
                                  """;

            // Khởi tạo tham số cho câu lệnh SQL
            var parameters = new
            {
                MaGCN = maGiayChungNhanTemp,
                MaDangKy = maDangKyTemp,
                GhiChu = "Giấy chứng nhận tạm thời."
            };
            if (connection.State == ConnectionState.Closed)
            {
                await connection.OpenAsync();
            }

            // Thực thi câu lệnh SQL
            await using var transaction = connection.BeginTransaction();
            try
            {
                await connection.ExecuteAsync(query, parameters, transaction);
                await transaction.CommitAsync();
                return maGiayChungNhanTemp;
            }
            catch (Exception e)
            {
                // Rollback transaction
                await transaction.RollbackAsync();
                Console.WriteLine(e);
                logger?.Error(e, "Lỗi khi tạo Giấy chứng nhận tạm thời.");
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            logger?.Error(ex, "Lỗi khi tạo Giấy chứng nhận tạm thời.");
            throw;
        }
    }

    /// <summary>
    /// Lấy mã Giấy chứng nhận lớn nhất.
    /// </summary>
    /// <param name="dvhc">Đơn vị hành chính.</param>
    /// <returns>Trả về mã Giấy chứng nhận lớn nhất.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn.</exception>
    private async Task<long> GetMaxMaGiayChungNhanAsync(DvhcRecord dvhc)
    {
        // Tạo câu lệnh SQL để lấy mã Giấy chứng nhận lớn nhất
        const string queryMaxGiayChungNhan = """
                                             SELECT ISNULL(MAX(gcn.MaGCN), 0) AS MaGCN
                                             FROM GCNQSDD gcn
                                                  INNER JOIN DangKyQSDD dk ON gcn.MaDangKy = dk.MaDangKy
                                                  INNER JOIN ThuaDat td ON dk.MaThuaDat = td.MaThuaDat
                                                  INNER JOIN ToBanDo td ON td.MaToBanDo = tbd.MaToBanDo
                                             WHERE MaDVHC = @MaDVHC
                                                AND gcn.MaGCN > @MinMaGCN
                                                AND gcn.MaGCN < @MaxMaGCN
                                             """;
        const string queryMaxGiayChungNhanLichSuOnDangKyLs = """
                                                             SELECT ISNULL(MAX(gcn.MaGCNLS), 0) AS MaGCN
                                                             FROM GCNQSDDLS gcn
                                                                      INNER JOIN DangKyQSDDLS dk ON gcn.MaDangKyLS = dk.MaDangKyLS
                                                                      INNER JOIN ThuaDatLS td ON dk.MaThuaDatLS = td.MaThuaDatLS
                                                                      INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                                             WHERE MaDVHC = @MaDVHC
                                                                 AND gcn.MaGCN > @MinMaGCN
                                                                 AND gcn.MaGCN < @MaxMaGCN
                                                             """;
        const string queryMaxGiayChungNhanLichSuOnDangKy = """
                                                           SELECT ISNULL(MAX(gcn.MaGCNLS), 0) AS MaGCN
                                                           FROM GCNQSDDLS gcn
                                                                    INNER JOIN DangKyQSDD dk ON gcn.MaDangKyLS = dk.MaDangKy
                                                                    INNER JOIN ThuaDat td ON dk.MaThuaDat = td.MaThuaDat
                                                                    INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                                           WHERE MaDVHC = @MaDVHC
                                                              AND gcn.MaGCN > @MinMaGCN
                                                              AND gcn.MaGCN < @MaxMaGCN
                                                           """;

        const string queryMaxGiayChungNhanLichSuOnDangKyLsOnThuaDat = """
                                                                      SELECT ISNULL(MAX(gcn.MaGCNLS), 0) AS MaGCN
                                                                      FROM GCNQSDDLS gcn
                                                                               INNER JOIN DangKyQSDDLS dk ON gcn.MaDangKyLS = dk.MaDangKyLS
                                                                               INNER JOIN ThuaDat td ON dk.MaThuaDatLS = td.MaThuaDat
                                                                               INNER JOIN ToBanDo tbd ON td.MaToBanDo = tbd.MaToBanDo
                                                                      WHERE MaDVHC = @MaDVHC
                                                                         AND gcn.MaGCN > @MinMaGCN
                                                                         AND gcn.MaGCN < @MaxMaGCN
                                                                      """;
        const string query = $"""
                              SELECT MAX(MaGCN)
                              FROM (
                                     {queryMaxGiayChungNhan}
                                  UNION
                                     {queryMaxGiayChungNhanLichSuOnDangKyLs}
                                  UNION
                                     {queryMaxGiayChungNhanLichSuOnDangKy}
                                  UNION
                                     {queryMaxGiayChungNhanLichSuOnDangKyLsOnThuaDat}
                                  ) AS CombinedResult
                              """;
        // Khởi tạo tham số cho câu lệnh SQL
        var parameters = new
        {
            MaDVHC = dvhc.MaDvhc,
            MinMaGCN = dvhc.Ma.GetMinPrimaryKey(),
            MaxMaGCN = dvhc.Ma.GetMaxPrimaryKey()
        };
        try
        {
            await using var connection = connectionString.GetConnection();
            return await connection.ExecuteScalarAsync<long>(query);
        }
        catch (Exception e)
        {
            logger?.Error(e, "Lỗi khi lấy mã Giấy chứng nhận lớn nhất.");
            throw;
        }
    }

    public async Task RenewMaGiayChungNhanAsync(DvhcRecord capXaSau, int limit = 100)
    {
        throw new NotImplementedException();
    }

    #endregion
}