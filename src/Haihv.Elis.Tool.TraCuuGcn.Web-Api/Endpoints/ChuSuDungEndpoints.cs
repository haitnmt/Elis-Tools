using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Endpoints;

public static class ChuSuDungEndpoints
{
    public static void MapChuSuDung(this WebApplication app)
    {
        app.MapGet("/elis/csd", GetChuSuDungBySoDinhDanh)
            .WithName("GetChuSuDung");
    }
    private static async Task<IResult> GetChuSuDungBySoDinhDanh(
        [FromBody] GiayChungNhan giayChungNhan, 
        [FromQuery] string soDinhDanh, 
        ILogger<Program> logger,
        IChuSuDungService chuSuDungService)
    {
        var result = await chuSuDungService.GetBySoDinhDanhAsync(giayChungNhan, soDinhDanh);
        return await Task.FromResult(result.Match(
            Results.Ok,
            ex => Results.BadRequest(ex.Message)));
    }
}