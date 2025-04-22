using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Exman.Models
{
    /// <summary>
    /// Model representing a Postman Environment format
    /// </summary>
    public class PostmanEnvironment
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("_postman_variable_scope")]
        public string Scope { get; set; } = string.Empty;
        
        [JsonPropertyName("_postman_exported_at")]
        public string ExportedAt { get; set; } = string.Empty;
        
        [JsonPropertyName("_postman_exported_using")]
        public string ExportedUsing { get; set; } = string.Empty;
        
        [JsonPropertyName("values")]
        public List<PostmanEnvironmentVariable> Values { get; set; } = new List<PostmanEnvironmentVariable>();
    }

    public class PostmanEnvironmentVariable
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;
        
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
        
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
        
        [JsonPropertyName("type")]
        public string Type { get; set; } = "default";
    }
}