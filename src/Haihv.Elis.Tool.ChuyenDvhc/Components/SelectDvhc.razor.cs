using Haihv.Elis.Tool.ChuyenDvhc.Data;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Haihv.Elis.Tool.ChuyenDvhc.Components;

public partial class SelectDvhc : ComponentBase
{
    [Parameter] public bool IsConnected { get; set; }

    [Inject] private IMemoryCache Cache { get; set; } = default!;
    private string? _connectionString = string.Empty;
    private ElisDataContext _dataContext = default!;
    private IEnumerable<DvhcRecord>? _capTinhs = [];
    private DvhcRecord? _capTinh;

    private IEnumerable<DvhcRecord> _capHuyens = [];
    private DvhcRecord? _capHuyen;

    private IEnumerable<DvhcRecord> _capXas = [];
    private DvhcRecord? _capXa;
    
    protected override void OnParametersSet()
    {
        if (!IsConnected || _capTinhs.Any()) return;
        _connectionString = Cache.Get<string>("ConnectionString");
        if (!string.IsNullOrWhiteSpace(_connectionString))
        {
            _dataContext = new ElisDataContext(_connectionString);
        }
    }

    private async Task<IEnumerable<DvhcRecord>?> GetCapTinh(string? value, CancellationToken token)
    {
        if (_capTinhs != null &&  _capTinhs.Any())
            return string.IsNullOrWhiteSpace(value)
                ? _capTinhs
                : _capTinhs.Where(x => x.Ten.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        _capTinhs = await Cache.GetOrCreateAsync("CapTinh", async entry =>
        {
            var dvhcs = await _dataContext.Dvhcs
                .Where(d => d.MaHuyen == 0 && d.MaXa == 0)
                .OrderBy(d => d.MaTinh).ToListAsync(cancellationToken: token);
            return dvhcs.Select(d => new DvhcRecord(d.MaTinh, d.Ten));
        });
        var dvhcRecords = _capTinhs.ToList();
        return string.IsNullOrWhiteSpace(value) && _capTinhs != null && dvhcRecords.Any()
                ? dvhcRecords
                : dvhcRecords.Where(x => x.Ten.Contains(value, StringComparison.InvariantCultureIgnoreCase));
    }

    private static string? CapTinhToString(CapTinh? capTinh)
    {
        return capTinh?.TenTinh;
    }
    
    private async Task<IEnumerable<CapHuyen>> GetCapHuyen(string? value, CancellationToken token)
    {
        if (_capTinh == null) return [];
        if (_capHuyens.Any())
            return string.IsNullOrWhiteSpace(value)
                ? _capHuyens
                : _capHuyens.Where(x => x.TenHuyen.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        
        var dvhcs = await _dataContext.Dvhcs
            .Where(d => d.MaTinh == _capTinh.MaTinh && d.MaXa == 0 && d.MaHuyen != 0)
            .OrderBy(d => d.MaHuyen)
            .ToListAsync(cancellationToken: token);
        _capHuyens = dvhcs.Select(d => new CapHuyen(d.MaHuyen, d.Ten));
        return string.IsNullOrWhiteSpace(value) ? 
            _capHuyens : 
            _capHuyens.Where(x => x.TenHuyen.Contains(value, StringComparison.InvariantCultureIgnoreCase));
    }
    private static string? CapHuyenToString(CapHuyen? capHuyen)
    {
        return capHuyen?.TenHuyen;
    }
    private async Task<IEnumerable<DvhcRecord>> GetCapXa(string? value, CancellationToken token)
    {
        if (_capHuyen == null) return [];
        if (_capXas.Any())
            return string.IsNullOrWhiteSpace(value)
                ? _capXas
                : _capXas.Where(x => x.TenXa.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        
        var dvhcs = await _dataContext.Dvhcs
            .Where(d => d.MaHuyen == _capHuyen.MaHuyen && d.MaXa != 0).SetOrGetDvhc(token)
        return string.IsNullOrWhiteSpace(value) ? 
            _capXas : 
            _capXas.Where(x => x.TenXa.Contains(value, StringComparison.InvariantCultureIgnoreCase));
    }
    
    private static string? DvhcToString(DvhcRecord? dvhcRecord)
    {
        return dvhcRecord?.Ten;
    }
    
    
}

public sealed record DvhcRecord(int Ma, string Ten);