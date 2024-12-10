using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tools.ChuyenDvhc.Data.Entities;

[PrimaryKey("MaThuaDat")]
public class ThuaDat
{
    public int MaThuaDat { get; set; }
    public int MaToBanDo { get; set; }
    public required string ThuaDatSo { get; set; }
    public string? GhiChu { get; set; }
}