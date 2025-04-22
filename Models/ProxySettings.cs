namespace Exman.Models
{
    /// <summary>
    /// Represents proxy configuration settings for HTTP requests
    /// </summary>
    public class ProxySettings
    {
        public enum ProxyType
        {
            None,
            HTTP,
            SOCKS4,
            SOCKS5
        }

        public bool Enabled { get; set; } = false;
        public ProxyType Type { get; set; } = ProxyType.None;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 8080;
        public bool UseAuthentication { get; set; } = false;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string BypassList { get; set; } = string.Empty;
    }
}