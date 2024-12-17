namespace Haihv.Elis.Tool.ChuyenDvhc;

public static class Settings
{
    public const string KeyConnectionString = "ConnectionString";
    public static string PathConnectionString =>
        Path.Combine(PathRootConfig(), "ConnectionInfo.inf");

    private const string KeyConfigDataTransfer = "ConfigDataTransfer";

    public static string KeyConfigToBanDoCu => KeyConfigDataTransfer + "ToBanDoCu";
    public static string KeyConfigGhiChuToBanDo => KeyConfigDataTransfer + "GhiChuToBanDo";
    public static string KeyConfigGhiChuThuaDat => KeyConfigDataTransfer + "GhiChuThuaDat";
    public static string KeyConfigGhiChuGiayChungNhan => KeyConfigDataTransfer + "GhiChuGiayChungNhan";
    public static string KeyThamChieuToBanDo => KeyConfigDataTransfer + "ThamChieuToBanDo";

    private static string PathRootConfig()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ElisTool");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }
}