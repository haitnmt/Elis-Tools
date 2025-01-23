using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Settings;
using InterpolatedSql.Dapper;
using Microsoft.Extensions.Caching.Hybrid;
using ILogger = Serilog.ILogger;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public class ThuaDatService(string connectionString, ILogger logger, HybridCache hybridCache)
{
    public record ThuaDatToBanDo(string SoTo, string SoThua, string DiaChi);

    public async ValueTask<ThuaDatToBanDo?> GetThuaDatToBanDoAsync(long maDangKy,
        CancellationToken cancellationToken = default)
    {
        if (maDangKy <= 0) return null;
        try
        {
            await using var dbConnection = connectionString.GetConnection();
            var query = dbConnection.SqlBuilder(
                $"""
                 SELECT TBD.SoTo AS SoTo, 
                        TD.ThuaDatSo AS SoThua, 
                        TBD.MaDVHC AS maDvhc,
                        TD.DiaChi AS DiaChi
                 FROM ThuaDat TD 
                     INNER JOIN ToBanDo TBD ON TD.MaToBanDo = TBD.MaToBanDo 
                     INNER JOIN DangKyQSDD DK ON TD.MaThuaDat = DK.MaThuaDat
                 WHERE DK.MaDangKy = {maDangKy}
                 """);
            var thuaDatToBanDos = (await query.QueryAsync(cancellationToken: cancellationToken))
                .ToList();
            if (thuaDatToBanDos.Count == 0) return null;
            var thuaDatToBanDo = thuaDatToBanDos.First();
            if (!int.TryParse(thuaDatToBanDo.maDvhc.ToString(), out int maDvhc)) return null;
            string diaChi = thuaDatToBanDo.DiaChi;
            diaChi =
                $"{(string.IsNullOrWhiteSpace(diaChi) ? "" : $"{diaChi}, ")}{await GetDiaChiByMaDvhcAsync(maDvhc, cancellationToken)}";
            string soThua = thuaDatToBanDo.SoThua.ToString();
            string toBanDo = thuaDatToBanDo.SoTo.ToString();
            return new ThuaDatToBanDo(
                toBanDo.Trim(),
                soThua.Trim(),
                diaChi.Trim().VietHoaDauChuoi()
            );
        }
        catch (Exception e)
        {
            logger.Error(e, "Lỗi khi lấy thông tin Thửa đất theo Mã GCN: {maDangKy}", maDangKy);
            throw;
        }
    }

    private async ValueTask<string> GetDiaChiByMaDvhcAsync(int maDvhc, CancellationToken cancellationToken = default)
    {
        if (maDvhc <= 0) return string.Empty;
        var cacheKey = CacheSettings.KeyDiaChiByMaDvhc(maDvhc);
        try
        {
            var diaChi = await hybridCache.GetOrCreateAsync(cacheKey,
                cancel => GetDiaChiByMaDvhcAsyncInDataAsync(maDvhc, cancel),
                cancellationToken: cancellationToken);
            return diaChi;
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Lỗi khi truy vấn dữ liệu Đơn vị hành chính từ cơ sở dữ liệu, {MaDVHC}", maDvhc);
            return string.Empty;
        }
    }

    private async ValueTask<string> GetDiaChiByMaDvhcAsyncInDataAsync(int maDvhc,
        CancellationToken cancellationToken = default)
    {
        if (maDvhc <= 0) return string.Empty;
        try
        {
            await using var dbConnection = connectionString.GetConnection();
            var query = dbConnection.SqlBuilder(
                $"""
                 SELECT DVHCXa.Ten AS TenXa, DVHCHuyen.Ten AS TenHuyen, DVHCTinh.Ten AS TenTinh
                 FROM   DVHC DVHCXa 
                     	INNER JOIN DVHC AS DVHCHuyen ON DVHCXa.MaHuyen = DVHCHuyen.MaHuyen AND DVHCHuyen.MaXa = 0
                     	INNER JOIN DVHC AS DVHCTinh ON DVHCXa.MaTinh = DVHCTinh.MaTinh AND DVHCTinh.MaHuyen = 0
                 WHERE DVHCXa.MaDVHC = {maDvhc}
                 """);
            var diaChiData = await query.QuerySingleOrDefaultAsync<dynamic?>(cancellationToken: cancellationToken);
            if (diaChiData is null) return string.Empty;
            string tenXa = diaChiData.TenXa.ToString();
            string tenHuyen = diaChiData.TenHuyen.ToString();
            string tenTinh = diaChiData.TenTinh.ToString();
            return $"{tenXa.ChuanHoaTenDonViHanhChinh()}, " +
                   $"{tenHuyen.ChuanHoaTenDonViHanhChinh()}, " +
                   $"{tenTinh.ChuanHoaTenDonViHanhChinh()}";
        }
        catch (Exception e)
        {
            logger.Error(e, "Lỗi khi lấy thông tin Đơn vị hành chính theo Mã DVHC: {maDVHC}", maDvhc);
            throw;
        }
    }
}

public static class ThuaDatExtension
{
    public static string ChuanHoaTenDonViHanhChinh(this string input, bool isLower = true)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length switch
        {
            0 => string.Empty,
            1 => isLower ? char.ToLower(input[0]) + input[1..] : input,
            _ =>
                $"{(isLower ? char.ToLower(words[0][0]) : char.ToUpper(words[0][0])) + words[0][1..]} {string.Join(' ', words.Skip(1))}"
        };
    }

    public static string VietHoaDauChuoi(this string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return char.ToUpper(input[0]) + input[1..];
    }
}