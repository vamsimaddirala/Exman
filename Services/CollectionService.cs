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
        /// Saves an API request to a specific folder in a collection
        /// </summary>
        public async Task<bool> SaveRequestToFolderAsync(string collectionId, string folderId, ApiRequest request)
        {
            // First try to get the collection directly (if collectionId is a top-level collection)
            var collection = await GetCollectionAsync(collectionId);
            
            // If collection is null, the collectionId might actually be a folder ID
            // We need to find which top-level collection contains this folder
            if (collection == null)
            {
                // Get all collections and search for the folder ID
                var allCollections = await GetCollectionsAsync();
                string rootCollectionId = null;
                
                foreach (var rootCollection in allCollections)
                {
                    if (rootCollection.Id == collectionId)
                    {
                        // It's a top-level collection after all, but couldn't be loaded earlier
                        collection = rootCollection;
                        break;
                    }
                    
                    // Search in the folder hierarchy
                    if (FindCollectionWithFolder(rootCollection, collectionId, out string folderPath))
                    {
                        rootCollectionId = rootCollection.Id;
                        collection = rootCollection;
                        // Since the collectionId is actually a folder ID, we need to use it as folderId
                        folderId = collectionId;
                        break;
                    }
                }
                
                // If we still don't have a collection, we can't save
                if (collection == null)
                {
                    return false;
                }
            }
            
            // Make sure the collection exists before proceeding
            if (collection == null)
            {
                return false;
            }
            
            // Generate new ID if needed
            if (string.IsNullOrEmpty(request.Id))
            {
                request.Id = Guid.NewGuid().ToString();
            }
            
            // Set collection ID in the request to the actual collection ID (not folder ID)
            request.CollectionId = collection.Id;
            
            // If no folder specified, operate at root level
            if (string.IsNullOrEmpty(folderId))
            {
                // Check if request with same ID already exists at root level
                var existingRequest = collection.Requests.FirstOrDefault(r => r.Id == request.Id);
                if (existingRequest != null)
                {
                    // Update existing request
                    int index = collection.Requests.IndexOf(existingRequest);
                    collection.Requests[index] = request;
                }
                else
                {
                    // Add new request
                    collection.Requests.Add(request);
                }
            }
            else
            {
                // Find the target folder in the collection
                bool found = false;
                Collection targetFolder = FindFolder(collection, folderId, ref found);
                
                if (found && targetFolder != null)
                {
                    // Check if request with same ID already exists in the folder
                    var existingRequest = targetFolder.Requests.FirstOrDefault(r => r.Id == request.Id);
                    if (existingRequest != null)
                    {
                        // Update existing request
                        int index = targetFolder.Requests.IndexOf(existingRequest);
                        targetFolder.Requests[index] = request;
                    }
                    else
                    {
                        // Add new request to the target folder
                        targetFolder.Requests.Add(request);
                    }
                }
                else
                {
                    // Fallback to root if folder not found
                    var existingRequest = collection.Requests.FirstOrDefault(r => r.Id == request.Id);
                    if (existingRequest != null)
                    {
                        // Update existing request
                        int index = collection.Requests.IndexOf(existingRequest);
                        collection.Requests[index] = request;
                    }
                    else
                    {
                        // Add new request
                        collection.Requests.Add(request);
                    }
                }
            }
            
            collection.UpdatedAt = DateTime.Now;
            
            // Save the updated collection
            return await UpdateCollectionAsync(collection);
        }
        
        /// <summary>
        /// Deletes an API request from a collection or any nested folder
        /// </summary>
        /// <param name="collectionId">ID of the collection or folder containing the request</param>
        /// <param name="requestId">ID of the request to delete</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> DeleteRequestFromFolderAsync(string collectionId, string requestId)
        {
            // First try to get the collection directly (if collectionId is a top-level collection)
            var collection = await GetCollectionAsync(collectionId);
            
            // If collection is null, the collectionId might actually be a folder ID
            // We need to find which top-level collection contains this folder
            if (collection == null)
            {
                // Get all collections and search for the folder ID
                var allCollections = await GetCollectionsAsync();
                
                foreach (var rootCollection in allCollections)
                {
                    if (rootCollection.Id == collectionId)
                    {
                        // It's a top-level collection after all, but couldn't be loaded earlier
                        collection = rootCollection;
                        break;
                    }
                    
                    // Search in the folder hierarchy
                    if (FindCollectionWithFolder(rootCollection, collectionId, out string folderPath))
                    {
                        // Found the collection containing the folder
                        // Now we need to find and remove the request from the specific folder
                        bool found = false;
                        bool deleted = DeleteRequestFromFolder(rootCollection, collectionId, requestId, ref found);
                        
                        if (deleted)
                        {
                            // Update the collection
                            rootCollection.UpdatedAt = DateTime.Now;
                            return await UpdateCollectionAsync(rootCollection);
                        }
                    }
                }
                
                // If we still don't have a collection, we can't delete
                if (collection == null)
                {
                    return false;
                }
            }
            
            // At this point, we have a top-level collection
            // First check if the request is in the root level of the collection
            var requestAtRoot = collection.Requests.FirstOrDefault(r => r.Id == requestId);
            if (requestAtRoot != null)
            {
                // Remove the request from root level
                collection.Requests.Remove(requestAtRoot);
                collection.UpdatedAt = DateTime.Now;
                return await UpdateCollectionAsync(collection);
            }
            
            // If not found at root, search in folders
            bool requestFound = false;
            bool requestDeleted = DeleteRequestFromFolders(collection.Folders, requestId, ref requestFound);
            
            if (requestDeleted)
            {
                collection.UpdatedAt = DateTime.Now;
                return await UpdateCollectionAsync(collection);
            }
            
            return false;
        }
        
        /// <summary>
        /// Helper method to delete a request from a specific folder
        /// </summary>
        private bool DeleteRequestFromFolder(Collection collection, string folderId, string requestId, ref bool found)
        {
            // Check if this is the target folder
            if (collection.Id == folderId)
            {
                found = true;
                var request = collection.Requests.FirstOrDefault(r => r.Id == requestId);
                if (request != null)
                {
                    return collection.Requests.Remove(request);
                }
                return false;
            }
            
            // If not, search in child folders
            foreach (var folder in collection.Folders)
            {
                bool result = DeleteRequestFromFolder(folder, folderId, requestId, ref found);
                if (found)
                {
                    return result;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Helper method to recursively search for and delete a request in folder hierarchy
        /// </summary>
        private bool DeleteRequestFromFolders(ObservableCollection<Collection> folders, string requestId, ref bool found)
        {
            if (folders == null || !folders.Any())
            {
                return false;
            }
            
            foreach (var folder in folders)
            {
                // Check if the request is in this folder
                var request = folder.Requests.FirstOrDefault(r => r.Id == requestId);
                if (request != null)
                {
                    found = true;
                    return folder.Requests.Remove(request);
                }
                
                // If not, check nested folders
                bool deleted = DeleteRequestFromFolders(folder.Folders, requestId, ref found);
                if (found)
                {
                    return deleted;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Helper method to find a folder by ID in a collection's hierarchy
        /// </summary>
        private Collection FindFolder(Collection parent, string folderId, ref bool found)
        {
            if (parent.Id == folderId)
            {
                found = true;
                return parent;
            }
            
            foreach (var folder in parent.Folders)
            {
                var result = FindFolder(folder, folderId, ref found);
                if (found)
                {
                    return result;
                }
            }
            
            return parent;
        }
        
        /// <summary>
        /// Helper method to find a collection containing a specific folder ID
        /// </summary>
        private bool FindCollectionWithFolder(Collection collection, string folderId, out string folderPath)
        {
            folderPath = string.Empty;
            
            // Check if the collection itself has the ID
            if (collection.Id == folderId)
            {
                folderPath = collection.Name;
                return true;
            }
            
            // Search in child folders
            foreach (var folder in collection.Folders)
            {
                if (folder.Id == folderId)
                {
                    folderPath = $"{collection.Name}/{folder.Name}";
                    return true;
                }
                
                // Search deeper
                string childPath;
                if (FindCollectionWithFolder(folder, folderId, out childPath) && !string.IsNullOrEmpty(childPath))
                {
                    folderPath = $"{collection.Name}/{childPath}";
                    return true;
                }
            }
            
            return false;
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