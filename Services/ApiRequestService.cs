using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Http;
using Exman.Models;
using System.Collections.Generic;

namespace Exman.Services
{
    /// <summary>
    /// Service for handling API requests and responses
    /// </summary>
    public class ApiRequestService : IApiRequestService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IEnvironmentService _environmentService;
        private readonly IRequestHistoryService _requestHistoryService;
        
        // Cache for all environment variables
        private Dictionary<string, RequestEnvironment> _environmentCache = new Dictionary<string, RequestEnvironment>();
        private RequestEnvironment? _activeEnvironment = null;

        public ApiRequestService(
            IHttpClientFactory httpClientFactory, 
            IEnvironmentService environmentService,
            IRequestHistoryService requestHistoryService)
        {
            _httpClientFactory = httpClientFactory;
            _environmentService = environmentService;
            _requestHistoryService = requestHistoryService;
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            // Initialize environment cache
            LoadAllEnvironmentsAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Load all environments into memory cache
        /// </summary>
        private async Task LoadAllEnvironmentsAsync()
        {
            try
            {
                var environments = await _environmentService.GetEnvironmentsAsync();
                _environmentCache.Clear();
                
                foreach (var env in environments)
                {
                    _environmentCache[env.Id] = env;
                    
                    // Set active environment
                    if (env.IsActive)
                    {
                        _activeEnvironment = env;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading environments into memory: {ex.Message}");
            }
        }

        public async Task<ApiResponse> SendRequestAsync(ApiRequest request, CancellationToken cancellationToken = default)
        {
            // Ensure environment cache is up-to-date before processing
            await LoadAllEnvironmentsAsync();
            
            // Process the request with environment variables first
            var processedRequest = _environmentService.ProcessRequest(request);
            
            // Add the request to history
            await _requestHistoryService.AddToHistoryAsync(request);
            
            var validationResult = ValidateRequest(processedRequest);
            if (!validationResult.IsValid)
            {
                return new ApiResponse
                {
                    ErrorMessage = validationResult.ErrorMessage
                };
            }

            var httpClient = CreateHttpClient(processedRequest);
            var httpRequestMessage = await CreateHttpRequestMessageAsync(processedRequest);

            var stopwatch = Stopwatch.StartNew();
            ApiResponse response = new ApiResponse();

            try
            {
                using var httpResponse = await httpClient.SendAsync(
                    httpRequestMessage, 
                    processedRequest.FollowRedirects ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                stopwatch.Stop();

                response.StatusCode = httpResponse.StatusCode;
                response.StatusDescription = httpResponse.ReasonPhrase ?? "";
                response.ResponseTime = stopwatch.Elapsed;
                response.ContentType = httpResponse.Content.Headers.ContentType?.MediaType ?? "";
                response.ContentLength = httpResponse.Content.Headers.ContentLength ?? 0;

                // Get headers
                foreach (var header in httpResponse.Headers)
                {
                    foreach (var value in header.Value)
                    {
                        response.Headers.Add(new Models.KeyValuePair
                        {
                            Key = header.Key,
                            Value = value
                        });
                    }
                }

                // Get content headers
                foreach (var header in httpResponse.Content.Headers)
                {
                    foreach (var value in header.Value)
                    {
                        response.Headers.Add(new Models.KeyValuePair
                        {
                            Key = header.Key,
                            Value = value
                        });
                    }
                }

                // Read body content
                response.Body = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Convert exception to an ApiResponse with error information
                response.ErrorMessage = ex.Message;
                response.ResponseTime = stopwatch.Elapsed;
                
                throw;
            }
        }

        public (bool IsValid, string ErrorMessage) ValidateRequest(ApiRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Url))
            {
                return (false, "URL cannot be empty");
            }

            try
            {
                var uri = new Uri(request.Url);
            }
            catch
            {
                return (false, "URL is not valid");
            }

            return (true, string.Empty);
        }

        private HttpClient CreateHttpClient(ApiRequest request)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = request.FollowRedirects,
                ServerCertificateCustomValidationCallback = request.VerifySsl ? null : (_, _, _, _) => true,
                CookieContainer = new CookieContainer(),
                UseCookies = true
            };

            // Configure proxy if enabled
            if (request.Proxy.Enabled && !string.IsNullOrEmpty(request.Proxy.Host) && request.Proxy.Port > 0)
            {
#if !__IOS__ && !__MACCATALYST__
                var proxyAddress = $"{request.Proxy.Host}:{request.Proxy.Port}";
                var proxy = new WebProxy(proxyAddress);

                if (request.Proxy.UseAuthentication && 
                    !string.IsNullOrEmpty(request.Proxy.Username) && 
                    !string.IsNullOrEmpty(request.Proxy.Password))
                {
                    proxy.Credentials = new NetworkCredential(request.Proxy.Username, request.Proxy.Password);
                }

                handler.Proxy = proxy;
                handler.UseProxy = true;
#endif
            }

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromMilliseconds(request.Timeout);

            return httpClient;
        }

        private async Task<HttpRequestMessage> CreateHttpRequestMessageAsync(ApiRequest request)
        {
            string url = request.Url;

            // Process query parameters
            if (request.QueryParameters.Any(p => p.Enabled))
            {
                var queryBuilder = new StringBuilder();

                // Append ? only if the URL doesn't already contain it
                if (!url.Contains('?'))
                {
                    queryBuilder.Append('?');
                }
                else if (!url.EndsWith('?'))
                {
                    // If ? exists but not at the end, we need to add &
                    queryBuilder.Append('&');
                }

                // Add query parameters
                foreach (var param in request.QueryParameters.Where(p => p.Enabled))
                {
                    queryBuilder.Append($"{WebUtility.UrlEncode(param.Key)}={WebUtility.UrlEncode(param.Value)}&");
                }

                // Remove trailing '&' if exists
                if (queryBuilder.Length > 0 && queryBuilder[queryBuilder.Length - 1] == '&')
                {
                    queryBuilder.Length--;
                }

                url += queryBuilder.ToString();
            }

            // Replace path variables
            foreach (var pathVar in request.PathVariables.Where(p => p.Enabled))
            {
                url = url.Replace($"{{{pathVar.Key}}}", WebUtility.UrlEncode(pathVar.Value));
            }

            // Convert our ApiHttpMethod enum to System.Net.Http.HttpMethod
            var httpMethod = request.Method.ToString() switch
            {
                nameof(ApiHttpMethod.GET) => System.Net.Http.HttpMethod.Get,
                nameof(ApiHttpMethod.POST) => System.Net.Http.HttpMethod.Post,
                nameof(ApiHttpMethod.PUT) => System.Net.Http.HttpMethod.Put,
                nameof(ApiHttpMethod.DELETE) => System.Net.Http.HttpMethod.Delete,
                nameof(ApiHttpMethod.HEAD) => System.Net.Http.HttpMethod.Head,
                nameof(ApiHttpMethod.OPTIONS) => System.Net.Http.HttpMethod.Options,
                nameof(ApiHttpMethod.TRACE) => System.Net.Http.HttpMethod.Trace,
                nameof(ApiHttpMethod.PATCH) => new System.Net.Http.HttpMethod("PATCH"),
                nameof(ApiHttpMethod.CONNECT) => new System.Net.Http.HttpMethod("CONNECT"),
                _ => System.Net.Http.HttpMethod.Get
            };

            var httpRequest = new HttpRequestMessage(httpMethod, url);

            // Add headers
            foreach (var header in request.Headers.Where(h => h.Enabled))
            {
                httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Add authentication headers
            if (request.Authentication.Enabled)
            {
                switch (request.Authentication.Type)
                {
                    case Authentication.AuthType.Basic:
                        var credentials = Convert.ToBase64String(
                            Encoding.ASCII.GetBytes($"{request.Authentication.Username}:{request.Authentication.Password}"));
                        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                        break;
                    case Authentication.AuthType.Bearer:
                        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.Authentication.Token);
                        break;
                    case Authentication.AuthType.ApiKey:
                        if (request.Authentication.AddToHeader)
                        {
                            httpRequest.Headers.TryAddWithoutValidation(
                                request.Authentication.ApiKeyName, 
                                request.Authentication.ApiKey);
                        }
                        else
                        {
                            // Add as query parameter
                            var apiKeyParam = $"{request.Authentication.ApiKeyName}={WebUtility.UrlEncode(request.Authentication.ApiKey)}";
                            if (url.Contains('?'))
                            {
                                url += $"&{apiKeyParam}";
                            }
                            else
                            {
                                url += $"?{apiKeyParam}";
                            }
                            httpRequest.RequestUri = new Uri(url);
                        }
                        break;
                    // Additional auth types could be implemented here
                }
            }

            // Add body if it's not a GET or HEAD request
            if (request.Method != ApiHttpMethod.GET && request.Method != ApiHttpMethod.HEAD)
            {
                switch (request.Body.Type)
                {
                    case RequestBody.BodyType.Raw:
                        httpRequest.Content = new StringContent(
                            request.Body.RawContent, 
                            Encoding.UTF8, 
                            request.Body.ContentType);
                        break;
                    case RequestBody.BodyType.FormData:
                        var formData = new MultipartFormDataContent();
                        foreach (var item in request.Body.FormData.Where(f => f.Enabled))
                        {
                            formData.Add(new StringContent(item.Value), item.Key);
                        }
                        httpRequest.Content = formData;
                        break;
                    case RequestBody.BodyType.UrlEncoded:
                        var formUrlEncoded = new List<KeyValuePair<string, string>>();
                        foreach (var item in request.Body.FormData.Where(f => f.Enabled))
                        {
                            formUrlEncoded.Add(new KeyValuePair<string, string>(item.Key, item.Value));
                        }
                        httpRequest.Content = new FormUrlEncodedContent(formUrlEncoded);
                        break;
                    case RequestBody.BodyType.GraphQL:
                        var graphQlObject = new
                        {
                            query = request.Body.GraphQLQuery,
                            variables = string.IsNullOrWhiteSpace(request.Body.GraphQLVariables)
                                ? new object()
                                : JsonSerializer.Deserialize<object>(request.Body.GraphQLVariables)
                        };
                        httpRequest.Content = new StringContent(
                            JsonSerializer.Serialize(graphQlObject),
                            Encoding.UTF8,
                            "application/json");
                        break;
                    case RequestBody.BodyType.Binary:
                        if (request.Body.BinaryData.Length > 0)
                        {
                            httpRequest.Content = new ByteArrayContent(request.Body.BinaryData);
                            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                        }
                        break;
                }
            }

            return httpRequest;
        }
    }
}