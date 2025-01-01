using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;

/// <summary>
/// Lớp mở rộng để khởi tạo dữ liệu.
/// </summary>
public class DataInitializer(string connectionString)
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
            { "Table", "nvarchar(255)" },
            { "RowId", "nvarchar(36)" },
            { "OldValue", "nvarchar(max)" },
            { "NewValue", "nvarchar(max)" },
            { "MaDvhc", "int" },
            { "ActivityTime", "datetime" }
        };


        // Kiểm tra xem bảng AuditChuyenDvhc có tồn tại hay không
        if (!await CheckTableExistAsync(dbContext, TableName))
        {
            // Nếu bảng không tồn tại thì tạo bảng mới
            var createTableSql = columns.Aggregate($"CREATE TABLE [{TableName}] (",
                (current, column) => current + $"[{column.Key}] {column.Value},");
            createTableSql = createTableSql.TrimEnd(',') + ")";
            await dbContext.Database.ExecuteSqlRawAsync(createTableSql);
        }
        else
        {
            // Xóa các cột không cần thiết
            var columnsInDb = await dbContext.GetColumnNamesAsync(TableName);
            foreach (var column in columnsInDb.Where(column => !columns.ContainsKey(column)))
            {
                var sql = $"ALTER TABLE [{TableName}] DROP COLUMN [{column}]";
                await dbContext.Database.ExecuteSqlRawAsync(sql);
            }

            // Nếu bảng tồn tại thì kiểm tra và thêm cột mới
            foreach (var column in columns)
            {
                if (await dbContext.CheckColumnExistAsync(TableName, column.Key)) continue;
                var sql = $"ALTER TABLE [{TableName}] ADD [{column.Key}] {column.Value}";
                await dbContext.Database.ExecuteSqlRawAsync(sql);
            }
        }
    }

    /// <summary>
    /// Kiểm tra xem bảng có tồn tại trong cơ sở dữ liệu hay không.
    /// </summary>
    /// <param name="dbContext">Ngữ cảnh cơ sở dữ liệu.</param>
    /// <param name="tableName">Tên bảng cần kiểm tra.</param>
    /// <returns>Trả về true nếu bảng tồn tại, ngược lại trả về false.</returns>
    private static async Task<bool> CheckTableExistAsync(this DbContext dbContext, string tableName)
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
    private static async Task<bool> CheckColumnExistAsync(this DbContext dbContext, string tableName, string columnName)
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

    private static async Task<List<string>> GetColumnNamesAsync(this DbContext dbContext, string tableName)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName";
        command.Parameters.Add(new SqlParameter("@TableName", tableName));
        await dbContext.Database.OpenConnectionAsync();
        var columns = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }
}