using System.Collections.ObjectModel;
using System.Text.Json;
using Exman.Models;

namespace Exman.Services
{
    /// <summary>
    /// Service for managing request history with local file storage
    /// </summary>
    public class RequestHistoryService : IRequestHistoryService
    {
        private readonly string _dataDirectory;
        private readonly string _historyFilePath;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ICollectionService _collectionService;
        private const int MaxHistoryItems = 100;
        
        public RequestHistoryService(ICollectionService collectionService)
        {
            _collectionService = collectionService;
            _dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "Exman");
                
            Directory.CreateDirectory(_dataDirectory);
            _historyFilePath = Path.Combine(_dataDirectory, "RequestHistory.json");
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }
        
        public async Task<IEnumerable<ApiRequest>> GetRecentRequestsAsync(int limit = 20)
        {
            var historyItems = await LoadHistoryItemsAsync();
            return historyItems
                .OrderByDescending(h => h.Timestamp)
                .Take(Math.Min(limit, historyItems.Count))
                .Select(h => h.Request);
        }
        
        public async Task<IEnumerable<RequestHistoryItem>> GetHistoryAsync()
        {
            var historyItems = await LoadHistoryItemsAsync();
            return historyItems.OrderByDescending(h => h.Timestamp);
        }
        
        public async Task AddToHistoryAsync(ApiRequest request)
        {
            // Create a minimal response for requests without responses
            var response = new ApiResponse
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Body = "No response data"
            };
            
            await AddToHistoryAsync(request, response);
        }
        
        public async Task AddToHistoryAsync(ApiRequest request, ApiResponse response)
        {
            // Create a new history item
            var historyItem = new RequestHistoryItem
            {
                Id = Guid.NewGuid().ToString(),
                Request = CloneRequest(request),
                Response = response,
                Timestamp = DateTime.Now
            };
            
            var history = await LoadHistoryItemsAsync();
            
            // Remove existing request with the same URL and method to avoid duplicates
            var existingItem = history.FirstOrDefault(h => 
                h.Request.Url == request.Url && h.Request.Method == request.Method);
                
            if (existingItem != null)
            {
                history.Remove(existingItem);
            }
            
            // Add the new request to the beginning of the list
            history.Insert(0, historyItem);
            
            // Keep only the most recent requests
            while (history.Count > MaxHistoryItems)
            {
                history.RemoveAt(history.Count - 1);
            }
            
            await SaveHistoryAsync(history);
        }
        
        public async Task RemoveFromHistoryAsync(string requestId)
        {
            if (string.IsNullOrEmpty(requestId))
                return;
                
            var history = await LoadHistoryItemsAsync();
            var itemToRemove = history.FirstOrDefault(h => h.Id == requestId);
            
            if (itemToRemove != null)
            {
                history.Remove(itemToRemove);
                await SaveHistoryAsync(history);
            }
        }
        
        public async Task ClearHistoryAsync()
        {
            await SaveHistoryAsync(new List<RequestHistoryItem>());
        }
        
        public async Task<bool> SaveToCollectionAsync(ApiRequest request, string collectionId)
        {
            // Create a copy of the request for the collection
            var requestCopy = CloneRequest(request);
            
            // Set a name if it's empty
            if (string.IsNullOrEmpty(requestCopy.Name))
            {
                requestCopy.Name = $"{requestCopy.Method} {requestCopy.Url}";
            }
            
            return await _collectionService.SaveRequestAsync(collectionId, requestCopy);
        }
        
        public async Task<ApiRequest?> GetRequestByIdAsync(string requestId)
        {
            if (string.IsNullOrEmpty(requestId))
                return null;
                
            var history = await LoadHistoryItemsAsync();
            var item = history.FirstOrDefault(h => h.Id == requestId);
            return item?.Request;
        }
        
        private async Task<List<RequestHistoryItem>> LoadHistoryItemsAsync()
        {
            if (!File.Exists(_historyFilePath))
                return new List<RequestHistoryItem>();
                
            try
            {
                using (var fileStream = new FileStream(_historyFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var streamReader = new StreamReader(fileStream))
                {
                    var json = await streamReader.ReadToEndAsync();
                    var result = JsonSerializer.Deserialize<List<RequestHistoryItem>>(json, _jsonOptions);
                    return result ?? new List<RequestHistoryItem>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading request history: {ex.Message}");
                return new List<RequestHistoryItem>();
            }
        }
        
        private async Task SaveHistoryAsync(List<RequestHistoryItem> history)
        {
            try
            {
                var json = JsonSerializer.Serialize(history, _jsonOptions);
                await File.WriteAllTextAsync(_historyFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving request history: {ex.Message}");
            }
        }
        
        private ApiRequest CloneRequest(ApiRequest source)
        {
            var clone = new ApiRequest
            {
                Id = string.IsNullOrEmpty(source.Id) ? Guid.NewGuid().ToString() : source.Id,
                Name = source.Name,
                Description = source.Description,
                Url = source.Url,
                Method = source.Method,
                FollowRedirects = source.FollowRedirects,
                VerifySsl = source.VerifySsl,
                LastUsed = source.LastUsed
            };
            
            // Clone collections
            foreach (var param in source.QueryParameters)
            {
                clone.QueryParameters.Add(new Models.KeyValuePair { Key = param.Key, Value = param.Value, Enabled = param.Enabled });
            }
            
            foreach (var param in source.PathVariables)
            {
                clone.PathVariables.Add(new Models.KeyValuePair { Key = param.Key, Value = param.Value, Enabled = param.Enabled });
            }
            
            foreach (var header in source.Headers)
            {
                clone.Headers.Add(new Models.KeyValuePair { Key = header.Key, Value = header.Value, Enabled = header.Enabled });
            }
            
            // Clone authentication
            clone.Authentication = new Authentication
            {
                Type = source.Authentication.Type,
                Token = source.Authentication.Token,
                Username = source.Authentication.Username,
                Password = source.Authentication.Password,
                ApiKey = source.Authentication.ApiKey,
                ApiKeyName = source.Authentication.ApiKeyName,
                AddToHeader = source.Authentication.AddToHeader
            };
            
            // Clone body
            clone.Body = new RequestBody
            {
                Type = source.Body.Type,
                ContentType = source.Body.ContentType,
                RawContent = source.Body.RawContent,
                GraphQLQuery = source.Body.GraphQLQuery,
                GraphQLVariables = source.Body.GraphQLVariables,
                BinaryData = source.Body.BinaryData,
                BinaryFileName = source.Body.BinaryFileName
            };
            
            foreach (var item in source.Body.FormData)
            {
                clone.Body.FormData.Add(new Models.KeyValuePair { Key = item.Key, Value = item.Value, Enabled = item.Enabled });
            }
            
            foreach (var item in source.Body.UrlEncodedData)
            {
                clone.Body.UrlEncodedData.Add(new Models.KeyValuePair { Key = item.Key, Value = item.Value, Enabled = item.Enabled });
            }
            
            // Clone proxy settings
            clone.Proxy = new ProxySettings
            {
                Enabled = source.Proxy.Enabled,
                Type = source.Proxy.Type,
                Host = source.Proxy.Host,
                Port = source.Proxy.Port,
                UseAuthentication = source.Proxy.UseAuthentication,
                Username = source.Proxy.Username,
                Password = source.Proxy.Password
            };
            
            return clone;
        }
    }
}