using Exman.Models;
using Exman.Services;
using System.Collections.ObjectModel;
using System.Net;

namespace Exman.Views
{
    public partial class RequestHistoryPage : ContentPage
    {
        private readonly IRequestHistoryService _requestHistoryService;
        private ObservableCollection<RequestHistoryItem> _historyItems = new();

        public RequestHistoryPage()
        {
            InitializeComponent();
            
            // Get service from dependency injection
            _requestHistoryService = MauiProgram.Services.GetService<IRequestHistoryService>();
            
            // Load history when page appears
            Loaded += RequestHistoryPage_Loaded;
        }

        private async void RequestHistoryPage_Loaded(object sender, EventArgs e)
        {
            await LoadHistoryAsync();
        }

        private async Task LoadHistoryAsync()
        {
            try
            {
                var historyItems = await _requestHistoryService.GetHistoryAsync();
                _historyItems.Clear();
                
                foreach (var item in historyItems.OrderByDescending(h => h.Timestamp))
                {
                    _historyItems.Add(item);
                }
                
                HistoryListView.ItemsSource = _historyItems;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load request history: {ex.Message}", "OK");
            }
        }

        private void OnHistoryItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is RequestHistoryItem historyItem)
            {
                // Use the request
                UseRequest(historyItem);
                
                // Clear selection
                HistoryListView.SelectedItem = null;
            }
        }

        private void OnUseRequestClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string historyItemId)
            {
                var historyItem = _historyItems.FirstOrDefault(h => h.Id == historyItemId);
                if (historyItem != null)
                {
                    UseRequest(historyItem);
                }
            }
        }

        private async void OnClearHistoryClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Confirm Clear History", "Are you sure you want to clear all request history? This action cannot be undone.", "Clear", "Cancel");
            
            if (confirm)
            {
                try
                {
                    await _requestHistoryService.ClearHistoryAsync();
                    _historyItems.Clear();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to clear history: {ex.Message}", "OK");
                }
            }
        }

        private void UseRequest(RequestHistoryItem historyItem)
        {
            // Navigate to API test page with this request
            // We'll pass the request ID through query parameters
            Shell.Current.GoToAsync($"//ApiTest?historyItemId={historyItem.Id}");
        }
    }
}