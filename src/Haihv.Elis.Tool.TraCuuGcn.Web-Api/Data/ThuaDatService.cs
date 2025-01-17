using InterpolatedSql.Dapper;
using ILogger = Serilog.ILogger;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public class ThuaDatService(
    IConnectionElisData connectionElisData,
    ILogger logger)
{
    private readonly List<string> _connectionStrings = connectionElisData.ConnectionStrings;

    public record ThuaDatToBanDo(string SoTo, string SoThua, string DiaChi);

    public async ValueTask<ThuaDatToBanDo?> GetThuaDatToBanDoAsync(long maDangKy,
        CancellationToken cancellationToken = default)
    {
        if (maDangKy <= 0) return null;
        try
        {
            foreach (var connectionString in _connectionStrings)
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
                if (thuaDatToBanDos.Count == 0) continue;
                var thuaDatToBanDo = thuaDatToBanDos.First();
                if (!int.TryParse(thuaDatToBanDo.maDvhc.ToString(), out int maDvhc)) return null;
                string diaChi = thuaDatToBanDo.DiaChi;
                diaChi = $"{diaChi}, {await GetDiaChiDonViHanhChinh(maDvhc, cancellationToken)}";
                string soThua = thuaDatToBanDo.SoThua.ToString();
                string toBanDo = thuaDatToBanDo.SoTo.ToString();
                return new ThuaDatToBanDo(
                    toBanDo.Trim(),
                    soThua.Trim(),
                    diaChi.Trim()
                );
            }
        }
        catch (Exception e)
        {
            logger.Error(e, "Lỗi khi lấy thông tin Thửa đất theo Mã GCN: {maDangKy}", maDangKy);
            throw;
        }

        return null;
    }

    private async ValueTask<string> GetDiaChiDonViHanhChinh(int maDvhc, CancellationToken cancellationToken = default)
    {
        if (maDvhc <= 0) return string.Empty;
        try
        {
            foreach (var connectionString in _connectionStrings)
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
                if (diaChiData is null) continue;
                string tenXa = diaChiData.TenXa.ToString();
                string tenHuyen = diaChiData.TenHuyen.ToString();
                string tenTinh = diaChiData.TenTinh.ToString();
                return $"{tenXa.ChuanHoaTenDonViHanhChinh()}, " +
                       $"{tenHuyen.ChuanHoaTenDonViHanhChinh()}, " +
                       $"{tenTinh.ChuanHoaTenDonViHanhChinh()}";
            }
        }
        catch (Exception e)
        {
            logger.Error(e, "Lỗi khi lấy thông tin Đơn vị hành chính theo Mã DVHC: {maDVHC}", maDvhc);
            throw;
        }

        return string.Empty;
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
}