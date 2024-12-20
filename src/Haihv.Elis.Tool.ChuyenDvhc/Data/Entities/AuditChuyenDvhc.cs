using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;

[Table("AuditChuyenDvhc")]
public sealed class AuditData
{
    [Column("Id", TypeName = "uniqueidentifier")]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [Column("MaDvhcTruoc", TypeName = "int")]
    public int MaDvhcTruoc { get; set; }

    [Column("TenDvhcTruoc", TypeName = "nvarchar(255)")]
    [MaxLength(255)]
    public string TenDvhcTruoc { get; set; } = string.Empty;

    [Column("ToBanDoTruoc", TypeName = "nvarchar(10)")]
    [MaxLength(10)]
    public string ToBanDoTruoc { get; set; } = string.Empty;

    [Column("SoThuaDatTruoc", TypeName = "nvarchar(10)")]
    [MaxLength(10)]
    public string SoThuaDatTruoc { get; set; } = string.Empty;

    [Column("MaDvhcSau", TypeName = "int")]
    public int MaDvhcSau { get; set; }

    [Column("TenDvhcSau", TypeName = "nvarchar(255)")]
    [MaxLength(255)]
    public string TenDvhcSau { get; set; } = string.Empty;

    [Column("ToBanDoSau", TypeName = "nvarchar(10)")]
    [MaxLength(10)]
    public string ToBanDoSau { get; set; } = string.Empty;

    [Column("SoThuaDatSau", TypeName = "nvarchar(10)")]
    [MaxLength(10)]
    public string SoThuaDatSau { get; set; } = string.Empty;

    [Column("NgayChuyen", TypeName = "datetime")]
    public DateTime NgayChuyen { get; set; } = DateTime.Now;
}