using Dapper;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Microsoft.Data.SqlClient;
using Serilog;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;

public class ThuaDatCuRepository(ILogger? logger = null, string? connectionString = null, SqlConnection? dbConnection = null) : DataRepository(logger, connectionString, dbConnection)
{
    private readonly ILogger? _logger = logger;

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
        await using var connection = await GetAndOpenConnectionAsync(cancellationToken);
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
                await connection.ExecuteAsync(upsertQuery, new {thuaDatCapNhat.MaThuaDat, ToBanDoCu = toBanDoCu});
            }
        }
        catch (Exception ex)
        {
            if (_logger == null) throw;
            _logger.Error(ex, "Lỗi khi tạo hoặc cập nhật thông tin Thửa Đất Cũ");
        }

    }
}