using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;

[Table("GCNQSDD")]
[PrimaryKey("MaGCN")]
public class GiayChungNhan
{
    [Column("MaGCN", TypeName = "bigint")] public long MaGcn { get; set; }

    [Column("MaDangKy", TypeName = "bigint")]
    public long MaDangKy { get; set; }

    [Column("MaGuid", TypeName = "nvarchar(36)")]
    public string MaGuid { get; set; } = string.Empty;
}

[Table("GCNQSDDLS")]
[PrimaryKey("MaGCNLS")]
public class GiayChungNhanLichSu
{
    [Column("MaGCNLS", TypeName = "bigint")]
    public long MaGcnLs { get; set; }

    [Column("MaDangKy", TypeName = "bigint")]
    public long MaDangKy { get; set; }

    [Column("MaGuid", TypeName = "nvarchar(36)")]
    public string MaGuid { get; set; } = string.Empty;
}

[Table("DangKyMDSDD")]
[PrimaryKey("MaDKMDSDD")]
public class DangKyMdsdd
{
    [Column("MaDKMDSDD", TypeName = "bigint")]
    public long MaDkMdsdd { get; set; }

    [Column("MaGCN", TypeName = "bigint")] public long MaGcn { get; set; }
}

[Table("DangKyMDSDDLS")]
[PrimaryKey("MaDKMDSDDLS")]
public class DangKyMdsddLichSu
{
    [Column("MaDKMDSDDLS", TypeName = "bigint")]
    public long MaDkMdsddLs { get; set; }

    [Column("MaGCNLS", TypeName = "bigint")]
    public long MaGcnLs { get; set; }
}

[Table("DangKyNGSDD")]
[PrimaryKey("MaDKNGSDD")]
public class DangKyNgsdd
{
    [Column("MaDKNGSDD", TypeName = "bigint")]
    public long MaDkNgsdd { get; set; }

    [Column("MaGCN", TypeName = "bigint")] public long MaGcn { get; set; }
}

[Table("DangKyNGSDDLS")]
[PrimaryKey("MaDKNGSDDLS")]
public class DangKyNgsddLichSu
{
    [Column("MaDKNGSDDLS", TypeName = "bigint")]
    public long MaDkNgsddLs { get; set; }

    [Column("MaGCNLS", TypeName = "bigint")]
    public long MaGcnLs { get; set; }
}