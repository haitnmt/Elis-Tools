using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;

public static class DonViHanhChinhExtensions
{
    /// <summary>
    /// Cập nhật Đơn Vị Hành Chính mới và các Đơn Vị Hành Chính bị sáp nhập.
    /// </summary>
    /// <param name="dataContext">Ngữ cảnh dữ liệu.</param>
    /// <param name="dvhcMoi">Đơn Vị Hành Chính mới.</param>
    /// <param name="maDvhcBiSapNhaps">Danh sách mã Đơn Vị Hành Chính bị sáp nhập.</param>
    /// <param name="formatTenDonViHanhChinhBiSapNhap">Định dạng tên Đơn Vị Hành Chính bị sáp nhập (tùy chọn).</param>
    /// <returns>Trả về true nếu cập nhật thành công, ngược lại false.</returns>
    public static async Task<bool> UpdateDonViHanhChinhAsync(this ElisDataContext dataContext, DvhcRecord dvhcMoi,
        List<int> maDvhcBiSapNhaps, string? formatTenDonViHanhChinhBiSapNhap = null)
    {
        // Lấy Đơn Vị Hành Chính mới từ cơ sở dữ liệu
        var dvhcMoiInDb = await dataContext.Dvhcs.FindAsync(dvhcMoi.MaDvhc);

        // Nếu không tìm thấy Đơn Vị Hành Chính mới thì trả về false
        if (dvhcMoiInDb == null && maDvhcBiSapNhaps.Count == 0) return false;

        // Nếu  có Đơn Vị Hành Chính bị sáp nhập thì thực hiện cập nhật
        if (maDvhcBiSapNhaps.Count != 0)
        {
            // Nếu formatTenDonViHanhChinhBiSapNhap rỗng thì sử dụng giá trị mặc định
            if (string.IsNullOrWhiteSpace(formatTenDonViHanhChinhBiSapNhap))
                formatTenDonViHanhChinhBiSapNhap = ThamSoThayThe.DefaultDonViHanhChinhBiSapNhap;

            // Cập nhật Tên Đơn Vị Hành Chính cũ
            foreach (var maDvhcBiSapNhap in maDvhcBiSapNhaps)
            {
                // Lấy Đơn Vị Hành Chính cũ từ cơ sở dữ liệu
                var dvhcInDb = await dataContext.Dvhcs.FindAsync(maDvhcBiSapNhap);

                // Nếu không tìm thấy Đơn Vị Hành Chính cũ thì bỏ qua
                if (dvhcInDb == null) continue;

                // Tạo ghi chú cho Đơn Vị Hành Chính cũ
                var ghiChuDvhc = formatTenDonViHanhChinhBiSapNhap
                    .Replace(ThamSoThayThe.DonViHanhChinh, dvhcMoi.Ten);

                if (dvhcInDb.Ten.Contains(dvhcMoi.Ten)) continue;
                dvhcInDb.Ten = $"{dvhcInDb.Ten} [{ghiChuDvhc}]";
                dataContext.Dvhcs.Update(dvhcInDb);
            }
        }

        // Cập nhật Tên Đơn Vị Hành Chính mới
        if (dvhcMoiInDb != null && dvhcMoiInDb.Ten != dvhcMoi.Ten)
        {
            dvhcMoiInDb.Ten = dvhcMoi.Ten;
            dataContext.Dvhcs.Update(dvhcMoiInDb);
        }

        await dataContext.SaveChangesAsync();
        return true;
    }
}