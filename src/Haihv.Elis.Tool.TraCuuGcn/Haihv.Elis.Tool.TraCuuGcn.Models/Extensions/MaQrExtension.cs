namespace Haihv.Elis.Tool.TraCuuGcn.Models.Extensions;

public static class MaQrExtension
{
    public static MaQrInfo ToMaQr(this string maQr)
    {
        var qrParts = maQr.Split('|');
        if (qrParts.Length != 8)
        {
            throw new ArgumentException("Mã QR không hợp lệ");
        }

        // Khởi tạo ThoiGianKhoiTao
        if (!DateTime.TryParseExact(qrParts[0], "dd-MM-yyyy-HH-mm-ss", null, System.Globalization.DateTimeStyles.None,
                out var thoiGianKhoiTao))
        {
            throw new ArgumentException("Mã QR không hợp lệ");
        }
        
        // Khởi tạo các thuộc tính khác
        var maDonVi = qrParts[1];
        var tenPhanMem = qrParts[2];
        var maHoSoTthc = qrParts[3];
        var serialNumber = qrParts[4];
        var maGcn = qrParts[5];

        // Khởi tạo ThoiGianChinhSua
        if (!DateTime.TryParseExact(qrParts[6], "dd-MM-yyyy-HH-mm-ss", null, System.Globalization.DateTimeStyles.None,
                out var thoiGianChinhSua))
        {
            throw new ArgumentException("Mã QR không hợp lệ");
        }
        // Khởi tạo SecurityCode
        if (!int.TryParse(qrParts[7], out var securityCode))
        {
            throw new ArgumentException("Mã QR không hợp lệ");
        }

        return new MaQrInfo
        {
            ThoiGianKhoiTao = thoiGianKhoiTao,
            ThoiGianChinhSua = thoiGianChinhSua,
            MaDonVi = maDonVi,
            TenPhanMem = tenPhanMem,
            MaHoSoTthc = maHoSoTthc,
            SerialNumber = serialNumber,
            MaGcn = maGcn,
            SecurityCode = securityCode
        };
    }
}