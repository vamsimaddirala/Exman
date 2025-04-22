using System.Collections.ObjectModel;

namespace Exman.Models
{
    /// <summary>
    /// Represents the body of an HTTP request with support for various formats
    /// </summary>
    public class RequestBody
    {
        public enum BodyType
        {
            None,
            Raw,
            FormData,
            UrlEncoded,
            GraphQL,
            Binary
        }

        public BodyType Type { get; set; } = BodyType.None;
        public string RawContent { get; set; } = string.Empty;
        public string ContentType { get; set; } = "text/plain";
        public ObservableCollection<KeyValuePair> FormData { get; set; } = new();
        public ObservableCollection<KeyValuePair> UrlEncodedData { get; set; } = new();
        public string GraphQLQuery { get; set; } = string.Empty;
        public string GraphQLVariables { get; set; } = string.Empty;
        public byte[] BinaryData { get; set; } = Array.Empty<byte>();
        public string BinaryFileName { get; set; } = string.Empty;
    }
}