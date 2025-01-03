namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;

/// <summary>
/// Lớp chứa các phương thức mở rộng để tạo và xử lý khóa chính.
/// </summary>
public static class PrimaryKeyExtensions
{
    /// <summary>
    /// Tạo khóa chính từ số thứ tự và mã đơn vị hành chính cấp xã.
    /// </summary>
    /// <param name="soThuTu">Số thứ tự</param>
    /// <param name="maDvhcCapXa">Mã đơn vị hành chính cấp xã</param>
    /// <returns>Khóa chính được tạo</returns>
    public static long GeneratePrimaryKey(this long soThuTu, int maDvhcCapXa)
        => maDvhcCapXa * 100000 + soThuTu;

    /// <summary>
    /// Lấy khóa chính lớn nhất có thể từ mã đơn vị hành chính cấp xã.
    /// </summary>
    /// <param name="maDvhcCapXa">Mã đơn vị hành chính cấp xã</param>
    /// <returns>Khóa chính lớn nhất có thể</returns>
    public static long GetMaxPrimaryKey(this int maDvhcCapXa)
        => maDvhcCapXa * 100000 + 99999;

    /// <summary>
    /// Lấy khóa chính nhỏ nhất có thể từ mã đơn vị hành chính cấp xã.
    /// </summary>
    /// <param name="maDvhcCapXa">Mã đơn vị hành chính cấp xã</param>
    /// <returns>Khóa chính nhỏ nhất có thể</returns>
    public static long GetMinPrimaryKey(this int maDvhcCapXa)
        => maDvhcCapXa * 100000;

    /// <summary>
    /// Loại bỏ mã đơn vị hành chính cấp xã trong khóa chính.
    /// </summary>
    /// <param name="primaryKey">Khóa chính</param>
    /// <returns>5 số cuối của mã</returns>
    public static long RemoveMaDvhcCapXaInPrimaryKey(this long primaryKey)
        => primaryKey % 100000;

    /// <summary>
    /// Tạo khóa chính tiếp theo từ khóa chính hiện tại.
    /// </summary>
    /// <param name="primaryKey">Khóa chính hiện tại</param>
    /// <param name="maDvhcCapXa">Mã đơn vị hành chính cấp xã</param>
    /// <returns>Khóa chính tiếp theo</returns>
    public static long GenerateNextPrimaryKey(this long primaryKey, int maDvhcCapXa = 0)
    {
        if (maDvhcCapXa == 0)
            return primaryKey + 1;
        return GeneratePrimaryKey(RemoveMaDvhcCapXaInPrimaryKey(primaryKey) + 1, maDvhcCapXa);
    }

    /// <summary>
    /// Tìm khóa chính nhỏ nhất chưa sử dụng trong tập hợp các khóa chính.
    /// </summary>
    /// <param name="primaryKeys">Tập hợp các khóa chính</param>
    /// <param name="currentPrimaryKey">Khóa chính hiện tại (tùy chọn).</param>
    /// <returns>Khóa chính nhỏ nhất chưa sử dụng</returns>
    public static long FindSmallestNotUsedPrimaryKey(this HashSet<long> primaryKeys, long currentPrimaryKey = 0)
    {
        if (primaryKeys.Count == 0)
        {
            throw new Exception("Không có khóa chính nào trong tập hợp.");
        }

        var min = primaryKeys.Min();
        min = currentPrimaryKey < min ? min : currentPrimaryKey;

        var max = primaryKeys.Max();
        for (var i = min; i <= max; i++)
        {
            if (!primaryKeys.Contains(i))
            {
                return i;
            }
        }

        return max + 1;
    }
}