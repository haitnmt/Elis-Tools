using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;

public static class DonViHanhChinhExtensions
{
    public static async Task<bool> UpdateDonViHanhChinhAsync(this ElisDataContext dataContext, DvhcRecord dvhcMoi,
        List<int> maDvhcBiSapNhaps, string? formatTenDonViHanhChinhBiSapNhap = null, string? ngaySapNhap = null)
    {
        // Lấy Đơn Vị Hành Chính mới từ cơ sở dữ liệu
        var dvhcMoiInDb = await dataContext.Dvhcs.FindAsync(dvhcMoi.MaDvhc);

        // Nếu không tìm thấy Đơn Vị Hành Chính mới thì trả về false
        if (dvhcMoiInDb == null) return false;

        // Cập nhật Tên Đơn Vị Hành Chính mới
        dataContext.Dvhcs.Update(dvhcMoiInDb);

        if (maDvhcBiSapNhaps.Count == 0)
            return true;

        if (string.IsNullOrWhiteSpace(formatTenDonViHanhChinhBiSapNhap))
            formatTenDonViHanhChinhBiSapNhap = ThamSoThayThe.DefaultDonViHanhChinhBiSapNhap;

        if (string.IsNullOrWhiteSpace(ngaySapNhap))
            ngaySapNhap = DateTime.Now.ToString(ThamSoThayThe.DinhDangNgaySapNhap);

        // Cập nhật Tên Đơn Vị Hành Chính cũ
        foreach (var maDvhcBiSapNhap in maDvhcBiSapNhaps)
        {
            // Lấy Đơn Vị Hành Chính cũ từ cơ sở dữ liệu
            var dvhcInDb = await dataContext.Dvhcs.FindAsync(maDvhcBiSapNhap);

            // Nếu không tìm thấy Đơn Vị Hành Chính cũ thì bỏ qua
            if (dvhcInDb == null) continue;

            // Tạo ghi chú cho Đơn Vị Hành Chính cũ
            var ghiChuDvhc = formatTenDonViHanhChinhBiSapNhap
                .Replace(ThamSoThayThe.NgaySapNhap, ngaySapNhap)
                .Replace(ThamSoThayThe.DonViHanhChinh, dvhcMoi.Ten);

            dvhcInDb.Ten = $"{dvhcInDb.Ten} [{ghiChuDvhc}]";
            dataContext.Dvhcs.Update(dvhcInDb);
        }

        await dataContext.SaveChangesAsync();
        return true;
    }
}