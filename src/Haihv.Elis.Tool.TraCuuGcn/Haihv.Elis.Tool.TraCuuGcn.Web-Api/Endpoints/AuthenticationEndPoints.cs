using Haihv.Elis.Tool.TraCuuGcn.Models;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Authenticate;
using Microsoft.AspNetCore.Mvc;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Endpoints;

public static class AuthenticationEndPoints
{
    /// <summary>
    /// Định nghĩa endpoint cho xác thực chủ sử dụng.
    /// </summary>
    /// <param name="app">Ứng dụng web để cấu hình endpoint.</param>
    public static void MapAuthenticationEndpoints(this WebApplication app)
    {
        app.MapPost("/elis/auth", PostAuthChuSuDungAsync)
            .WithName("PostAuthChuSuDungAsync");
    }

    /// <summary>
    /// Xử lý yêu cầu xác thực chủ sử dụng.
    /// </summary>
    /// <param name="authChuSuDung">Thông tin xác thực chủ sử dụng.</param>
    /// <param name="logger">Logger để ghi log.</param>
    /// <param name="authenticationService">Dịch vụ xác thực.</param>
    /// <returns>Kết quả xác thực dưới dạng <see cref="IResult"/>.</returns>
    private static async Task<IResult> PostAuthChuSuDungAsync(
        [FromBody] AuthChuSuDung authChuSuDung,
        ILogger<Program> logger,
        IAuthenticationService authenticationService)
    {
        var result = await authenticationService.AuthChuSuDungAsync(authChuSuDung);
        return await Task.FromResult(result.Match(
            Results.Ok,
            ex => Results.BadRequest(ex.Message)));
    }
}