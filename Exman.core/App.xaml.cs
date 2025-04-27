namespace Exman
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
           
            var window = new Window(new MainPage())
            {
                Title = "ExMan",
                Width = 1500,
                Height = 850,
            };
            

            return window;
        }
    }
}
