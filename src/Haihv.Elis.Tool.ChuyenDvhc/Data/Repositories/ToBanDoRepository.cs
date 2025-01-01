using System.Data;
using Dapper;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Microsoft.Data.SqlClient;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;

/// <summary>
/// Repository để thao tác với bảng ToBanDo
/// </summary>
/// <param name="connectionString">Chuỗi kết nối đến cơ sở dữ liệu</param>
public class ToBanDoRepository(string connectionString)
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
        await using var dbConnection = new SqlConnection(connectionString);
        return await UpdateToBanDoAsync(dbConnection, thamChieuToBanDos, formatGhiChuToBanDo, ngaySapNhap);
    }

    private static async Task<int> UpdateToBanDoAsync(SqlConnection dbConnection,
        List<ThamChieuToBanDo> thamChieuToBanDos,
        string? formatGhiChuToBanDo = null, string? ngaySapNhap = null)
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
        if (dbConnection.State != ConnectionState.Open)
            await dbConnection.OpenAsync();
        const string sqlUpdate = """
                                 UPDATE ToBanDo
                                 SET SoTo = @SoTo, MaDvhc = @MaDvhc, GhiChu = @GhiChu
                                 WHERE MaToBanDo = @MaToBanDo
                                 """;
        // Cập nhật toàn bộ khối dữ liệu
        return await dbConnection.ExecuteAsync(sqlUpdate, toBanDos);
    }


    /// <summary>
    /// Tạo một Tờ Bản Đồ tạm thời.
    /// </summary>
    /// <param name="maToBanDo">Mã Tờ Bản Đồ (tùy chọn).</param>
    /// <returns>Mã của Tờ Bản Đồ tạm thời được tạo.</returns>
    public async Task<long> CreateTempToBanDoAsync(long? maToBanDo = null)
    {
        await using var dbConnection = new SqlConnection(connectionString);
        return await CreateTempToBanDoAsync(dbConnection, maToBanDo);
    }

    private static async Task<long> CreateTempToBanDoAsync(SqlConnection dbConnection,
        long? maToBanDo = null)
    {
        if (dbConnection.State != ConnectionState.Open)
            await dbConnection.OpenAsync();
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
                                   """;
        await dbConnection.ExecuteAsync(upsertQuery, toBanDo);
        return toBanDo.MaToBanDo;
    }


    /// <summary>
    /// Lấy danh sách Tờ Bản Đồ theo mã đơn vị hành chính.
    /// </summary>
    /// <param name="maDvhc">Mã đơn vị hành chính.</param>
    /// <param name="cancellationToken">Token hủy bỏ (tùy chọn).</param>
    /// <returns>Danh sách các Tờ Bản Đồ.</returns>
    public async Task<IEnumerable<ToBanDo>> GetToBanDosAsync(int maDvhc, CancellationToken cancellationToken = default)
    {
        await using var dbConnection = new SqlConnection(connectionString);
        return await GetToBanDosAsync(dbConnection, maDvhc, cancellationToken);
    }

    private static async Task<IEnumerable<ToBanDo>> GetToBanDosAsync(SqlConnection dbConnection, int maDvhc,
        CancellationToken cancellationToken = default)
    {
        if (dbConnection.State != ConnectionState.Open)
            await dbConnection.OpenAsync(cancellationToken);
        const string query = """
                             SELECT MaToBanDo, SoTo, MaDvhc, TyLe, GhiChu
                             FROM ToBanDo
                             WHERE MaDvhc = @MaDvhc
                             """;
        return await dbConnection.QueryAsync<ToBanDo>(query, new { MaDvhc = maDvhc });
    }
}