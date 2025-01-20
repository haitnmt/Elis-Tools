using Haihv.Elis.Tool.TraCuuGcn.Record;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Settings;
using InterpolatedSql.Dapper;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Caching.Hybrid;
using ILogger = Serilog.ILogger;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public interface IChuSuDungService
{
    /// <summary>
    /// Lấy thông tin chủ sử dụng theo số định danh.
    /// </summary>
    /// <param name="serial">Serial (Số phát hành) của Giấy chứng nhận.</param>
    /// <param name="soDinhDanh">Số định danh.</param>
    /// <param name="cancellationToken">Token hủy bỏ.</param>
    /// <returns>Kết quả chứa thông tin xác thực chủ sử dụng hoặc lỗi.</returns>
    ValueTask<Result<AuthChuSuDung>> GetAuthChuSuDungBySoDinhDanhAsync(string? serial = null, string? soDinhDanh = null,
        CancellationToken cancellationToken = default);
}

public sealed class ChuSuDungService(
    IGiayChungNhanService giayChungNhanService,
    IConnectionElisData connectionElisData,
    ILogger logger,
    HybridCache hybridCache) : IChuSuDungService
{
    #region Xác thực chủ sử dụng

    /// <summary>
    /// Lấy thông tin chủ sử dụng theo số định danh.
    /// </summary>
    /// <param name="serial">Serial (Số phát hành) của Giấy chứng nhận.</param>
    /// <param name="soDinhDanh">Số định danh.</param>
    /// <param name="cancellationToken">Token hủy bỏ.</param>
    /// <returns>Kết quả chứa thông tin chủ sử dụng hoặc lỗi.</returns>
    public async ValueTask<Result<AuthChuSuDung>> GetAuthChuSuDungBySoDinhDanhAsync(string? serial = null,
        string? soDinhDanh = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(soDinhDanh) || string.IsNullOrWhiteSpace(serial))
            return new Result<AuthChuSuDung>(new ValueIsNullException("Số định danh không hợp lệ!"));
        var cacheKey = CacheSettings.KeyChuSuDung(soDinhDanh, serial);
        try
        {
            var chuSuDung = await hybridCache.GetOrCreateAsync(cacheKey,
                cancel => GetAuthChuSuDungInDataAsync(serial, soDinhDanh, cancel),
                cancellationToken: cancellationToken);
            return chuSuDung ?? new Result<AuthChuSuDung>(new ValueIsNullException("Không tìm thấy chủ sử dụng!"));
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Lỗi khi truy vấn dữ liệu chủ sử dụng từ cơ sở dữ liệu, {SoDDinhDanh}", soDinhDanh);
            return new Result<AuthChuSuDung>(exception);
        }
    }

    /// <summary>
    /// Lấy thông tin chủ sử dụng từ cơ sở dữ liệu theo số định danh và Serial.
    /// </summary>
    /// <param name="serial">Serial (Số phát hành) của Giấy chứng nhận.</param>
    /// <param name="soDinhDanh">Số định danh.</param>
    /// <param name="cancellationToken">Token hủy bỏ.</param>
    /// <returns>Thông tin chủ sử dụng hoặc null nếu không tìm thấy.</returns>
    private async ValueTask<AuthChuSuDung?> GetAuthChuSuDungInDataAsync(
        string? serial = null,
        string? soDinhDanh = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(soDinhDanh) || string.IsNullOrWhiteSpace(serial))
            return null;
        var giayChungNhanResult = await giayChungNhanService.GetBySerialAsync(serial, cancellationToken);
        return await giayChungNhanResult.Match(
            giayChungNhan => GetAuthChuSuDungInDataAsync(giayChungNhan, soDinhDanh, cancellationToken),
            ex => throw ex);
    }

    /// <summary>
    /// Lấy thông tin chủ sử dụng từ cơ sở dữ liệu theo số định danh và giấy chứng nhận.
    /// </summary>
    /// <param name="giayChungNhan">Giấy chứng nhận.</param>
    /// <param name="soDinhDanh">Số định danh.</param>
    /// <param name="cancellationToken">Token hủy bỏ.</param>
    /// <returns>Thông tin chủ sử dụng hoặc null nếu không tìm thấy.</returns>
    private async ValueTask<AuthChuSuDung?> GetAuthChuSuDungInDataAsync(
        GiayChungNhan? giayChungNhan = null,
        string? soDinhDanh = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(soDinhDanh) ||
            giayChungNhan is null ||
            string.IsNullOrWhiteSpace(giayChungNhan.Serial))
            return null;
        try
        {
            var connectionName = await hybridCache.GetOrCreateAsync<string?>(
                CacheSettings.ConnectionName(giayChungNhan.Serial),
                _ => ValueTask.FromResult<string?>(null),
                cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(connectionName)) return null;
            var connectionString = connectionElisData.GetConnectionString(connectionName);
            await using var dbConnection = connectionString.GetConnection();
            var query = dbConnection.SqlBuilder(
                $"""
                 SELECT TOP(1) SoDinhDanh, HoVaTen, GioiTinh, NamSinh, DiaChi1, DiaChi2
                 FROM (SELECT DISTINCT CSD.SoDinhDanh1 AS SoDinhDanh, 
                                       CSD.Ten1 AS HoVaTen
                       FROM ChuSuDung CSD
                            INNER JOIN GCNQSDD GCN ON CSD.MaChuSuDung = GCN.MaChuSuDung
                        WHERE LOWER(CSD.SoDinhDanh1) = LOWER({soDinhDanh}) AND GCN.MaGCN = {giayChungNhan.MaGcn}
                       UNION
                       SELECT DISTINCT CSD.SoDinhDanh2 AS SoDinhDanh, 
                                       CSD.Ten2 AS HoVaTen
                       FROM ChuSuDung CSD
                            INNER JOIN GCNQSDD GCN ON CSD.MaChuSuDung = GCN.MaChuSuDung
                        WHERE LOWER(CSD.SoDinhDanh2) = LOWER({soDinhDanh}) AND GCN.MaGCN = {giayChungNhan.MaGcn}
                        ) AS CSD
                 """);
            var chuSuDungData =
                await query.QueryFirstOrDefaultAsync<dynamic?>(cancellationToken: cancellationToken);
            if (chuSuDungData is null) return null;
            return new AuthChuSuDung(
                giayChungNhan.Serial,
                chuSuDungData.SoDinhDanh,
                chuSuDungData.HoVaTen);
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Lỗi khi truy vấn dữ liệu chủ sử dụng từ cơ sở dữ liệu, {SoDDinhDanh}", soDinhDanh);
            throw;
        }
    }

    #endregion

    #region Lấy thông tin chủ sử dụng

    private async ValueTask<(ChuSuDung? ChuSuDung, ChuSuDungQuanHe? ChuSuDungQuanHe)> GetChuSuDungInDataAsync(
        GiayChungNhan? giayChungNhan = null,
        string? soDinhDanh = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(soDinhDanh) ||
            giayChungNhan is null ||
            string.IsNullOrWhiteSpace(giayChungNhan.Serial))
            return (null, null);
        try
        {
            var connectionName = await hybridCache.GetOrCreateAsync<string?>(
                CacheSettings.ConnectionName(giayChungNhan.Serial),
                _ => ValueTask.FromResult<string?>(null),
                cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(connectionName)) return (null, null);

            var connectionString = connectionElisData.GetConnectionString(connectionName);
            await using var dbConnection = connectionString.GetConnection();
            var query = dbConnection.SqlBuilder(
                $"""
                 SELECT DISTINCT CSD.MaDoiTuong AS MaDoiTuong,
                                 CSD.Ten1 AS Ten, 
                                 CSD.SoDinhDanh1 AS SoDinhDanh, 
                                 CSD.loaiSDD1 AS LoaiSdd,
                                 CSD.GioiTinh1 AS GioiTinh, 
                                 CSD.MaQuocTich1 AS MaQuocTich,
                                 CSD.DiaChi1 AS DiaChi,
                                 CSD.Ten2 AS Ten2,
                                 CSD.SoDinhDanh2 AS SoDinhDanh2,
                                 CSD.loaiSDD2 AS LoaiSdd2,
                                 CSD.GioiTinh2 AS GioiTinh2,
                                 CSD.MaQuocTich2 AS MaQuocTich2,
                                 CSD.QuanHe AS QuanHe,
                                 CSD.DiaChi2 AS DiaChi2
                                       
                       FROM ChuSuDung CSD
                            INNER JOIN GCNQSDD GCN ON CSD.MaChuSuDung = GCN.MaChuSuDung
                        WHERE LOWER(CSD.SoDinhDanh1) = LOWER({soDinhDanh}) AND GCN.MaGCN = {giayChungNhan.MaGcn}
                 """);
            var chuSuDungData =
                await query.QueryFirstOrDefaultAsync<ChuSuDungData?>(cancellationToken: cancellationToken);
            if (chuSuDungData is null) return (null, null);
            return (GetChuSuDung(chuSuDungData), null);
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Lỗi khi truy vấn dữ liệu chủ sử dụng từ cơ sở dữ liệu, {SoDDinhDanh}", soDinhDanh);
            throw;
        }
    }

    private record ChuSuDungData(
        int MaDoiTuong,
        string? Ten,
        string? SoDinhDanh,
        int LoaiSdd,
        int GioiTinh,
        string? DiaChi,
        string? Ten2,
        string? SoDinhDanh2,
        int LoaiSdd2,
        int GioiTinh2,
        string? QuanHe,
        string? DiaChi2
    );

    private static ChuSuDung? GetChuSuDung(ChuSuDungData chuSuDungData)
    {
        if (string.IsNullOrWhiteSpace(chuSuDungData.Ten)) return null;
        var ten = chuSuDungData.MaDoiTuong switch
        {
            16 => chuSuDungData.GioiTinh switch
            {
                1 => $"Ông {chuSuDungData.Ten}",
                2 => $"Bà {chuSuDungData.Ten}",
                _ => chuSuDungData.Ten
            },
            _ => chuSuDungData.Ten
        };
        var giayTo = chuSuDungData.MaDoiTuong switch
        {
            16 => chuSuDungData.LoaiSdd switch
            {
                1 => $"CMND: {chuSuDungData.SoDinhDanh}",
                2 => $"CMQĐ: {chuSuDungData.SoDinhDanh}",
                3 => $"Hộ chiếu: {chuSuDungData.SoDinhDanh}",
                4 => $"Giấy khai sinh: {chuSuDungData.SoDinhDanh}",
                5 => $"CCCD: {chuSuDungData.SoDinhDanh}",
                6 => $"CMSQ: {chuSuDungData.SoDinhDanh}",
                7 => $"CC: {chuSuDungData.SoDinhDanh}",
                _ => chuSuDungData.SoDinhDanh
            },
            _ => chuSuDungData.SoDinhDanh
        };
        return new ChuSuDung(
            ten,
            giayTo,
            chuSuDungData.DiaChi
        );
    }

    private static ChuSuDungQuanHe? GetChuSuDungQuanHe(ChuSuDungData chuSuDungData)
    {
        if (string.IsNullOrWhiteSpace(chuSuDungData.Ten2)) return null;
        var quanHe = chuSuDungData.QuanHe;
        quanHe = string.IsNullOrWhiteSpace(quanHe)
            ? $"{(chuSuDungData.MaDoiTuong == 16 ? $"{(chuSuDungData.GioiTinh2 == 1 ? "chồng" : "vợ")}" : "")}"
            : quanHe;
        var ten = chuSuDungData.MaDoiTuong switch
        {
            16 => chuSuDungData.GioiTinh2 switch
            {
                1 => $"Ông {chuSuDungData.Ten2}",
                2 => $"Bà {chuSuDungData.Ten2}",
                _ => chuSuDungData.Ten2
            },
            _ => chuSuDungData.Ten2
        };
        ten = quanHe is "chồng" or "vợ" ? $"và {quanHe}: {ten}" : $"{quanHe}: {ten}";
        var giayTo = chuSuDungData.MaDoiTuong switch
        {
            16 => chuSuDungData.LoaiSdd2 switch
            {
                1 => $"CMND: {chuSuDungData.SoDinhDanh2}",
                2 => $"CMQĐ: {chuSuDungData.SoDinhDanh2}",
                3 => $"Hộ chiếu: {chuSuDungData.SoDinhDanh2}",
                4 => $"Giấy khai sinh: {chuSuDungData.SoDinhDanh2}",
                5 => $"CCCD: {chuSuDungData.SoDinhDanh2}",
                6 => $"CMSQ: {chuSuDungData.SoDinhDanh2}",
                7 => $"CC: {chuSuDungData.SoDinhDanh2}",
                _ => chuSuDungData.SoDinhDanh2
            },
            _ => chuSuDungData.SoDinhDanh2
        };
        return new ChuSuDungQuanHe(
            ten,
            giayTo,
            chuSuDungData.DiaChi2 ?? chuSuDungData.DiaChi
        );
    }

    private async Task<string?> GetQuocTichAsync(string connectionString, int maQuocTich,
        CancellationToken cancellationToken = default)
    {
        if (maQuocTich <= 0) return null;
        var cacheKey = CacheSettings.KeyQuocTich(maQuocTich);
        try
        {
            var quocTich = await hybridCache.GetOrCreateAsync(cacheKey,
                cancel => GetQuocTichInDataAsync(connectionString, maQuocTich, cancel),
                cancellationToken: cancellationToken);
            return quocTich;
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Lỗi khi truy vấn dữ liệu Quốc tịch từ cơ sở dữ liệu, {MaQuocTich}", maQuocTich);
            return null;
        }
    }

    private async ValueTask<string?> GetQuocTichInDataAsync(string connectionString, int maQuocTich,
        CancellationToken cancellationToken = default)
    {
        if (maQuocTich <= 0) return null;
        try
        {
            await using var dbConnection = connectionString.GetConnection();
            var query = dbConnection.SqlBuilder(
                $"""
                 SELECT TenQuocGia
                 FROM QuocGia
                 WHERE MaQuocGia = {maQuocTich}
                 """);
            var quocTich = await query.QueryFirstOrDefaultAsync<string?>(cancellationToken: cancellationToken);
            return quocTich;
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Lỗi khi truy vấn dữ liệu Quốc tịch từ cơ sở dữ liệu, {MaQuocTich}", maQuocTich);
            return null;
        }
    }

    #endregion
}