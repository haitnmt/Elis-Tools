using Haihv.Elis.Tool.TraCuuGcn.Record;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Endpoints;

public static class ChuSuDungEndpoints
{
    /// <summary>
    /// Định nghĩa endpoint để lấy thông tin chủ sử dụng.
    /// </summary>
    /// <param name="app">Ứng dụng web.</param>
    public static void MapChuSuDung(this WebApplication app)
    {
        app.MapGet("/elis/csd", GetChuSuDungBySoDinhDanh)
            .WithName("GetChuSuDung")
            .RequireAuthorization();
    }

    /// <summary>
    /// Lấy thông tin chủ sử dụng theo số định danh.
    /// </summary>
    /// <param name="serial">Serial (Số phát hành) của Giấy chứng nhận.</param>
    /// <param name="soDinhDanh">Số định danh.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="chuSuDungService">Dịch vụ chủ sử dụng.</param>
    /// <returns>Kết quả truy vấn chủ sử dụng.</returns>
    private static async Task<IResult> GetChuSuDungBySoDinhDanh(
        [FromQuery] string serial,
        [FromQuery] string soDinhDanh,
        ILogger<Program> logger,
        IChuSuDungService chuSuDungService)
    {
        var result = await chuSuDungService.GetAuthChuSuDungBySoDinhDanhAsync(serial, soDinhDanh);
        return await Task.FromResult(result.Match(
            Results.Ok,
            ex => Results.BadRequest(ex.Message)));
    }
}