using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Authenticate;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Endpoints;

public static class ChuSuDungEndpoints
{
    public static void MapChuSuDung(this WebApplication app)
    {
        app.MapGet("/elis/csd", GetChuSuDungBySoDinhDanh)
            .WithName("GetChuSuDung");
        app.MapPost("/elis/auth-csd", PostAuthChuSuDungAsync)
            .WithName("PostAuthChuSuDungAsync");
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

    private static async Task<IResult> PostAuthChuSuDungAsync(
        [FromBody] GiayChungNhan giayChungNhan,
        [FromQuery] string soDinhDanh,
        ILogger<Program> logger,
        IChuSuDungService chuSuDungService,
        TokenProvider tokenProvider)
    {
        var result = await chuSuDungService.GetBySoDinhDanhAsync(giayChungNhan, soDinhDanh);
        return await Task.FromResult(result.Match(
            chuSuDung =>
            {
                var authChuSuDung = new AuthChuSuDung(chuSuDung.SoDinhDanh, chuSuDung.HoVaTen);
                var (token, tokenId) = tokenProvider.GenerateToken(authChuSuDung);
                return Results.Ok(new AccessToken(token, tokenId));
            },
            ex => Results.BadRequest(ex.Message)));
    }

    private record AccessToken(string Token, string TokenId);
}