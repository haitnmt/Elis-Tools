using System.Data;
using Dapper;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Microsoft.Data.SqlClient;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;

/// <summary>
/// Repository quản lý các đơn vị hành chính.
/// </summary>
/// <param name="connectionString">Chuỗi kết nối cơ sở dữ liệu.</param>
public sealed class DonViHanhChinhRepository(string connectionString)
{
    /// <summary>
    /// Cập nhật thông tin đơn vị hành chính mới và các đơn vị hành chính bị sáp nhập.
    /// </summary>
    /// <param name="dvhcMoi">Thông tin đơn vị hành chính mới.</param>
    /// <param name="maDvhcBiSapNhaps">Danh sách mã đơn vị hành chính bị sáp nhập.</param>
    /// <param name="formatTenDonViHanhChinhBiSapNhap">Định dạng tên đơn vị hành chính bị sáp nhập (tùy chọn).</param>
    public async Task UpdateDonViHanhChinhAsync(DvhcRecord dvhcMoi,
        List<int> maDvhcBiSapNhaps, string? formatTenDonViHanhChinhBiSapNhap = null)
    {
        await using var dbConnection = new SqlConnection(connectionString);
        await dbConnection.OpenAsync();

        // Cập nhật đơn vị hành chính mới
        var dvhc = new Dvhc
        {
            MaDvhc = dvhcMoi.MaDvhc,
            Ten = dvhcMoi.Ten
        };
        const string updateQuery = """
                                   UPDATE DVHC
                                     SET Ten = @Ten
                                     WHERE MaDVHC = @MaDVHC
                                   """;
        await dbConnection.ExecuteAsync(updateQuery, dvhc);

        // Cập nhật các đơn vị hành chính bị sáp nhập
        if (maDvhcBiSapNhaps.Count == 0) return;

        // Nếu formatTenDonViHanhChinhBiSapNhap rỗng thì sử dụng giá trị mặc định
        if (string.IsNullOrWhiteSpace(formatTenDonViHanhChinhBiSapNhap))
            formatTenDonViHanhChinhBiSapNhap = ThamSoThayThe.DefaultDonViHanhChinhBiSapNhap;

        List<Dvhc> dvhcs = [];
        dvhcs.AddRange(maDvhcBiSapNhaps.Select(maDvhcBiSapNhap =>
            new Dvhc
            {
                MaDvhc = maDvhcBiSapNhap,
                Ten = formatTenDonViHanhChinhBiSapNhap.Replace(ThamSoThayThe.DonViHanhChinh, dvhcMoi.Ten)
            }));
        const string updateQueryBiSapNhap = """
                                            UPDATE DVHC
                                              SET Ten = @Ten
                                              WHERE MaDVHC = @MaDVHC
                                            """;
        await dbConnection.ExecuteAsync(updateQueryBiSapNhap, dvhcs);
    }

    /// <summary>
    /// Lấy danh sách các đơn vị hành chính cấp tỉnh.
    /// </summary>
    /// <param name="dbConnection">Kết nối cơ sở dữ liệu.</param>
    /// <param name="cancellationToken">Token hủy bỏ để hủy tác vụ không đồng bộ.</param>
    /// <returns>Danh sách các đơn vị hành chính cấp tỉnh.</returns>
    private static async Task<IEnumerable<DvhcRecord>> GetCapTinhAsync(SqlConnection dbConnection,
        CancellationToken cancellationToken = default)
    {
        if (dbConnection.State != ConnectionState.Open)
            await dbConnection.OpenAsync(cancellationToken);
        const string query = """
                             SELECT MaDVHC, MaTinh, Ten
                             FROM DVHC
                             WHERE MaHuyen = 0 AND MaXa = 0
                             ORDER BY MaTinh
                             """;
        return await dbConnection.QueryAsync<DvhcRecord>(query);
    }

    /// <summary>
    /// Lấy danh sách các đơn vị hành chính cấp tỉnh.
    /// </summary>
    /// <param name="cancellationToken"> Token hủy bỏ để hủy tác vụ không đồng bộ.</param>
    /// <returns>Danh sách các đơn vị hành chính cấp tỉnh.</returns>
    public async Task<IEnumerable<DvhcRecord>> GetCapTinhAsync(CancellationToken cancellationToken = default)
        => await GetCapTinhAsync(connectionString, cancellationToken);

    /// <summary>
    /// Lấy danh sách các đơn vị hành chính cấp tỉnh.
    /// </summary>
    /// <param name="connectionString">Chuỗi kết nối cơ sở dữ liệu.</param>
    /// <param name="cancellationToken">Token hủy bỏ để hủy tác vụ không đồng bộ.</param>
    /// <returns>Danh sách các đơn vị hành chính cấp tỉnh.</returns>
    private static async Task<IEnumerable<DvhcRecord>> GetCapTinhAsync(string connectionString,
        CancellationToken cancellationToken = default)
    {
        await using var dbConnection = new SqlConnection(connectionString);
        await dbConnection.OpenAsync(cancellationToken);
        return await GetCapTinhAsync(dbConnection, cancellationToken);
    }

    /// <summary>
    /// Lấy danh sách các đơn vị hành chính cấp huyện.
    /// </summary>
    /// <param name="dbConnection">Kết nối cơ sở dữ liệu.</param>
    /// <param name="maTinh">Mã tỉnh.</param>
    /// <param name="cancellationToken">Token hủy bỏ để hủy tác vụ không đồng bộ.</param>
    /// <returns>Danh sách các đơn vị hành chính cấp huyện.</returns>
    private static async Task<IEnumerable<DvhcRecord>> GetCapHuyenAsync(SqlConnection dbConnection, int maTinh,
        CancellationToken cancellationToken = default)
    {
        if (dbConnection.State != ConnectionState.Open)
            await dbConnection.OpenAsync(cancellationToken);
        const string query = """
                             SELECT MaDVHC, MaHuyen, Ten
                             FROM DVHC
                             WHERE MaTinh = @MaTinh AND MaXa = 0 AND MaHuyen != 0
                             ORDER BY MaHuyen
                             """;
        return await dbConnection.QueryAsync<DvhcRecord>(query, new { MaTinh = maTinh });
    }

    /// <summary>
    /// Lấy danh sách các đơn vị hành chính cấp huyện.
    /// </summary>
    /// <param name="maTinh">Mã tỉnh.</param>
    /// <param name="cancellationToken">Token hủy bỏ để hủy tác vụ không đồng bộ.</param>
    /// <returns>Danh sách các đơn vị hành chính cấp huyện.</returns>
    public async Task<IEnumerable<DvhcRecord>> GetCapHuyenAsync(int maTinh,
        CancellationToken cancellationToken = default)
        => await GetCapHuyenAsync(connectionString, maTinh, cancellationToken);

    private static async Task<IEnumerable<DvhcRecord>> GetCapHuyenAsync(string connectionString, int maTinh,
        CancellationToken cancellationToken = default)
    {
        await using var dbConnection = new SqlConnection(connectionString);
        await dbConnection.OpenAsync(cancellationToken);
        return await GetCapHuyenAsync(dbConnection, maTinh, cancellationToken);
    }


    private static async Task<IEnumerable<DvhcRecord>> GetCapXaAsync(SqlConnection dbConnection, int maHuyen,
        CancellationToken cancellationToken = default)
    {
        if (dbConnection.State != ConnectionState.Open)
            await dbConnection.OpenAsync(cancellationToken);
        const string query = """
                             SELECT MaDVHC, MaXa, Ten
                             FROM DVHC
                             WHERE MaHuyen = @MaHuyen AND MaXa != 0
                             ORDER BY MaXa
                             """;
        return await dbConnection.QueryAsync<DvhcRecord>(query, new { MaHuyen = maHuyen });
    }

    private static async Task<IEnumerable<DvhcRecord>> GetCapXaAsync(string connectionString, int maHuyen,
        CancellationToken cancellationToken = default)
    {
        await using var dbConnection = new SqlConnection(connectionString);
        await dbConnection.OpenAsync(cancellationToken);
        return await GetCapXaAsync(dbConnection, maHuyen, cancellationToken);
    }

    /// <summary>
    /// Lấy danh sách các đơn vị hành chính cấp xã.
    /// </summary>
    /// <param name="maHuyen">Mã huyện.</param>
    /// <param name="cancellationToken">Token hủy bỏ để hủy tác vụ không đồng bộ.</param>
    /// <returns>Danh sách các đơn vị hành chính cấp xã.</returns>
    public async Task<IEnumerable<DvhcRecord>> GetCapXaAsync(int maHuyen, CancellationToken cancellationToken = default)
        => await GetCapXaAsync(connectionString, maHuyen, cancellationToken);
}