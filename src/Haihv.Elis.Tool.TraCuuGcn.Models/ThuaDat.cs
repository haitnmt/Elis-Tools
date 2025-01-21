namespace Haihv.Elis.Tool.TraCuuGcn.Models;

public record ThuaDat(
    string ThuaDatSo,
    string ToBanDo,
    string DiaChi,
    string DienTich,
    string LoaiDat,
    string ThoiHan,
    string HinhThuc,
    string NguonGoc
);

public record ThuaDatPublic(
    string ThuaDatSo,
    string ToBanDo,
    string DiaChi,
    string DienTich
);