using System.Globalization;
using System.Text;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Authenticate;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Endpoints;

public static class AuthenticationEndPoints
{
    public static void MapAuthentication(this WebApplication app)
    {
        app.MapPost("/elis/auth", PostAuthChuSuDungAsync)
            .WithName("PostAuthChuSuDungAsync");
    }

    private static async Task<IResult> PostAuthChuSuDungAsync(
        [FromBody] GiayChungNhan giayChungNhan,
        [FromQuery] string soDinhDanh,
        [FromQuery] string hoVaTen,
        ILogger<Program> logger,
        IChuSuDungService chuSuDungService,
        TokenProvider tokenProvider)
    {
        var result = await chuSuDungService.GetBySoDinhDanhAsync(giayChungNhan, soDinhDanh);
        return await Task.FromResult(result.Match(
            chuSuDung =>
            {
                // So sánh thông tin chủ sử dụng từ cơ sở dữ liệu với thông tin gửi lên từ client
                // Không phân biệt hoa thường và dấu tiếng Việt
                if (!CompareVietnameseStrings(chuSuDung.HoVaTen, hoVaTen))
                {
                    return Results.BadRequest("Thông tin chủ sử dụng không chính xác!");
                }

                var authChuSuDung = new AuthChuSuDung(chuSuDung.SoDinhDanh, chuSuDung.HoVaTen);
                var (token, tokenId) = tokenProvider.GenerateToken(authChuSuDung);
                return Results.Ok(new AccessToken(token, tokenId));
            },
            ex => Results.BadRequest(ex.Message)));
    }

    private static bool CompareVietnameseStrings(string str1, string str2)
    {
        str1 = RemoveDiacritics(str1);
        str2 = RemoveDiacritics(str2);
        // So sánh không phân biệt hoa thường
        return string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase);
    }

    private static string RemoveDiacritics(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Chuyển chuỗi về dạng chuẩn NFD
        var normalized = input.Normalize(NormalizationForm.FormD);

        // Loại bỏ các ký tự không phải ký tự cơ bản (dấu)
        return new string(normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());
    }
}