using Haihv.Elis.Tools.ChuyenDvhc.Data;

namespace Haihv.Elis.Tools.ChuyenDvhc
{
    public partial class AppShell
    {
        private string? ConnectionString { get; set; }
        public AppShell()
        {
            InitializeComponent();
            ConnectionString = CacheManager.GetConnectionString();
        }
        
        private void OnConnectionSuccessful(object sender, EventArgs e)
        {
            ConnectionString = CacheManager.GetConnectionString();
            if (string.IsNullOrWhiteSpace(ConnectionString)) return;
            ConfigDataTransform.IsEnabled = true;
            TabConfigDataTransform.IsEnabled = true;
        }
    }
}
