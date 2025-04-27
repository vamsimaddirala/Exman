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
            if (!File.Exists(_historyFilePath))
                return new List<ApiRequest>();
            
            try 
            {
                using (var fileStream = new FileStream(_historyFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var streamReader = new StreamReader(fileStream))
                {
                    var json = streamReader.ReadToEnd();
                    var allRequests = JsonSerializer.Deserialize<List<ApiRequest>>(json, _jsonOptions) ?? new List<ApiRequest>();
                    
                    // Return most recent requests first, limited by the count
                    return allRequests
                        .OrderByDescending(r => r.LastUsed)
                        .Take(Math.Min(limit, allRequests.Count));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading request history: {ex.Message}");
                return new List<ApiRequest>();
            }
        }
        
        public async Task<IEnumerable<ApiRequest>> GetHistoryAsync()
        {
            if (!File.Exists(_historyFilePath))
                return new List<ApiRequest>();
            
            try 
            {
                using (var fileStream = new FileStream(_historyFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var streamReader = new StreamReader(fileStream))
                {
                    var json = streamReader.ReadToEnd();
                    var allRequests = JsonSerializer.Deserialize<List<ApiRequest>>(json, _jsonOptions) ?? new List<ApiRequest>();
                    
                    // Return most recent requests first
                    return allRequests.OrderByDescending(r => r.LastUsed);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading request history: {ex.Message}");
                return new List<ApiRequest>();
            }
        }
        
        public async Task<IEnumerable<ApiResponse>> GetRequestHistoryAsync()
        {
            if (!File.Exists(_historyFilePath))
            {
                return new List<ApiResponse>();
            }
            
            try
            {
                using (var fileStream = new FileStream(_historyFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var streamReader = new StreamReader(fileStream))
                {
                    var json = streamReader.ReadToEnd();
                    return JsonSerializer.Deserialize<List<ApiResponse>>(json, _jsonOptions) ?? new List<ApiResponse>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading request history: {ex.Message}");
                return new List<ApiResponse>();
            }
        }
        
        public async Task AddToHistoryAsync(ApiRequest request)
        {
            // Set last used timestamp
            request.LastUsed = DateTime.Now;
            
            // Create a copy of the request to store in history
            var requestCopy = CloneRequest(request);
            
            var history = await LoadHistoryAsync();
            
            // Remove existing request with the same URL and method to avoid duplicates
            var existingRequest = history.FirstOrDefault(r => 
                r.Url == requestCopy.Url && r.Method == requestCopy.Method);
                
            if (existingRequest != null)
            {
                history.Remove(existingRequest);
            }
            
            // Add the new request to the beginning of the list
            history.Insert(0, requestCopy);
            
            // Keep only the most recent requests
            while (history.Count > MaxHistoryItems)
            {
                history.RemoveAt(history.Count - 1);
            }
            
            await SaveHistoryAsync(history);
        }
        
        public async Task AddToHistoryAsync(ApiRequest request, ApiResponse response)
        {
            // Simply call the existing method for now
            // In a future enhancement, we could store the response data too
            await AddToHistoryAsync(request);
        }
        
        public async Task RemoveFromHistoryAsync(string requestId)
        {
            if (string.IsNullOrEmpty(requestId))
                return;
                
            var history = await LoadHistoryAsync();
            var requestToRemove = history.FirstOrDefault(r => r.Id == requestId);
            
            if (requestToRemove != null)
            {
                history.Remove(requestToRemove);
                await SaveHistoryAsync(history);
            }
        }
        
        public async Task ClearHistoryAsync()
        {
            await SaveHistoryAsync(new List<ApiRequest>());
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
                
            var history = await LoadHistoryAsync();
            return history.FirstOrDefault(r => r.Id == requestId);
        }
        
        private async Task<List<ApiRequest>> LoadHistoryAsync()
        {
            if (!File.Exists(_historyFilePath))
                return new List<ApiRequest>();
                
            try
            {
                using (var fileStream = new FileStream(_historyFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var streamReader = new StreamReader(fileStream))
                {
                    var json = streamReader.ReadToEnd();
                    return JsonSerializer.Deserialize<List<ApiRequest>>(json, _jsonOptions) ?? new List<ApiRequest>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading request history: {ex.Message}");
                return new List<ApiRequest>();
            }
        }
        
        private async Task SaveHistoryAsync(List<ApiRequest> history)
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