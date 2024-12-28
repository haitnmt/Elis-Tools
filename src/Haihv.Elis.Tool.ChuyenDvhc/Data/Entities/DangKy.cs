using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;

[Table("DangKyQSDD")]
[PrimaryKey("MaDangKy")]
public class DangKy
{
    [Column("MaDangKy", TypeName = "bigint")]
    public long MaDangKy { get; set; }

    [Column("MaThuaDat", TypeName = "bigint")]
    public long MaThuaDat { get; set; }

    [Column("MaGuid", TypeName = "nvarchar(36)")]
    public string MaGuid { get; set; } = string.Empty;
}

[Table("DangKyQSDDLS")]
[PrimaryKey("MaDangKyLS")]
public class DangKyLichSu
{
    [Column("MaDangKyLS", TypeName = "bigint")]
    public long MaDangKyLs { get; set; }

    [Column("MaThuaDatLS", TypeName = "bigint")]
    public long MaThuaDatLs { get; set; }

    [Column("MaGuid", TypeName = "nvarchar(36)")]
    public string MaGuid { get; set; } = string.Empty;
}

[Table("DangKy_TheChap")]
[PrimaryKey("Ma")]
public class DangKyTheChap
{
    [Column("Ma", TypeName = "bigint")] public long Ma { get; set; }

    [Column("MaDangKy", TypeName = "bigint")]
    public long MaDangKy { get; set; }
}

[Table("DangKy_GopVon")]
[PrimaryKey("Ma")]
public class DangKyGopVon
{
    [Column("Ma", TypeName = "bigint")] public long Ma { get; set; }

    [Column("MaDangKy", TypeName = "bigint")]
    public long MaDangKy { get; set; }
}