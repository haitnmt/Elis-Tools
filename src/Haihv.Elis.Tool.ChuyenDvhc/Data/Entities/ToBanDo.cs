using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;

[Table("ToBanDo")]
[PrimaryKey("MaToBanDo")]
public sealed class ToBanDo
{
    [Column("MaToBanDo", TypeName = "bigint")]
    public long MaToBanDo { get; set; }
    
    [Column("MaDVHC", TypeName = "int")]
    public int MaDvhc { get; set; }
    
    [Column("SoTo", TypeName = "nvarchar(50)")]
    [MaxLength(50)]
    public string SoTo { get; set; } = string.Empty;
    
    [Column("TyLe", TypeName = "int")]
    public int TyLe { get; set; }
    
    [Column("GhiChu", TypeName = "nvarchar(200)")]
    [MaxLength(200)]
    public string GhiChu { get; set; } = string.Empty;
}