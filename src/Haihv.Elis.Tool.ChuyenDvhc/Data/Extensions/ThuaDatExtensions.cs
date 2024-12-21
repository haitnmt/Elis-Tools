using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;

public static class ThuaDatExtensions
{
    public static async Task<int> GetCountAsync(this ElisDataContext dbContext, List<long> toBanDos)
    {
        return await dbContext.ThuaDats.CountAsync(x => toBanDos.Contains(x.MaToBanDo));
    }

}