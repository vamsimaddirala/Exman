using Exman.Models;

namespace Exman.Services
{
    /// <summary>
    /// Service for managing environment variables for API requests
    /// </summary>
    public interface IEnvironmentService
    {
        /// <summary>
        /// Event that fires when the active environment changes or is updated
        /// </summary>
        event Action<RequestEnvironment?>? EnvironmentChanged;
        
        /// <summary>
        /// Get all environments
        /// </summary>
        Task<IEnumerable<RequestEnvironment>> GetEnvironmentsAsync();
        
        /// <summary>
        /// Get an environment by ID
        /// </summary>
        Task<RequestEnvironment?> GetEnvironmentAsync(string id);
        
        /// <summary>
        /// Create a new environment
        /// </summary>
        Task<RequestEnvironment> CreateEnvironmentAsync(RequestEnvironment environment);
        
        /// <summary>
        /// Update an existing environment
        /// </summary>
        Task<bool> UpdateEnvironmentAsync(RequestEnvironment environment);
        
        /// <summary>
        /// Delete an environment
        /// </summary>
        Task<bool> DeleteEnvironmentAsync(string id);
        
        /// <summary>
        /// Get the active environment
        /// </summary>
        Task<RequestEnvironment?> GetActiveEnvironmentAsync();
        
        /// <summary>
        /// Set the active environment
        /// </summary>
        Task SetActiveEnvironmentAsync(string id);
        
        /// <summary>
        /// Set the current environment (alias for SetActiveEnvironmentAsync)
        /// </summary>
        Task SetCurrentEnvironmentAsync(string id);
        
        /// <summary>
        /// Process a string and replace any environment variables with their values
        /// </summary>
        string ProcessString(string input);
        
        /// <summary>
        /// Process an API request by replacing environment variables in URLs, headers, etc.
        /// </summary>
        ApiRequest ProcessRequest(ApiRequest request);
        
        /// <summary>
        /// Import an environment from a Postman environment JSON file
        /// </summary>
        /// <param name="filePath">Path to the Postman environment JSON file</param>
        /// <returns>The imported environment</returns>
        Task<RequestEnvironment> ImportPostmanEnvironmentAsync(string filePath);
        
        /// <summary>
        /// Import an environment from a Postman environment JSON string
        /// </summary>
        /// <param name="json">JSON content of a Postman environment</param>
        /// <returns>The imported environment</returns>
        Task<RequestEnvironment> ImportPostmanEnvironmentFromJsonAsync(string json);
    }
}