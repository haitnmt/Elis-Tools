using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_App.Client.Components;

public partial class BarcodeScanner : IDisposable
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    private bool _isScanning;
    private string _scanResult = "";
    private List<VideoDevice> _videoDevices = [];
    private DotNetObjectReference<BarcodeScanner>? _objRef;
    private string _selectedDeviceId = "";
    private string _errorMessage = "Chưa tải";

    protected override void OnInitialized()
    {
        Console.WriteLine("Component initialized");
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        Console.WriteLine($"OnAfterRenderAsync called, firstRender: {firstRender}");
        if (firstRender)
        {
            Console.WriteLine("OnAfterRenderAsync");
            try
            {
                _objRef = DotNetObjectReference.Create(this);
                await JsRuntime.InvokeVoidAsync("barcodeScanner.initialize", _objRef);
                await LoadVideoDevices();
            }
            catch (Exception ex)
            {
                _errorMessage = "Error initializing camera: " + ex.Message;
                StateHasChanged();
            }
        }
    }

    private async Task LoadVideoDevices()
    {
        try
        {
            _videoDevices = await JsRuntime.InvokeAsync<List<VideoDevice>>("barcodeScanner.listVideoDevices");
            if (_videoDevices.Any())
            {
                _selectedDeviceId = _videoDevices[0].DeviceId;
                StateHasChanged();
            }
            else
            {
                _errorMessage = "No cameras found";
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            _errorMessage = "Error loading cameras: " + ex.Message;
            StateHasChanged();
        }
    }

    private async Task StartScanning()
    {
        try
        {
            _errorMessage = "";
            _isScanning = true;
            await JsRuntime.InvokeVoidAsync("barcodeScanner.startScanning", _selectedDeviceId);
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _errorMessage = "Error starting scanner: " + ex.Message;
            _isScanning = false;
            StateHasChanged();
        }
    }

    private async Task ResetScanning()
    {
        try
        {
            _isScanning = false;
            _scanResult = "";
            _errorMessage = "";
            await JsRuntime.InvokeVoidAsync("barcodeScanner.resetScanning");
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _errorMessage = "Error resetting scanner: " + ex.Message;
            StateHasChanged();
        }
    }

    private async Task OnVideoSourceChange(ChangeEventArgs e)
    {
        _selectedDeviceId = e.Value?.ToString() ?? "";
        if (_isScanning)
        {
            await ResetScanning();
            await StartScanning();
        }
    }

    [JSInvokable]
    public void OnScanResult(string result)
    {
        _scanResult = result;
        StateHasChanged();
    }

    [JSInvokable]
    public void OnScanError(string error)
    {
        _errorMessage = error;
        _isScanning = false;
        StateHasChanged();
    }

    void IDisposable.Dispose()
    {
        if (_objRef != null)
        {
            try
            {
                _ = ResetScanning();
                JsRuntime.InvokeVoidAsync("barcodeScanner.dispose");
                _objRef.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
            finally
            {
                _objRef.Dispose();
            }
        }
    }

    public class VideoDevice
    {
        public string DeviceId { get; set; } = "";
        public string Label { get; set; } = "";
    }
}