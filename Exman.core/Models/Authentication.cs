namespace Exman.Models
{
    /// <summary>
    /// Represents authentication settings for HTTP requests
    /// </summary>
    public class Authentication
    {
        public enum AuthType
        {
            None,
            Basic,
            Bearer,
            ApiKey,
            OAuth1,
            OAuth2,
            Digest,
            NTLM,
            AWSSignature,
            Custom
        }

        public AuthType Type { get; set; } = AuthType.None;
        public bool Enabled { get; set; } = false;

        // Basic Auth
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // Bearer Token
        public string Token { get; set; } = string.Empty;

        // API Key
        public string ApiKey { get; set; } = string.Empty;
        public string ApiKeyName { get; set; } = string.Empty;
        public bool AddToHeader { get; set; } = true;

        // OAuth 1.0
        public string ConsumerKey { get; set; } = string.Empty;
        public string ConsumerSecret { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string TokenSecret { get; set; } = string.Empty;
        public string SignatureMethod { get; set; } = "HMAC-SHA1";

        // OAuth 2.0
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string AuthUrl { get; set; } = string.Empty;
        public string AccessTokenUrl { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        
        // Digest Auth
        public string Realm { get; set; } = string.Empty;
        public string Nonce { get; set; } = string.Empty;
        public string Algorithm { get; set; } = "MD5";

        // NTLM
        public string Domain { get; set; } = string.Empty;
        public string Workstation { get; set; } = string.Empty;

        // AWS Signature
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string SignatureVersion { get; set; } = "v4";

        // Custom
        public string CustomScript { get; set; } = string.Empty;
    }
}