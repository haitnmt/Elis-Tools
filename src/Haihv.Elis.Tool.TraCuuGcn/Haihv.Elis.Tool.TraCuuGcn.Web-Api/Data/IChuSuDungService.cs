using Haihv.Elis.Tool.TraCuuGcn.Models;
using LanguageExt.Common;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public interface IChuSuDungService
{
    /// <summary>
    /// Lấy thông tin chủ sử dụng theo số định danh.
    /// </summary>
    /// <param name="maGcn">Mã GCN của Giấy chứng nhận.</param>
    /// <param name="soDinhDanh">Số định danh.</param>
    /// <param name="cancellationToken">Token hủy bỏ.</param>
    /// <returns>Kết quả chứa thông tin xác thực chủ sử dụng hoặc lỗi.</returns>
    ValueTask<Result<AuthChuSuDung>> GetResultAuthChuSuDungAsync(long maGcn = 0, string? soDinhDanh = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin chủ sử dụng và quan hệ chủ sử dụng.
    /// </summary>
    /// <param name="maGcn">Mã GCN của Giấy chứng nhận.</param>
    /// <param name="soDinhDanh">Số định danh.</param>
    /// <param name="cancellationToken">Token hủy bỏ.</param>
    /// <returns>Kết quả chứa thông tin chủ sử dụng hoặc lỗi.</returns>
    ValueTask<Result<ChuSuDungInfo?>> GetResultAsync(
        long maGcn = 0,
        string? soDinhDanh = null,
        CancellationToken cancellationToken = default);
}