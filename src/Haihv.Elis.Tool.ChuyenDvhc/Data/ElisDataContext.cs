using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data;

public class ElisDataContext(string connectionString) : DbContext
{
    public DbSet<ThuaDat> ThuaDats { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(connectionString);
    }
    
}