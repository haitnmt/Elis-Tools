using System.Data;
using Dapper;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Microsoft.Data.SqlClient;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;

public static class DonViHanhChinhExtensions
{
    public static async Task UpdateDonViHanhChinhAsync(this DvhcRecord dvhcMoi, string connectionString,
        List<int> maDvhcBiSapNhaps, string? formatTenDonViHanhChinhBiSapNhap = null)
    {
        await using var dbConnection = new SqlConnection(connectionString);
        await dbConnection.OpenAsync();
        // Update DonViHanhChinh moi
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
        // Update DonViHanhChinh bị sáp nhập
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

    private static async Task<IEnumerable<DvhcRecord>> GetCapTinhAsync(this SqlConnection dbConnection, CancellationToken cancellationToken = default)
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
    
    public static async Task<IEnumerable<DvhcRecord>> GetCapTinhAsync(this string connectionString, CancellationToken cancellationToken = default)
    {
        await using var dbConnection = new SqlConnection(connectionString);
        await dbConnection.OpenAsync(cancellationToken);
        return await GetCapTinhAsync(dbConnection, cancellationToken);
    }

    private static async Task<IEnumerable<DvhcRecord>> GetCapHuyenAsync(this SqlConnection dbConnection, int maTinh, CancellationToken cancellationToken = default)
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
    
    public static async Task<IEnumerable<DvhcRecord>> GetCapHuyenAsync(this string connectionString, int maTinh, CancellationToken cancellationToken = default)
    {
        await using var dbConnection = new SqlConnection(connectionString);
        await dbConnection.OpenAsync(cancellationToken);
        return await GetCapHuyenAsync(dbConnection, maTinh, cancellationToken);
    }

    private static async Task<IEnumerable<DvhcRecord>> GetCapXaAsync(this SqlConnection dbConnection, int maHuyen, CancellationToken cancellationToken = default)
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
    
    public static async Task<IEnumerable<DvhcRecord>> GetCapXaAsync(this string connectionString, int maHuyen, CancellationToken cancellationToken = default)
    {
        await using var dbConnection = new SqlConnection(connectionString);
        await dbConnection.OpenAsync(cancellationToken);
        return await GetCapXaAsync(dbConnection, maHuyen, cancellationToken);
    }
    
}