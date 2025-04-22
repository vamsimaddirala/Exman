using Exman.Models;
using Exman.Services;
using System.Collections.ObjectModel;

namespace Exman.Views
{
    public partial class EnvironmentsPage : ContentPage
    {
        private readonly IEnvironmentService _environmentService;
        private ObservableCollection<RequestEnvironment> _environments = new();
        private ObservableCollection<Variable> _variables = new();
        private RequestEnvironment _currentEnvironment;
        private string _currentEnvironmentId;
        private bool _isNewEnvironment = false;

        public EnvironmentsPage()
        {
            InitializeComponent();
            
            // Get service from dependency injection
            _environmentService = MauiProgram.Services.GetService<IEnvironmentService>();
            
            // Load environments when the page appears
            Loaded += EnvironmentsPage_Loaded;
        }

        private async void EnvironmentsPage_Loaded(object sender, EventArgs e)
        {
            await LoadEnvironmentsAsync();
        }

        private async Task LoadEnvironmentsAsync()
        {
            try
            {
                var environments = await _environmentService.GetEnvironmentsAsync();
                _environments.Clear();
                
                foreach (var env in environments.OrderBy(e => e.Name))
                {
                    _environments.Add(env);
                }
                
                EnvironmentsListView.ItemsSource = _environments;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load environments: {ex.Message}", "OK");
            }
        }

        private void OnEnvironmentSelected(object sender, SelectionChangedEventArgs e)
        {
            // Clear selection
            EnvironmentsListView.SelectedItem = null;
        }

        private void OnNewEnvironmentClicked(object sender, EventArgs e)
        {
            ShowNewEnvironmentDialog();
        }

        private void ShowNewEnvironmentDialog()
        {
            _isNewEnvironment = true;
            _currentEnvironmentId = null;
            _currentEnvironment = null;
            
            // Update UI
            PopupTitleLabel.Text = "Create New Environment";
            SaveButton.Text = "Create";
            EnvironmentNameEntry.Text = string.Empty;
            EnvironmentDescriptionEntry.Text = string.Empty;
            
            // Clear variables
            _variables.Clear();
            VariablesListView.ItemsSource = _variables;
            
            // Show popup
            EditEnvironmentPopup.IsVisible = true;
        }

        private async void OnEditEnvironmentClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string environmentId)
            {
                try
                {
                    var environment = await _environmentService.GetEnvironmentAsync(environmentId);
                    if (environment == null)
                    {
                        await DisplayAlert("Error", "Environment not found", "OK");
                        return;
                    }
                    
                    _isNewEnvironment = false;
                    _currentEnvironmentId = environmentId;
                    _currentEnvironment = environment;
                    
                    // Update UI
                    PopupTitleLabel.Text = "Edit Environment";
                    SaveButton.Text = "Save";
                    EnvironmentNameEntry.Text = environment.Name;
                    EnvironmentDescriptionEntry.Text = environment.Description;
                    
                    // Load variables
                    _variables = new ObservableCollection<Variable>(environment.Variables ?? new ObservableCollection<Variable>());
                    VariablesListView.ItemsSource = _variables;
                    
                    // Show popup
                    EditEnvironmentPopup.IsVisible = true;
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to load environment: {ex.Message}", "OK");
                }
            }
        }

        private async void OnDeleteEnvironmentClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string environmentId)
            {
                bool confirm = await DisplayAlert("Confirm Delete", "Are you sure you want to delete this environment? This action cannot be undone.", "Delete", "Cancel");
                
                if (confirm)
                {
                    try
                    {
                        await _environmentService.DeleteEnvironmentAsync(environmentId);
                        await LoadEnvironmentsAsync();
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Failed to delete environment: {ex.Message}", "OK");
                    }
                }
            }
        }

        private async void OnSaveEnvironmentClicked(object sender, EventArgs e)
        {
            string name = EnvironmentNameEntry.Text?.Trim() ?? string.Empty;
            string description = EnvironmentDescriptionEntry.Text?.Trim() ?? string.Empty;
            
            if (string.IsNullOrEmpty(name))
            {
                await DisplayAlert("Validation Error", "Environment name is required", "OK");
                return;
            }
            
            try
            {
                if (_isNewEnvironment)
                {
                    // Create new environment
                    var newEnvironment = new RequestEnvironment
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = name,
                        Description = description,
                        Variables = _variables,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    
                    await _environmentService.UpdateEnvironmentAsync(newEnvironment);
                }
                else if (_currentEnvironment != null)
                {
                    // Update existing environment
                    _currentEnvironment.Name = name;
                    _currentEnvironment.Description = description;
                    _currentEnvironment.Variables = _variables;
                    _currentEnvironment.UpdatedAt = DateTime.Now;
                    
                    await _environmentService.UpdateEnvironmentAsync(_currentEnvironment);
                }
                
                // Hide popup and refresh list
                EditEnvironmentPopup.IsVisible = false;
                await LoadEnvironmentsAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to save environment: {ex.Message}", "OK");
            }
        }

        private void OnCancelEditClicked(object sender, EventArgs e)
        {
            // Hide popup
            EditEnvironmentPopup.IsVisible = false;
        }

        private void OnAddVariableClicked(object sender, EventArgs e)
        {
            // Add a new variable
            _variables.Add(new Variable { Key = "", Value = "", Enabled = true });
        }

        private void OnDeleteVariableClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Variable variable)
            {
                _variables.Remove(variable);
            }
        }

        private async void OnImportPostmanEnvironmentClicked(object sender, EventArgs e)
        {
            // File picker for selecting Postman environment files
            var options = new PickOptions
            {
                PickerTitle = "Select Postman Environment",
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
                    
                    // Convert and save environment
                    var converter = new PostmanCollectionConverter();
                    var environment = await converter.ConvertFromPostmanEnvironmentAsync(jsonContent);
                    
                    if (environment != null)
                    {
                        await _environmentService.UpdateEnvironmentAsync(environment);
                        await LoadEnvironmentsAsync();
                        await DisplayAlert("Success", "Postman environment imported successfully", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Import Error", $"Failed to import environment: {ex.Message}", "OK");
            }
        }

        private async void OnExportEnvironmentClicked(object sender, EventArgs e)
        {
            if (_environments.Count == 0)
            {
                await DisplayAlert("Export Error", "No environments to export", "OK");
                return;
            }
            
            // Let user pick an environment to export
            var options = new List<string>();
            foreach (var environment in _environments)
            {
                options.Add(environment.Name);
            }
            
            string selectedOption = await DisplayActionSheet("Select Environment to Export", "Cancel", null, options.ToArray());
            if (selectedOption != "Cancel" && !string.IsNullOrEmpty(selectedOption))
            {
                var selectedEnvironment = _environments.FirstOrDefault(e => e.Name == selectedOption);
                if (selectedEnvironment != null)
                {
                    try
                    {
                        // Convert to Postman format
                        var converter = new PostmanCollectionConverter();
                        string json = await converter.ConvertToPostmanEnvironmentAsync(selectedEnvironment);
                        
                        if (!string.IsNullOrEmpty(json))
                        {
                            // Save file
                            var filename = $"{selectedEnvironment.Name.Replace(" ", "_")}_environment.json";
                            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), filename);
                            File.WriteAllText(path, json);
                            
                            await DisplayAlert("Export Success", $"Environment exported to {path}", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Export Error", $"Failed to export environment: {ex.Message}", "OK");
                    }
                }
            }
        }
    }
}