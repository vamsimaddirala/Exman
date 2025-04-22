using System.Collections.ObjectModel;
using System.Text.Json;
using Exman.Models;

namespace Exman.Services
{
    /// <summary>
    /// Converts Postman Collection format to Exman Collection format
    /// </summary>
    public class PostmanCollectionConverter
    {
        /// <summary>
        /// Convert a Postman collection string to Exman Collection object
        /// </summary>
        /// <param name="json">JSON string in Postman Collection format</param>
        /// <returns>Converted Exman Collection</returns>
        public static Collection? ConvertFromJson(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                var postmanCollection = JsonSerializer.Deserialize<PostmanCollection>(json, options);
                
                if (postmanCollection == null)
                {
                    return null;
                }

                return ConvertFromPostmanCollection(postmanCollection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting Postman collection: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Convert a PostmanCollection to Exman Collection
        /// </summary>
        /// <param name="postmanCollection">Postman Collection object</param>
        /// <returns>Converted Exman Collection</returns>
        public static Collection ConvertFromPostmanCollection(PostmanCollection postmanCollection)
        {
            var collection = new Collection
            {
                Name = postmanCollection.Info.Name,
                Description = postmanCollection.Info.Description ?? string.Empty,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // Convert variables
            if (postmanCollection.Variables != null)
            {
                collection.Variables = new ObservableCollection<Variable>(
                    postmanCollection.Variables.Select(v => new Variable
                    {
                        Key = v.Key,
                        Value = v.Value,
                        Description = v.Description ?? string.Empty
                    })
                );
            }

            // Process items recursively
            ProcessItems(postmanCollection.Items, collection);

            return collection;
        }

        /// <summary>
        /// Process Postman items recursively and add them to the collection
        /// </summary>
        private static void ProcessItems(List<PostmanItem> items, Collection parentCollection)
        {
            foreach (var item in items)
            {
                if (item.IsFolder)
                {
                    // This is a folder, create a folder in the collection
                    var folder = new Collection.Folder
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = item.Name,
                        Description = item.Description ?? string.Empty,
                        ParentId = parentCollection.Id,
                        Folders = new System.Collections.ObjectModel.ObservableCollection<Collection.Folder>(),
                        Requests = new System.Collections.ObjectModel.ObservableCollection<ApiRequest>()
                    };

                    // Process nested items
                    if (item.Items != null)
                    {
                        ProcessNestedItems(item.Items, folder);
                    }

                    // Add this folder to parent
                    parentCollection.Folders.Add(folder);
                }
                else if (item.Request != null)
                {
                    // This is a request, convert it
                    var request = ConvertPostmanRequest(item);
                    parentCollection.Requests.Add(request);
                }
            }
        }

        /// <summary>
        /// Process nested Postman items and add them to the folder
        /// </summary>
        private static void ProcessNestedItems(List<PostmanItem> items, Collection.Folder parentFolder)
        {
            foreach (var item in items)
            {
                if (item.IsFolder)
                {
                    // This is a nested folder
                    var nestedFolder = new Collection.Folder
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = item.Name,
                        Description = item.Description ?? string.Empty,
                        ParentId = parentFolder.Id,
                        Folders = new System.Collections.ObjectModel.ObservableCollection<Collection.Folder>(),
                        Requests = new System.Collections.ObjectModel.ObservableCollection<ApiRequest>()
                    };

                    // Process nested items recursively
                    if (item.Items != null)
                    {
                        ProcessNestedItems(item.Items, nestedFolder);
                    }

                    // Add this folder to parent
                    parentFolder.Folders.Add(nestedFolder);
                }
                else if (item.Request != null)
                {
                    // This is a request, convert it
                    var request = ConvertPostmanRequest(item);
                    parentFolder.Requests.Add(request);
                }
            }
        }

        /// <summary>
        /// Convert a Postman request item to Exman ApiRequest
        /// </summary>
        private static ApiRequest ConvertPostmanRequest(PostmanItem item)
        {
            var request = new ApiRequest
            {
                Name = item.Name,
                Description = item.Request?.Description ?? string.Empty,
                Method = ParseHttpMethod(item.Request?.Method ?? "GET"),
                Url = item.Request?.Url?.Raw ?? string.Empty
            };

            // Add headers
            if (item.Request?.Headers != null)
            {
                foreach (var header in item.Request.Headers)
                {
                    if (header.Disabled != true)
                    {
                        request.Headers.Add(new Models.KeyValuePair { Key = header.Key, Value = header.Value });
                    }
                }
            }

            // Add query parameters
            if (item.Request?.Url?.Query != null)
            {
                foreach (var param in item.Request.Url.Query)
                {
                    if (param.Disabled != true)
                    {
                        request.QueryParameters.Add(new Models.KeyValuePair { Key = param.Key, Value = param.Value });
                    }
                }
            }

            // Add path variables
            if (item.Request?.Url?.Variables != null)
            {
                foreach (var variable in item.Request.Url.Variables)
                {
                    request.PathVariables.Add(new Models.KeyValuePair { Key = variable.Key, Value = variable.Value });
                }
            }

            // Process request body
            if (item.Request?.Body != null)
            {
                ConvertPostmanBody(item.Request.Body, request);
            }

            // Process authentication
            if (item.Request?.Auth != null)
            {
                ConvertPostmanAuth(item.Request.Auth, request);
            }

            return request;
        }

        /// <summary>
        /// Convert a Postman request body to Exman RequestBody
        /// </summary>
        private static void ConvertPostmanBody(PostmanBody postmanBody, ApiRequest request)
        {
            switch (postmanBody.Mode?.ToLower())
            {
                case "raw":
                    request.Body.ContentType = DetermineContentType(postmanBody);
                    request.Body.RawContent = postmanBody.Raw ?? string.Empty;
                    break;

                case "formdata":
                    if (postmanBody.FormData != null)
                    {
                        request.Body.ContentType = "multipart/form-data";
                        foreach (var field in postmanBody.FormData)
                        {
                            if (field.Disabled != true)
                            {
                                request.Body.FormData.Add(new Models.KeyValuePair { Key = field.Key, Value = field.Value });
                            }
                        }
                    }
                    break;

                case "urlencoded":
                    if (postmanBody.UrlEncoded != null)
                    {
                        request.Body.ContentType = "application/x-www-form-urlencoded";
                        foreach (var field in postmanBody.UrlEncoded)
                        {
                            if (field.Disabled != true)
                            {
                                request.Body.FormData.Add(new Models.KeyValuePair { Key = field.Key, Value = field.Value });
                            }
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Determine content type from Postman body options
        /// </summary>
        private static string DetermineContentType(PostmanBody body)
        {
            // Check if we have language specified in options
            if (body.Options?.Raw?.Language != null)
            {
                switch (body.Options.Raw.Language.ToLower())
                {
                    case "json": return "application/json";
                    case "xml": return "application/xml";
                    case "html": return "text/html";
                    case "text": return "text/plain";
                    case "javascript": return "application/javascript";
                }
            }

            // Try to guess from content
            if (!string.IsNullOrWhiteSpace(body.Raw))
            {
                string trimmed = body.Raw.Trim();
                if ((trimmed.StartsWith("{") && trimmed.EndsWith("}")) || 
                    (trimmed.StartsWith("[") && trimmed.EndsWith("]")))
                {
                    return "application/json";
                }
                if (trimmed.StartsWith("<") && trimmed.EndsWith(">"))
                {
                    return "application/xml";
                }
            }

            // Default
            return "text/plain";
        }

        /// <summary>
        /// Convert Postman auth to Exman Authentication
        /// </summary>
        private static void ConvertPostmanAuth(PostmanAuth auth, ApiRequest request)
        {
            switch (auth.Type.ToLower())
            {
                case "basic":
                    if (auth.Basic != null)
                    {
                        request.Authentication.Type = Authentication.AuthType.Basic;
                        var username = auth.Basic.FirstOrDefault(a => a.Key == "username");
                        var password = auth.Basic.FirstOrDefault(a => a.Key == "password");
                        request.Authentication.Username = username?.Value ?? string.Empty;
                        request.Authentication.Password = password?.Value ?? string.Empty;
                    }
                    break;

                case "bearer":
                    if (auth.Bearer != null)
                    {
                        request.Authentication.Type = Authentication.AuthType.Bearer;
                        var token = auth.Bearer.FirstOrDefault(a => a.Key == "token");
                        request.Authentication.Token = token?.Value ?? string.Empty;
                    }
                    break;

                case "apikey":
                    if (auth.ApiKey != null)
                    {
                        request.Authentication.Type = Authentication.AuthType.ApiKey;
                        var key = auth.ApiKey.FirstOrDefault(a => a.Key == "key");
                        var value = auth.ApiKey.FirstOrDefault(a => a.Key == "value");
                        request.Authentication.ApiKeyName = key?.Value ?? string.Empty;
                        request.Authentication.ApiKey = value?.Value ?? string.Empty;

                        // Handle location (header or query)
                        var location = auth.ApiKey.FirstOrDefault(a => a.Key == "in")?.Value ?? "header";
                        request.Authentication.AddToHeader = !location.Equals("query", StringComparison.OrdinalIgnoreCase);
                    }
                    break;

                default:
                    request.Authentication.Type = Authentication.AuthType.None;
                    break;
            }
        }

        /// <summary>
        /// Parse HTTP method string to ApiHttpMethod enum
        /// </summary>
        private static ApiHttpMethod ParseHttpMethod(string method)
        {
            if (Enum.TryParse<ApiHttpMethod>(method, true, out var result))
            {
                return result;
            }
            return ApiHttpMethod.GET;
        }

        /// <summary>
        /// Convert from Postman Environment JSON string to RequestEnvironment object
        /// </summary>
        public async Task<RequestEnvironment> ConvertFromPostmanEnvironmentAsync(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                var postmanEnv = JsonSerializer.Deserialize<PostmanEnvironment>(json, options);
                
                if (postmanEnv == null)
                {
                    throw new Exception("Failed to parse Postman environment");
                }

                var environment = new RequestEnvironment
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = postmanEnv.Name,
                    Description = string.Empty, // PostmanEnvironment doesn't have a description field
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Variables = new ObservableCollection<Variable>()
                };

                // Convert variables
                if (postmanEnv.Values != null)
                {
                    foreach (var value in postmanEnv.Values)
                    {
                        environment.Variables.Add(new Variable 
                        { 
                            Key = value.Key, 
                            Value = value.Value,
                            Enabled = value.Enabled,
                            Type = "string",
                            IsSecret = false
                        });
                    }
                }

                return environment;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting Postman environment: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Convert to Postman Environment format
        /// </summary>
        public async Task<string> ConvertToPostmanEnvironmentAsync(RequestEnvironment environment)
        {
            var postmanEnv = new PostmanEnvironment
            {
                Id = environment.Id,
                Name = environment.Name,
                Scope = "environment",
                ExportedAt = DateTime.Now.ToString("o"),
                ExportedUsing = "Exman",
                Values = new List<PostmanEnvironmentVariable>()
            };

            // Convert variables
            if (environment.Variables != null)
            {
                foreach (var variable in environment.Variables)
                {
                    postmanEnv.Values.Add(new PostmanEnvironmentVariable
                    {
                        Key = variable.Key,
                        Value = variable.Value,
                        Enabled = variable.Enabled,
                        Type = "default"
                    });
                }
            }

            // Serialize to JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(postmanEnv, options);
        }

        /// <summary>
        /// Convert a Postman collection to Exman Collection asynchronously
        /// </summary>
        public async Task<Collection> ConvertFromPostmanCollectionAsync(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                var postmanCollection = JsonSerializer.Deserialize<PostmanCollection>(json, options);
                
                if (postmanCollection == null)
                {
                    throw new Exception("Failed to parse Postman collection");
                }

                return ConvertFromPostmanCollection(postmanCollection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting Postman collection: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Convert an Exman Collection to Postman format asynchronously
        /// </summary>
        public async Task<string> ConvertToPostmanCollectionAsync(Collection collection)
        {
            var postmanCollection = new PostmanCollection
            {
                Info = new PostmanInfo
                {
                    Name = collection.Name,
                    Description = collection.Description,
                    Schema = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
                },
                Items = new List<PostmanItem>()
            };

            // Convert requests
            foreach (var request in collection.Requests)
            {
                postmanCollection.Items.Add(ConvertToPostmanItem(request));
            }

            // Convert folders
            foreach (var folder in collection.Folders)
            {
                var folderItem = new PostmanItem
                {
                    Name = folder.Name,
                    Description = folder.Description,
                    Items = new List<PostmanItem>()
                };

                // Add requests in this folder
                foreach (var request in folder.Requests)
                {
                    folderItem.Items.Add(ConvertToPostmanItem(request));
                }

                postmanCollection.Items.Add(folderItem);
            }

            // Serialize to JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(postmanCollection, options);
        }

        /// <summary>
        /// Convert an Exman ApiRequest to Postman Item
        /// </summary>
        private PostmanItem ConvertToPostmanItem(ApiRequest request)
        {
            var postmanItem = new PostmanItem
            {
                Name = request.Name,
                Request = new PostmanRequest
                {
                    Method = request.Method.ToString(),
                    Description = request.Description,
                    Headers = new List<PostmanHeader>(),
                    Url = new PostmanUrl
                    {
                        Raw = request.Url,
                        Query = new List<PostmanQueryParam>()
                    }
                }
            };

            // Add headers
            foreach (var header in request.Headers.Where(h => h.Enabled))
            {
                postmanItem.Request.Headers.Add(new PostmanHeader
                {
                    Key = header.Key,
                    Value = header.Value
                });
            }

            // Add query parameters
            foreach (var param in request.QueryParameters.Where(p => p.Enabled))
            {
                postmanItem.Request.Url.Query.Add(new PostmanQueryParam
                {
                    Key = param.Key,
                    Value = param.Value
                });
            }

            // Handle authentication
            if (request.Authentication.Type != Authentication.AuthType.None)
            {
                // Add authentication based on type
                // This would be implemented based on the authentication types
            }

            return postmanItem;
        }
    }
}