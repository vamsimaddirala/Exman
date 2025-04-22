using Exman.Models;
using Exman.Services;
using System.Collections.ObjectModel;

namespace Exman.Views
{
    public partial class HomePage : ContentPage
    {
        private readonly ICollectionService _collectionService;
        private ObservableCollection<Collection> _collections = new();

        public HomePage()
        {
            InitializeComponent();
            
            // Get the collection service from DI
            _collectionService = MauiProgram.Services.GetService<ICollectionService>();
            
            // Load collections when the page appears
            Loaded += HomePage_Loaded;
        }

        private async void HomePage_Loaded(object sender, EventArgs e)
        {
            await LoadCollectionsAsync();
        }

        private async Task LoadCollectionsAsync()
        {
            try
            {
                var collections = await _collectionService.GetCollectionsAsync();
                _collections.Clear();
                
                foreach (var collection in collections.OrderByDescending(c => c.UpdatedAt))
                {
                    _collections.Add(collection);
                }
                
                CollectionsView.ItemsSource = _collections;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load collections: {ex.Message}", "OK");
            }
        }

        private void OnStartTestingClicked(object sender, EventArgs e)
        {
            Shell.Current.GoToAsync("//ApiTest");
        }

        private void OnViewCollectionsClicked(object sender, EventArgs e)
        {
            Shell.Current.GoToAsync("//Collections");
        }

        private void OnTryApiTesterClicked(object sender, EventArgs e)
        {
            Shell.Current.GoToAsync("//ApiTest");
        }

        private void OnManageCollectionsClicked(object sender, EventArgs e)
        {
            Shell.Current.GoToAsync("//Collections");
        }

        private void OnExploreAuthenticationClicked(object sender, EventArgs e)
        {
            Shell.Current.GoToAsync("//ApiTest");
        }

        private void OnCreateCollectionClicked(object sender, EventArgs e)
        {
            Shell.Current.GoToAsync("//Collections?action=create");
        }

        private void OnCollectionSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Collection collection)
            {
                CollectionsView.SelectedItem = null; // Clear selection
                Shell.Current.GoToAsync($"//ApiTest?collectionId={collection.Id}");
            }
        }

        private void OnOpenCollectionClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string collectionId)
            {
                Shell.Current.GoToAsync($"//ApiTest?collectionId={collectionId}");
            }
        }
    }
}