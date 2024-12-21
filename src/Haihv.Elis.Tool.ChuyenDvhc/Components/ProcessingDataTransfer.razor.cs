using Haihv.Elis.Tool.ChuyenDvhc.Data;
using Haihv.Elis.Tool.ChuyenDvhc.Extensions;
using Haihv.Elis.Tool.ChuyenDvhc.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Caching.Memory;
using MudBlazor;
using Color = MudBlazor.Color;

namespace Haihv.Elis.Tool.ChuyenDvhc.Components;

public partial class ProcessingDataTransfer
{
    [Inject] private IMemoryCache MemoryCache { get; set; } = null!;
    [Parameter] public bool IsAuditEnabled { get; set; }
    [Parameter] public EventCallback<bool> IsFinishedChanged { get; set; }


    private ElisDataContext? _dbContext = null;
    private string _timeKhoiTaoDuLieu = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
    private Color _colorKhoiTaoDuLieu = Color.Primary;
    private bool _isCompletedKhoiTaoDuLieu = false;
    private bool _isProcessingKhoiTaoDuLieu = false;
    private string? _errorKhoiTaoDuLieu = string.Empty;
    private string? _connectionString;
    private bool _isFinished;
    private MudTimeline _timeLine = new ();

    protected override void OnInitialized()
    {
        _connectionString = MemoryCache.Get<string>(CacheDataConnection.ConnectionString);
        if (!string.IsNullOrWhiteSpace(_connectionString))
        {
            _dbContext = new ElisDataContext(_connectionString);
        }
        else
        {
            SetFinished();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        await CreateAuditTable();
    }

    /// <summary>
    /// Đánh dấu quá trình xử lý đã hoàn thành và kích hoạt sự kiện thay đổi trạng thái.
    /// </summary>
    private void SetFinished()
    {
        _isFinished = true;
        IsFinishedChanged.InvokeAsync(_isFinished);
    }

    /// <summary>
    /// Tạo hoặc thay đổi bảng audit nếu được kích hoạt.
    /// </summary>
    /// <returns>Task đại diện cho thao tác không đồng bộ.</returns>
    private async Task CreateAuditTable()
    {
        // Kiểm tra nếu audit được kích hoạt, _dbContext không null, và quá trình chưa hoàn thành hoặc đang xử lý
        if (IsAuditEnabled && _dbContext != null && !_isCompletedKhoiTaoDuLieu && !_isProcessingKhoiTaoDuLieu)
        {
            _isProcessingKhoiTaoDuLieu = true;
            _errorKhoiTaoDuLieu = string.Empty;
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
                SetFinished();
            }
            finally
            {
                // Cập nhật thời gian, trạng thái hoàn thành và trạng thái xử lý
                _timeKhoiTaoDuLieu = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                _isCompletedKhoiTaoDuLieu = true;
                _isProcessingKhoiTaoDuLieu = false;
                StateHasChanged();
            }
        }
    }
}