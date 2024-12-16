namespace Haihv.Elis.Tool.ChuyenDvhc
{
    public partial class App
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Lấy tỷ lệ thu phóng của màn hình:
            var scale = DeviceDisplay.MainDisplayInfo.Density;

            return new Window(new MainPage())
            {
                Title = "Chuyển đổi đơn vị hành chính",
                Height = 710 * scale,
                Width = 880 * scale,
                MinimumHeight = 710,
                MinimumWidth = 880,
            };
        }
    }
}