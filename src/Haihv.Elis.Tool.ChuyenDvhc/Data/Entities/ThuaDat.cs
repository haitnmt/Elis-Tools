using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;

[Table("ThuaDat")]
[PrimaryKey("MaThuaDat")]
public sealed class ThuaDat
{
    [Column("MaThuaDat", TypeName = "bigint")]
    public long MaThuaDat { get; set; }
    [Column("MaToBanDo", TypeName = "bigint")]
    public long MaToBanDo { get; set; }
    [Column("ThuaDatSo", TypeName = "char(10)")]
    [MaxLength(10)]
    public required string ThuaDatSo { get; set; }
    [Column("GhiChu", TypeName = "nvarchar(2000)")]
    [MaxLength(2000)]
    public string? GhiChu { get; set; }
}