using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;

/// <summary>
/// Lớp mở rộng để khởi tạo dữ liệu.
/// </summary>
public static class DataInitializer
{
    private const string TableName = "AuditChuyenDvhc";

    /// <summary>
    /// Tạo hoặc thay đổi bảng AuditChuyenDvhc.
    /// </summary>
    /// <param name="dbContext">Ngữ cảnh cơ sở dữ liệu.</param>
    /// <returns>Trả về một nhiệm vụ không đồng bộ.</returns>
    public static async Task CreatedOrAlterAuditTable(this DbContext dbContext)
    {
        var columns = new Dictionary<string, string>
        {
            { "Id", "uniqueidentifier" },
            { "MaDvhcTruoc", "int" },
            { "TenDvhcTruoc", "nvarchar(255)" },
            { "ToBanDoTruoc", "nvarchar(10)" },
            { "SoThuaDatTruoc", "nvarchar(10)" },
            { "MaDvhcSau", "int" },
            { "TenDvhcSau", "nvarchar(255)" },
            { "ToBanDoSau", "nvarchar(10)" },
            { "SoThuaDatSau", "nvarchar(10)" },
            { "NgayChuyen", "datetime" }
        };

        if (!await CheckTableExistAsync(dbContext, TableName))
        {
            var createTableSql = columns.Aggregate($"CREATE TABLE {TableName} (",
                (current, column) => current + $"{column.Key} {column.Value},");
            createTableSql = createTableSql.TrimEnd(',') + ")";
            await dbContext.Database.ExecuteSqlRawAsync(createTableSql);
        }
        else
        {
            foreach (var column in columns)
            {
                if (!await CheckColumnExistAsync(dbContext, TableName, column.Key))
                {
                    await dbContext.Database.ExecuteSqlAsync(
                        $"ALTER TABLE {TableName} ADD {column.Key} {column.Value}");
                }
            }
        }
    }

    /// <summary>
    /// Kiểm tra xem bảng có tồn tại trong cơ sở dữ liệu hay không.
    /// </summary>
    /// <param name="dbContext">Ngữ cảnh cơ sở dữ liệu.</param>
    /// <param name="tableName">Tên bảng cần kiểm tra.</param>
    /// <returns>Trả về true nếu bảng tồn tại, ngược lại trả về false.</returns>
    private static async Task<bool> CheckTableExistAsync(DbContext dbContext, string tableName)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName";
        command.Parameters.Add(new SqlParameter("@TableName", tableName));
        await dbContext.Database.OpenConnectionAsync();
        var isTableExist = false;
        if (int.TryParse((await command.ExecuteScalarAsync())?.ToString(), out var result))
        {
            isTableExist = result == 1;
        }

        return isTableExist;
    }

    /// <summary>
    /// Kiểm tra xem cột có tồn tại trong bảng hay không.
    /// </summary>
    /// <param name="dbContext">Ngữ cảnh cơ sở dữ liệu.</param>
    /// <param name="tableName">Tên bảng.</param>
    /// <param name="columnName">Tên cột.</param>
    /// <returns>Trả về true nếu cột tồn tại, ngược lại trả về false.</returns>
    private static async Task<bool> CheckColumnExistAsync(DbContext dbContext, string tableName, string columnName)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            "SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName";
        command.Parameters.Add(new SqlParameter("@TableName", tableName));
        command.Parameters.Add(new SqlParameter("@ColumnName", columnName));
        await dbContext.Database.OpenConnectionAsync();
        var isColumnExist = false;
        if (int.TryParse((await command.ExecuteScalarAsync())?.ToString(), out var result))
        {
            isColumnExist = result == 1;
        }

        return isColumnExist;
    }
}