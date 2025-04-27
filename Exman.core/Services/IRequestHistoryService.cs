using Exman.Models;

namespace Exman.Services
{
    /// <summary>
    /// Service for managing request history
    /// </summary>
    public interface IRequestHistoryService
    {
        /// <summary>
        /// Get recent API requests
        /// </summary>
        Task<IEnumerable<ApiRequest>> GetRecentRequestsAsync(int limit = 20);
        
        /// <summary>
        /// Add a request to history
        /// </summary>
        Task AddToHistoryAsync(ApiRequest request);
        
        /// <summary>
        /// Add a request and its response to history
        /// </summary>
        Task AddToHistoryAsync(ApiRequest request, ApiResponse response);
        
        /// <summary>
        /// Clear request history
        /// </summary>
        Task ClearHistoryAsync();
        
        /// <summary>
        /// Save request to a collection
        /// </summary>
        Task<bool> SaveToCollectionAsync(ApiRequest request, string collectionId);
        
        /// <summary>
        /// Get request history
        /// </summary>
        Task<IEnumerable<ApiRequest>> GetHistoryAsync();
        
        /// <summary>
        /// Remove a request from history
        /// </summary>
        Task RemoveFromHistoryAsync(string requestId);

        /// <summary>
        /// Get a request from history by its ID
        /// </summary>
        /// <param name="requestId">The ID of the request to retrieve</param>
        /// <returns>The request if found, or null if not found</returns>
        Task<ApiRequest?> GetRequestByIdAsync(string requestId);
    }
}