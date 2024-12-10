using Haihv.Elis.Tools.ChuyenDvhc.Data;
using Microsoft.EntityFrameworkCore;

namespace Haihv.Elis.Tools.ChuyenDvhc;

public partial class DataConnection
{
    public event EventHandler ConnectionSuccessful = null!;
    private string ConnectionString { get; set; } = string.Empty;
    public DataConnection()
    {
        InitializeComponent();
    }
   
    private async void OnCheckConnectionClicked(object sender, EventArgs e)
    {
        try
        {
            var serverName = ServerNameEntry.Text;
            var databaseName = DatabaseNameEntry.Text;
            var userName = UserNameEntry.Text;
            var password = PasswordEntry.Text;
            var connectionString = $"Server={serverName};" +
                                   $"Database={databaseName};" +
                                   $"User Id={userName};" +
                                   $"Password={password};" +
                                   $"TrustServerCertificate={TrustServerCertificate.IsChecked};";
            
            await using var context = new ElisDataContext(connectionString);
            await context.Database.OpenConnectionAsync();
            await DisplayAlert("Success", "Kết nối thành công!", "OK");
            BorderWarning.IsVisible = true;
            ConnectionString = connectionString;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Kết nối thất bại: {ex.Message}", "OK");
        }
    }

    private void OnConfirmBackup(object? sender, EventArgs e)
    {
        if (!ConfirmBackup.IsChecked) return;
        CacheManager.SetConnectionString(ConnectionString);
        ConnectionSuccessful(this, EventArgs.Empty);
    }
}