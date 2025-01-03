using Dapper;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Microsoft.Data.SqlClient;
using Serilog;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;

/// <summary>
/// Repository để thao tác với bảng ToBanDo
/// </summary>
/// <param name="connectionString">Chuỗi kết nối đến cơ sở dữ liệu</param>
/// <param name="logger">Đối tượng ghi log (tùy chọn)</param>
public class ToBanDoRepository(string connectionString, ILogger? logger = null)
{
    private const long DefaultTempMaToBanDo = long.MaxValue;

    /// <summary>
    /// Cập nhật thông tin Tờ Bản Đồ.
    /// </summary>
    /// <param name="thamChieuToBanDos">Danh sách tham chiếu Tờ Bản Đồ.</param>
    /// <param name="formatGhiChuToBanDo">Định dạng ghi chú Tờ Bản Đồ (tùy chọn).</param>
    /// <param name="ngaySapNhap">Ngày sắp nhập (tùy chọn).</param>
    /// <returns>Số lượng bản ghi được cập nhật.</returns>
    public async Task<int> UpdateToBanDoAsync(List<ThamChieuToBanDo> thamChieuToBanDos,
        string? formatGhiChuToBanDo = null, string? ngaySapNhap = null)
    {
        try
        {
            if (thamChieuToBanDos.Count == 0)
                return 0;
            // Nếu formatGhiChuToBanDo rỗng thì sử dụng giá trị mặc định
            if (string.IsNullOrWhiteSpace(formatGhiChuToBanDo))
                formatGhiChuToBanDo = ThamSoThayThe.DefaultGhiChuToBanDo;

            // Nếu ngaySapNhap rỗng thì sử dụng ngày hiện tại
            if (string.IsNullOrWhiteSpace(ngaySapNhap))
                ngaySapNhap = DateTime.Now.ToString(ThamSoThayThe.DinhDangNgaySapNhap);

            List<ToBanDo> toBanDos = [];
            toBanDos.AddRange(from thamChieuToBanDo in thamChieuToBanDos
                let ghiChuToBanDo = formatGhiChuToBanDo.Replace(ThamSoThayThe.NgaySapNhap, ngaySapNhap)
                    .Replace(ThamSoThayThe.ToBanDo, thamChieuToBanDo.SoToBanDoTruoc)
                    .Replace(ThamSoThayThe.DonViHanhChinh, thamChieuToBanDo.TenDvhcTruoc)
                select new ToBanDo
                {
                    MaToBanDo = thamChieuToBanDo.MaToBanDoTruoc, SoTo = thamChieuToBanDo.SoToBanDoSau,
                    MaDvhc = thamChieuToBanDo.MaDvhcSau, GhiChu = ghiChuToBanDo
                });
            const string sqlUpdate = $"""
                                      UPDATE ToBanDo
                                      SET SoTo = @SoTo, MaDvhc = @MaDvhc, GhiChu = @GhiChu
                                      WHERE MaToBanDo = @MaToBanDo
                                      """;
            // Cập nhật toàn bộ khối dữ liệu
            return await connectionString
                .GetConnection()
                .ExecuteAsync(sqlUpdate, toBanDos);
        }
        catch (Exception ex)
        {
            if (logger == null) throw;
            logger.Error(ex, "Lỗi khi cập nhật thông tin Tờ Bản Đồ.");
            return 0;
        }
    }

    /// <summary>
    /// Tạo một Tờ Bản Đồ tạm thời.
    /// </summary>
    /// <param name="connection">Kết nối cơ sở dữ liệu.</param>
    /// <param name="maToBanDo">Mã Tờ Bản Đồ (tùy chọn).</param>
    /// <param name="logger">Đối tượng ghi log (tùy chọn).</param>
    /// <returns>Mã của Tờ Bản Đồ tạm thời được tạo.</returns>
    public static async Task<long> CreateTempToBanDoAsync(SqlConnection connection, long? maToBanDo = null,
        ILogger? logger = null)
    {
        try
        {
            var toBanDo = new ToBanDo
            {
                MaToBanDo = maToBanDo ?? DefaultTempMaToBanDo,
                SoTo = "Temp",
                MaDvhc = 100001,
                GhiChu = "Tờ bản đồ tạm thời"
            };

            const string upsertQuery = """
                                       MERGE INTO ToBanDo AS target
                                       USING (SELECT @MaToBanDo AS MaToBanDo) AS source
                                       ON target.MaToBanDo = source.MaToBanDo
                                       WHEN NOT MATCHED THEN
                                           INSERT (MaToBanDo, SoTo, MaDvhc, GhiChu)
                                           VALUES (@MaToBanDo, @SoTo, @MaDvhc, @GhiChu);
                                       WHEN MATCHED THEN
                                           UPDATE SET SoTo = @SoTo, MaDvhc = @MaDvhc, GhiChu = @GhiChu;
                                       """;
            await connection.ExecuteAsync(upsertQuery, toBanDo);
            return toBanDo.MaToBanDo;
        }
        catch (Exception e)
        {
            if (logger == null) throw;
            logger.Error(e, "Lỗi khi tạo Tờ Bản Đồ tạm thời. [MaToBanDo: {MaToBanDo}]", maToBanDo);
            return long.MinValue;
        }
    }

    /// <summary>
    /// Lấy danh sách Tờ Bản Đồ theo mã đơn vị hành chính.
    /// </summary>
    /// <param name="maDvhc">Mã đơn vị hành chính.</param>
    /// <returns>Danh sách các Tờ Bản Đồ.</returns>
    public async Task<IEnumerable<ToBanDo>> GetToBanDosAsync(int maDvhc)
    {
        // Lấy kết nối cơ sở dữ liệu
        await using var connection = connectionString.GetConnection();
        try
        {
            const string query = """
                                 SELECT MaToBanDo, SoTo, MaDvhc, TyLe, GhiChu
                                 FROM ToBanDo
                                 WHERE MaDvhc = @MaDvhc
                                 """;
            return await connection.QueryAsync<ToBanDo>(query, new { MaDvhc = maDvhc });
        }
        catch (Exception ex)
        {
            if (logger == null) throw;
            logger.Error(ex, "Lỗi khi lấy danh sách Tờ Bản Đồ theo mã đơn vị hành chính. [MaDVHC: {MaDVHC}]", maDvhc);
            return [];
        }
    }


    /// <summary>
    /// Lấy mã Tờ Bản Đồ lớn nhất theo mã đơn vị hành chính.
    /// </summary>
    /// <param name="maDvhc">Mã đơn vị hành chính.</param>
    /// <returns>Mã Tờ Bản Đồ lớn nhất.</returns>
    public async Task<long> GetMaxMaToBanDosAsync(int maDvhc)
    {
        try
        {
            await using var connection = connectionString.GetConnection();
            const string query = """
                                 SELECT MAX(MaToBanDo) AS MaToBanDo
                                 FROM ToBanDo
                                 WHERE MaDvhc = @MaDvhc AND MaToBanDo > @MinMaToBanDo AND MaToBanDo < @MaxMaToBanDo)
                                 """;
            return await connection.ExecuteScalarAsync<long>(query,
                new
                {
                    MaDvhc = maDvhc,
                    MinMaToBanDo = maDvhc.GetMinPrimaryKey(),
                    MaxMaToBanDo = maDvhc.GetMaxPrimaryKey()
                });
        }
        catch (Exception e)
        {
            logger?.Error(e, "Lỗi khi lấy mã Tờ Bản Đồ lớn nhất theo mã đơn vị hành chính. [MaDVHC: {MaDVHC}]", maDvhc);
            throw;
        }
    }

    /// <summary>
    /// Lấy danh sách mã Tờ Bản Đồ theo mã đơn vị hành chính.
    /// </summary>
    /// <param name="maDvhc">Mã đơn vị hành chính.</param>
    /// <param name="minMaToBanDo">Mã Tờ Bản Đồ bắt đầu (tùy chọn).</param>
    /// <param name="limit">Số lượng bản ghi tối đa cần lấy.</param>
    /// <returns>Danh sách mã Tờ Bản Đồ.</returns>
    private async Task<IEnumerable<long>> GetMaToBanDosNeedRenewAsync(int maDvhc, long? minMaToBanDo = null,
        int limit = 100)
    {
        try
        {
            await using var connection = connectionString.GetConnection();
            var minMaInDvhc = maDvhc.GetMinPrimaryKey();
            minMaToBanDo ??= minMaInDvhc;
            var query = $"""
                         SELECT TOP(@Limit) MaToBanDo
                         FROM ToBanDo
                         WHERE MaDvhc = @MaDvhc AND MaToBanDo >= @MaToBanDo
                         ORDER BY MaToBanDo {(minMaToBanDo > minMaInDvhc ? "DESC" : "ASC")}
                         """;
            return await connection.QueryAsync<long>(query,
                new
                {
                    Limit = limit,
                    MaDvhc = maDvhc,
                    MaToBanDo = minMaToBanDo
                });
        }
        catch (Exception e)
        {
            logger?.Error(e, "Lỗi khi lấy danh sách mã Tờ Bản Đồ theo mã đơn vị hành chính. [MaDVHC: {MaDVHC}]",
                maDvhc);
            throw;
        }
    }

    /// <summary>
    /// Lấy danh sách mã Tờ Bản Đồ chưa được sử dụng theo mã đơn vị hành chính.
    /// </summary>
    /// <param name="maDvhc">Mã đơn vị hành chính.</param>
    /// <param name="minMaToBanDo">Mã Tờ Bản Đồ bắt đầu (tùy chọn).</param>
    /// <param name="limit">Số lượng bản ghi tối đa cần lấy.</param>
    /// <returns>Danh sách mã Tờ Bản Đồ chưa được sử dụng.</returns>
    private async Task<IEnumerable<long>> GetUnusedMaToBanDosAsync(int maDvhc, long? minMaToBanDo = null,
        int limit = 100)
    {
        try
        {
            await using var connection = connectionString.GetConnection();
            const string query = """
                                 SELECT TOP(@Limit) MaToBanDo
                                 FROM ToBanDo
                                 WHERE (MaDvhc = @MaDvhc) AND (MaToBanDo > @MinMaToBanDo)
                                 ORDER BY MaToBanDo
                                 """;
            List<long> result = [];
            var maToBanDo = minMaToBanDo ?? maDvhc.GetMinPrimaryKey();
            while (result.Count == 0)
            {
                var usedIds = (await connection.QueryAsync<long>(query,
                    new
                    {
                        Limit = limit,
                        MaDvhc = maDvhc,
                        MinMaToBanDo = maToBanDo
                    })).ToList();
                var maxId = usedIds.Count > 0 ? usedIds.Max() : 0;
                if (maxId > 0)
                {
                    for (var i = maToBanDo; i < maxId; i++)
                    {
                        if (!usedIds.Contains(i))
                        {
                            result.Add(i);
                        }
                    }
                }

                if (result.Count == 0)
                {
                    maToBanDo = long.Max(maToBanDo, maxId) + limit;
                }
            }

            return result;
        }
        catch (Exception e)
        {
            logger?.Error(e,
                """
                Lỗi khi lấy danh sách mã Tờ Bản Đồ chưa được sử dụng theo mã đơn vị hành chính. 
                [MaDVHC: {MaDVHC}], [MinMaToBanDo: {MinMaToBanDo}] 
                """,
                maDvhc, minMaToBanDo);
            throw;
        }
    }

    public async Task<(long MaxIdRenewed, Queue<long>? UnusedIds)> ReNewMaToBanDoAsync(
        int maDvhc, long? minMaToBanDo = null, Queue<long>? unusedIds = null, long? tempMaToBanDo = null,
        int limit = 100)
    {
        try
        {
            if (unusedIds == null || unusedIds.Count == 0)
            {
                unusedIds = new Queue<long>(await GetUnusedMaToBanDosAsync(maDvhc, minMaToBanDo, limit));
            }

            await using var connection = connectionString.GetConnection();
            tempMaToBanDo = await CreateTempToBanDoAsync(connection, tempMaToBanDo);
            var thuaDatRepository = new ThuaDatRepository(connectionString);
            // Lấy các mã Tờ Bản Đồ cần cập nhật
            var maToBanDosNeedRenew = (await GetMaToBanDosNeedRenewAsync(maDvhc, minMaToBanDo, limit)).ToList();
            var newId = unusedIds.Dequeue();
            foreach (var maToBanDo in maToBanDosNeedRenew)
            {
                // Cập nhật mã tờ bản đồ của thửa đất qua mã tờ bàn đò tạm thời
                if (await thuaDatRepository.UpdateMaToBanDoAsync(maToBanDo, tempMaToBanDo.Value))
                {
                    const string sql = """
                                       UPDATE ToBanDo
                                       SET MaToBanDo = @NewId
                                       WHERE MaToBanDo = @OldId
                                       """;
                    await connection.ExecuteAsync(sql, new { NewId = newId, OldId = maToBanDo });
                }

                // Cập nhật mã tờ bản đồ của thửa đất sang mã tờ bản đồ mới
                await thuaDatRepository.UpdateMaToBanDoAsync(tempMaToBanDo.Value, maToBanDo);
                newId = unusedIds.Dequeue();
                if (unusedIds.Count == 0)
                {
                    unusedIds = new Queue<long>(await GetUnusedMaToBanDosAsync(maDvhc, newId, limit));
                }
            }

            return (maToBanDosNeedRenew.Max(), unusedIds);
        }
        catch (Exception e)
        {
            logger?.Error(e, "Lỗi khi cập nhật mã Tờ Bản Đồ theo mã đơn vị hành chính. [MaDVHC: {MaDVHC}]", maDvhc);
            throw;
        }
    }
}