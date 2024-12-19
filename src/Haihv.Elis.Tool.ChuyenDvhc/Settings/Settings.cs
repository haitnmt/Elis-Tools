namespace Haihv.Elis.Tool.ChuyenDvhc.Settings;

public static class FilePath
{
    public const string KeyConnectionString = "ConnectionString";

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