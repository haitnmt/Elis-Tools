using System;
using System.IO;

namespace Haihv.Elis.Tool.ChuyenDvhc;

public static class Settings
{
    public const string KeyConnectionString = "ConnectionString";

    public static string PathRootConfig()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ElisTool");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
#if WINDOWS
        return path + "\\";
#endif
        return path + "/";
    }
}