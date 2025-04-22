using System.Text.Json;
using System.Collections.ObjectModel;
using Exman.Models;

namespace Exman.Services
{
    /// <summary>
    /// Service for managing collections of API requests with local file storage
    /// </summary>
    public class CollectionService : ICollectionService
    {
        private readonly string _dataDirectory;
        private readonly string _collectionDirectory;
        private readonly JsonSerializerOptions _jsonOptions;
        
        public CollectionService()
        {
            _dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "Exman");
                
            _collectionDirectory = Path.Combine(_dataDirectory, "Collections");
            
            // Create directories if they don't exist
            Directory.CreateDirectory(_dataDirectory);
            Directory.CreateDirectory(_collectionDirectory);
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }
        
        public async Task<IEnumerable<Collection>> GetCollectionsAsync()
        {
            var collections = new List<Collection>();
            
            // Get all collection files
            var files = Directory.GetFiles(_collectionDirectory, "*.json");
            
            foreach (var file in files)
            {
                try
                {
                    using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var streamReader = new StreamReader(fileStream))
                    {
                        var json = streamReader.ReadToEnd();
                        var collection = JsonSerializer.Deserialize<Collection>(json, _jsonOptions);
                        
                        if (collection != null)
                        {
                            collections.Add(collection);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading collection: {ex.Message}");
                }
            }
            
            return collections;
        }
        
        public async Task<Collection?> GetCollectionAsync(string id)
        {
            var filePath = GetCollectionFilePath(id);
            
            if (!File.Exists(filePath))
            {
                return null;
            }
            
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var streamReader = new StreamReader(fileStream))
                {
                    var json = streamReader.ReadToEnd();
                    return JsonSerializer.Deserialize<Collection>(json, _jsonOptions);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading collection: {ex.Message}");
                return null;
            }
        }
        
        public async Task<Collection> CreateCollectionAsync(Collection collection)
        {
            // Ensure the collection has a valid ID
            if (string.IsNullOrEmpty(collection.Id))
            {
                collection.Id = Guid.NewGuid().ToString();
            }
            
            collection.CreatedAt = DateTime.Now;
            collection.UpdatedAt = DateTime.Now;
            
            await SaveCollectionToFileAsync(collection);
            
            return collection;
        }
        
        public async Task<bool> UpdateCollectionAsync(Collection collection)
        {
            var filePath = GetCollectionFilePath(collection.Id);
            
            if (!File.Exists(filePath))
            {
                return false;
            }
            
            collection.UpdatedAt = DateTime.Now;
            
            try
            {
                await SaveCollectionToFileAsync(collection);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating collection: {ex.Message}");
                return false;
            }
        }
        
        public Task<bool> DeleteCollectionAsync(string id)
        {
            var filePath = GetCollectionFilePath(id);
            
            if (!File.Exists(filePath))
            {
                return Task.FromResult(false);
            }
            
            try
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting collection: {ex.Message}");
                return Task.FromResult(false);
            }
        }
        
        public async Task<bool> SaveRequestAsync(string collectionId, ApiRequest request)
        {
            var collection = await GetCollectionAsync(collectionId);
            
            if (collection == null)
            {
                return false;
            }
            
            // Generate new ID if needed
            if (string.IsNullOrEmpty(request.Id))
            {
                request.Id = Guid.NewGuid().ToString();
            }
            
            // Add the request to the collection
            collection.Requests.Add(request);
            collection.UpdatedAt = DateTime.Now;
            
            // Save the updated collection
            return await UpdateCollectionAsync(collection);
        }
        
        public async Task<bool> UpdateRequestAsync(string collectionId, ApiRequest request)
        {
            var collection = await GetCollectionAsync(collectionId);
            
            if (collection == null)
            {
                return false;
            }
            
            // Find the existing request
            var existingRequest = collection.Requests.FirstOrDefault(r => r.Id == request.Id);
            
            if (existingRequest == null)
            {
                return false;
            }
            
            // Replace the existing request
            int index = collection.Requests.IndexOf(existingRequest);
            collection.Requests[index] = request;
            collection.UpdatedAt = DateTime.Now;
            
            // Save the updated collection
            return await UpdateCollectionAsync(collection);
        }
        
        public async Task<bool> DeleteRequestAsync(string collectionId, string requestId)
        {
            var collection = await GetCollectionAsync(collectionId);
            
            if (collection == null)
            {
                return false;
            }
            
            // Find the request
            var request = collection.Requests.FirstOrDefault(r => r.Id == requestId);
            
            if (request == null)
            {
                return false;
            }
            
            // Remove the request
            collection.Requests.Remove(request);
            collection.UpdatedAt = DateTime.Now;
            
            // Save the updated collection
            return await UpdateCollectionAsync(collection);
        }
        
        public async Task<bool> ExportCollectionsAsync(string filePath, IEnumerable<string> collectionIds)
        {
            var collections = new ObservableCollection<Collection>();
            
            foreach (var id in collectionIds)
            {
                var collection = await GetCollectionAsync(id);
                if (collection != null)
                {
                    collections.Add(collection);
                }
            }
            
            try
            {
                var json = JsonSerializer.Serialize(collections, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting collections: {ex.Message}");
                return false;
            }
        }
        
        public async Task<IEnumerable<Collection>> ImportCollectionsAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new ObservableCollection<Collection>();
            }
            
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var importedCollections = JsonSerializer.Deserialize<ObservableCollection<Collection>>(json, _jsonOptions);
                
                if (importedCollections == null)
                {
                    return new ObservableCollection<Collection>();
                }
                
                // Save each imported collection
                foreach (var collection in importedCollections)
                {
                    // Generate new ID to avoid conflicts
                    collection.Id = Guid.NewGuid().ToString();
                    collection.UpdatedAt = DateTime.Now;
                    
                    await SaveCollectionToFileAsync(collection);
                }
                
                return importedCollections;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing collections: {ex.Message}");
                return new ObservableCollection<Collection>();
            }
        }
        
        /// <summary>
        /// Imports a Postman collection from a JSON string
        /// </summary>
        public async Task<Collection?> ImportPostmanCollectionFromJsonAsync(string json)
        {
            try
            {
                // Use the PostmanCollectionConverter to convert the JSON to an Exman Collection
                var collection = PostmanCollectionConverter.ConvertFromJson(json);
                
                if (collection == null)
                {
                    return null;
                }
                
                // Save the collection to file
                collection.Id = Guid.NewGuid().ToString();
                collection.CreatedAt = DateTime.Now;
                collection.UpdatedAt = DateTime.Now;
                
                await SaveCollectionToFileAsync(collection);
                
                return collection;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing Postman collection from JSON: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Imports a Postman collection from a file
        /// </summary>
        public async Task<Collection?> ImportPostmanCollectionAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }
            
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var streamReader = new StreamReader(fileStream))
                {
                    string json = streamReader.ReadToEnd();
                    return await ImportPostmanCollectionFromJsonAsync(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing Postman collection from file: {ex.Message}");
                return null;
            }
        }
        
        public async Task<bool> AddRequestToCollectionAsync(string collectionId, ApiRequest request)
        {
            var collection = await GetCollectionAsync(collectionId);
            
            if (collection == null)
            {
                return false;
            }
            
            // Ensure the request has an ID
            if (string.IsNullOrEmpty(request.Id))
            {
                request.Id = Guid.NewGuid().ToString();
            }
            
            // Make sure the collection has a requests list
            if (collection.Requests == null)
            {
                collection.Requests = new ObservableCollection<ApiRequest>();
            }
            
            // Add the request to the collection
            collection.Requests.Add(request);
            collection.UpdatedAt = DateTime.Now;
            
            // Save the collection
            try
            {
                await SaveCollectionToFileAsync(collection);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding request to collection: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Adds a new collection to the store
        /// </summary>
        public async Task<bool> AddCollectionAsync(Collection collection)
        {
            // Ensure the collection has a valid ID
            if (string.IsNullOrEmpty(collection.Id))
            {
                collection.Id = Guid.NewGuid().ToString();
            }
            
            collection.CreatedAt = DateTime.Now;
            collection.UpdatedAt = DateTime.Now;
            
            try
            {
                await SaveCollectionToFileAsync(collection);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding collection: {ex.Message}");
                return false;
            }
        }
        
        private string GetCollectionFilePath(string id)
        {
            return Path.Combine(_collectionDirectory, $"{id}.json");
        }
        
        private async Task SaveCollectionToFileAsync(Collection collection)
        {
            var filePath = GetCollectionFilePath(collection.Id);
            var json = JsonSerializer.Serialize(collection, _jsonOptions);
            
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    await streamWriter.WriteAsync(json);
                    await streamWriter.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving collection to file: {ex.Message}");
                throw;
            }
        }
    }
}