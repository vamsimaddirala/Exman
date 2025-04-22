using Exman.Models;

namespace Exman.Services
{
    /// <summary>
    /// Interface for managing collections of API requests
    /// </summary>
    public interface ICollectionService
    {
        /// <summary>
        /// Gets all top-level collections
        /// </summary>
        Task<IEnumerable<Collection>> GetCollectionsAsync();
        
        /// <summary>
        /// Gets a collection by its ID
        /// </summary>
        Task<Collection?> GetCollectionAsync(string id);
        
        /// <summary>
        /// Creates a new collection
        /// </summary>
        Task<Collection> CreateCollectionAsync(Collection collection);
        
        /// <summary>
        /// Updates an existing collection
        /// </summary>
        Task<bool> UpdateCollectionAsync(Collection collection);
        
        /// <summary>
        /// Deletes a collection by its ID
        /// </summary>
        Task<bool> DeleteCollectionAsync(string id);
        
        /// <summary>
        /// Adds a new collection
        /// </summary>
        Task<bool> AddCollectionAsync(Collection collection);
        
        /// <summary>
        /// Saves an API request to a collection
        /// </summary>
        Task<bool> SaveRequestAsync(string collectionId, ApiRequest request);
        
        /// <summary>
        /// Updates an existing API request
        /// </summary>
        Task<bool> UpdateRequestAsync(string collectionId, ApiRequest request);
        
        /// <summary>
        /// Deletes an API request from a collection
        /// </summary>
        Task<bool> DeleteRequestAsync(string collectionId, string requestId);
        
        /// <summary>
        /// Exports collections to a file
        /// </summary>
        Task<bool> ExportCollectionsAsync(string filePath, IEnumerable<string> collectionIds);
        
        /// <summary>
        /// Imports collections from a file
        /// </summary>
        Task<IEnumerable<Collection>> ImportCollectionsAsync(string filePath);
        
        /// <summary>
        /// Imports a Postman collection from a JSON string
        /// </summary>
        /// <param name="json">JSON string containing Postman collection data</param>
        /// <returns>The imported collection if successful, null otherwise</returns>
        Task<Collection?> ImportPostmanCollectionFromJsonAsync(string json);
        
        /// <summary>
        /// Imports a Postman collection from a file
        /// </summary>
        /// <param name="filePath">Path to the Postman collection JSON file</param>
        /// <returns>The imported collection if successful, null otherwise</returns>
        Task<Collection?> ImportPostmanCollectionAsync(string filePath);
        
        /// <summary>
        /// Adds an API request to a collection
        /// </summary>
        Task<bool> AddRequestToCollectionAsync(string collectionId, ApiRequest request);
    }
}