using Exman.Models;
using Exman.Services;
using Exman.Utilities;
using System.Collections.ObjectModel;

namespace Exman.Views
{
    public partial class CollectionsPage : ContentPage
    {
        private readonly ICollectionService _collectionService;
        private ObservableCollection<Collection> _collections = new();
        private Collection _currentCollection;
        private string _currentCollectionId;
        private bool _isNewCollection = false;

        public CollectionsPage()
        {
            InitializeComponent();
            
            // Get service from dependency injection
            _collectionService = MauiProgram.Services.GetService<ICollectionService>();
            
            // Load collections when the page appears
            Loaded += CollectionsPage_Loaded;
        }

        private async void CollectionsPage_Loaded(object sender, EventArgs e)
        {
            await LoadCollectionsAsync();
            
            // Check for query parameters
            var queryParams = Shell.Current.CurrentState.GetQueryParameters();
            if (queryParams.ContainsKey("action") && queryParams["action"] == "create")
            {
                ShowNewCollectionDialog();
            }
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
                
                CollectionsListView.ItemsSource = _collections;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load collections: {ex.Message}", "OK");
            }
        }

        private void OnCollectionSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Collection collection)
            {
                // Navigate to collection in API test page
                Shell.Current.GoToAsync($"//ApiTest?collectionId={collection.Id}");
                
                // Clear selection
                CollectionsListView.SelectedItem = null;
            }
        }

        private void OnNewCollectionClicked(object sender, EventArgs e)
        {
            ShowNewCollectionDialog();
        }

        private void ShowNewCollectionDialog()
        {
            _isNewCollection = true;
            _currentCollectionId = null;
            _currentCollection = null;
            
            // Update UI
            PopupTitleLabel.Text = "Create New Collection";
            SaveButton.Text = "Create";
            CollectionNameEntry.Text = string.Empty;
            CollectionDescriptionEditor.Text = string.Empty;
            
            // Show popup
            EditCollectionPopup.IsVisible = true;
        }

        private async void OnEditCollectionClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string collectionId)
            {
                try
                {
                    var collection = await _collectionService.GetCollectionAsync(collectionId);
                    if (collection == null)
                    {
                        await DisplayAlert("Error", "Collection not found", "OK");
                        return;
                    }
                    
                    _isNewCollection = false;
                    _currentCollectionId = collectionId;
                    _currentCollection = collection;
                    
                    // Update UI
                    PopupTitleLabel.Text = "Edit Collection";
                    SaveButton.Text = "Save";
                    CollectionNameEntry.Text = collection.Name;
                    CollectionDescriptionEditor.Text = collection.Description;
                    
                    // Show popup
                    EditCollectionPopup.IsVisible = true;
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to load collection: {ex.Message}", "OK");
                }
            }
        }

        private async void OnDeleteCollectionClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string collectionId)
            {
                bool confirm = await DisplayAlert("Confirm Delete", "Are you sure you want to delete this collection? This action cannot be undone.", "Delete", "Cancel");
                
                if (confirm)
                {
                    try
                    {
                        await _collectionService.DeleteCollectionAsync(collectionId);
                        await LoadCollectionsAsync();
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to delete collection: {ex.Message}", "OK");
                    }
                }
            }
        }

        private async void OnSaveCollectionClicked(object sender, EventArgs e)
        {
            string name = CollectionNameEntry.Text?.Trim() ?? string.Empty;
            string description = CollectionDescriptionEditor.Text?.Trim() ?? string.Empty;
            
            if (string.IsNullOrEmpty(name))
            {
                await DisplayAlert("Validation Error", "Collection name is required", "OK");
                return;
            }
            
            try
            {
                if (_isNewCollection)
                {
                    // Create new collection
                    var newCollection = new Collection
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = name,
                        Description = description,
                        Requests = new ObservableCollection<ApiRequest>(),
                        Folders = new ObservableCollection<Collection.Folder>(),
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    
                    await _collectionService.AddCollectionAsync(newCollection);
                }
                else if (_currentCollection != null)
                {
                    // Update existing collection
                    _currentCollection.Name = name;
                    _currentCollection.Description = description;
                    _currentCollection.UpdatedAt = DateTime.Now;
                    
                    await _collectionService.UpdateCollectionAsync(_currentCollection);
                }
                
                // Hide popup and refresh list
                EditCollectionPopup.IsVisible = false;
                await LoadCollectionsAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to save collection: {ex.Message}", "OK");
            }
        }

        private void OnCancelEditClicked(object sender, EventArgs e)
        {
            // Hide popup
            EditCollectionPopup.IsVisible = false;
        }

        private async void OnImportPostmanCollectionClicked(object sender, EventArgs e)
        {
            // File picker for selecting Postman collection files
            var options = new PickOptions
            {
                PickerTitle = "Select Postman Collection",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".json" } },
                    { DevicePlatform.macOS, new[] { "json" } },
                    { DevicePlatform.iOS, new[] { "public.json" } },
                    { DevicePlatform.Android, new[] { "application/json" } }
                })
            };
            
            try
            {
                var result = await FilePicker.PickAsync(options);
                if (result != null)
                {
                    // Get file content
                    var stream = await result.OpenReadAsync();
                    using var reader = new StreamReader(stream);
                    var jsonContent = await reader.ReadToEndAsync();
                    
                    // Convert and save collection
                    var converter = new PostmanCollectionConverter();
                    var collection = await converter.ConvertFromPostmanCollectionAsync(jsonContent);
                    
                    if (collection != null)
                    {
                        await _collectionService.AddCollectionAsync(collection);
                        await LoadCollectionsAsync();
                        await DisplayAlert("Success", "Postman collection imported successfully", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Import Error", $"Failed to import collection: {ex.Message}", "OK");
            }
        }

        private async void OnExportCollectionClicked(object sender, EventArgs e)
        {
            if (_collections.Count == 0)
            {
                await DisplayAlert("Export Error", "No collections to export", "OK");
                return;
            }
            
            // Let user pick a collection to export
            var options = new List<string>();
            foreach (var collection in _collections)
            {
                options.Add(collection.Name);
            }
            
            string selectedOption = await DisplayActionSheet("Select Collection to Export", "Cancel", null, options.ToArray());
            if (selectedOption != "Cancel" && !string.IsNullOrEmpty(selectedOption))
            {
                var selectedCollection = _collections.FirstOrDefault(c => c.Name == selectedOption);
                if (selectedCollection != null)
                {
                    try
                    {
                        // Convert to Postman format
                        var converter = new PostmanCollectionConverter();
                        string json = await converter.ConvertToPostmanCollectionAsync(selectedCollection);
                        
                        if (!string.IsNullOrEmpty(json))
                        {
                            // Save file
                            var filename = $"{selectedCollection.Name.Replace(" ", "_")}_collection.json";
                            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), filename);
                            File.WriteAllText(path, json);
                            
                            await DisplayAlert("Export Success", $"Collection exported to {path}", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Export Error", $"Failed to export collection: {ex.Message}", "OK");
                    }
                }
            }
        }
    }
}