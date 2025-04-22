namespace Exman
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            
            // Register routes for navigation
            Routing.RegisterRoute(nameof(Views.ApiTestPage), typeof(Views.ApiTestPage));
            Routing.RegisterRoute(nameof(Views.CollectionsPage), typeof(Views.CollectionsPage));
            Routing.RegisterRoute(nameof(Views.EnvironmentsPage), typeof(Views.EnvironmentsPage));
            Routing.RegisterRoute(nameof(Views.RequestHistoryPage), typeof(Views.RequestHistoryPage));
        }
    }
}