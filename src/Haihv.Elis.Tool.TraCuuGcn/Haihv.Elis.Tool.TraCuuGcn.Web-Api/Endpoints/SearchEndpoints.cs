using Haihv.Elis.Tool.TraCuuGcn.Models;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Endpoints;

public static class SearchEndpoints
{
    public static void MapSearchEndpoints(this WebApplication app)
    {
        app.MapGet("/elis/search", GetSearchResultAsync)
            .WithName("GetGcnInfoAsync");
    }

    private static async Task<IResult> GetSearchResultAsync([FromQuery] string? query,
        ISearchService searchService)
    {
        var result = await searchService.GetResultAsync(query);
        return await Task.FromResult(result.Match(
            info=> Results.Ok(new Response<GiayChungNhanWithMaQrInfo>(info)),
            ex => Results.BadRequest(new Response<GiayChungNhanWithMaQrInfo>(ex.Message))));
    }
    
}