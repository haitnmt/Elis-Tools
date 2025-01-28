using Haihv.Elis.Tool.TraCuuGcn.Models;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Settings;
using InterpolatedSql.Dapper;
using LanguageExt;
using LanguageExt.Common;
using Newtonsoft.Json;
using ZiggyCreatures.Caching.Fusion;
using ILogger = Serilog.ILogger;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public sealed class ChuSuDungService(
    IGiayChungNhanService giayChungNhanService,
    IConnectionElisData connectionElisData,
    ILogger logger,
    IFusionCache fusionCache) : IChuSuDungService
{
    #region Xác thực chủ sử dụng

    /// <summary>
    /// Lấy thông tin chủ sử dụng theo số định danh.
    /// </summary>
    /// <param name="maGcn">Mã GCN của Giấy chứng nhận.</param>
    /// <param name="soDinhDanh">Số định danh.</param>
    /// <param name="cancellationToken">Token hủy bỏ.</param>
    /// <returns>Kết quả chứa thông tin chủ sử dụng hoặc lỗi.</returns>
    public async ValueTask<Result<AuthChuSuDung>> GetResultAuthChuSuDungAsync(long maGcn = 0,
        string? soDinhDanh = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(soDinhDanh) ||  maGcn <= 0)
            return new Result<AuthChuSuDung>(new ValueIsNullException("Số định danh không hợp lệ!"));
        var cacheKey = CacheSettings.KeyAuthentication(soDinhDanh, maGcn);
        try
        {
            var chuSuDung = await fusionCache.GetOrSet(cacheKey,
                cancel => GetAuthChuSuDungInDataAsync(maGcn, soDinhDanh, cancel),
                token: cancellationToken);
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
    /// <param name="maGcn">Mã GCN của Giấy chứng nhận.</param>
    /// <param name="soDinhDanh">Số định danh.</param>
    /// <param name="cancellationToken">Token hủy bỏ.</param>
    /// <returns>Thông tin chủ sử dụng hoặc null nếu không tìm thấy.</returns>
    private async ValueTask<AuthChuSuDung?> GetAuthChuSuDungInDataAsync(
        long maGcn = 0,
        string? soDinhDanh = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(soDinhDanh) || maGcn <= 0)
            return null;
        var giayChungNhanResult = await giayChungNhanService.GetResultAsync(maGcn: maGcn, cancellationToken: cancellationToken);
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
            giayChungNhan.MaGcn <= 0)
            return null;
        try
        {
            var connectionElis = await connectionElisData.GetConnectionElis(giayChungNhan.MaGcn);
            foreach (var connection in connectionElis)
            {
                await using var dbConnection = connection.ConnectionString.GetConnection();
                var query = dbConnection.SqlBuilder(
                    $"""
                     SELECT TOP(1) SoDinhDanh, HoVaTen
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
                if (chuSuDungData is null) continue;
                return new AuthChuSuDung(
                    giayChungNhan.MaGcn,
                    chuSuDungData.SoDinhDanh,
                    chuSuDungData.HoVaTen);
            }
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Lỗi khi truy vấn dữ liệu chủ sử dụng từ cơ sở dữ liệu, {SoDDinhDanh}", soDinhDanh);
            throw;
        }
        return null;
    }

    #endregion

    #region Lấy thông tin chủ sử dụng
    
    /// <summary>
    /// Lấy thông tin chủ sử dụng và quan hệ chủ sử dụng.
    /// </summary>
    /// <param name="maGcn">Mã GCN của Giấy chứng nhận.</param> 
    /// <param name="soDinhDanh">Số định danh.</param>
    /// <param name="cancellationToken">Token hủy bỏ.</param>
    /// <returns>Kết quả chứa thông tin chủ sử dụng hoặc lỗi.</returns>
    public async ValueTask<Result<ChuSuDungInfo?>> GetResultAsync(
        long maGcn = 0,
        string? soDinhDanh = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(soDinhDanh) || maGcn <= 0)
            return null;
        var cacheKey = CacheSettings.KeyChuSuDung(soDinhDanh, maGcn);
        var chuSuDung = await fusionCache.GetOrSet(cacheKey,
            cancel => GetChuSuDungInDataAsync(maGcn, soDinhDanh, cancel),
            token: cancellationToken);
        return chuSuDung ?? new Result<ChuSuDungInfo?>(new ValueIsNullException("Không tìm thấy chủ sử dụng!"));
    }

    private async ValueTask<ChuSuDungInfo?> GetChuSuDungInDataAsync(
        long? maGcn = null,
        string? soDinhDanh = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(soDinhDanh) ||
            maGcn is null || maGcn <= 0)
            return null;
        var giayChungNhanResult = await giayChungNhanService.GetResultAsync(maGcn: maGcn.Value, cancellationToken: cancellationToken);
        return await giayChungNhanResult.Match(
            giayChungNhan => GetChuSuDungInDataAsync(giayChungNhan, soDinhDanh, cancellationToken),
            ex => throw ex);
    }
    
    private async ValueTask<ChuSuDungInfo?> GetChuSuDungInDataAsync(
        GiayChungNhan? giayChungNhan = null,
        string? soDinhDanh = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(soDinhDanh) ||
            giayChungNhan is null ||
            giayChungNhan.MaGcn <= 0)
            return null;
        try
        {
            var connectionElis = await connectionElisData.GetConnectionElis(giayChungNhan.MaGcn);
            foreach (var connection in connectionElis)
            {
                await using var dbConnection = connection.ConnectionString.GetConnection();
                var query = dbConnection.SqlBuilder(
                    $"""
                     SELECT DISTINCT CSD.MaDoiTuong AS MaDoiTuong,
                                     CSD.Ten1 AS Ten, 
                                     CSD.SoDinhDanh1 AS SoDinhDanh, 
                                     CSD.loaiSDD1 AS LoaiSdd,
                                     CSD.GioiTinh1 AS GioiTinh, 
                                     CSD.DiaChi1 AS DiaChi,
                                     CSD.MaQuocTich1 AS MaQuocTich,
                                     CSD.Ten2 AS Ten2,
                                     CSD.SoDinhDanh2 AS SoDinhDanh2,
                                     CSD.loaiSDD2 AS LoaiSdd2,
                                     CSD.GioiTinh2 AS GioiTinh2,
                                     CSD.QuanHe AS QuanHe,
                                     CSD.DiaChi2 AS DiaChi2,
                                     CSD.MaQuocTich2 AS MaQuocTich2
                                           
                           FROM ChuSuDung CSD
                                INNER JOIN GCNQSDD GCN ON CSD.MaChuSuDung = GCN.MaChuSuDung
                           WHERE GCN.MaGCN = {giayChungNhan.MaGcn} 
                             AND (LOWER(CSD.SoDinhDanh1) = LOWER({soDinhDanh}) OR LOWER(CSD.SoDinhDanh2) = LOWER({soDinhDanh}))  
                     """);
                var chuSuDungData =
                    await query.QueryFirstOrDefaultAsync<ChuSuDungData?>(cancellationToken: cancellationToken);
                if (chuSuDungData is null) return null;
                var chuSuDung = await GetChuSuDungAsync(chuSuDungData);
                if (chuSuDung is null) return null;
                var chuSuDungQuanHe = await GetChuSuDungQuanHeAsync(chuSuDungData);
                return new ChuSuDungInfo(chuSuDung, chuSuDungQuanHe);
            }
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Lỗi khi truy vấn dữ liệu chủ sử dụng từ cơ sở dữ liệu, {SoDDinhDanh}", soDinhDanh);
            throw;
        }
        return null;
    }

    private record ChuSuDungData(
        int MaDoiTuong,
        string Ten,
        string SoDinhDanh,
        int LoaiSdd,
        int GioiTinh,
        string DiaChi,
        int MaQuocTich,
        string Ten2,
        string SoDinhDanh2,
        int LoaiSdd2,
        int GioiTinh2,
        string QuanHe,
        string DiaChi2,
        int MaQuocTich2
    );
    
    private async Task<ChuSuDung?> GetChuSuDungAsync(ChuSuDungData chuSuDungData)
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
        var quocTich = await GetQuocTichAsync(chuSuDungData.MaQuocTich) ?? string.Empty;
        return new ChuSuDung(
            ten,
            giayTo,
            chuSuDungData.DiaChi,
            quocTich
        );
    }

    private async Task<ChuSuDungQuanHe?> GetChuSuDungQuanHeAsync(ChuSuDungData chuSuDungData)
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
        ten = quanHe is "chồng" or "vợ" ? $"Và {quanHe}: {ten}" : $"{quanHe}: {ten}";
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
        var quoctich = await GetQuocTichAsync(chuSuDungData.MaQuocTich2) ?? string.Empty;
        return new ChuSuDungQuanHe(
            ten,
            giayTo,
            string.IsNullOrWhiteSpace(chuSuDungData.DiaChi2) ? 
                chuSuDungData.DiaChi : chuSuDungData.DiaChi2,
            quoctich
        );
    }

    private async ValueTask<string?> GetQuocTichAsync(int maQuocTich = 1,
        CancellationToken cancellationToken = default)
    {
        if (maQuocTich <= 1) return null;
        var cacheKey = CacheSettings.KeyQuocTich(maQuocTich);
        try
        {
            var quocTich = await fusionCache.GetOrSet(cacheKey,
                cancel => GetQuocTichInJsonAsync(null, maQuocTich, cancel),
                token: cancellationToken);
            return quocTich;
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Lỗi khi truy vấn dữ liệu Quốc tịch, {MaQuocTich}", maQuocTich);
            return null;
        }
    }

    private record QuocTich(int MaQuocTich, string TenQuocTich);

    private async ValueTask<string?> GetQuocTichInJsonAsync(string? path = null, int maQuocTich = 1,
        CancellationToken cancellationToken = default)
    {
        switch (maQuocTich)
        {
            case <= 1:
                return null;
            default:
                path ??= "QuocTich.json";
                try
                {
                    // Đọc nội dung file JSON
                    var jsonContent = await File.ReadAllTextAsync(path, cancellationToken);
                    // Deserialize JSON thành danh sách đối tượng
                    var countries = JsonConvert.DeserializeObject<List<QuocTich>>(jsonContent);
                    // Lấy tên quốc tịch theo mã quốc tịch
                    var country = countries?.FirstOrDefault(c => c.MaQuocTich == maQuocTich);
                    return country?.TenQuocTich;
                }
                catch (Exception exception)
                {
                    logger.Error(exception, "Lỗi khi truy vấn dữ liệu Quốc tịch, {MaQuocTich}", maQuocTich);
                    return null;
                }
        }
    }

    #endregion
}
