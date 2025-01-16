using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using ILogger = Serilog.ILogger;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

/// <summary>
/// Interface cho dữ liệu kết nối ELIS.
/// </summary>
public interface IConnectionElisData
{
    /// <summary>
    /// Danh sách các kết nối ELIS.
    /// </summary>
    List<ConnectionElis> ConnectionElis { get; }

    /// <summary>
    /// Danh sách các chuỗi kết nối.
    /// </summary>
    List<string> ConnectionStrings { get; }
}

/// <summary>
/// Lớp ConnectionElisData quản lý kết nối và cấu hình ELIS.
/// </summary>
/// <param name="configuration">Cấu hình ứng dụng.</param>
/// <param name="logger">Logger để ghi log.</param>
/// <param name="memoryCache">Bộ nhớ đệm để lưu trữ tạm thời.</param>
public sealed class ConnectionElisData(
    IConfiguration configuration,
    ILogger logger,
    IMemoryCache memoryCache) : IConnectionElisData
{
    private const string KeyCache = "ElisConnections";
    private const string SectionName = "ElisSql";
    private const string SectionData = "Databases";
    private const string KeyName = "Name";
    private const string KeyMaDvhc = "MaDvhc";
    private const string KeyConnectionString = "ConnectionString";

    /// <summary>
    /// Danh sách các kết nối ELIS.
    /// </summary>
    public List<ConnectionElis> ConnectionElis =>
        memoryCache.GetOrCreate(KeyCache, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return GetConnection();
        }) ?? [];

    /// <summary>
    /// Danh sách các chuỗi kết nối.
    /// </summary>
    public List<string> ConnectionStrings => ConnectionElis.Select(x => x.ConnectionString).ToList();

    /// <summary>
    /// Lấy danh sách các kết nối từ cấu hình.
    /// </summary>
    /// <returns>Danh sách các kết nối ELIS.</returns>
    private List<ConnectionElis> GetConnection()
    {
        var section = configuration.GetSection(SectionName);
        var data = section.GetSection(SectionData).GetChildren().ToList();
        List<ConnectionElis> result = [];
        foreach (var configurationSection in data)
        {
            var name = configurationSection[KeyName] ?? string.Empty;
            var maDvhc = configurationSection[KeyMaDvhc] ?? string.Empty;
            var connectionString = configurationSection[KeyConnectionString] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(connectionString)) continue;
            using var connection = new SqlConnection(connectionString);
            try
            {
                connection.Open();
                result.Add(new ConnectionElis(name, int.TryParse(maDvhc, out var maDvhcInt) ? maDvhcInt : 0,
                    connectionString));
                connection.Close();
            }
            catch (Exception e)
            {
                logger.Error(e, "Lỗi khi kiểm tra kết nối ELIS, kết nối {Name}.", name);
            }
        }

        if (result.Count == 0)
        {
            logger.Error("Không tìm thấy cấu hình kết nối ELIS.");
        }

        return result;
    }
}

/// <summary>
/// Bản ghi ConnectionElis đại diện cho một kết nối ELIS.
/// </summary>
/// <param name="Name">Tên kết nối.</param>
/// <param name="MaDvhc">Mã đơn vị hành chính.</param>
/// <param name="ConnectionString">Chuỗi kết nối.</param>
public record ConnectionElis(string Name, int MaDvhc, string ConnectionString);