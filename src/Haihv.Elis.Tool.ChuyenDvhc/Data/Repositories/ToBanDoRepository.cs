using System.Data;
using Dapper;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Microsoft.Data.SqlClient;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;

public static class ToBanDoRepositories
{
    private const long DefaultTempMaToBanDo = long.MaxValue;
    
    public static async Task<int> UpdateToBanDoAsync(this SqlConnection dbConnection,
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
    
    public static async Task<long> CreateTempToBanDoAsync(this SqlConnection dbConnection,
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
}