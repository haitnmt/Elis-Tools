using Haihv.Elis.Tool.ChuyenDvhc.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Haihv.Elis.Tool.ChuyenDvhc.Components;

public partial class SelectDvhc : ComponentBase
{
    [Inject] private IMemoryCache Cache { get; set; } = null!;
    [Parameter] public bool IsConnected { get; set; }
    [Parameter] public bool IsBefore { get; set; }
    [Parameter] public bool MultiCapXa { get; set; }
    [Parameter] public DvhcRecord? CapTinh { get; set; }
    [Parameter] public EventCallback<DvhcRecord?> CapTinhChanged { get; set; }
    [Parameter] public DvhcRecord? CapHuyen { get; set; }
    [Parameter] public EventCallback<DvhcRecord?> CapHuyenChanged { get; set; }
    [Parameter] public IEnumerable<DvhcRecord?>? CapXas { get; set; } = [];
    [Parameter] public EventCallback<IEnumerable<DvhcRecord?>?> CapXaChanged { get; set; }


    private string? _connectionString = string.Empty;

    private ElisDataContext _dataContext = null!;

    private IEnumerable<DvhcRecord> _capTinhs = [];
    private string _tenTinh = string.Empty;

    private IEnumerable<DvhcRecord> _capHuyens = [];
    private string _tenHuyen = string.Empty;

    private IEnumerable<DvhcRecord> _capXas = [];
    protected override void OnParametersSet()
    {
        if (!IsConnected || _capTinhs.Any()) return;
        _connectionString = Cache.Get<string>("ConnectionString");
        if (!string.IsNullOrWhiteSpace(_connectionString))
        {
            _dataContext = new ElisDataContext(_connectionString);
        }
        if (CapHuyen is not null)
        {
            SetCapHuyen();
        }
    }

    private async Task<IEnumerable<DvhcRecord>> GetCapTinh(string? value, CancellationToken token)
    {
        if (_capTinhs.Any())
            return string.IsNullOrWhiteSpace(value)
                ? _capTinhs
                : _capTinhs.Where(x => x.Ten.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        var dvhcRecords = await Cache.GetOrCreateAsync("CapTinh", async _ =>
        {
            var dvhcs = await _dataContext.Dvhcs
                .Where(d => d.MaHuyen == 0 && d.MaXa == 0)
                .OrderBy(d => d.MaTinh).ToListAsync(cancellationToken: token);
            return dvhcs.Select(d => new DvhcRecord(d.MaDvhc, d.MaTinh, d.Ten));
        });
        _capTinhs = dvhcRecords ?? [];
        return string.IsNullOrWhiteSpace(value)
                ? _capTinhs
                : _capTinhs.Where(x => x.Ten.Contains(value, StringComparison.InvariantCultureIgnoreCase));
    }

    private async Task<IEnumerable<DvhcRecord>> GetCapHuyen(string? value, CancellationToken token)
    {
        if (CapTinh is not { Ma: > 0 }) return [];
        if (_capHuyens.Any())
            return string.IsNullOrWhiteSpace(value)
                ? _capHuyens
                : _capHuyens.Where(x => x.Ten.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        var dvhcRecords = await Cache.GetOrCreateAsync($"CapHuyen:{CapTinh.Ma}", async _ =>
        {
            var dvhcs = await _dataContext.Dvhcs
                .Where(d => d.MaTinh == CapTinh.Ma && d.MaXa == 0 && d.MaHuyen != 0)
                .OrderBy(d => d.MaTinh).ToListAsync(cancellationToken: token);
            return dvhcs.Select(d => new DvhcRecord(d.MaDvhc, d.MaHuyen, d.Ten));
        });

        _capHuyens = dvhcRecords ?? [];
        return string.IsNullOrWhiteSpace(value) ?
            _capHuyens :
            _capHuyens.Where(x => x.Ten.Contains(value, StringComparison.InvariantCultureIgnoreCase));
    }
    private async Task GetCapXa()
    {
        if (CapHuyen is not { Ma: > 0 }) return;

        var dvhcRecords = await Cache.GetOrCreateAsync($"CapXa:{CapHuyen.Ma}", async _ =>
        {
            var dvhcs = await _dataContext.Dvhcs
                .Where(d => d.MaHuyen == CapHuyen.Ma && d.MaXa != 0)
                .OrderBy(d => d.MaTinh).ToListAsync();
            return dvhcs.Select(d => new DvhcRecord(d.MaDvhc, d.MaXa, d.Ten));
        });

        _capXas = dvhcRecords ?? [];
    }

    private static string? DvhcToString(DvhcRecord? dvhcRecord)
    {
        return dvhcRecord?.Ten;
    }
    private void SetCapTinh()
    {
        if (CapTinh is not { Ma: > 0 } || CapTinh.Ten == _tenTinh) return;
        _capHuyens = [];
        CapHuyen = null;
        CapXas = [];
        _tenTinh = CapTinh.Ten;
        CapTinhChanged.InvokeAsync(CapTinh);
    }
    private void SetCapHuyen()
    {
        if (CapHuyen is not { Ma: > 0 } || CapHuyen.Ten == _tenHuyen) return;
        CapXas = [];
        _tenHuyen = CapHuyen.Ten;
        CapHuyenChanged.InvokeAsync(CapHuyen);
        _ = GetCapXa();
    }
    private static string? CapXaToString(DvhcRecord? dvhcRecord)
    {
        return dvhcRecord?.Ten;
    }

    private void SetCapXa(IEnumerable<DvhcRecord?>? dvhcRecords)
    {
        CapXas = dvhcRecords;
        CapXaChanged.InvokeAsync(CapXas);
    }
}

public sealed record DvhcRecord(int MaDvhc, int Ma, string Ten);
