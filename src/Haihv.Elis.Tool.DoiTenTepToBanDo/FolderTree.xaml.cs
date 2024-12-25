using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Controls;
using Haihv.Elis.Tool.DoiTenTepToBanDo.Models;

namespace Haihv.Elis.Tool.DoiTenTepToBanDo;

public partial class FolderTree : UserControl
{
    public FolderTree()
    {
        InitializeComponent();
        var folders = GetRootFolders();
    }
    
    private ObservableCollection<FolderModel> GetRootFolders()
    {
        var rootFolders = new ObservableCollection<FolderModel>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType is not (DriveType.Fixed or DriveType.Network)) continue;
            var rootFolder = new FolderModel
            {
                Name = drive.VolumeLabel + " (" + drive.Name + ")",
                Path = drive.Name
            };
            rootFolder.SubFolders = GetSubFolders(rootFolder.Path);
            rootFolders.Add(rootFolder);
        }

        return rootFolders;
    }

    private ObservableCollection<FolderModel> GetSubFolders(string rootFolderPath)
    {
        var subFolders = new ObservableCollection<FolderModel>();
        try
        {
            foreach (var folderPath in Directory.GetDirectories(rootFolderPath))
            {
                var folder = new FolderModel
                {
                    Name = Path.GetFileName(folderPath),
                    Path = folderPath,
                };
                subFolders.Add(folder);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return subFolders;
    }
    
}