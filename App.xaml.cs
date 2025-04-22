namespace Exman
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);
            
            window.Title = "ExMan";
            window.Width = 1500;
            window.Height = 850;
            
            return window;
        }
    }
}
