using System.Collections.ObjectModel;

namespace Exman.Models
{
    /// <summary>
    /// Represents an API request with all its components
    /// </summary>
    public class ApiRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "New Request";
        public string CollectionId { get; set; } = string.Empty;
        public string RootCollectionId { get; set; } = string.Empty;
        public ApiHttpMethod Method { get; set; } = ApiHttpMethod.GET;
        public string Url { get; set; } = string.Empty;
        public ObservableCollection<KeyValuePair> Headers { get; set; } = new();
        public ObservableCollection<KeyValuePair> QueryParameters { get; set; } = new();
        public ObservableCollection<KeyValuePair> PathVariables { get; set; } = new();
        public RequestBody Body { get; set; } = new();
        public Authentication Authentication { get; set; } = new();
        public int Timeout { get; set; } = 30000; // 30 seconds default
        public bool FollowRedirects { get; set; } = true;
        public bool VerifySsl { get; set; } = true;
        public ProxySettings Proxy { get; set; } = new();
        public DateTime LastUsed { get; set; } = DateTime.Now;
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Creates a deep copy of the current ApiRequest
        /// </summary>
        /// <returns>A new ApiRequest instance with the same values</returns>
        public ApiRequest Clone()
        {
            var clone = new ApiRequest
            {
                Id = Id,
                Name = Name,
                CollectionId = CollectionId,
                RootCollectionId = RootCollectionId,
                Method = Method,
                Url = Url,
                Timeout = Timeout,
                FollowRedirects = FollowRedirects,
                VerifySsl = VerifySsl,
                LastUsed = LastUsed
            };

            // Deep copy collections
            foreach (var header in Headers)
            {
                clone.Headers.Add(new KeyValuePair { Key = header.Key, Value = header.Value });
            }

            foreach (var param in QueryParameters)
            {
                clone.QueryParameters.Add(new KeyValuePair { Key = param.Key, Value = param.Value });
            }

            foreach (var variable in PathVariables)
            {
                clone.PathVariables.Add(new KeyValuePair { Key = variable.Key, Value = variable.Value });
            }

            // Deep copy authentication
            clone.Authentication = new Authentication
            {
                Type = Authentication.Type,
                Username = Authentication.Username,
                Password = Authentication.Password,
                Token = Authentication.Token
            };

            // Deep copy body
            clone.Body = new RequestBody
            {
                ContentType = Body.ContentType,
                RawContent = Body.RawContent
            };

            // Copy form data if exists
            if (Body.FormData != null)
            {
                foreach (var field in Body.FormData)
                {
                    clone.Body.FormData.Add(new KeyValuePair { Key = field.Key, Value = field.Value });
                }
            }

            // Deep copy proxy settings
            clone.Proxy = new ProxySettings
            {
                Enabled = Proxy.Enabled,
                Host = Proxy.Host,
                Port = Proxy.Port,
                Username = Proxy.Username,
                Password = Proxy.Password
            };

            return clone;
        }
    }
}