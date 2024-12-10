using Haihv.Elis.Tools.ChuyenDvhc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tools.ChuyenDvhc.Data;

public class ElisDataContext(string connectionString) : DbContext
{
    public DbSet<ThuaDat> ThuaDats { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(connectionString);
    }
    
}