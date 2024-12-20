using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data;

public sealed class ElisDataContext(string connectionString) : DbContext
{
    public DbSet<ThuaDat> ThuaDats { get; set; } = default!;
    public DbSet<Dvhc> Dvhcs { get; set; } = default!;

    public DbSet<ThuaDatCu> ThuaDatCus { get; set; } = default!;

    public DbSet<ToBanDo> ToBanDos { get; set; } = default!;

    public DbSet<AuditData> AuditDatas { get; set; } = default!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(connectionString);
    }
}