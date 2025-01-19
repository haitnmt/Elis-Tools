using System.Security.Claims;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Settings;
using Microsoft.Extensions.Caching.Hybrid;
using ILogger = Serilog.ILogger;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Authenticate;

public interface IAuthenticationService
{
    ValueTask<bool> CheckAuthenticationAsync(GiayChungNhan? giayChungNhan, ClaimsPrincipal? claimsPrincipal, 
        CancellationToken cancellationToken = default);
}

public sealed class AuthenticationService(
    IChuSuDungService chuSuDungService,
    ILogger logger,
    HybridCache hybridCache) : IAuthenticationService
{
    public async ValueTask<bool> CheckAuthenticationAsync(GiayChungNhan? giayChungNhan, ClaimsPrincipal? claimsPrincipal, 
        CancellationToken cancellationToken = default)
    {
        if (giayChungNhan is null || 
            claimsPrincipal is null ) return false;
        var soDinhDanh = claimsPrincipal.GetSoDinhDanh();
        if (string.IsNullOrWhiteSpace(soDinhDanh)) return false;
        var chuSuDungResult = await hybridCache.GetOrCreateAsync(CacheSettings.KeyChuSuDung(soDinhDanh,giayChungNhan.Serial), 
            cancel => chuSuDungService.GetBySoDinhDanhAsync(giayChungNhan, soDinhDanh, cancel), 
            cancellationToken: cancellationToken);
        return chuSuDungResult.Match(
            chuSuDung =>
            {
                logger.Information("Xác thực thành công! SoDinhDanh: {SoDinhDanh}", soDinhDanh);
                return chuSuDung.SoDinhDanh == soDinhDanh;
            },
            _ =>
            {
                logger.Warning("Xác thực thất bại! Số định danh: {SoDinhDanh}", soDinhDanh);
                return false;
            });
    }
}