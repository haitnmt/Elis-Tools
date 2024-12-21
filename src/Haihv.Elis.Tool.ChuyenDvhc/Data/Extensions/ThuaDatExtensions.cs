using System.Data;
using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;

public static class ThuaDatExtensions
{
    /// <summary>
    /// Lấy số lượng Thửa Đất dựa trên danh sách Mã Tờ Bản Đồ.
    /// </summary>
    /// <param name="dbContext">
    /// Ngữ cảnh cơ sở dữ liệu.
    /// </param>
    /// <param name="maDvhcBiSapNhap">Danh sách Mã Tờ Bản Đồ.</param>
    /// <returns>Số lượng Thửa Đất.</returns>
    public static async Task<int> GetCountThuaDatAsync(this DbContext dbContext, List<int> maDvhcBiSapNhap)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        var parameters = new List<string>();

        for (var i = 0; i < maDvhcBiSapNhap.Count; i++)
        {
            var parameterName = $"@MaToBanDo{i}";
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = maDvhcBiSapNhap[i];
            parameter.DbType = DbType.Int64;
            command.Parameters.Add(parameter);
            parameters.Add(parameterName);
        }
        var sql = $"""
                   SELECT COUNT(ThuaDat.MaThuaDat) 
                   FROM   ThuaDat INNER JOIN ToBanDo ON ThuaDat.MaToBanDo = ToBanDo.MaToBanDo
                   WHERE (ToBanDo.MaDVHC IN ({string.Join(", ", parameters)}))
                   """;
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync();
        return int.TryParse(result?.ToString(), out var count) ? count : 0;
    }
}