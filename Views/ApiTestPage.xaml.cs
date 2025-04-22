using Exman.Models;
using Exman.Services;
using Exman.Utilities;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using System.Net;

namespace Exman.Views
{
    public partial class ApiTestPage : ContentPage
    {
        private readonly IApiRequestService _apiRequestService;
        private readonly ICollectionService _collectionService;
        private readonly IEnvironmentService _environmentService;
        private readonly IRequestHistoryService _requestHistoryService;
        
        private ApiRequest _currentRequest;
        private ApiResponse _currentResponse;
        private ObservableCollection<TreeItem> _collectionItems = new();
        private List<string> _httpMethods = new() { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };
        
        private string _selectedMethod = "GET";
        private string _requestUrl = string.Empty;
        private string _currentRequestTab = "params";
        private string _currentResponseTab = "response";
        private bool _hasResponse = false;
        private int _responseStatusCode = 0;
        private string _responseStatusText = string.Empty;
        private long _responseTime = 0;
        private bool _isResponseSuccess = false;
        
        public ApiTestPage()
        {
            InitializeComponent();
            
            // Get services from dependency injection
            _apiRequestService = MauiProgram.Services.GetService<IApiRequestService>();
            _collectionService = MauiProgram.Services.GetService<ICollectionService>();
            _environmentService = MauiProgram.Services.GetService<IEnvironmentService>();
            _requestHistoryService = MauiProgram.Services.GetService<IRequestHistoryService>();
            
            // Initialize request
            _currentRequest = new ApiRequest
            {
                Method = ApiHttpMethod.GET,
                Url = string.Empty,
                Headers = new ObservableCollection<Models.KeyValuePair>(),
                QueryParameters = new ObservableCollection<Models.KeyValuePair>(),
                Body = new RequestBody()
            };
            
            // Set up the method picker
            MethodPicker.ItemsSource = _httpMethods;
            MethodPicker.SelectedItem = _selectedMethod;
            
            // Set the binding context
            BindingContext = this;
            
            // Load collections when page appears
            Loaded += ApiTestPage_Loaded;
        }
        
        // Properties for binding
        public string SelectedMethod 
        { 
            get => _selectedMethod; 
            set 
            { 
                _selectedMethod = value;
                OnPropertyChanged();
                if (_currentRequest != null)
                {
                    _currentRequest.Method = (ApiHttpMethod)Enum.Parse(typeof(ApiHttpMethod), value);
                }
            }
        }
        
        public string RequestUrl 
        { 
            get => _requestUrl; 
            set 
            { 
                _requestUrl = value;
                OnPropertyChanged();
                if (_currentRequest != null)
                {
                    _currentRequest.Url = value;
                }
            }
        }
        
        public string CurrentRequestTab 
        { 
            get => _currentRequestTab; 
            set 
            { 
                _currentRequestTab = value;
                OnPropertyChanged();
                UpdateRequestTabContent();
            }
        }
        
        public string CurrentResponseTab 
        { 
            get => _currentResponseTab; 
            set 
            { 
                _currentResponseTab = value;
                OnPropertyChanged();
                UpdateResponseTabContent();
            }
        }
        
        public bool HasResponse
        {
            get => _hasResponse;
            set
            {
                _hasResponse = value;
                OnPropertyChanged();
            }
        }
        
        public int ResponseStatusCode
        {
            get => _responseStatusCode;
            set
            {
                _responseStatusCode = value;
                OnPropertyChanged();
            }
        }
        
        public string ResponseStatusText
        {
            get => _responseStatusText;
            set
            {
                _responseStatusText = value;
                OnPropertyChanged();
            }
        }
        
        public long ResponseTime
        {
            get => _responseTime;
            set
            {
                _responseTime = value;
                OnPropertyChanged();
            }
        }
        
        public bool IsResponseSuccess
        {
            get => _isResponseSuccess;
            set
            {
                _isResponseSuccess = value;
                OnPropertyChanged();
            }
        }
        
        public List<string> HttpMethods => _httpMethods;
        
        private async void ApiTestPage_Loaded(object sender, EventArgs e)
        {
            await LoadCollectionsAsync();
            
            // Check for query parameters to load a specific collection or request
            var queryParams = Shell.Current.CurrentState.GetQueryParameters();
            if (queryParams.ContainsKey("collectionId"))
            {
                string collectionId = queryParams["collectionId"];
                // TODO: Load selected collection
            }
            
            if (queryParams.ContainsKey("requestId"))
            {
                string requestId = queryParams["requestId"];
                // TODO: Load selected request
            }
        }
        
        private async Task LoadCollectionsAsync()
        {
            try
            {
                var collections = await _collectionService.GetCollectionsAsync();
                _collectionItems.Clear();
                
                foreach (var collection in collections)
                {
                    // Add collection item
                    var collectionItem = new TreeItem
                    {
                        Id = collection.Id,
                        Name = collection.Name,
                        Type = TreeItemType.Collection,
                        Level = 0,
                        IsExpanded = false,
                        IsRequest = false
                    };
                    _collectionItems.Add(collectionItem);
                    
                    // Add folders and requests if collection is expanded
                    if (collectionItem.IsExpanded)
                    {
                        AddCollectionChildren(collection, collectionItem);
                    }
                }
                
                CollectionTreeView.ItemsSource = _collectionItems;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load collections: {ex.Message}", "OK");
            }
        }
        
        private void AddCollectionChildren(Collection collection, TreeItem parentItem)
        {
            int insertIndex = _collectionItems.IndexOf(parentItem) + 1;
            
            // Add folders
            foreach (var folder in collection.Folders)
            {
                var folderItem = new TreeItem
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    Type = TreeItemType.Folder,
                    Level = parentItem.Level + 1,
                    IsExpanded = false,
                    IsRequest = false,
                    ParentId = parentItem.Id
                };
                _collectionItems.Insert(insertIndex++, folderItem);
                
                // Add requests in this folder if expanded
                if (folderItem.IsExpanded)
                {
                    foreach (var request in folder.Requests)
                    {
                        var requestItem = new TreeItem
                        {
                            Id = request.Id,
                            Name = request.Name,
                            Type = TreeItemType.Request,
                            Level = folderItem.Level + 1,
                            Method = request.Method.ToString(),
                            IsRequest = true,
                            ParentId = folderItem.Id
                        };
                        _collectionItems.Insert(insertIndex++, requestItem);
                    }
                }
            }
            
            // Add requests at the collection level
            foreach (var request in collection.Requests)
            {
                var requestItem = new TreeItem
                {
                    Id = request.Id,
                    Name = request.Name,
                    Type = TreeItemType.Request,
                    Level = parentItem.Level + 1,
                    Method = request.Method.ToString(),
                    IsRequest = true,
                    ParentId = parentItem.Id
                };
                _collectionItems.Insert(insertIndex++, requestItem);
            }
        }
        
        private void UpdateRequestTabContent()
        {
            // Clear current content
            RequestTabContent.Content = null;
            
            // Create content based on selected tab
            switch (CurrentRequestTab)
            {
                case "params":
                    RequestTabContent.Content = CreateParamsTabContent();
                    break;
                case "auth":
                    RequestTabContent.Content = CreateAuthTabContent();
                    break;
                case "headers":
                    RequestTabContent.Content = CreateHeadersTabContent();
                    break;
                case "body":
                    RequestTabContent.Content = CreateBodyTabContent();
                    break;
                case "script":
                    RequestTabContent.Content = CreateScriptTabContent();
                    break;
            }
        }
        
        private View CreateParamsTabContent()
        {
            // Create a grid for query parameters
            var grid = new Grid
            {
                RowDefinitions = new RowDefinitionCollection
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
                }
            };
            
            // Header
            var headerLabel = new Label
            {
                Text = "Query Parameters",
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            grid.Add(headerLabel, 0, 0);
            
            // Parameters table
            var paramsGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                RowSpacing = 5,
                ColumnSpacing = 5
            };
            
            // TODO: Add parameters from current request
            
            // Add button row
            var addButton = new Button
            {
                Text = "Add Parameter",
                HorizontalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 10, 0, 0)
            };
            addButton.Clicked += OnAddParameterClicked;
            
            var stackLayout = new VerticalStackLayout();
            stackLayout.Add(paramsGrid);
            stackLayout.Add(addButton);
            
            grid.Add(stackLayout, 0, 1);
            
            return grid;
        }
        
        private View CreateAuthTabContent()
        {
            // TODO: Implement authorization tab content
            return new Label { Text = "Authentication settings will be displayed here." };
        }
        
        private View CreateHeadersTabContent()
        {
            // TODO: Implement headers tab content
            return new Label { Text = "Headers will be displayed here." };
        }
        
        private View CreateBodyTabContent()
        {
            // TODO: Implement body tab content
            return new Label { Text = "Request body will be displayed here." };
        }
        
        private View CreateScriptTabContent()
        {
            // TODO: Implement pre-request script tab content
            return new Label { Text = "Pre-request script will be displayed here." };
        }
        
        private void UpdateResponseTabContent()
        {
            // Clear current content
            ResponseTabContent.Content = null;
            
            if (!HasResponse)
            {
                ResponseTabContent.Content = new Label
                {
                    Text = "Send a request to see the response",
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = Color.FromArgb("#999999")
                };
                return;
            }
            
            // Create content based on selected tab
            switch (CurrentResponseTab)
            {
                case "response":
                    ResponseTabContent.Content = CreateResponseBodyContent();
                    break;
                case "respHeaders":
                    ResponseTabContent.Content = CreateResponseHeadersContent();
                    break;
                case "tests":
                    ResponseTabContent.Content = CreateTestsContent();
                    break;
            }
        }
        
        private View CreateResponseBodyContent()
        {
            if (_currentResponse == null) return new Label { Text = "No response data" };
            
            // TODO: Format response body based on content type
            var editor = new Editor
            {
                Text = _currentResponse.Body ?? "No response body",
                IsReadOnly = true,
                AutoSize = EditorAutoSizeOption.TextChanges,
                BackgroundColor = Colors.Transparent
            };
            
            return editor;
        }
        
        private View CreateResponseHeadersContent()
        {
            if (_currentResponse == null || _currentResponse.Headers == null) 
                return new Label { Text = "No response headers" };
            
            // Create a grid to display headers
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }
                },
                RowSpacing = 5,
                ColumnSpacing = 10
            };
            
            // Add header rows
            int row = 0;
            foreach (var header in _currentResponse.Headers)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                grid.Add(new Label { Text = header.Key, LineBreakMode = LineBreakMode.WordWrap }, 0, row);
                grid.Add(new Label { Text = header.Value, LineBreakMode = LineBreakMode.WordWrap }, 1, row);
                
                row++;
            }
            
            return new ScrollView { Content = grid };
        }
        
        private View CreateTestsContent()
        {
            // TODO: Implement tests tab content
            return new Label { Text = "Test results will be displayed here." };
        }
        
        private async void OnAddCollectionClicked(object sender, EventArgs e)
        {
            string name = await DisplayPromptAsync("New Collection", "Enter collection name:", initialValue: "New Collection");
            if (string.IsNullOrWhiteSpace(name)) return;
            
            try
            {
                var newCollection = new Collection
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Description = "",
                    Requests = new ObservableCollection<ApiRequest>(),
                    Folders = new ObservableCollection<Collection.Folder>(),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                
                await _collectionService.AddCollectionAsync(newCollection);
                await LoadCollectionsAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to create collection: {ex.Message}", "OK");
            }
        }
        
        private void OnRequestSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is not TreeItem selectedItem)
            {
                return;
            }
            
            // Clear selection
            CollectionTreeView.SelectedItem = null;
            
            if (selectedItem.IsRequest)
            {
                // Load the request
                LoadRequest(selectedItem.Id);
            }
            else
            {
                // Toggle expansion state for collections and folders
                ToggleItemExpansion(selectedItem);
            }
        }
        
        private void ToggleItemExpansion(TreeItem item)
        {
            // Find the item in the collection
            int index = _collectionItems.IndexOf(item);
            if (index < 0) return;
            
            // Toggle expansion state
            item.IsExpanded = !item.IsExpanded;
            
            if (item.IsExpanded)
            {
                // Expand: add child items
                if (item.Type == TreeItemType.Collection)
                {
                    // Find the collection
                    var collection = _collectionService.GetCollectionsAsync().Result
                        .FirstOrDefault(c => c.Id == item.Id);
                    if (collection != null)
                    {
                        AddCollectionChildren(collection, item);
                    }
                }
                else if (item.Type == TreeItemType.Folder)
                {
                    // Find the folder's parent collection
                    var collection = _collectionService.GetCollectionsAsync().Result
                        .FirstOrDefault(c => c.Folders.Any(f => f.Id == item.Id));
                    if (collection != null)
                    {
                        var folder = collection.Folders.FirstOrDefault(f => f.Id == item.Id);
                        if (folder != null)
                        {
                            // Add folder's requests
                            int insertIndex = index + 1;
                            foreach (var request in folder.Requests)
                            {
                                var requestItem = new TreeItem
                                {
                                    Id = request.Id,
                                    Name = request.Name,
                                    Type = TreeItemType.Request,
                                    Level = item.Level + 1,
                                    Method = request.Method.ToString(),
                                    IsRequest = true,
                                    ParentId = item.Id
                                };
                                _collectionItems.Insert(insertIndex++, requestItem);
                            }
                        }
                    }
                }
            }
            else
            {
                // Collapse: remove all child items
                List<TreeItem> itemsToRemove = new List<TreeItem>();
                for (int i = index + 1; i < _collectionItems.Count; i++)
                {
                    var currentItem = _collectionItems[i];
                    if (currentItem.Level <= item.Level)
                    {
                        break;
                    }
                    
                    itemsToRemove.Add(currentItem);
                }
                
                foreach (var itemToRemove in itemsToRemove)
                {
                    _collectionItems.Remove(itemToRemove);
                }
            }
        }
        
        private async void LoadRequest(string requestId)
        {
            // Find the request in collections
            var collections = await _collectionService.GetCollectionsAsync();
            
            foreach (var collection in collections)
            {
                // Check collection-level requests
                var request = collection.Requests.FirstOrDefault(r => r.Id == requestId);
                if (request != null)
                {
                    SetCurrentRequest(request);
                    return;
                }
                
                // Check requests in folders
                foreach (var folder in collection.Folders)
                {
                    request = folder.Requests.FirstOrDefault(r => r.Id == requestId);
                    if (request != null)
                    {
                        SetCurrentRequest(request);
                        return;
                    }
                }
            }
        }
        
        private void SetCurrentRequest(ApiRequest request)
        {
            _currentRequest = request;
            
            // Update UI
            SelectedMethod = request.Method.ToString();
            RequestUrl = request.Url;
            
            // Reset response
            _currentResponse = null;
            HasResponse = false;
            
            // Update tabs
            CurrentRequestTab = "params";
            CurrentResponseTab = "response";
        }
        
        private async void OnSendRequestClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(RequestUrl))
            {
                await DisplayAlert("Error", "Please enter a URL", "OK");
                return;
            }
            
            try
            {
                LoadingOverlay.IsVisible = true;
                
                // Prepare request
                var apiRequest = new ApiRequest
                {
                    Method = (ApiHttpMethod)Enum.Parse(typeof(ApiHttpMethod), SelectedMethod),
                    Url = RequestUrl,
                    Headers = _currentRequest?.Headers ?? new ObservableCollection<Models.KeyValuePair>(),
                    QueryParameters = _currentRequest?.QueryParameters ?? new ObservableCollection<Models.KeyValuePair>(),
                    Body = _currentRequest?.Body ?? new RequestBody()
                };
                
                // Send request
                var startTime = DateTime.Now;
                var response = await _apiRequestService.SendRequestAsync(apiRequest);
                var endTime = DateTime.Now;
                
                // Update response data
                _currentResponse = response;
                HasResponse = true;
                ResponseStatusCode = (int)response.StatusCode;
                ResponseStatusText = GetStatusText(response.StatusCode);
                ResponseTime = (long)(endTime - startTime).TotalMilliseconds;
                IsResponseSuccess = response.StatusCode >= HttpStatusCode.OK && response.StatusCode < HttpStatusCode.BadRequest;
                
                // Update response status UI
                UpdateResponseStatusUI(response);
                
                // Add to history
                await _requestHistoryService.AddToHistoryAsync(apiRequest, response);
                
                // Update response tab
                UpdateResponseTabContent();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to send request: {ex.Message}", "OK");
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
            }
        }
        
        private void OnTabClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string tabName)
            {
                CurrentRequestTab = tabName;
            }
        }
        
        private void OnResponseTabClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string tabName)
            {
                CurrentResponseTab = tabName;
            }
        }
        
        private void OnMethodChanged(object sender, EventArgs e)
        {
            if (MethodPicker.SelectedItem is string method)
            {
                SelectedMethod = method;
            }
        }
        
        private void OnUrlTextChanged(object sender, TextChangedEventArgs e)
        {
            RequestUrl = e.NewTextValue;
        }
        
        private void OnAddParameterClicked(object sender, EventArgs e)
        {
            // Add a new query parameter
            if (_currentRequest.QueryParameters == null)
            {
                _currentRequest.QueryParameters = new ObservableCollection<Models.KeyValuePair>();
            }
            
            _currentRequest.QueryParameters.Add(new Models.KeyValuePair { Key = "", Value = "" });
            
            // Update UI
            UpdateRequestTabContent();
        }
        
        private void UpdateResponseStatusUI(ApiResponse response)
        {
            // Update the response status UI elements
            if (response != null)
            {
                int statusCodeValue = (int)response.StatusCode;
                ResponseStatusCodeLabel.Text = statusCodeValue.ToString();
                
                // Use extension method or direct access to get status text
                // If StatusText property doesn't exist in ApiResponse, we create our own logic
                string statusText = GetStatusText(response.StatusCode);
                ResponseStatusTextLabel.Text = statusText;
                
                // Set color based on status code range
                if (statusCodeValue >= 200 && statusCodeValue < 300)
                {
                    ResponseStatusContainer.BackgroundColor = Colors.Green;
                }
                else if (statusCodeValue >= 300 && statusCodeValue < 400)
                {
                    ResponseStatusContainer.BackgroundColor = Colors.Blue;
                }
                else if (statusCodeValue >= 400 && statusCodeValue < 500)
                {
                    ResponseStatusContainer.BackgroundColor = Colors.Orange;
                }
                else if (statusCodeValue >= 500)
                {
                    ResponseStatusContainer.BackgroundColor = Colors.Red;
                }
                else
                {
                    ResponseStatusContainer.BackgroundColor = Colors.Gray;
                }
            }
        }

        private string GetStatusText(HttpStatusCode statusCode)
        {
            switch (statusCode)
            {
                case HttpStatusCode.Continue: return "Continue";
                case HttpStatusCode.SwitchingProtocols: return "Switching Protocols";
                case HttpStatusCode.Processing: return "Processing";
                case HttpStatusCode.EarlyHints: return "Early Hints";
                case HttpStatusCode.OK: return "OK";
                case HttpStatusCode.Created: return "Created";
                case HttpStatusCode.Accepted: return "Accepted";
                case HttpStatusCode.NonAuthoritativeInformation: return "Non-Authoritative Information";
                case HttpStatusCode.NoContent: return "No Content";
                case HttpStatusCode.ResetContent: return "Reset Content";
                case HttpStatusCode.PartialContent: return "Partial Content";
                case HttpStatusCode.MultiStatus: return "Multi-Status";
                case HttpStatusCode.AlreadyReported: return "Already Reported";
                case HttpStatusCode.IMUsed: return "IM Used";
                case HttpStatusCode.Ambiguous: return "Multiple Choices";
                case HttpStatusCode.Moved: return "Moved Permanently";
                case HttpStatusCode.Found: return "Found";
                case HttpStatusCode.RedirectMethod: return "See Other";
                case HttpStatusCode.NotModified: return "Not Modified";
                case HttpStatusCode.UseProxy: return "Use Proxy";
                case HttpStatusCode.Unused: return "Unused";
                case HttpStatusCode.RedirectKeepVerb: return "Temporary Redirect";
                case HttpStatusCode.PermanentRedirect: return "Permanent Redirect";
                case HttpStatusCode.BadRequest: return "Bad Request";
                case HttpStatusCode.Unauthorized: return "Unauthorized";
                case HttpStatusCode.PaymentRequired: return "Payment Required";
                case HttpStatusCode.Forbidden: return "Forbidden";
                case HttpStatusCode.NotFound: return "Not Found";
                case HttpStatusCode.MethodNotAllowed: return "Method Not Allowed";
                case HttpStatusCode.NotAcceptable: return "Not Acceptable";
                case HttpStatusCode.ProxyAuthenticationRequired: return "Proxy Authentication Required";
                case HttpStatusCode.RequestTimeout: return "Request Timeout";
                case HttpStatusCode.Conflict: return "Conflict";
                case HttpStatusCode.Gone: return "Gone";
                case HttpStatusCode.LengthRequired: return "Length Required";
                case HttpStatusCode.PreconditionFailed: return "Precondition Failed";
                case HttpStatusCode.RequestEntityTooLarge: return "Request Entity Too Large";
                case HttpStatusCode.RequestUriTooLong: return "Request URI Too Long";
                case HttpStatusCode.UnsupportedMediaType: return "Unsupported Media Type";
                case HttpStatusCode.RequestedRangeNotSatisfiable: return "Requested Range Not Satisfiable";
                case HttpStatusCode.ExpectationFailed: return "Expectation Failed";
                case HttpStatusCode.MisdirectedRequest: return "Misdirected Request";
                case HttpStatusCode.UnprocessableEntity: return "Unprocessable Entity";
                case HttpStatusCode.Locked: return "Locked";
                case HttpStatusCode.FailedDependency: return "Failed Dependency";
                case HttpStatusCode.UpgradeRequired: return "Upgrade Required";
                case HttpStatusCode.PreconditionRequired: return "Precondition Required";
                case HttpStatusCode.TooManyRequests: return "Too Many Requests";
                case HttpStatusCode.RequestHeaderFieldsTooLarge: return "Request Header Fields Too Large";
                case HttpStatusCode.UnavailableForLegalReasons: return "Unavailable For Legal Reasons";
                case HttpStatusCode.InternalServerError: return "Internal Server Error";
                case HttpStatusCode.NotImplemented: return "Not Implemented";
                case HttpStatusCode.BadGateway: return "Bad Gateway";
                case HttpStatusCode.ServiceUnavailable: return "Service Unavailable";
                case HttpStatusCode.GatewayTimeout: return "Gateway Timeout";
                case HttpStatusCode.HttpVersionNotSupported: return "HTTP Version Not Supported";
                case HttpStatusCode.VariantAlsoNegotiates: return "Variant Also Negotiates";
                case HttpStatusCode.InsufficientStorage: return "Insufficient Storage";
                case HttpStatusCode.LoopDetected: return "Loop Detected";
                case HttpStatusCode.NotExtended: return "Not Extended";
                case HttpStatusCode.NetworkAuthenticationRequired: return "Network Authentication Required";
                default: return $"Unknown Status ({(int)statusCode})";
            }
        }
        
        // Tree item class for collection view
        public class TreeItem : BindableObject
        {
            private string _id;
            private string _name;
            private TreeItemType _type;
            private int _level;
            private string _method;
            private bool _isExpanded;
            private bool _isRequest;
            private string _parentId;

            public string Id
            {
                get => _id;
                set
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }

            public string Name
            {
                get => _name;
                set
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }

            public TreeItemType Type
            {
                get => _type;
                set
                {
                    _type = value;
                    OnPropertyChanged();
                }
            }

            public int Level
            {
                get => _level;
                set
                {
                    _level = value;
                    OnPropertyChanged();
                }
            }

            public string Method
            {
                get => _method;
                set
                {
                    _method = value;
                    OnPropertyChanged();
                }
            }

            public bool IsExpanded
            {
                get => _isExpanded;
                set
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }

            public bool IsRequest
            {
                get => _isRequest;
                set
                {
                    _isRequest = value;
                    OnPropertyChanged();
                }
            }

            public string ParentId
            {
                get => _parentId;
                set
                {
                    _parentId = value;
                    OnPropertyChanged();
                }
            }
        }

        public enum TreeItemType
        {
            Collection,
            Folder,
            Request
        }
    }
}