using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Exman.Models
{
    /// <summary>
    /// Represents a Postman Collection format for import/export
    /// </summary>
    public class PostmanCollection
    {
        [JsonPropertyName("info")]
        public PostmanInfo Info { get; set; } = new();

        [JsonPropertyName("item")]
        public List<PostmanItem> Items { get; set; } = new();

        [JsonPropertyName("variable")]
        public List<PostmanVariable>? Variables { get; set; }

        [JsonPropertyName("auth")]
        public PostmanAuth? Auth { get; set; }

        [JsonPropertyName("event")]
        public List<PostmanEvent>? Events { get; set; }
    }

    public class PostmanInfo
    {
        [JsonPropertyName("_postman_id")]
        public string? PostmanId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "Imported Collection";

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("schema")]
        public string? Schema { get; set; }
    }

    public class PostmanItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("item")]
        public List<PostmanItem>? Items { get; set; }

        [JsonPropertyName("request")]
        public PostmanRequest? Request { get; set; }

        [JsonPropertyName("response")]
        public List<PostmanResponse>? Responses { get; set; }

        // Returns true if this item is a folder (contains other items)
        [JsonIgnore]
        public bool IsFolder => Items != null && Items.Count > 0;
    }

    public class PostmanRequest
    {
        [JsonPropertyName("method")]
        public string Method { get; set; } = "GET";

        [JsonPropertyName("header")]
        public List<PostmanHeader>? Headers { get; set; }

        [JsonPropertyName("url")]
        public PostmanUrl? Url { get; set; }

        [JsonPropertyName("body")]
        public PostmanBody? Body { get; set; }

        [JsonPropertyName("auth")]
        public PostmanAuth? Auth { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    public class PostmanHeader
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("disabled")]
        public bool? Disabled { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    public class PostmanUrl
    {
        [JsonPropertyName("raw")]
        public string Raw { get; set; } = string.Empty;

        [JsonPropertyName("protocol")]
        public string? Protocol { get; set; }

        [JsonPropertyName("host")]
        public List<string>? Host { get; set; }

        [JsonPropertyName("path")]
        public List<string>? Path { get; set; }

        [JsonPropertyName("query")]
        public List<PostmanQueryParam>? Query { get; set; }

        [JsonPropertyName("variable")]
        public List<PostmanUrlVariable>? Variables { get; set; }
    }

    public class PostmanQueryParam
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("disabled")]
        public bool? Disabled { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    public class PostmanUrlVariable
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    public class PostmanBody
    {
        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "raw";

        [JsonPropertyName("raw")]
        public string? Raw { get; set; }

        [JsonPropertyName("formdata")]
        public List<PostmanFormData>? FormData { get; set; }

        [JsonPropertyName("urlencoded")]
        public List<PostmanUrlEncoded>? UrlEncoded { get; set; }

        [JsonPropertyName("options")]
        public PostmanBodyOptions? Options { get; set; }
    }

    public class PostmanFormData
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";

        [JsonPropertyName("disabled")]
        public bool? Disabled { get; set; }
    }

    public class PostmanUrlEncoded
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("disabled")]
        public bool? Disabled { get; set; }
    }

    public class PostmanBodyOptions
    {
        [JsonPropertyName("raw")]
        public PostmanBodyRawOptions? Raw { get; set; }
    }

    public class PostmanBodyRawOptions
    {
        [JsonPropertyName("language")]
        public string? Language { get; set; }
    }

    public class PostmanAuth
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "noauth";

        [JsonPropertyName("basic")]
        public List<PostmanAuthDetail>? Basic { get; set; }

        [JsonPropertyName("bearer")]
        public List<PostmanAuthDetail>? Bearer { get; set; }

        [JsonPropertyName("apikey")]
        public List<PostmanAuthDetail>? ApiKey { get; set; }
    }

    public class PostmanAuthDetail
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    public class PostmanResponse
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("originalRequest")]
        public PostmanRequest? OriginalRequest { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("code")]
        public int? Code { get; set; }

        [JsonPropertyName("header")]
        public List<PostmanHeader>? Headers { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }
    }

    public class PostmanEvent
    {
        [JsonPropertyName("listen")]
        public string Listen { get; set; } = string.Empty;

        [JsonPropertyName("script")]
        public PostmanScript? Script { get; set; }
    }

    public class PostmanScript
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text/javascript";

        [JsonPropertyName("exec")]
        public List<string>? Exec { get; set; }
    }

    public class PostmanVariable
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}