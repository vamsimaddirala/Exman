using System.Collections.ObjectModel;
using System.Net;

namespace Exman.Models
{
    /// <summary>
    /// Represents an API response with all its components
    /// </summary>
    public class ApiResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
        public ObservableCollection<KeyValuePair> Headers { get; set; } = new();
        public string ContentType { get; set; } = string.Empty;
        public long ContentLength { get; set; }
        public string Body { get; set; } = string.Empty;
        public byte[] RawBody { get; set; } = Array.Empty<byte>();
        public TimeSpan ResponseTime { get; set; }
        public int RedirectCount { get; set; }
        public ObservableCollection<Cookie> Cookies { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
        
        public bool IsSuccess => (int)StatusCode >= 200 && (int)StatusCode < 300;
    }
    
    public class Cookie
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Path { get; set; } = "/";
        public DateTime? Expires { get; set; }
        public bool HttpOnly { get; set; }
        public bool Secure { get; set; }
    }
}