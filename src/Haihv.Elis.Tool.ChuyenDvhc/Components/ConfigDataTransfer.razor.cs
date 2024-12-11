using Haihv.Elis.Tool.ChuyenDvhc.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Haihv.Elis.Tool.ChuyenDvhc.Components;

public partial class ConfigDataTransfer
{
    [Parameter] public bool IsConnected { get; set; }

    [Inject] private IMemoryCache Cache { get; set; } = default!;
    private string? _connectionString = string.Empty;
    private ElisDataContext _dataContext = default!;
    private string _tenDvhcSau = string.Empty;
    private IEnumerable<CapTinh> _capTinhs = [];
    private CapTinh? _capTinh;

    private IEnumerable<CapHuyen> _capHuyens = [];
    private CapHuyen? _capHuyen;

    private IEnumerable<CapXa> _capXas = [];
    private CapXa? _capXa;
    
    protected override void OnParametersSet()
    {
        if (!IsConnected || _capTinhs.Any()) return;
        _connectionString = Cache.Get<string>("ConnectionString");
        if (!string.IsNullOrWhiteSpace(_connectionString))
        {
            _dataContext = new ElisDataContext(_connectionString);
        }
    }

    private async Task<IEnumerable<CapTinh>> GetCapTinh(string? value, CancellationToken token)
    {
        if (_capTinhs.Any())
            return string.IsNullOrWhiteSpace(value)
                ? _capTinhs
                : _capTinhs.Where(x => x.TenTinh.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        
        var dvhcs = await _dataContext.Dvhcs
            .Where(d => d.MaHuyen == 0 && d.MaXa == 0)
            .OrderBy(d => d.MaTinh)
            .ToListAsync(cancellationToken: token);
        _capTinhs = dvhcs.Select(d => new CapTinh()
        {
            MaTinh = d.MaTinh,
            TenTinh = d.Ten
        });
        return string.IsNullOrWhiteSpace(value) ? 
            _capTinhs : 
            _capTinhs.Where(x => x.TenTinh.Contains(value, StringComparison.InvariantCultureIgnoreCase));
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
    private async Task<IEnumerable<CapXa>> GetCapXa(string? value, CancellationToken token)
    {
        if (_capHuyen == null) return [];
        if (_capXas.Any())
            return string.IsNullOrWhiteSpace(value)
                ? _capXas
                : _capXas.Where(x => x.TenXa.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        
        var dvhcs = await _dataContext.Dvhcs
            .Where(d => d.MaHuyen == _capHuyen.MaHuyen && d.MaXa != 0)
            .OrderBy(d => d.MaXa)
            .ToListAsync(cancellationToken: token);
        _capXas = dvhcs.Select(d => new CapXa(d.MaDvhc, d.MaXa, d.Ten));
        return string.IsNullOrWhiteSpace(value) ? 
            _capXas : 
            _capXas.Where(x => x.TenXa.Contains(value, StringComparison.InvariantCultureIgnoreCase));
    }
    private static string? CapXaToString(CapXa? capXa)
    {
        return capXa?.TenXa;
    }
}

public class CapTinh
{
    public int MaTinh { get; init; }

    public required string TenTinh { get; init;}
};

public sealed record CapHuyen(int MaHuyen, string TenHuyen);

public sealed record CapXa(int MaDvhc, int MaXa, string TenXa);