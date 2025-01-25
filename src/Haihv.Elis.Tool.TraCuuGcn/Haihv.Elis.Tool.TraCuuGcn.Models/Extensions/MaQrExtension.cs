namespace Haihv.Elis.Tool.TraCuuGcn.Models;

public static class MaQrExtension
{
    public static MaQrInfo ToMaQr(this string maQr)
    {
        var qrParts = maQr.Split('|');
        if (qrParts.Length != 8)
        {
            throw new ArgumentException("Invalid QR code format");
        }

        // Khởi tạo ThoiGianKhoiTao
        if (!DateTime.TryParse(qrParts[0], out var thoiGianKhoiTao))
        {
            throw new ArgumentException("Invalid ThoiGianKhoiTao format");
        }

        // Khởi tạo ThoiGianChinhSua
        if (!DateTime.TryParse(qrParts[1], out var thoiGianChinhSua))
        {
            throw new ArgumentException("Invalid ThoiGianChinhSua format");
        }

        // Khởi tạo các thuộc tính khác
        var maDonVi = qrParts[2];
        var tenPhanMem = qrParts[3];
        var maHoSoTthc = qrParts[4];
        var serialNumber = qrParts[5];
        var maGcn = qrParts[6];

        // Khởi tạo SecurityCode
        if (!int.TryParse(qrParts[7], out var securityCode))
        {
            throw new ArgumentException("Invalid SecurityCode format");
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