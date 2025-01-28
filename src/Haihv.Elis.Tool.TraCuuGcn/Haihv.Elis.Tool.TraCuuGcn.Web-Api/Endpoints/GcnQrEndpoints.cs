using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Endpoints;

public static class GcnQrEndpoints
{
    public static void MapGcnQrEndpoints(this WebApplication app)
    {
        app.MapGet("/elis/qr-info", GetQrInfoAsync)
            .WithName("GetQrInfoAsync");
    }

    private static async Task<IResult> GetQrInfoAsync([FromQuery] string maQr, ILogger<Program> logger,
        IGcnQrService gcnQrService)
    {
        var result = await gcnQrService.GetResultAsync(maQr);
        return await Task.FromResult(result.Match(
            Results.Ok,
            ex => Results.BadRequest(ex.Message)));
    }
}