using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;

public static class ThuaDatCuExtensions
{
    /// <summary>
    /// Tạo hoặc cập nhật thông tin Thửa Đất Cũ.
    /// </summary>
    /// <param name="dataContext">Ngữ cảnh dữ liệu Elis.</param>
    /// <param name="thuaDatCapNhats">
    /// Danh sách Thửa Đất cần cập nhật.
    /// </param>
    /// <param name="formatToBanDoCu">
    /// Định dạng tờ bản đồ cũ.
    /// </param>
    /// <param name="ngaySapNhap">
    /// Ngày sáp nhập.
    /// </param>
    /// <returns>Trả về true nếu có bản ghi được tạo hoặc cập nhật, ngược lại trả về false.</returns>
    public static async Task<bool> CreateOrUpdateThuaDatCuAsync(this ElisDataContext dataContext,
        List<ThuaDatCapNhat> thuaDatCapNhats, string? formatToBanDoCu = null, string? ngaySapNhap = null)
    {
        if (thuaDatCapNhats.Count == 0)
            return false;

        if (string.IsNullOrWhiteSpace(formatToBanDoCu))
            formatToBanDoCu = ThamSoThayThe.DefaultToBanDoCu;

        if (string.IsNullOrWhiteSpace(ngaySapNhap))
            ngaySapNhap = DateTime.Now.ToString(ThamSoThayThe.DinhDangNgaySapNhap);

        foreach (var thuaDatCapNhat in from thuaDatCapNhat in thuaDatCapNhats
                 let toBanDoCu = formatToBanDoCu
                     .Replace(ThamSoThayThe.ToBanDo, thuaDatCapNhat.ToBanDo)
                     .Replace(ThamSoThayThe.DonViHanhChinh, thuaDatCapNhat.TenDonViHanhChinh)
                 select thuaDatCapNhat)
        {
            var thuaDatCuInDb = await dataContext.ThuaDatCus.FindAsync(thuaDatCapNhat.MaThuaDat);
            if (thuaDatCuInDb == null)
            {
                await dataContext.ThuaDatCus.AddAsync(new ThuaDatCu()
                {
                    MaThuaDat = thuaDatCapNhat.MaThuaDat,
                    ToBanDoCu = thuaDatCapNhat.ToBanDo
                });
            }
            else
            {
                thuaDatCuInDb.ToBanDoCu = string.IsNullOrWhiteSpace(thuaDatCuInDb.ToBanDoCu)
                    ? thuaDatCapNhat.ToBanDo
                    : $"{thuaDatCuInDb.ToBanDoCu} - [{thuaDatCapNhat.ToBanDo}]";
                dataContext.ThuaDatCus.Update(thuaDatCuInDb);
            }
        }

        await dataContext.SaveChangesAsync();
        return true;
    }
}