using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;

public static class ToBanDoExtensions
{
    private const long DefaultTempMaToBanDo = long.MaxValue;

    /// <summary>
    /// Cập nhật thông tin tờ bản đồ.
    /// </summary>
    /// <param name="dataContext">Ngữ cảnh dữ liệu Elis.</param>
    /// <param name="thamChieuToBanDos">Danh sách tham chiếu tờ bản đồ.</param>
    /// <param name="formatGhiChuToBanDo">Định dạng ghi chú tờ bản đồ (tùy chọn).</param>
    /// <param name="ngaySapNhap">Ngày sắp nhập (tùy chọn).</param>
    /// <returns>Trả về true nếu cập nhật thành công, ngược lại false.</returns>
    public static async Task<bool> UpdateToBanDoAsync(this ElisDataContext dataContext,
        List<ThamChieuToBanDo> thamChieuToBanDos, string? formatGhiChuToBanDo = null, string? ngaySapNhap = null)
    {
        if (thamChieuToBanDos.Count == 0)
            return false;

        // Nếu formatGhiChuToBanDo rỗng thì sử dụng giá trị mặc định
        if (string.IsNullOrWhiteSpace(formatGhiChuToBanDo))
            formatGhiChuToBanDo = ThamSoThayThe.DefaultGhiChuToBanDo;

        // Nếu ngaySapNhap rỗng thì sử dụng ngày hiện tại
        if (string.IsNullOrWhiteSpace(ngaySapNhap))
            ngaySapNhap = DateTime.Now.ToString(ThamSoThayThe.DinhDangNgaySapNhap);

        foreach (var thamChieuToBanDo in thamChieuToBanDos)
        {
            // Tạo ghi chú cho tờ bản đồ
            var ghiChuToBanDo = formatGhiChuToBanDo
                .Replace(ThamSoThayThe.NgaySapNhap, ngaySapNhap)
                .Replace(ThamSoThayThe.ToBanDo, thamChieuToBanDo.SoToBanDoTruoc)
                .Replace(ThamSoThayThe.DonViHanhChinh, thamChieuToBanDo.TenDvhcTruoc);

            // Lấy tờ bản đồ từ cơ sở dữ liệu
            var toBanDoInDb = await dataContext.ToBanDos.FindAsync(thamChieuToBanDo.MaToBanDoTruoc);

            // Nếu tờ bản đồ không tồn tại thì bỏ qua
            if (toBanDoInDb == null) continue;

            // Nếu tờ bản đồ sau khác 0 thì cập nhật lại mã tờ bản đồ
            if (thamChieuToBanDo.MaToBanDoSau != 0)
            {
                toBanDoInDb.MaToBanDo = thamChieuToBanDo.MaToBanDoSau;
            }

            // Cập nhật mã đơn vị hành chính cho tờ bản đồ
            toBanDoInDb.MaDvhc = thamChieuToBanDo.MaDvhcSau;
            toBanDoInDb.SoTo = thamChieuToBanDo.SoToBanDoSau;
            // Cập nhật ghi chú cho tờ bản đồ
            toBanDoInDb.GhiChu = string.IsNullOrWhiteSpace(toBanDoInDb.GhiChu)
                ? ghiChuToBanDo
                : $"{toBanDoInDb.GhiChu} - [{ghiChuToBanDo}]";

            // Cập nhật tờ bản đồ vào cơ sở dữ liệu
            dataContext.ToBanDos.Update(toBanDoInDb);
        }

        // Lưu thay đổi vào cơ sở dữ liệu
        await dataContext.SaveChangesAsync();
        return true;
    }

    public static async Task<long> CreateTempToBanDoAsync(this ElisDataContext dbContext,
        long? maToBanDo = null)
    {
        try
        {
            var toBanDo = new ToBanDo
            {
                MaToBanDo = maToBanDo ?? DefaultTempMaToBanDo,
                SoTo = "Temp",
                MaDvhc = 100001,
                GhiChu = "Tờ bản đồ tạm thời"
            };

            // Kiểm tra tồn tại:
            var toBanDoInDb = await dbContext.ToBanDos.FindAsync(toBanDo.MaToBanDo);
            if (toBanDoInDb != null)
                return toBanDo.MaToBanDo;

            // Thêm mới nếu không tồn tại
            dbContext.ToBanDos.Add(toBanDo);
            await dbContext.SaveChangesAsync();

            return toBanDo.MaToBanDo;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return long.MinValue;
        }
    }
}