using System.Data;
using Microsoft.Data.SqlClient;
using Serilog;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;

/// <summary>
/// Lớp DataRepository quản lý kết nối cơ sở dữ liệu.
/// </summary>
public class DataRepository(ILogger? logger = null, string? connectionString = null, SqlConnection? dbConnection = null)
{
    /// <summary>Lấy kết nối cơ sở dữ liệu.</summary>
    /// <param name="cancellationToken">Token hủy.</param>
    /// <exception cref="ArgumentNullException">
    /// Nếu chuỗi kết nối hoặc kết nối cơ sở dữ liệu không được cung cấp.
    /// </exception>
    /// <returns>
    /// Đối tượng SqlConnection đã mở kết nối.
    /// </returns>
    public async Task<SqlConnection> GetAndOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (dbConnection != null)
        {
            if (dbConnection.State != ConnectionState.Open)
                await dbConnection.OpenAsync(cancellationToken);
            return dbConnection;
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger?.Error("Chuỗi kết nối cơ sở dữ liệu và kết nối cơ sở dữ liệu không được cung cấp.");
            ArgumentNullException.ThrowIfNull(connectionString);
        }
        try
        {
            // Mở kết nối cơ sở dữ liệu
            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch (Exception ex)
        {
            logger?.Error(ex, "Lỗi khi mở kết nối cơ sở dữ liệu.");
            throw;
        }
        
    }
}