using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;

[Table("CayLS")]
[PrimaryKey("MaCayLS")]
public sealed class CayLs
{
    [Column("MaCayLS", TypeName = "nvarchar(36)")]
    public required string MaCayLs { get; set; }

    [Column("MaDangKyHT", TypeName = "bigint")]
    public long MaDangKyHt { get; set; }

    [Column("MaDangKyLS", TypeName = "bigint")]
    public long MaDangKyLs { get; set; }
}