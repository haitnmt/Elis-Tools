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
    
    private ElisDataContext _dbContext = null!;
    private string? _connectionString;
    private bool _isFinished;
    private List<ThamChieuToBanDo> _thamChieuToBanDos = [];
    private string _toBanDoCu = string.Empty;
    private string _ghiChuToBanDo = string.Empty;
    private string _ghiChuThuaDat = string.Empty;
    private string _ghiChuGiayChungNhan = string.Empty;
    private List<int> _maDvhcBiSapNhap = [];
    private int _totalThuaDat;
    protected override void OnInitialized()
    {
        _connectionString = MemoryCache.Get<string>(CacheDataConnection.ConnectionString);
        if (!string.IsNullOrWhiteSpace(_connectionString))
        {
            _dbContext = new ElisDataContext(_connectionString);
            _thamChieuToBanDos = MemoryCache.Get<List<ThamChieuToBanDo>>(CacheThamSoBanDo.ThamChieuToBanDo) ?? [];
            if (_thamChieuToBanDos.Count == 0)
            {
                SetMessage("Dữ liệu tham chiếu tờ bản đồ không tồn tại.");
                return;
            }
            var capXaTruoc = MemoryCache.Get<List<DvhcRecord?>?>(CacheThamSoDvhc.CapXaTruoc) ?? null;
            var capXaSau = MemoryCache.Get<DvhcRecord?>(CacheThamSoDvhc.CapXaSau) ?? null;
            _maDvhcBiSapNhap = capXaTruoc?.Where(x => x != null).Select(x => x!.MaDvhc).ToList() ?? [];
            _maDvhcBiSapNhap.Remove(capXaSau?.MaDvhc ?? 0);
            _toBanDoCu = MemoryCache.Get<string>(CacheThamSoDuLieu.ToBanDoCu) ?? ThamSoThayThe.DefaultToBanDoCu;
            _ghiChuToBanDo = MemoryCache.Get<string>(CacheThamSoDuLieu.GhiChuToBanDo) ?? ThamSoThayThe.DefaultGhiChuToBanDo;
            _ghiChuThuaDat = MemoryCache.Get<string>(CacheThamSoDuLieu.GhiChuThuaDat) ?? ThamSoThayThe.DefaultGhiChuThuaDat;
            _ghiChuGiayChungNhan = MemoryCache.Get<string>(CacheThamSoDuLieu.GhiChuGiayChungNhan) ?? ThamSoThayThe.DefaultGhiChuGiayChungNhan;
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
                _messageThuaDatLichSuSuccess = "Không có đơn vị hành chính nào được sáp nhập.";
                _messageUpdateGiayChungNhanSuccess = "Không có đơn vị hành chính nào được sáp nhập.";
                _messageUpdateThuaDatSuccess = "Không có đơn vị hành chính nào được sáp nhập.";
                _messageUpdateToBanDoSuccess = "Không có đơn vị hành chính nào được sáp nhập.";
                _colorThuaDatLichSu = Color.Success;
                _colorUpdateGiayChungNhan = Color.Success;
                _colorUpdateThuaDat = Color.Success;
                _colorUpdateToBanDo = Color.Success;
                _timeThuaDatLichSu = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                _timeUpdateGiayChungNhan = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                _timeUpdateThuaDat = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                _timeUpdateToBanDo = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                _isCompletedThuaDatLichSu = true;
                _isCompletedUpdateGiayChungNhan = true;
                _isCompletedUpdateThuaDat = true;
                _isCompletedUpdateToBanDo = true;
                SetMessage("Không có đơn vị hành chính nào được sáp nhập.", Severity.Info, false);
            }
            else
            {
                // Cập nhật dữ liệu Thửa đất lịch sử
                await CreateThuaDatLichSu();

                // Cập nhật dữ liệu Giấy chứng nhận
                await UpdatingGiayChungNhan();

                // Cập nhật dữ liệu Thửa đất
                await UpdatingThuaDat();

                // Cập nhật dữ liệu Tờ bản đồ
                await UpdatingToBanDo();
            }
            
            // Cập nhật dữ liệu Đơn vị hành chính
            await UpdatingDonViHanhChinh();
        }
    }
    
    /// <summary>
    /// Đánh dấu quá trình xử lý đã hoàn thành và kích hoạt sự kiện thay đổi trạng thái.
    /// </summary>
    private void SetMessage(string message = "Có lỗi xảy ra", Severity severity = Severity.Error, bool isFinished = true)
    {
        Snackbar.Add(message, severity, options =>
        {
            
        });
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
        // Kiểm tra nếu audit được kích hoạt, _dbContext không null, và quá trình chưa hoàn thành hoặc đang xử lý
        if (IsAuditEnabled && !_isCompletedKhoiTaoDuLieu && !_isProcessingKhoiTaoDuLieu)
        {
            _isProcessingKhoiTaoDuLieu = true;
            _colorKhoiTaoDuLieu = Color.Primary;
            StateHasChanged();
            try
            {
                // Tạo hoặc thay đổi bảng audit
                await _dbContext.CreatedOrAlterAuditTable();
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
    }
    
    private Color _colorThuaDatLichSu= Color.Default;
    private string _timeThuaDatLichSu = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    private bool _isCompletedThuaDatLichSu= false;
    private bool _isProcessingThuaDatLichSu= false;
    private string _messageThuaDatLichSuSuccess = "Đã hoàn thành";
    private string? _errorThuaDatLichSu= string.Empty;
    private long _totalThuaDatLichSu;
    private long _currentThuaDatLichSu;
    
    private async Task CreateThuaDatLichSu()
    {
        if(!_isProcessingThuaDatLichSu && !_isCompletedThuaDatLichSu)
        {       
            _colorThuaDatLichSu = Color.Primary;
            _isProcessingThuaDatLichSu = true;
            StateHasChanged();
            try
            {
                // Lấy tổng số thửa đất
                _totalThuaDat = await _dbContext.GetCountThuaDatAsync(_maDvhcBiSapNhap);
                if (_totalThuaDat == 0)
                {
                    const string message = "Không có thửa đất nào được tìm thấy.";
                    _messageUpdateGiayChungNhanSuccess = message;
                    _messageUpdateThuaDatSuccess = message;
                    _messageUpdateToBanDoSuccess = message;
                    _colorThuaDatLichSu = Color.Success;
                    _colorUpdateGiayChungNhan = Color.Success;
                    _colorUpdateThuaDat = Color.Success;
                    _colorUpdateToBanDo = Color.Success;
                    _timeThuaDatLichSu = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                    _timeUpdateGiayChungNhan = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                    _timeUpdateThuaDat = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                    _timeUpdateToBanDo = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                    _isCompletedThuaDatLichSu = true;
                    _isCompletedUpdateGiayChungNhan = true;
                    _isCompletedUpdateThuaDat = true;
                    _isCompletedUpdateToBanDo = true;
                    SetMessage(message, Severity.Info, false);
                    return;
                }
                _totalThuaDatLichSu = _totalThuaDat;
                _currentThuaDatLichSu = 0;
                StateHasChanged();

                // await foreach (var thuaDatLichSu in _dbContext.ThuaDatCus.AsAsyncEnumerable())
                // {
                //     _currentThuaDatLichSu++;
                //     StateHasChanged();
                // }
            }
            catch (Exception ex)
            {
                _errorThuaDatLichSu = ex.Message;
                _colorThuaDatLichSu = Color.Error;
                SetMessage("Không thể tạo dữ liệu thửa đất lịch sử.");
            }
            finally
            {
                // Cập nhật thời gian, trạng thái hoàn thành và trạng thái xử lý
                _timeThuaDatLichSu = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                _isCompletedThuaDatLichSu = true;
                StateHasChanged();
            }

        }
    }
    
    private Color _colorUpdateGiayChungNhan = Color.Default;
    private string _timeUpdateGiayChungNhan = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    private bool _isCompletedUpdateGiayChungNhan = false;
    private bool _isProcessingUpdateGiayChungNhan = false;
    private string? _errorUpdateGiayChungNhan= string.Empty;
    private string _messageUpdateGiayChungNhanSuccess = "Đã hoàn thành";
    private long _totalUpdateGiayChungNhan;
    private long _currentUpdateGiayChungNhan;
    private async Task UpdatingGiayChungNhan()
    {
        _colorUpdateGiayChungNhan = Color.Default;
        _isProcessingUpdateGiayChungNhan = true;
        StateHasChanged();
        await Task.Delay(1000);
    }
    
    private Color _colorUpdateThuaDat = Color.Default;
    private string _timeUpdateThuaDat = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    private bool _isCompletedUpdateThuaDat = false;
    private bool _isProcessingUpdateThuaDat = false;
    private string _messageUpdateThuaDatSuccess = "Đã hoàn thành";
    private string? _errorUpdateThuaDat= string.Empty;
    private long _totalUpdateThuaDat;
    private long _currentUpdateThuaDat;
    
    private async Task UpdatingThuaDat()
    {
        if (!_isFinished && !_isCompletedUpdateThuaDat && !_isProcessingUpdateThuaDat)
        {
            
        }
        _colorUpdateThuaDat = Color.Default;
        _isProcessingUpdateThuaDat = true;
        StateHasChanged();
        await Task.Delay(1000);
    }
    
    
    private Color _colorUpdateToBanDo = Color.Default;
    private string _timeUpdateToBanDo = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    private bool _isCompletedUpdateToBanDo = false;
    private bool _isProcessingUpdateToBanDo = false;
    private string _messageUpdateToBanDoSuccess = "Đã hoàn thành";
    private string? _errorUpdateToBanDo= string.Empty;
    private long _totalUpdateToBanDo;
    private long _currentUpdateToBanDo;
    
    private async Task UpdatingToBanDo()
    {
        _colorUpdateToBanDo = Color.Default;
        _isProcessingUpdateToBanDo = true;
        StateHasChanged();
        await Task.Delay(1000);
    }
    
    
    private Color _colorUpdateDvhc = Color.Default;
    private string _timeUpdateDvhc = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    private bool _isCompletedUpdateDvhc = false;
    private bool _isProcessingUpdateDvhc = false;
    private string? _errorUpdateDvhc= string.Empty;
    private async Task UpdatingDonViHanhChinh()
    {
        _colorUpdateDvhc = Color.Default;
        _isProcessingUpdateDvhc = true;
        StateHasChanged();
        await Task.Delay(1000);
        _colorUpdateDvhc = Color.Success;
        _isCompletedUpdateDvhc = true;
        SetMessage("Đã hoàn thành", Severity.Success);
    }
}