using System;
using System.Collections.ObjectModel;

namespace Exman.Models
{
    // Class to represent a history item with additional properties for UI binding
    public class RequestHistoryItem
    {
        public string Id { get; set; } = string.Empty;
        public ApiRequest Request { get; set; } = null!;
        public ApiResponse Response { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        
        public bool IsSuccess => Response != null && 
                              (int)(Response.StatusCode) >= 200 && 
                              (int)(Response.StatusCode) < 400;

        // For convenience in the Razor UI
        public string Name => Request?.Name ?? string.Empty;
        public string Url => Request?.Url ?? string.Empty;
        public ApiHttpMethod Method => Request?.Method ?? ApiHttpMethod.GET;
        public DateTime LastUsed => Timestamp;
        
        // Clone method for creating a copy of the request
        public ApiRequest Clone()
        {
            var clone = new ApiRequest
            {
                Id = Guid.NewGuid().ToString(),
                Name = Request.Name,
                Url = Request.Url,
                Method = Request.Method,
            };
            
            // Clone collections properly
            if (Request.Headers != null)
            {
                clone.Headers = new ObservableCollection<KeyValuePair>();
                foreach (var h in Request.Headers)
                {
                    clone.Headers.Add(new KeyValuePair { Key = h.Key, Value = h.Value, Enabled = h.Enabled });
                }
            }
            
            // Clone body
            if (Request.Body != null)
            {
                clone.Body = new RequestBody
                {
                    ContentType = Request.Body.ContentType,
                    RawContent = Request.Body.RawContent
                };
            }
            
            // Clone authentication
            if (Request.Authentication != null)
            {
                clone.Authentication = new Authentication
                {
                    Type = Request.Authentication.Type,
                    Username = Request.Authentication.Username,
                    Password = Request.Authentication.Password,
                    Token = Request.Authentication.Token
                };
            }
            
            return clone;
        }
    }
}