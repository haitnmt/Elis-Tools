namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Settings;

public static class CacheSettings
{
    public const string ElisConnections = "ElisConnections";
    public static string ConnectionName(string serial) => $"ConnectionName:{serial}";
    public static string KeyGiayChungNhan(string serial) => $"GCN:{serial}";
    public static string KeyThuaDat(string serial) => $"ThuaDat:{serial}";
    public static string KeyDiaChiByMaDvhc(int maDvhc) => $"DVHC:{maDvhc}";
    
    public static string KeyChuSuDung(string soDinhDanh, string serial) => $"ChuSuDung:{soDinhDanh}:{serial}";
}