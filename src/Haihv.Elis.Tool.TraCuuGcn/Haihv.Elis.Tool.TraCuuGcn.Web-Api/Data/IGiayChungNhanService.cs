using Haihv.Elis.Tool.TraCuuGcn.Models;
using LanguageExt.Common;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public interface IGiayChungNhanService
{
    /// <summary>
    /// Lấy thông tin Giấy chứng nhận theo số serial.
    /// </summary>
    /// <param name="serial">Số serial của Giấy chứng nhận.</param>
    /// <param name="maGcn">Mã GCN của Giấy chứng nhận.</param>
    /// <param name="cancellationToken">Token hủy bỏ tác vụ không bắt buộc.</param>
    /// <returns>Kết quả chứa thông tin Giấy chứng nhận hoặc lỗi nếu không tìm thấy.</returns>
    Task<Result<GiayChungNhan>> GetResultAsync(string? serial = null, long maGcn = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin Giấy chứng nhận theo số serial.
    /// </summary>
    /// <param name="serial">Số serial của Giấy chứng nhận.</param>
    /// <param name="maGcn"></param>
    /// <param name="cancellationToken">Token hủy bỏ tác vụ không bắt buộc.</param>
    /// <returns>Kết quả chứa thông tin Giấy chứng nhận.</returns>
    /// <exception cref="Exception">Ném ra ngoại lệ nếu có lỗi xảy ra trong quá trình truy vấn cơ sở dữ liệu.</exception>
    Task<GiayChungNhan?> GetAsync(string? serial = null, long maGcn = 0,
        CancellationToken cancellationToken = default);
}