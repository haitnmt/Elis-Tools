using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Endpoints;

public static class GiayChungNhanEndpoints
{
    public static void MapGiayChungNhans(this WebApplication app)
    {
        app.MapGet("/elis/gcn", GetGiayChungNhanBySerial)
            .WithName("GetGiayChungNhan");
    }

    private static async Task<IResult> GetGiayChungNhanBySerial([FromQuery] string serial, ILogger<Program> logger,
        IGiayChungNhanService giayChungNhanService)
    {
        var result = await giayChungNhanService.GetBySerialAsync(serial);
        return await Task.FromResult(result.Match(
            Results.Ok,
            ex => Results.BadRequest(ex.Message)));
    }
}