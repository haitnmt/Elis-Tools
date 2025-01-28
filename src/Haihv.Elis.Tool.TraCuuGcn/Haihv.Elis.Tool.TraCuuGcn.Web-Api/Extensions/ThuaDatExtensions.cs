using Haihv.Elis.Tool.TraCuuGcn.Models;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;

public static class ThuaDatExtensions
{
    public static ThuaDatPublic ConvertToThuaDatPublic(this ThuaDat thuaDat)
    {
        return new ThuaDatPublic(
            thuaDat.ThuaDatSo,
            thuaDat.ToBanDo,
            thuaDat.DiaChi,
            thuaDat.DienTich
        );
    }
}