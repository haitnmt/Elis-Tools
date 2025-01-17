using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Endpoints;

public static class GiayChungNhanEndpoints
{
    public static void MapGiayChungNhan(this WebApplication app)
    {
        app.MapGet("/elis/gcn", GetGiayChungNhanBySerial)
            .WithName("GetGiayChungNhan");
        app.MapGet("/elis/thua-dat", GetThuaDatByGiayChungNhan)
            .WithName("GetThuaDatByGiayChungNhan")
            .RequireAuthorization();
        app.MapGet("/elis/thua-dat-public", GetThuaDatPublicByGiayChungNhan)
            .WithName("GetThuaDatPublicByGiayChungNhan");
    }

    private static async Task<IResult> GetGiayChungNhanBySerial([FromQuery] string serial, ILogger<Program> logger,
        IGiayChungNhanService giayChungNhanService)
    {
        var result = await giayChungNhanService.GetBySerialAsync(serial);
        return await Task.FromResult(result.Match(
            Results.Ok,
            ex => Results.BadRequest(ex.Message)));
    }

    private static async Task<IResult> GetThuaDatByGiayChungNhan([FromBody] GiayChungNhan giayChungNhan,
        ILogger<Program> logger,
        IGiayChungNhanService giayChungNhanService)
    {
        var result = await giayChungNhanService.GetThuaDatByGiayChungNhanAsync(giayChungNhan);
        return await Task.FromResult(result.Match(
            Results.Ok,
            ex => Results.BadRequest(ex.Message)));
    }

    private static async Task<IResult> GetThuaDatPublicByGiayChungNhan([FromBody] GiayChungNhan giayChungNhan,
        ILogger<Program> logger,
        IGiayChungNhanService giayChungNhanService)
    {
        var result = await giayChungNhanService.GetThuaDatByGiayChungNhanAsync(giayChungNhan);
        return await Task.FromResult(result.Match(
            thuaDat => Results.Ok(thuaDat.ConvertToThuaDatPublic()),
            ex => Results.BadRequest(ex.Message)));
    }
}