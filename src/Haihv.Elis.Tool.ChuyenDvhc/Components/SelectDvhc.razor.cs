using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Repositories;
using Microsoft.AspNetCore.Components;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Serilog.Core;

namespace Haihv.Elis.Tool.ChuyenDvhc.Components;

public partial class SelectDvhc : ComponentBase
{
    [Inject] private HybridCache HybridCache { get; set; } = null!;
    [Inject] private IMemoryCache MemoryCache { get; set; } = null!;
    [Inject] private Logger Logger { get; set; } = null!;
    [Parameter] public bool IsBefore { get; set; }
    [Parameter] public DvhcRecord? CapTinh { get; set; }
    [Parameter] public EventCallback<DvhcRecord?> CapTinhChanged { get; set; }
    [Parameter] public DvhcRecord? CapHuyen { get; set; }
    [Parameter] public EventCallback<DvhcRecord?> CapHuyenChanged { get; set; }
    [Parameter] public IEnumerable<DvhcRecord> CapXas { get; set; } = [];
    [Parameter] public EventCallback<IEnumerable<DvhcRecord>> CapXaChanged { get; set; }


    private string? _connectionString = string.Empty;
    
    private IEnumerable<DvhcRecord> _capTinhs = [];
    private string _tenTinh = string.Empty;

    private IEnumerable<DvhcRecord> _capHuyens = [];
    private string _tenHuyen = string.Empty;
    
    private IEnumerable<DvhcRecord> _capXas = [];

    protected override void OnInitialized()
    {
        _connectionString = MemoryCache.Get<string>(CacheDataConnection.ConnectionString);
        if (!string.IsNullOrWhiteSpace(_connectionString)) return;
        Logger.Error("Connection string is null or empty");
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!IsConnected || _capTinhs.Any()) return;
        if (CapHuyen == null || _tenHuyen == CapHuyen.Ten) return;
        _tenHuyen = CapHuyen.Ten;
        await GetCapXa();
    }

    private async Task<IEnumerable<DvhcRecord>> GetCapTinh(string? value, CancellationToken cancellationToken = default)
    {
        if (_capTinhs.Any())
            return string.IsNullOrWhiteSpace(value)
                ? _capTinhs
                : _capTinhs.Where(x => x.Ten.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        var donViHanhChinhRepository = new DonViHanhChinhRepository(Logger, _connectionString);
        var dvhcRecords =
            await HybridCache.GetOrCreateAsync("CapTinh", async cancel => 
                    await donViHanhChinhRepository.GetCapTinhAsync(cancel),
                cancellationToken: cancellationToken);

        _capTinhs = dvhcRecords;
        return string.IsNullOrWhiteSpace(value)
            ? _capTinhs
            : _capTinhs.Where(x => x.Ten.Contains(value, StringComparison.InvariantCultureIgnoreCase));
    }
    
    private async Task<IEnumerable<DvhcRecord>> GetCapHuyen(string? value,
        CancellationToken cancellationToken = default)
    {
        if (CapTinh is not { Ma: > 0 }) return [];
        if (_capHuyens.Any())
            return string.IsNullOrWhiteSpace(value)
                ? _capHuyens
                : _capHuyens.Where(x => x.Ten.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        var donViHanhChinhRepository = new DonViHanhChinhRepository(Logger, _connectionString);
        var dvhcRecords = 
            await HybridCache.GetOrCreateAsync($"CapHuyen:{CapTinh.Ma}", async cancel => 
                await donViHanhChinhRepository.GetCapHuyenAsync(CapTinh.Ma, cancel), 
                cancellationToken: cancellationToken);

        _capHuyens = dvhcRecords;
        return string.IsNullOrWhiteSpace(value)
            ? _capHuyens
            : _capHuyens.Where(x => x.Ten.Contains(value, StringComparison.InvariantCultureIgnoreCase));
    }

    private async Task GetCapXa(CancellationToken cancellationToken = default)
    {
        if (CapHuyen is not { Ma: > 0 }) return;
        var donViHanhChinhRepository = new DonViHanhChinhRepository(Logger, _connectionString);
        var dvhcRecords = 
            await HybridCache.GetOrCreateAsync($"CapXa:{CapHuyen.Ma}", async cancel =>
                await donViHanhChinhRepository.GetCapXaAsync(CapHuyen.Ma, cancel)
                , cancellationToken: cancellationToken);
        _capXas = dvhcRecords;
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
        _tenHuyen = CapHuyen.Ten;
        CapXas = [];
        CapHuyenChanged.InvokeAsync(CapHuyen);
        _ = GetCapXa();
    }

    private static string? CapXaToString(DvhcRecord? capXa)
    {
        return capXa?.Ten;
    }

    private void SetCapXa(IEnumerable<DvhcRecord?>? dvhcRecords)
    {
        CapXas = dvhcRecords?.Where(x => x != null).Select(x => x!).ToList() ?? [];
        CapXaChanged.InvokeAsync(CapXas);
    }
}