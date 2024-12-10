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
                MaximumHeight = 740,
                MaximumWidth = 960,
                MinimumHeight = 740,
                MinimumWidth = 960
            };
        }
    }
}
