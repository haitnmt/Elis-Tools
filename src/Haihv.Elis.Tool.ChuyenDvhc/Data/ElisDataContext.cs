using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data;

public partial class ElisDataContext(string connectionString) : DbContext
{
    public DbSet<ThuaDat> ThuaDats { get; set; }
    public DbSet<Dvhc> Dvhcs { get; set; }
    
    public DbSet<ThuaDatCu> ThuaDatCus { get; set; }
    
    public DbSet<ToBanDo> ToBanDos { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(connectionString);
    }
    
}