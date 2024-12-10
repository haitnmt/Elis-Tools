namespace Haihv.Elis.Tools.ChuyenDvhc
{
    public partial class App
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var windows = new Window(new AppShell())
            {
                Width = 880,
                Height = 660,
                MaximumWidth = 880,
                MaximumHeight = 660,
                MinimumWidth = 880,
                MinimumHeight = 660,
            };
            return windows;
        }
    }
}