namespace Exman.Models
{
    /// <summary>
    /// Represents a key-value pair for various uses in the application
    /// </summary>
    public class KeyValuePair
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents an environment variable
    /// </summary>
    public class Variable : KeyValuePair
    {
        public bool IsSecret { get; set; } = false;
        public string Type { get; set; } = "string";
    }
}