namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Settings;

public static class CacheSettings
{
    public const string ElisConnections = "ElisConnections";
    public static string ConnectionName(string serial) => $"ConnectionName:{serial.ChuanHoa()}";
    public static string KeyGiayChungNhan(string serial) => $"GCN:{serial.ChuanHoa()}";
    public static string KeyThuaDat(string serial) => $"ThuaDat:{serial.ChuanHoa()}";
    public static string KeyDiaChiByMaDvhc(int maDvhc) => $"DVHC:{maDvhc}";

    public static string KeyChuSuDung(string soDinhDanh, string serial)
        => $"ChuSuDung:{soDinhDanh.ChuanHoa()}:{serial.ChuanHoa()}";

    public static string KeyAuthentication(string soDinhDanh, string serial)
        => $"Authentication:{soDinhDanh.ChuanHoa()}:{serial.ChuanHoa()}";

    private static string ChuanHoa(this string input) => input.Trim().ToUpper();

    public static string KeyQuocTich(int maQuocTich) => $"QuocTich:{maQuocTich}";
}