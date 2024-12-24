using Haihv.Elis.Tool.ChuyenDvhc.Data;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Entities;
using Haihv.Elis.Tool.ChuyenDvhc.Data.Extensions;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Caching.Memory;
using MudBlazor;
using Color = MudBlazor.Color;

namespace Haihv.Elis.Tool.ChuyenDvhc.Components;

public partial class ProcessingDataTransfer
{
    [Inject] private IMemoryCache MemoryCache { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Parameter] public bool IsAuditEnabled { get; set; }
    [Parameter] public EventCallback<bool> IsFinishedChanged { get; set; }

    private ElisDataContext _dataContext = null!;
    private string? _connectionString;
    private bool _isFinished;
    private List<ThamChieuToBanDo> _thamChieuToBanDos = [];
    private string _toBanDoCu = string.Empty;
    private string _ghiChuToBanDo = string.Empty;
    private string _ghiChuThuaDat = string.Empty;
    private string _ghiChuGiayChungNhan = string.Empty;
    private string _ngaySapNhap = string.Empty;
    private int _limit = 100;

    private List<DvhcRecord?>? _capXaTruoc;
    private List<int> _maDvhcBiSapNhap = [];
    private DvhcRecord? _capXaSau;

    protected override void OnInitialized()
    {
        _connectionString = MemoryCache.Get<string>(CacheDataConnection.ConnectionString);
        if (!string.IsNullOrWhiteSpace(_connectionString))
        {
            _dataContext = new ElisDataContext(_connectionString);
            _thamChieuToBanDos = MemoryCache.Get<List<ThamChieuToBanDo>>(CacheThamSoBanDo.ThamChieuToBanDo) ?? [];
            if (_thamChieuToBanDos.Count == 0)
            {
                SetMessage("Dữ liệu tham chiếu tờ bản đồ không tồn tại.");
                return;
            }

            _capXaTruoc = MemoryCache.Get<List<DvhcRecord?>?>(CacheThamSoDvhc.CapXaTruoc) ?? null;
            if (_capXaTruoc == null || _capXaTruoc.Count == 0)
            {
                SetMessage("Dữ liệu đơn vị hành chính cấp xã trước không tồn tại.");
                return;
            }

            _capXaSau = MemoryCache.Get<DvhcRecord?>(CacheThamSoDvhc.CapXaSau) ?? null;
            if (_capXaSau == null)
            {
                SetMessage("Dữ liệu đơn vị hành chính cấp xã sau không tồn tại.");
                return;
            }

            _maDvhcBiSapNhap = _capXaTruoc?.Where(x => x != null)
                .Select(x => x!.MaDvhc)
                .ToList() ?? [];
            _maDvhcBiSapNhap.Remove(_capXaSau?.MaDvhc ?? 0);
            _toBanDoCu = MemoryCache.Get<string>(CacheThamSoDuLieu.ToBanDoCu) ??
                         ThamSoThayThe.DefaultToBanDoCu;
            _ghiChuToBanDo = MemoryCache.Get<string>(CacheThamSoDuLieu.GhiChuToBanDo) ??
                             ThamSoThayThe.DefaultGhiChuToBanDo;
            _ghiChuThuaDat = MemoryCache.Get<string>(CacheThamSoDuLieu.GhiChuThuaDat) ??
                             ThamSoThayThe.DefaultGhiChuThuaDat;
            _ghiChuGiayChungNhan = MemoryCache.Get<string>(CacheThamSoDuLieu.GhiChuGiayChungNhan) ??
                                   ThamSoThayThe.DefaultGhiChuGiayChungNhan;
            _ngaySapNhap = MemoryCache.TryGetValue(CacheThamSoDvhc.NgaySatNhap, out DateTime ngaySapNhap)
                ? ngaySapNhap.ToString(ThamSoThayThe.DinhDangNgaySapNhap)
                : DateTime.Now.ToString(ThamSoThayThe.DinhDangNgaySapNhap);
        }
        else
        {
            SetMessage("Kết nối cơ sở dữ liệu không tồn tại.");
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_isFinished) return;
        await StartProcessing();
    }

    private async Task StartProcessing()
    {
        // Tạo hoặc thay đổi bảng audit nếu được kích hoạt
        await CreateAuditTable();

        if (_isCompletedKhoiTaoDuLieu || !IsAuditEnabled)
        {
            if (_maDvhcBiSapNhap.Count == 0)
            {
                _messageSuccessThamChieuThuaDat = "Không có đơn vị hành chính nào được sáp nhập.";
                _messageUpdateToBanDoSuccess = "Không có đơn vị hành chính nào được sáp nhập.";
                _colorThamChieuThuaDat = Color.Success;
                _colorUpdateToBanDo = Color.Success;
                _timeThamChieuThuaDat = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                _timeUpdateToBanDo = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                _isCompletedThamChieuThuaDat = true;
                _isCompletedUpdateToBanDo = true;
                SetMessage("Không có đơn vị hành chính nào được sáp nhập.", Severity.Info, false);
                // Cập nhật dữ liệu Đơn vị hành chính
                await UpdatingDonViHanhChinh();
            }
            else
            {
                // Cập nhật dữ liệu Thửa đất lịch sử
                await CreateThamChieuThuaDat();
            }
        }
    }

    /// <summary>
    /// Đánh dấu quá trình xử lý đã hoàn thành và kích hoạt sự kiện thay đổi trạng thái.
    /// </summary>
    private void SetMessage(string message = "Có lỗi xảy ra", Severity severity = Severity.Error,
        bool isFinished = true)
    {
        Snackbar.Add(message, severity);
        if (!isFinished) return;
        _isFinished = true;
        IsFinishedChanged.InvokeAsync(_isFinished);
    }

    private string _timeKhoiTaoDuLieu = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    private Color _colorKhoiTaoDuLieu = Color.Default;
    private bool _isCompletedKhoiTaoDuLieu = false;
    private bool _isProcessingKhoiTaoDuLieu = false;
    private string? _errorKhoiTaoDuLieu = string.Empty;

    /// <summary>
    /// Tạo hoặc thay đổi bảng audit nếu được kích hoạt.
    /// </summary>
    /// <returns>Task đại diện cho thao tác không đồng bộ.</returns>
    private async Task CreateAuditTable()
    {
        // Kiểm tra nếu audit được kích hoạt, _dataContext không null, và quá trình chưa hoàn thành hoặc đang xử lý
        if (!IsAuditEnabled || _isCompletedKhoiTaoDuLieu || _isProcessingKhoiTaoDuLieu)
            return;
        _isProcessingKhoiTaoDuLieu = true;
        _colorKhoiTaoDuLieu = Color.Primary;
        StateHasChanged();
        try
        {
            // Tạo hoặc thay đổi bảng audit
            await _dataContext.CreatedOrAlterAuditTable();
            _colorKhoiTaoDuLieu = Color.Success;
        }
        catch (Exception ex)
        {
            // Xử lý lỗi nếu có ngoại lệ
            _errorKhoiTaoDuLieu = ex.Message;
            _colorKhoiTaoDuLieu = Color.Error;
            SetMessage("Không thể tạo bảng audit.");
        }
        finally
        {
            // Cập nhật thời gian, trạng thái hoàn thành và trạng thái xử lý
            _timeKhoiTaoDuLieu = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            _isCompletedKhoiTaoDuLieu = true;
            StateHasChanged();
        }
    }

    private Color _colorThamChieuThuaDat = Color.Default;
    private string _timeThamChieuThuaDat = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    private bool _isCompletedThamChieuThuaDat = false;
    private bool _isProcessingThamChieuThuaDat = false;
    private string _messageSuccessThamChieuThuaDat = "Đã hoàn thành";
    private string? _errorThamChieuThuaDat = string.Empty;
    private long _totalThamChieuThuaDat;
    private long _currentThamChieuThuaDat;
    private long _bufferThamChieuThuaDat;

    private async Task CreateThamChieuThuaDat()
    {
        if (_isProcessingThamChieuThuaDat || _isCompletedThamChieuThuaDat || _capXaTruoc == null ||
            _capXaTruoc.Count == 0 || _capXaSau == null)
            return;
        _colorThamChieuThuaDat = Color.Primary;
        _isProcessingThamChieuThuaDat = true;
        StateHasChanged();
        try
        {
            // Lấy tổng số thửa đất
            _totalThamChieuThuaDat = await _dataContext.GetCountThuaDatAsync(_maDvhcBiSapNhap);
            if (_totalThamChieuThuaDat == 0)
            {
                const string message = "Không có thửa đất nào được tìm thấy.";
                _messageUpdateToBanDoSuccess = message;
                _colorThamChieuThuaDat = Color.Success;
                _colorUpdateToBanDo = Color.Success;
                _timeThamChieuThuaDat = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                _timeUpdateToBanDo = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                _isCompletedThamChieuThuaDat = true;
                _isCompletedUpdateToBanDo = true;
                SetMessage(message, Severity.Info, false);
                // Cập nhật dữ liệu Đơn vị hành chính
                await UpdatingDonViHanhChinh();
                return;
            }

            _currentThamChieuThuaDat = 0;
            _bufferThamChieuThuaDat = Math.Min(_limit, _totalThamChieuThuaDat);
            StateHasChanged();

            foreach (var dvhcBiSapNhap in _capXaTruoc.Where(x => x != null && _maDvhcBiSapNhap.Contains(x.MaDvhc)))
            {
                if (dvhcBiSapNhap == null)
                    continue;
                var minMaThuaDat = long.MinValue;
                while (true)
                {
                    // Lấy danh sách Thửa Đất cần cập nhật và cập nhật ghi chú Thửa Đất
                    var thuaDatToBanDos =
                        await _dataContext.UpdateAndGetThuaDatToBanDoAsync(dvhcBiSapNhap,
                            minMaThuaDat: minMaThuaDat,
                            limit: _limit,
                            formatGhiChuThuaDat: _ghiChuThuaDat,
                            ngaySapNhap: _ngaySapNhap);
                    if (thuaDatToBanDos.Count == 0)
                        break;

                    // Tạo hoặc cập nhật thông tin Thửa Đất Cũ
                    await _dataContext.CreateOrUpdateThuaDatCuAsync(thuaDatToBanDos, _toBanDoCu);

                    // Cập nhật Ghi chú Giấy chứng nhận
                    await _dataContext.UpdateGhiChuGiayChungNhan(thuaDatToBanDos, _ghiChuGiayChungNhan, _ngaySapNhap);

                    minMaThuaDat = thuaDatToBanDos[^1].MaThuaDat;
                    _currentThamChieuThuaDat += thuaDatToBanDos.Count;
                    _bufferThamChieuThuaDat = _currentThamChieuThuaDat +
                                              Math.Min(_limit, _totalThamChieuThuaDat - _currentThamChieuThuaDat);
                    StateHasChanged();
                }
            }

            _colorThamChieuThuaDat = Color.Success;
        }
        catch (Exception ex)
        {
            _errorThamChieuThuaDat = ex.Message;
            _colorThamChieuThuaDat = Color.Error;
            SetMessage("Không thể tạo dữ liệu thửa đất lịch sử.");
        }
        finally
        {
            // Cập nhật thời gian, trạng thái hoàn thành và trạng thái xử lý
            _timeThamChieuThuaDat = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            _isCompletedThamChieuThuaDat = true;
            StateHasChanged();
        }

        // Nếu không có lỗi xảy ra thì cập nhật dữ liệu Tờ bản đồ
        if (string.IsNullOrWhiteSpace(_errorThamChieuThuaDat))
            // Cập nhật dữ liệu Tờ bản đồ
            await UpdatingToBanDo();
    }


    private Color _colorUpdateToBanDo = Color.Default;
    private string _timeUpdateToBanDo = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    private bool _isCompletedUpdateToBanDo = false;
    private bool _isProcessingUpdateToBanDo = false;
    private string _messageUpdateToBanDoSuccess = "Đã hoàn thành";
    private string? _errorUpdateToBanDo = string.Empty;

    private async Task UpdatingToBanDo()
    {
        if (_isProcessingUpdateToBanDo || _isCompletedUpdateToBanDo)
            return;
        _colorUpdateToBanDo = Color.Default;
        _isProcessingUpdateToBanDo = true;
        StateHasChanged();
        try
        {
            await _dataContext.UpdateToBanDoAsync(_thamChieuToBanDos, _ghiChuToBanDo, _ngaySapNhap);
            _colorUpdateToBanDo = Color.Success;
        }
        catch (Exception ex)
        {
            _errorUpdateToBanDo = ex.Message;
            _colorUpdateToBanDo = Color.Error;
            SetMessage("Không thể cập nhật dữ liệu tờ bản đồ.");
        }
        finally
        {
            _timeUpdateToBanDo = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            _isCompletedUpdateToBanDo = true;
            StateHasChanged();
        }

        // Nếu không có lỗi xảy ra thì cập nhật dữ liệu Đơn vị hành chính
        if (string.IsNullOrWhiteSpace(_errorUpdateToBanDo))
            // Cập nhật dữ liệu Đơn vị hành chính
            await UpdatingDonViHanhChinh();
    }


    private Color _colorUpdateDvhc = Color.Default;
    private string _timeUpdateDvhc = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    private bool _isCompletedUpdateDvhc = false;
    private bool _isProcessingUpdateDvhc = false;
    private string? _errorUpdateDvhc = string.Empty;

    private async Task UpdatingDonViHanhChinh()
    {
        if (_isProcessingUpdateDvhc || _isCompletedUpdateDvhc)
            return;
        _colorUpdateDvhc = Color.Default;
        _isProcessingUpdateDvhc = true;
        StateHasChanged();
        if (_capXaSau == null)
        {
            _colorUpdateDvhc = Color.Error;
            _errorUpdateDvhc = "Không tìm thấy đơn vị hành chính cấp xã sau.";
            SetMessage("Không thể cập nhật dữ liệu đơn vị hành chính.");
            return;
        }

        try
        {
            var tenCapXaMoi = MemoryCache.Get<string>(CacheThamSoDvhc.TenDvhcSau) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(tenCapXaMoi))
            {
                _capXaSau = _capXaSau with { Ten = tenCapXaMoi };
            }

            await _dataContext.UpdateDonViHanhChinhAsync(_capXaSau, _maDvhcBiSapNhap);
            _colorUpdateDvhc = Color.Success;
            SetMessage("Đã hoàn thành", Severity.Success);
        }
        catch (Exception ex)
        {
            _colorUpdateDvhc = Color.Error;
            _errorUpdateDvhc = ex.Message;
            SetMessage("Không thể cập nhật dữ liệu đơn vị hành chính.");
        }
        finally
        {
            _timeUpdateDvhc = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            _isCompletedUpdateDvhc = true;
            StateHasChanged();
        }
    }
}