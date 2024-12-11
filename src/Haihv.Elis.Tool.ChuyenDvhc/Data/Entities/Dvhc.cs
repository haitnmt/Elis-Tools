using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;

[Table("DVHC")]
[PrimaryKey("MaDvhc")]
public class Dvhc
{
    [Column("MaDVHC", TypeName = "int")]
    public int MaDvhc { get; set; }
    [Column("Ten", TypeName = "nvarchar(50")]
    [MaxLength(50)]
    public required string Ten { get; set; }
    [Column("MaTinh", TypeName = "int")]
    public int MaTinh { get; set; }
    [Column("MaHuyen", TypeName = "int")]
    public int MaHuyen { get; set; }
    [Column("MaXa", TypeName = "int")]
    public int MaXa { get; set; }
}