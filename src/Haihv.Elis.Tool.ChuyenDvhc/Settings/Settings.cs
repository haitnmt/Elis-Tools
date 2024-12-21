namespace Haihv.Elis.Tool.ChuyenDvhc.Settings;

public static class FilePath
{
    public static string PathConnectionString =>
        Path.Combine(PathRootConfig(), "ConnectionInfo.inf");

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

public static class ThamSoThayThe
{
    public static string ToBanDo => "{TBD}";
    public static string SoThua => "{TD}";
    public static string DonViHanhChinh => "{DVHC}";
    public static string NgaySapNhap => "{NSN}";
}