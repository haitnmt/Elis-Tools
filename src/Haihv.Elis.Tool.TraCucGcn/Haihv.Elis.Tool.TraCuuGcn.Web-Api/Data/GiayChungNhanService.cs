using Haihv.Elis.Tool.TraCuuGcn.Models;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Settings;
using InterpolatedSql.Dapper;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Caching.Hybrid;
using ILogger = Serilog.ILogger;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public interface IGiayChungNhanService
{
    /// <summary>
    /// Lấy thông tin Giấy chứng nhận theo số serial.
    /// </summary>
    /// <param name="serial">Số serial của Giấy chứng nhận.</param>
    /// <param name="cancellationToken">Token hủy bỏ tác vụ không bắt buộc.</param>
    /// <returns>Kết quả chứa thông tin Giấy chứng nhận hoặc lỗi nếu không tìm thấy.</returns>
    ValueTask<Result<GiayChungNhan>> GetBySerialAsync(string serial,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin Thửa đất theo Serial của Giấy chứng nhận.
    /// </summary>
    /// <param name="serial">Số serial của Giấy chứng nhận.</param>
    /// <param name="cancellationToken">Token hủy bỏ tác vụ không bắt buộc.</param>
    /// <returns>Kết quả chứa thông tin Thửa đất hoặc lỗi nếu không tìm thấy.</returns>
    ValueTask<Result<ThuaDat>> GetThuaDatAsync(string serial, CancellationToken cancellationToken = default);
}

public sealed class GiayChungNhanService(
    IConnectionElisData connectionElisData,
    ILogger logger,
    HybridCache hybridCache) :
    IGiayChungNhanService
{
    private readonly List<ConnectionElis> _connectionElis = connectionElisData.ConnectionElis;

    /// <summary>
    /// Lấy thông tin Giấy chứng nhận theo số serial.
    /// </summary>
    /// <param name="serial">Số serial của Giấy chứng nhận.</param>
    /// <param name="cancellationToken">Token hủy bỏ tác vụ không bắt buộc.</param>
    /// <returns>Kết quả chứa thông tin Giấy chứng nhận hoặc lỗi nếu không tìm thấy.</returns>
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
            if (giayChungNhan.NgayKy < DateTime.Now.AddYears(-100) ||
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

    /// <summary>
    /// Lấy thông tin Thửa đất theo Serial của Giấy chứng nhận.
    /// </summary>
    /// <param name="serial">Serial (Số phát hành) của Giấy chứng nhận.</param>
    /// <param name="cancellationToken">Token hủy bỏ tác vụ không bắt buộc.</param>
    /// <returns>Kết quả chứa thông tin Thửa đất hoặc lỗi nếu không tìm thấy.</returns>
    public async ValueTask<Result<ThuaDat>> GetThuaDatAsync(string serial,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheSettings.KeyThuaDat(serial);
        try
        {
            var thuaDat = await hybridCache.GetOrCreateAsync(cacheKey,
                cancel => GetThuaDatInDatabaseAsync(serial, cancel),
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
                            CASE WHEN NgayKy < '1990-01-01' 
                                THEN NgayVaoSo 
                                ELSE NgayKy 
                            END AS NgayKy, 
                            NguoiKy, 
                            SoVaoSo
                     FROM GCNQSDD
                     WHERE LOWER(SoSerial) = LOWER({serial})
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

    /// <summary>
    /// Lấy thông tin Thửa đất theo Serial của Giấy chứng nhận từ cơ sở dữ liệu.
    /// </summary>
    /// <param name="serial">Serial (Số phát hành) của Giấy chứng nhận.</param>
    /// <param name="cancellationToken">Token hủy bỏ tác vụ không bắt buộc.</param>
    /// <returns>Kết quả chứa thông tin Thửa đất hoặc lỗi nếu không tìm thấy.</returns>
    private async ValueTask<ThuaDat?> GetThuaDatInDatabaseAsync(
        string? serial, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serial)) return null;
        var giayChungNhanResult = await GetBySerialAsync(serial, cancellationToken);
        return await giayChungNhanResult.Match(
            giayChungNhan => GetThuaDatInDatabaseAsync(giayChungNhan, cancellationToken),
            ex => throw ex);
    }

    /// <summary>
    /// Lấy thông tin Thửa đất theo  Giấy chứng nhận từ cơ sở dữ liệu.
    /// </summary>
    /// <param name="giayChungNhan">Giấy chứng nhận.</param>
    /// <param name="cancellationToken">Token hủy bỏ tác vụ không bắt buộc.</param>
    /// <returns>Kết quả chứa thông tin Thửa đất hoặc lỗi nếu không tìm thấy.</returns>
    private async ValueTask<ThuaDat?> GetThuaDatInDatabaseAsync(
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
            var mucDichService = new MucDichAndHinhThucService(connectionString, logger);
            var nguonGocService = new NguonGocService(connectionString, logger);
            var thuaDatService = new ThuaDatService(connectionString, logger, hybridCache);
            var (loaiDat, thoiHan, hinhThuc) =
                await mucDichService.GetMucDichSuDungAsync(giayChungNhan.MaGcn, cancellationToken);
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
                hinhThuc,
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