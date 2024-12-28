using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;

[Table("TS_ThuaDat_TaiSan")]
[PrimaryKey("idTD_TS")]
public class ThuaDatTaiSan
{
    [Column("idTD_TS", TypeName = "nvarchar(36)")]
    public string IdTdTs { get; set; } = string.Empty;

    [Column("idThuaDat", TypeName = "bigint")]
    public long IdThuaDat { get; set; }
}

[Table("TS_LichSu")]
[PrimaryKey("idBDTS")]
public class TaiSanLichSu
{
    [Column("idBDTS", TypeName = "nvarchar(36)")]
    public string IdBdTs { get; set; } = string.Empty;

    [Column("idThuaDat", TypeName = "bigint")]
    public long IdThuaDat { get; set; }
}