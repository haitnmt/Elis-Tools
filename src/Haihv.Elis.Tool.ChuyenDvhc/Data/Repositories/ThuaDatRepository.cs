﻿using Dapper;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Serilog;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;

public sealed class ThuaDatRepository(string connectionString, ILogger? logger = null)
{
    public const long DefaultTempMaThuaDat = 0;

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
        const string query = """
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
        var thuaDats = (await connection.QueryAsync<(long MaThuaDat, string ThuaDatSo, string SoTo)>(query, parameters))
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

    /// <summary>
    /// Tạo Thửa Đất tạm thời.
    /// </summary>
    /// <param name="tempMaToBanDo">Mã Tờ Bản Đồ.</param>
    /// <param name="maThuaDatTemp">
    /// Mã Thửa Đất tạm thời (tùy chọn).
    /// Mặc định: <see cref="ToBanDoRepository.DefaultTempMaToBanDo"/>
    /// </param>
    /// <param name="reCreateTempToBanDo">Tạo lại Tờ Bản Đồ tạm thời (tùy chọn). Mặc định = false.</param>
    /// <returns>Mã Thửa Đất tạm thời.</returns>
    private async Task<long> CreateTempThuaDatAsync(long maThuaDatTemp = DefaultTempMaThuaDat,
        long tempMaToBanDo = ToBanDoRepository.DefaultTempMaToBanDo, bool reCreateTempToBanDo = false)
    {
        try
        {
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();

            if (reCreateTempToBanDo && tempMaToBanDo == ToBanDoRepository.DefaultTempMaToBanDo)
            {
                // Tạo lại Tờ Bản Đồ tạm thời
                tempMaToBanDo = await ToBanDoRepository.CreateTempToBanDoAsync(connection, tempMaToBanDo, logger: logger);
            }
            
            // Tạo câu lệnh SQL để tạo hoặc cập nhật Thửa Đất tạm thời
            const string sqlThuaDat = """
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
                                      END;
                                      """;
            const string sqlThuaDatLs = """
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
                                          END;
                                        """;
            var query = $"""
                           {sqlThuaDat}
                           {sqlThuaDatLs}
                         """;
            // Tạo tham số cho câu lệnh SQL
            var parameters = new
            {
                MaThuaDat = maThuaDatTemp,
                MaToBanDo = tempMaToBanDo,
                ThuaDatSo = "Temp",
                GhiChu = "Thửa đất tạm thời"
            };
            // Sử dụng giao dịch để đảm bảo tính nhất quán
            await using var transaction = connection.BeginTransaction();

            try
            {
                // Thực thi câu lệnh SQL trong giao dịch
                await connection.ExecuteAsync(query, parameters, transaction: transaction);
                // Commit giao dịch nếu thành công
                transaction.Commit();
            }
            catch
            {
                // Rollback giao dịch nếu có lỗi
                transaction.Rollback();
                throw;
            }

            return parameters.MaThuaDat;
        }
        catch (Exception e)
        {
            if (logger == null) throw;
            logger.Error(e,
                "Lỗi khi tạo Thửa Đất tạm thời. [MaToBanDo: {TempMaToBanDo}, MaThuaDatTemp: {MaThuaDatTemp}]",
                tempMaToBanDo, maThuaDatTemp);
            return long.MinValue;
        }
    }

    /// <summary>
    /// Lấy Mã Thửa Đất lớn nhất của Đơn Vị Hành Chính.
    /// </summary>
    /// <param name="dvhc">Bản ghi Đơn Vị Hành Chính.</param>
    /// <returns>Mã Thửa Đất lớn nhất.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn.</exception>
    private async Task<long> GetMaxMaThuaDatAsync(DvhcRecord dvhc)
    {
        try
        {
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();
            // Tạo câu lệnh query SQL
            const string sqlThuaDat = """
                                      SELECT MaThuaDat AS MaThuaDat
                                      FROM ThuaDat INNER JOIN
                                       ToBanDo ON ThuaDat.MaToBanDo = ToBanDo.MaToBanDo
                                      WHERE MaDVHC = @MaDvhc AND MaThuaDat > @MinMaThuaDat AND MaThuaDat < @MaxMaThuaDat
                                      """;
            const string sqlThuaDatLs = """
                                        SELECT MaThuaDatLS AS MaThuaDat
                                        FROM ThuaDatLS INNER JOIN
                                         ToBanDo ON ThuaDatLS.MaToBanDo = ToBanDo.MaToBanDo
                                        WHERE MaDVHC = @MaDvhc AND MaThuaDatLS > @MinMaThuaDat AND MaThuaDatLS < @MaxMaThuaDat
                                        """;
            const string query = $"""
                                  SELECT MAX(MaThuaDat) AS MaxValue
                                  FROM (
                                      {sqlThuaDat}
                                      UNION
                                      {sqlThuaDatLs}
                                  ) AS CombinedResults;
                                  """;
            var param = new
            {
                dvhc.MaDvhc,
                MinMaThuaDat = dvhc.Ma.GetMinPrimaryKey(),
                MaxMaThuaDat = dvhc.Ma.GetMaxPrimaryKey()
            };
            // Thực thi câu lệnh SQL
            return await connection.ExecuteScalarAsync<long>(query, param);
        }
        catch (Exception e)
        {
            logger?.Error(e, "Lỗi khi lấy Mã Thửa Đất lớn nhất của Đơn Vị Hành Chính. [MaDVHC: {MaDVHC}]", dvhc.MaDvhc);
            throw;
        }
    }

    /// <summary>
    /// Kiểm tra xem số lượng Thửa Đất có vượt quá giới hạn không.
    /// </summary>
    /// <param name="dvhc">Đơn vị hành chính.</param>
    /// <returns>Trả về true nếu không vượt quá giới hạn, ngược lại trả về false.</returns>
    private async Task<bool> CheckOverflowAsync(DvhcRecord dvhc)
    {
        // Lấy tổng số lượng thửa đất của đơn vị hành chính
        const string query = """
                             SELECT COUNT(*)
                             FROM ThuaDat INNER JOIN
                              ToBanDo ON ThuaDat.MaToBanDo = ToBanDo.MaToBanDo
                             WHERE MaDVHC = @MaDvhc
                             """;
        await using var connection = connectionString.GetConnection();
        var count = await connection.ExecuteScalarAsync<int>(query, new { dvhc.MaDvhc });
        return count <= PrimaryKeyExtensions.GetMaximumPrimaryKey();
    }

    /// <summary>
    /// Lấy danh sách Mã Thửa Đất chưa sử dụng theo Đơn Vị Hành Chính.
    /// </summary>
    /// <param name="dvhc">Đơn vị hành chính.</param>
    /// <param name="minMaThuaDat">Mã Thửa Đất tối thiểu (tùy chọn).</param>
    /// <param name="limit">Số lượng bản ghi tối đa cần lấy.</param>
    /// <returns>Danh sách Mã Thửa Đất chưa sử dụng.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn.</exception>
    private async Task<SortedSet<long>> GetUnusedMaThuaDatAsync(DvhcRecord dvhc, long? minMaThuaDat = null,
        int limit = 100)
    {
        try
        {
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();

            // Tạo câu lệnh query SQL
            const string sqlThuaDat = """
                                      SELECT TOP(@Limit) MaThuaDat AS MaThuaDat
                                      FROM ThuaDat INNER JOIN
                                       ToBanDo ON ThuaDat.MaToBanDo = ToBanDo.MaToBanDo
                                      WHERE MaDVHC = @MaDvhc AND MaThuaDat > @MinMaThuaDat AND MaThuaDat < @MaxMaThuaDatInDvhc
                                      """;
            const string sqlThuaDatLs = """
                                        SELECT TOP(@Limit) MaThuaDatLS AS MaThuaDat
                                        FROM ThuaDatLS INNER JOIN
                                         ToBanDo ON ThuaDatLS.MaToBanDo = ToBanDo.MaToBanDo
                                        WHERE MaDVHC = @MaDvhc AND MaThuaDatLS > @MinMaThuaDat AND MaThuaDatLS < @MaxMaThuaDatInDvhc
                                        """;
            const string query = $"""
                                  SELECT MAX(MaThuaDat) AS MaxValue
                                  FROM (
                                      {sqlThuaDat}
                                      UNION
                                      {sqlThuaDatLs}
                                  ) AS CombinedResults;
                                  """;

            SortedSet<long> result = [];
            var maThuaDat = minMaThuaDat ?? dvhc.Ma.GetMinPrimaryKey();
            var maxMaThuaDat = await GetMaxMaThuaDatAsync(dvhc);
            var maxMaThuaDatInDvhc = dvhc.Ma.GetMaxPrimaryKey();
            while (result.Count == 0)
            {
                if (maThuaDat >= maxMaThuaDat)
                {
                    return new SortedSet<long>(Enumerable.Range(0, limit).Select(i => maThuaDat + i));
                }

                var param = new
                {
                    dvhc.MaDvhc,
                    MinMaThuaDat = maThuaDat,
                    MaxMaThuaDatInDvhc = maxMaThuaDatInDvhc,
                    Limit = limit
                };
                var usedMaThuaDat = (await connection.QueryAsync<long>(query, param)).ToHashSet();
                if (usedMaThuaDat.Count == 0)
                {
                    // Trả về danh sách có limit phần tử liên tục từ maThuaDat
                    return new SortedSet<long>(Enumerable.Range(0, limit).Select(i => maxMaThuaDat + i));
                }

                // Tìm các Mã Thửa Đất chưa sử dụng
                var localMaThuaDat = maThuaDat;
                var allMaThuaDat = new SortedSet<long>(Enumerable.Range(0, limit).Select(i => localMaThuaDat + i));
                result = new SortedSet<long>(allMaThuaDat.Except(usedMaThuaDat));
                if (result.Count == 0)
                {
                    maThuaDat = usedMaThuaDat.Max();
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            logger?.Error(ex,
                "Lỗi khi lấy danh sách Mã Thửa Đất chưa sử dụng theo Đơn Vị Hành Chính. [DVHC: {DVHC}], [MinMaThuaDat: {MinMaThuaDat}]",
                dvhc, minMaThuaDat);
            throw;
        }
    }

    /// <summary>
    /// Lấy danh sách Mã Thửa Đất cần làm mới.
    /// </summary>
    /// <param name="dvhc">Đơn vị hành chính.</param>
    /// <param name="minMaThuaDat">Mã Thửa Đất tối thiểu (tùy chọn).</param>
    /// <param name="tempMaThuaDat">Mã Thửa Đất tạm thời (mặc định là <see cref="DefaultTempMaThuaDat"/>).</param>
    /// <param name="limit">Số lượng bản ghi tối đa cần lấy.</param>
    /// <returns>Danh sách mã Tờ Bản Đồ.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn.</exception>
    private async Task<IEnumerable<long>> GetMaThuaDatsNeedRenewAsync(DvhcRecord dvhc, long? minMaThuaDat = null,
        long tempMaThuaDat = DefaultTempMaThuaDat, int limit = 100)
    {
        try
        {
            // Lấy kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();

            // Khởi tạo các giá trị mặc định cho các tham số
            var minMaInDvhc = dvhc.Ma.GetMinPrimaryKey();
            minMaThuaDat ??= long.MinValue;

            // Câu lệnh SQL để lấy danh sách Mã Thửa Đất cần làm mới
            var sqlThuaDat = $"""
                              SELECT TOP(@Limit) MaThuaDat
                              FROM ThuaDat INNER JOIN
                               ToBanDo ON ThuaDat.MaToBanDo = ToBanDo.MaToBanDo
                              WHERE MaDVHC = @MaDvhc AND MaThuaDat > @MinMaThuaDat AND MaThuaDat <> @TempMaThuaDat
                              {(minMaThuaDat < minMaInDvhc ? "AND MaThuaDat < @MinMaInDvhc" : "")}
                              ORDER BY MaThuaDat {(minMaThuaDat > minMaInDvhc ? "DESC" : "ASC")}
                              """;
            var sqlThuaDatLs = $"""
                                SELECT TOP(@Limit) MaThuaDat
                                FROM ThuaDatLS INNER JOIN
                                 ToBanDo ON ThuaDatLS.MaToBanDo = ToBanDo.MaToBanDo
                                WHERE MaDVHC = @MaDvhc AND MaThuaDatLS > @MinMaThuaDat AND MaThuaDatLS <> @TempMaThuaDat
                                {(minMaThuaDat < minMaInDvhc ? "AND MaThuaDatLS < @MinMaInDvhc" : "")}
                                ORDER BY MaThuaDatLS {(minMaThuaDat > minMaInDvhc ? "DESC" : "ASC")}
                                """;
            var query = $"""
                          SELECT MaThuaDat AS MaxValue
                          FROM (
                              {sqlThuaDat} 
                              UNION
                              {sqlThuaDatLs}
                              ) AS CombinedResults;
                         """;
            return await connection.QueryAsync<long>(query,
                new
                {
                    Limit = limit,
                    dvhc.MaDvhc,
                    MaToBanDo = minMaThuaDat,
                    MinMaThuaDat = minMaInDvhc,
                    TempMaThuaDat = tempMaThuaDat
                });
        }
        catch (Exception exception)
        {
            logger?.Error(exception, "Lỗi khi lấy danh sách Mã Thửa Đất cần làm mới. [DVHC: {DVHC}]", dvhc);
            throw;
        }
    }

    /// <summary>
    /// Gia hạn Mã Thửa Đất.
    /// </summary>
    /// <param name="capXaSau">Bản ghi cấp xã sau khi sáp nhập.</param>
    /// <param name="tempMaToBanDo">
    /// Mã Tờ Bản Đồ tạm thời. (<see cref="ToBanDoRepository.DefaultTempMaToBanDo"/>)
    /// </param>
    /// <param name="limit">Số lượng giới hạn kết quả trả về.</param>
    /// <returns>Task bất đồng bộ.</returns>
    /// <exception cref="OverflowException">Ném ra ngoại lệ khi số lượng Tờ Bản Đồ đã đạt giới hạn tối đa.</exception>
    /// <exception cref="Exception">Ném ra ngoại lệ khi có lỗi xảy ra trong quá trình cập nhật.</exception>
    public async Task<long> RenewMaThuaDatAsync(DvhcRecord capXaSau, long tempMaToBanDo = ToBanDoRepository.DefaultTempMaToBanDo, int limit = 100)
    {
        try
        {
            // Kiểm tra xem có cần cập nhật không
            if (!await CheckOverflowAsync(capXaSau))
            {
                logger?.Error("Số lượng Thửa Đất của Đơn Vị Hành Chính vượt quá giới hạn. [DVHC: {DVHC}]", capXaSau);
                throw new OverflowException("Số lượng Thửa Đất của Đơn Vị Hành Chính vượt quá giới hạn.");
            }
            // Lấy thông tin kết nối cơ sở dữ liệu
            await using var connection = connectionString.GetConnection();
            // Tạo mã thửa đất tạm thời 
            var tempMaThuaDat = await CreateTempThuaDatAsync(DefaultTempMaThuaDat, tempMaToBanDo);
            
            // Khởi tạo các giá trị ban đầu:
            // Mã Thửa Đất bắt đầu
            long? startId = null;
            // Danh sách Mã Thửa Đất chưa được sử dụng
            var unusedIds = new Queue<long>();
            // Mã thửa đất mới
            long? newMaThuaDat = null;
            // Mã thửa đất nhỏ nhất của đơn vị hành chính
            var minMaInDvhc = capXaSau.Ma.GetMinPrimaryKey();
            
            // Câu lệnh SQL để cập nhật Mã Thửa Đất
            
            const string queryUpdateDangKyTempMaThuaDat = """
                                                          UPDATE DangKyQSDD
                                                          SET MaThuaDat = @TempMaThuaDat
                                                          WHERE MaThuaDat = @OldMaThuaDat;

                                                          UPDATE DangKyQSDDLS
                                                          SET MaThuaDatLS = @TempMaThuaDat
                                                          WHERE MaThuaDatLS = @OldMaThuaDat;
                                                          """;
            
            const string queryUpdateThuaDat = """
                                       UPDATE ThuaDat
                                       SET MaThuaDat = @NewMaThuaDat
                                       WHERE MaThuaDat = @OldMaThuaDat;

                                       UPDATE ThuaDatLS
                                        SET MaThuaDatLS = @NewMaThuaDat
                                        WHERE MaThuaDatLS = @OldMaThuaDat;

                                       UPDATE ThuaDatCu
                                       SET MaThuaDat = @NewMaThuaDat
                                       WHERE MaThuaDat = @OldMaThuaDat;
                                       """;
            const string queryUpdateDangKyNewMaThuaDat = """
                                                         UPDATE DangKyQSDD
                                                         SET MaThuaDat = @NewMaThuaDat
                                                         WHERE MaThuaDat = @TempMaThuaDat;

                                                         UPDATE DangKyQSDDLS
                                                         SET MaThuaDatLS = @NewMaThuaDat
                                                         WHERE MaThuaDatLS = @TempMaThuaDat;
                                                         """;
            const string queryUpdate = $"""
                                       {queryUpdateDangKyTempMaThuaDat}
                                       {queryUpdateThuaDat}
                                       {queryUpdateDangKyNewMaThuaDat}
                                       """;
            while (true)
            {
                // Lấy danh sách mã thửa đất cần cập nhật
                var maThuaDatNeedRenew =
                    (await GetMaThuaDatsNeedRenewAsync(capXaSau, startId, tempMaToBanDo, limit)).ToList();
                foreach (var oldMaToBanDo in maThuaDatNeedRenew)
                {
                    if (unusedIds.Count == 0)
                    {
                        unusedIds = new Queue<long>(await GetUnusedMaThuaDatAsync(capXaSau, newMaThuaDat, limit));
                    }

                    newMaThuaDat = unusedIds.Dequeue();
                    if (newMaThuaDat > oldMaToBanDo && oldMaToBanDo > minMaInDvhc)
                    {
                        maThuaDatNeedRenew = [];
                        break;
                    }
                        
                    var param = new
                    {
                        TempMaThuaDat = tempMaThuaDat,
                        NewMaThuaDat = newMaThuaDat,
                        OldMaThuaDat = oldMaToBanDo
                    };
                    
                    await connection.ExecuteAsync(queryUpdate, param);
                }

                // Nếu không còn mã thửa đất cần cập nhật hoặc đã cập nhật hết
                if (maThuaDatNeedRenew.Count == 0 && startId > minMaInDvhc) break;

                // Lấy mã thửa đất bắt đầu tiếp theo để cập nhật
                startId = maThuaDatNeedRenew.Count > 0 ? maThuaDatNeedRenew.Max() : minMaInDvhc;
                startId = startId <= minMaInDvhc ? startId + 1 : newMaThuaDat + 1;
            }
            return tempMaThuaDat;
        }
        catch (Exception exception)
        {
            logger?.Error(exception, "Lỗi khi Làm mới Mã Thửa Đất. [DVHC: {DVHC}]", capXaSau);
            throw;
        }
    }
}