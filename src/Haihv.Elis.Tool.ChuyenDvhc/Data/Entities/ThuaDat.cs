using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;

[PrimaryKey("MaThuaDat")]
public class ThuaDat
{
    public int MaThuaDat { get; set; }
    public int MaToBanDo { get; set; }
    public required string ThuaDatSo { get; set; }
    public string? GhiChu { get; set; }
}