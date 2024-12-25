using System.Collections.ObjectModel;

namespace Haihv.Elis.Tool.DoiTenTepToBanDo.Models;

public sealed class FolderModel
{
    public required string Name { get; set; }
    public required string Path { get; set; }
    public ObservableCollection<FolderModel> SubFolders { get; set; } = [];
}