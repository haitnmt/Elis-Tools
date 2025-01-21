namespace Haihv.Elis.Tool.TraCuuGcn.Web_Lib;
public partial class GiayChungNhanMaVach
{
    private string _barcode = string.Empty;

    private bool _isScannerVisible;
    
    private void ShowScanner()
    {
        _isScannerVisible = true;
        StateHasChanged();
    }

    private void HideScanner()
    {
        _isScannerVisible = false;
    }

    private void HandleBarcodeScanned(string barcode)
    {
        _barcode = barcode;
        _isScannerVisible = false;
    }
}