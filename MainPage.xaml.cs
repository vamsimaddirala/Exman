namespace Exman
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            
            // The real UI is in AppShell now, so navigate there
            Dispatcher.Dispatch(async () => 
            {
                await Shell.Current.GoToAsync("//Home");
            });
        }
    }
}
