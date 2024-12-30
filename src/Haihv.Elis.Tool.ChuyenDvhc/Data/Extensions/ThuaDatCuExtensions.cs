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
    ///     Danh sách Thửa Đất cần cập nhật.
    /// </param>
    /// <param name="formatToBanDoCu">
    ///     Định dạng tờ bản đồ cũ.
    /// </param>
    /// <returns>Trả về true nếu có bản ghi được tạo hoặc cập nhật, ngược lại trả về false.</returns>
    public static async Task CreateOrUpdateThuaDatCuAsync(this ElisDataContext dataContext,
        List<ThuaDatCapNhat> thuaDatCapNhats, string? formatToBanDoCu = null)
    {
        if (thuaDatCapNhats.Count == 0) return;

        if (string.IsNullOrWhiteSpace(formatToBanDoCu))
            formatToBanDoCu = ThamSoThayThe.DefaultToBanDoCu;

        foreach (var thuaDatCapNhat in thuaDatCapNhats)
        {
            var toBanDoCu = formatToBanDoCu
                .Replace(ThamSoThayThe.ToBanDo, thuaDatCapNhat.ToBanDo)
                .Replace(ThamSoThayThe.DonViHanhChinh, thuaDatCapNhat.TenDonViHanhChinh);
            var thuaDatCuInDb = await dataContext.ThuaDatCus.FindAsync(thuaDatCapNhat.MaThuaDat);
            if (thuaDatCuInDb == null)
            {
                dataContext.ThuaDatCus.Add(new ThuaDatCu()
                {
                    MaThuaDat = thuaDatCapNhat.MaThuaDat,
                    ToBanDoCu = toBanDoCu
                });
            }
            else
            {
                thuaDatCuInDb.ToBanDoCu = string.IsNullOrWhiteSpace(thuaDatCuInDb.ToBanDoCu)
                    ? toBanDoCu
                    : $"{thuaDatCuInDb.ToBanDoCu} - [{toBanDoCu}]";
                dataContext.ThuaDatCus.Update(thuaDatCuInDb);
            }
        }

        await dataContext.SaveChangesAsync();
    }

    /// <summary>
    /// Cập nhật mã Thửa Đất Cũ.
    /// </summary>
    /// <param name="dataContext">Ngữ cảnh dữ liệu Elis.</param>
    /// <param name="value">Giá trị mã Thửa Đất cũ.</param>
    /// <param name="newValue">Giá trị mã Thửa Đất mới.</param>
    public static async Task UpdateMaThuaDatCuAsync(this ElisDataContext dataContext, long value, long newValue)
    {
        var thuaDatCu = await dataContext.ThuaDatCus.FindAsync(value);
        if (thuaDatCu == null) return;
        thuaDatCu.MaThuaDat = newValue;
        dataContext.ThuaDatCus.Update(thuaDatCu);
        await dataContext.SaveChangesAsync();
    }
}