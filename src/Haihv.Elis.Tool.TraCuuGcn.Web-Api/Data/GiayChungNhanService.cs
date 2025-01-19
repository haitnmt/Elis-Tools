using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Settings;
using InterpolatedSql.Dapper;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Caching.Hybrid;
using ILogger = Serilog.ILogger;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public interface IGiayChungNhanService
{
    ValueTask<Result<GiayChungNhan>> GetBySerialAsync(string serial,
        CancellationToken cancellationToken = default);
    ValueTask<Result<ThuaDat>> GetThuaDatByGiayChungNhanAsync(GiayChungNhan giayChungNhan,
        CancellationToken cancellationToken = default);
}

public sealed class GiayChungNhanService(
    IConnectionElisData connectionElisData,
    ILogger logger,
    HybridCache hybridCache) : IGiayChungNhanService
{
    private readonly List<ConnectionElis> _connectionElis = connectionElisData.ConnectionElis;
   
    public async ValueTask<Result<GiayChungNhan>> GetBySerialAsync(string serial,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheSettings.KeyGiayChungNhan(serial);
        try
        {
            var giayChungNhan = await hybridCache.GetOrCreateAsync(cacheKey, 
                cancel => GetBySerialInDataBaseAsync(serial, cancel), 
                cancellationToken: cancellationToken);
            if (giayChungNhan is null)
                return new Result<GiayChungNhan>(new ValueIsNullException("Không tìm thấy giấy chứng nhận!"));
            if (giayChungNhan.NgayKy < DateTime.Now.AddYears(-10) ||
                giayChungNhan.NgayKy > DateTime.Now ||
                string.IsNullOrWhiteSpace(giayChungNhan.SoVaoSo))
            {
                return new Result<GiayChungNhan>(new ValueIsNullException("Giấy chứng nhận chưa có hiệu lực!"));
            }

            return giayChungNhan;
        }
        catch (Exception exception)
        {
            return new Result<GiayChungNhan>(exception);
        }
    }

    public async ValueTask<Result<ThuaDat>> GetThuaDatByGiayChungNhanAsync(GiayChungNhan giayChungNhan,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheSettings.KeyThuaDat(giayChungNhan.Serial);
        try
        {
            var thuaDat = await hybridCache.GetOrCreateAsync(cacheKey, 
                cancel => GetThuaDatInDataBaseByGiayChungNhanAsync(giayChungNhan, cancel), 
                cancellationToken: cancellationToken);
            return thuaDat ?? new Result<ThuaDat>(new ValueIsNullException("Không tìm thấy thông tin thửa đất!"));
        }
        catch (Exception exception)
        {
            return new Result<ThuaDat>(exception);
        }
    }
    
    private async ValueTask<GiayChungNhan?> GetBySerialInDataBaseAsync(string serial,
        CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var (connectionName, _, connectionString) in _connectionElis)
            {
                await using var dbConnection = connectionString.GetConnection();
                var query = dbConnection.SqlBuilder(
                    $"""
                     SELECT MaGCN AS MaGcn, 
                            MaDangKy AS MaDangKy, 
                            MaHinhThucSH AS MaHinhThucSh,
                            DienTichRieng,
                            DienTichChung,
                            SoSerial AS Serial, 
                            NgayKy, 
                            NguoiKy, 
                            SoVaoSo
                     FROM GCNQSDD
                     WHERE SoSerial = {serial}
                     """);
                var giayChungNhan =
                    await query.QueryFirstOrDefaultAsync<GiayChungNhan?>(cancellationToken: cancellationToken);
                if (giayChungNhan is null) continue;
                await hybridCache.SetAsync(
                    CacheSettings.ConnectionName(giayChungNhan.Serial), 
                    connectionName, 
                    cancellationToken: cancellationToken);
                return giayChungNhan;
            }
        }
        catch (Exception e)
        {
            logger.Error(e, "Lỗi khi lấy thông tin Giấy chứng nhận theo Serial: {Serial}", serial);
            throw;
        }

        return null;
    }

    private async ValueTask<ThuaDat?> GetThuaDatInDataBaseByGiayChungNhanAsync(
        GiayChungNhan? giayChungNhan, CancellationToken cancellationToken = default)
    {
        if (giayChungNhan is null ||
            giayChungNhan.MaDangKy == 0 ||
            giayChungNhan.MaGcn == 0 ||
            string.IsNullOrWhiteSpace(giayChungNhan.Serial) ||
            string.IsNullOrWhiteSpace(giayChungNhan.SoVaoSo)) return null;
        var connectionName = await hybridCache.GetOrCreateAsync(CacheSettings.ConnectionName(giayChungNhan.Serial),
            _ => ValueTask.FromResult<string?>(null),
            cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(connectionName)) return null;
        var connectionString = connectionElisData.GetConnectionString(connectionName);
        try
        {
            var mucDichService = new MucDichService(connectionString, logger);
            var nguonGocService = new NguonGocService(connectionString, logger);
            var thuaDatService = new ThuaDatService(connectionString, logger, hybridCache);
            var (loaiDat, thoiHan) = await mucDichService.GetMucDichSuDungAsync(giayChungNhan.MaGcn, cancellationToken);
            var nguonGoc = await nguonGocService.GetNguonGocSuDungAsync(giayChungNhan.MaGcn, cancellationToken);
            var thuaDatToBanDo = await thuaDatService.GetThuaDatToBanDoAsync(giayChungNhan.MaDangKy, cancellationToken);
            if (thuaDatToBanDo is null) return null;
            return new ThuaDat(
                thuaDatToBanDo.SoThua,
                thuaDatToBanDo.SoTo,
                thuaDatToBanDo.DiaChi,
                $"{giayChungNhan.DienTichRieng + giayChungNhan.DienTichChung} m²",
                loaiDat,
                thoiHan,
                "",
                nguonGoc
            );
        }
        catch (Exception e)
        {
            logger.Error(e, "Lỗi khi lấy thông tin Thửa đất theo Giấy chứng nhận: {Serial}", giayChungNhan.Serial);
            throw;
        }
    }
}

public record GiayChungNhan(
    long MaGcn,
    long MaDangKy,
    int MaHinhThucSh,
    double DienTichRieng,
    double DienTichChung,
    string Serial,
    DateTime NgayKy,
    string NguoiKy,
    string SoVaoSo
);

public record ThuaDat(
    string ThuaDatSo,
    string ToBanDo,
    string DiaChi,
    string DienTich,
    string LoaiDat,
    string ThoiHan,
    string HinhThuc,
    string NguonGoc
);

public record ThuaDatPublic(
    string ThuaDatSo,
    string ToBanDo,
    string DiaChi,
    string DienTich
);

public static class GiayChungNhanServiceExtensions
{
    public static ThuaDatPublic ConvertToThuaDatPublic(this ThuaDat thuaDat)
    {
        return new ThuaDatPublic(
            thuaDat.ThuaDatSo,
            thuaDat.ToBanDo,
            thuaDat.DiaChi,
            thuaDat.DienTich
        );
    }
}