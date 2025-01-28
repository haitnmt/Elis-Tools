using System.Globalization;
using System.Security.Claims;
using System.Text;
using Haihv.Elis.Tool.TraCuuGcn.Models;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Settings;
using LanguageExt;
using LanguageExt.Common;
using ZiggyCreatures.Caching.Fusion;
using ILogger = Serilog.ILogger;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Authenticate;

public interface IAuthenticationService
{
    /// <summary>
    /// Kiểm tra xác thực.
    /// </summary>
    /// <param name="maGcn">Mã GCN.</param>
    /// <param name="claimsPrincipal">Thông tin xác thực của người dùng.</param>
    /// <param name="cancellationToken">Token hủy bỏ.</param>
    /// <returns>Trả về true nếu xác thực thành công, ngược lại trả về false.</returns>
    ValueTask<bool> CheckAuthenticationAsync(long maGcn = 0, ClaimsPrincipal? claimsPrincipal = null,
        CancellationToken cancellationToken = default);

    ValueTask<Result<AccessToken>> AuthChuSuDungAsync(AuthChuSuDung? authChuSuDung);
}

public sealed class AuthenticationService(
    IChuSuDungService chuSuDungService,
    ILogger logger,
    IFusionCache fusionCache,
    TokenProvider tokenProvider) : IAuthenticationService
{
    /// <summary>
    /// Kiểm tra xác thực.
    /// </summary>
    /// <param name="maGcn">Mã GCN.</param>
    /// <param name="claimsPrincipal">Thông tin xác thực của người dùng.</param>
    /// <param name="cancellationToken">Token hủy bỏ.</param>
    /// <returns>Trả về true nếu xác thực thành công, ngược lại trả về false.</returns>
    public async ValueTask<bool> CheckAuthenticationAsync(long maGcn = 0, ClaimsPrincipal? claimsPrincipal = null,
        CancellationToken cancellationToken = default)
    {
        if (claimsPrincipal is null || maGcn <= 0) return false;
        var soDinhDanh = claimsPrincipal.GetSoDinhDanh();
        if (string.IsNullOrWhiteSpace(soDinhDanh)) return false;
        var chuSuDung = await fusionCache.GetOrDefaultAsync<string>(CacheSettings.KeyAuthentication(soDinhDanh, maGcn),
            token: cancellationToken);
        if (chuSuDung is not null) return true;
        var chuSuDungResult =
            await chuSuDungService.GetResultAuthChuSuDungAsync(maGcn, soDinhDanh, cancellationToken);
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
        var maGcn = authChuSuDung.MaGcn;
        var hoTen = authChuSuDung.HoVaTen;
        if (string.IsNullOrWhiteSpace(soDinhDanh) || maGcn <= 0 ||
            string.IsNullOrWhiteSpace(hoTen))
            return new Result<AccessToken>(new ValueIsNullException("Thông tin xác thực không hợp lệ!"));
        var chuSuDung = await chuSuDungService.GetResultAuthChuSuDungAsync(maGcn, soDinhDanh);
        return chuSuDung.Match(
            csd =>
            {
                if (!CompareVietnameseStrings(csd.HoVaTen, hoTen))
                    return new Result<AccessToken>(new ValueIsNullException("Thông tin xác thực không hợp lệ!"));
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