using Haihv.Elis.Tool.TraCuuGcn.Models;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Authenticate;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Endpoints;

public static class GiayChungNhanEndpoints
{
    /// <summary>
    /// Định nghĩa các endpoint cho Giấy Chứng Nhận.
    /// </summary>
    /// <param name="app">Ứng dụng web.</param>
    public static void MapGiayChungNhanEndpoints(this WebApplication app)
    {
        app.MapGet("/elis/gcn", GetGiayChungNhanBySerial)
            .WithName("GetGiayChungNhan");

        app.MapGet("/elis/thua-dat", GetThuaDatByGiayChungNhan)
            .WithName("GetThuaDatByGiayChungNhan")
            .RequireAuthorization();

        app.MapGet("/elis/thua-dat-public", GetThuaDatPublicBySerialAsync)
            .WithName("GetThuaDatPublicByGiayChungNhan");
    }

    /// <summary>
    /// Lấy thông tin Giấy Chứng Nhận theo số serial.
    /// </summary>
    /// <param name="serial">Số serial của Giấy Chứng Nhận.</param>
    /// <param name="logger">Logger để ghi log.</param>
    /// <param name="giayChungNhanService">Dịch vụ Giấy Chứng Nhận.</param>
    /// <returns>Kết quả truy vấn Giấy Chứng Nhận.</returns>
    private static async Task<IResult> GetGiayChungNhanBySerial([FromQuery] string serial, ILogger<Program> logger,
        IGiayChungNhanService giayChungNhanService)
    {
        var result = await giayChungNhanService.GetResultAsync(serial);
        return await Task.FromResult(result.Match(
            Results.Ok,
            ex => Results.BadRequest(ex.Message)));
    }

    /// <summary>
    /// Lấy thông tin Thửa Đất theo Giấy Chứng Nhận.
    /// </summary>
    /// <param name="maGcn">Mã GCN của Giấy Chứng Nhận.</param>
    /// <param name="httpContext">Ngữ cảnh HTTP hiện tại.</param>
    /// <param name="logger">Logger để ghi log.</param>
    /// <param name="authenticationService">Dịch vụ xác thực.</param>
    /// <param name="thuaDatService">Dịch vụ Giấy Chứng Nhận.</param>
    /// <returns>Kết quả truy vấn Thửa Đất.</returns>
    private static async Task<IResult> GetThuaDatByGiayChungNhan([FromQuery] long maGcn,
        HttpContext httpContext,
        ILogger<Program> logger,
        IAuthenticationService authenticationService,
        IThuaDatService thuaDatService)
    {
        // Lấy thông tin người dùng theo token từ HttpClient
        var user = httpContext.User;
        if (!await authenticationService.CheckAuthenticationAsync(maGcn, user))
        {
            return Results.Unauthorized();
        }

        var result = await thuaDatService.GetResultAsync(maGcn);
        return await Task.FromResult(result.Match(
            Results.Ok,
            ex => Results.BadRequest(ex.Message)));
    }

    /// <summary>
    /// Lấy thông tin Thửa Đất công khai theo Giấy Chứng Nhận.
    /// </summary>
    /// <param name="maGcn">Số serial của Giấy Chứng Nhận.</param>
    /// <param name="logger">Logger để ghi log.</param>
    /// <param name="thuaDatService">Dịch vụ Giấy Chứng Nhận.</param>
    /// <returns>Kết quả truy vấn Thửa Đất công khai.</returns>
    private static async Task<IResult> GetThuaDatPublicBySerialAsync([FromQuery] long maGcn,
        ILogger<Program> logger,
        IThuaDatService thuaDatService)
    {
        var result = await thuaDatService.GetResultAsync(maGcn);
        return await Task.FromResult(result.Match(
            thuaDat => Results.Ok(thuaDat.ConvertToThuaDatPublic()),
            ex => Results.BadRequest(ex.Message)));
    }
}