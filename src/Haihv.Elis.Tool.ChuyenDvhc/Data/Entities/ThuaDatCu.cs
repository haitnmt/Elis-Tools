using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;

[PrimaryKey("MaThuaDat")]
public sealed class ThuaDatCu
{
    [Column("MaThuaDat", TypeName = "bigint")]
    public long MaThuaDat { get; set; }
    
    [Column("ToBDCu", TypeName = "nvarchar(50)")]
    [MaxLength(50)]
    public string ToBanDoCu { get; set; } = string.Empty;
    
    [Column("ThuaDatCu", TypeName = "nvarchar(50)")]
    [MaxLength(50)]
    public string ThuaDatCuSo { get; set; } = string.Empty;
    
    [Column("DTCu", TypeName = "float")]
    public float DienTichCu { get; set; }    
    
    [Column("SoGCNCu", TypeName = "nvarchar(50)")]
    [MaxLength(50)]
    public string SoGcnCu { get; set; } = string.Empty;
    
    [Column("NgayCapCu", TypeName = "nvarchar(50)")]
    [MaxLength(50)]
    public string NgayCapCu { get; set; } = string.Empty;
    
    [Column("LoaiDatCu", TypeName = "nvarchar(50)")]
    [MaxLength(50)]
    public string LoaiDatCu { get; set; } = string.Empty;
    
    [Column("THSDCu", TypeName = "nvarchar(50)")]
    [MaxLength(50)]
    public string ThoiHanSuDungCu { get; set; } = string.Empty;
}