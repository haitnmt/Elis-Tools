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
            return new Window(new MainPage())
            {
                Title = "Chuyển đổi đơn vị hành chính",
                MaximumHeight = 710,
                MaximumWidth = 880,
                MinimumHeight = 710,
                MinimumWidth = 880,
                
            };
        }
    }
}
