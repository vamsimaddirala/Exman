using Exman.Models;

namespace Exman.Services
{
    /// <summary>
    /// Interface for API request service
    /// </summary>
    public interface IApiRequestService
    {
        /// <summary>
        /// Sends an HTTP request and returns the response
        /// </summary>
        /// <param name="request">The API request to send</param>
        /// <param name="cancellationToken">Cancellation token for cancelling the request</param>
        /// <returns>The API response</returns>
        Task<ApiResponse> SendRequestAsync(ApiRequest request, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Validates a request before sending
        /// </summary>
        /// <param name="request">The request to validate</param>
        /// <returns>True if valid, otherwise false with error message</returns>
        (bool IsValid, string ErrorMessage) ValidateRequest(ApiRequest request);
    }
}