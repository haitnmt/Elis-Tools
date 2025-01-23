using System.Globalization;
using System.Security.Claims;
using System.Text;
using Haihv.Elis.Tool.TraCuuGcn.Models;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Settings;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Caching.Hybrid;
using ILogger = Serilog.ILogger;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Authenticate;

public interface IAuthenticationService
{
    ValueTask<bool> CheckAuthenticationAsync(string serial, ClaimsPrincipal? claimsPrincipal,
        CancellationToken cancellationToken = default);

    ValueTask<Result<AccessToken>> AuthChuSuDungAsync(AuthChuSuDung? authChuSuDung);
}

public sealed class AuthenticationService(
    IChuSuDungService chuSuDungService,
    ILogger logger,
    HybridCache hybridCache,
    TokenProvider tokenProvider) : IAuthenticationService
{
    public async ValueTask<bool> CheckAuthenticationAsync(string serial, ClaimsPrincipal? claimsPrincipal,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serial) || claimsPrincipal is null) return false;
        var soDinhDanh = claimsPrincipal.GetSoDinhDanh();
        if (string.IsNullOrWhiteSpace(soDinhDanh)) return false;
        var chuSuDung = await hybridCache.GetOrCreateAsync<string?>(CacheSettings.KeyAuthentication(soDinhDanh, serial),
            _ => ValueTask.FromResult<string?>(null),
            cancellationToken: cancellationToken);
        if (chuSuDung is not null) return true;
        var chuSuDungResult =
            await chuSuDungService.GetAuthChuSuDungBySoDinhDanhAsync(serial, soDinhDanh, cancellationToken);
        return chuSuDungResult.Match(
            csd =>
            {
                if (!CompareVietnameseStrings(csd.HoVaTen, claimsPrincipal.GetHoVaTen()))
                {
                    logger.Warning("Xác thực thất bại! Số định danh: {SoDinhDanh}", soDinhDanh);
                    return false;
                }

                logger.Information("Xác thực thành công! SoDinhDanh: {SoDinhDanh}", soDinhDanh);
                return true;
            },
            _ =>
            {
                logger.Warning("Xác thực thất bại! Số định danh: {SoDinhDanh}", soDinhDanh);
                return false;
            });
    }

    public async ValueTask<Result<AccessToken>> AuthChuSuDungAsync(AuthChuSuDung? authChuSuDung)
    {
        if (authChuSuDung is null)
            return new Result<AccessToken>(new ValueIsNullException("Thông tin xác thực không hợp lệ!"));
        var soDinhDanh = authChuSuDung.SoDinhDanh;
        var serial = authChuSuDung.Serial;
        var hoTen = authChuSuDung.HoVaTen;
        if (string.IsNullOrWhiteSpace(soDinhDanh) || string.IsNullOrWhiteSpace(serial) ||
            string.IsNullOrWhiteSpace(hoTen))
            return new Result<AccessToken>(new ValueIsNullException("Thông tin xác thực không hợp lệ!"));
        var chuSuDung = await chuSuDungService.GetAuthChuSuDungBySoDinhDanhAsync(serial, soDinhDanh);
        return chuSuDung.Match(
            csd =>
            {
                if (!CompareVietnameseStrings(csd.HoVaTen, hoTen))
                    return new Result<AccessToken>(new ValueIsNullException("Thông tin xác thực không hợp lệ!"));
                authChuSuDung = csd with { Serial = serial };
                var accessToken = tokenProvider.GenerateToken(authChuSuDung);
                return new Result<AccessToken>(accessToken);
            },
            _ => new Result<AccessToken>(new ValueIsNullException("Không tìm thấy chủ sử dụng!")));
    }

    /// <summary>
    /// So sánh hai chuỗi tiếng Việt không phân biệt hoa thường và dấu.
    /// </summary>
    /// <param name="str1">Chuỗi thứ nhất.</param>
    /// <param name="str2">Chuỗi thứ hai.</param>
    /// <returns>Trả về true nếu hai chuỗi giống nhau, ngược lại trả về false.</returns>
    private static bool CompareVietnameseStrings(string? str1, string? str2)
    {
        if (string.IsNullOrWhiteSpace(str1) || string.IsNullOrWhiteSpace(str2)) return false;
        str1 = RemoveDiacritics(str1);
        str2 = RemoveDiacritics(str2);
        // So sánh không phân biệt hoa thường
        return string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Loại bỏ các dấu tiếng Việt và ký tự đặc biệt khỏi chuỗi.
    /// </summary>
    /// <param name="input">Chuỗi đầu vào cần loại bỏ dấu.</param>
    /// <returns>Chuỗi đã được loại bỏ dấu.</returns>
    private static string RemoveDiacritics(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Chuyển chuỗi về dạng chuẩn NFD
        var normalized = input.Normalize(NormalizationForm.FormD);

        // Loại bỏ các ký tự không phải ký tự cơ bản (dấu) và ký tự không phải chữ, số
        return new string(normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark &&
                        char.IsLetterOrDigit(c))
            .ToArray());
    }
}