using System.Text.Json;
using System.Text.RegularExpressions;
using Exman.Models;

namespace Exman.Services
{
    /// <summary>
    /// Service for managing environment variables with local file storage
    /// </summary>
    public class EnvironmentService : IEnvironmentService
    {
        private readonly string _dataDirectory;
        private readonly string _environmentDirectory;
        private readonly JsonSerializerOptions _jsonOptions;
        
        // Event that fires when the environment changes
        public event Action<RequestEnvironment?>? EnvironmentChanged;
        
        // Cache of the current active environment
        private RequestEnvironment? _activeEnvironment;
        
        public EnvironmentService()
        {
            _dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "Exman");
                
            _environmentDirectory = Path.Combine(_dataDirectory, "Environments");
            
            // Create directories if they don't exist
            Directory.CreateDirectory(_dataDirectory);
            Directory.CreateDirectory(_environmentDirectory);
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            // Initialize active environment
            InitializeActiveEnvironmentAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Initialize the active environment on startup
        /// </summary>
        private async Task InitializeActiveEnvironmentAsync()
        {
            _activeEnvironment = await GetActiveEnvironmentAsync();
        }
        
        public async Task<IEnumerable<RequestEnvironment>> GetEnvironmentsAsync()
        {
            var environments = new List<RequestEnvironment>();
            
            // Get all environment files
            var files = Directory.GetFiles(_environmentDirectory, "*.json");
            
            foreach (var file in files)
            {
                try
                {
                    // Use synchronous file reading with a FileStream for better control
                    using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var streamReader = new StreamReader(fileStream))
                    {
                        var json = streamReader.ReadToEnd();
                        var environment = JsonSerializer.Deserialize<RequestEnvironment>(json, _jsonOptions);
                        
                        if (environment != null)
                        {
                            environments.Add(environment);
                            
                            // Keep track of the active environment
                            if (environment.IsActive && _activeEnvironment == null)
                            {
                                _activeEnvironment = environment;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading environment: {ex.Message}");
                }
            }
            
            return environments;
        }
        
        public async Task<RequestEnvironment?> GetEnvironmentAsync(string id)
        {
            var filePath = GetEnvironmentFilePath(id);
            
            if (!File.Exists(filePath))
            {
                return null;
            }
            
            try
            {
                // Use synchronous file reading with better control
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var streamReader = new StreamReader(fileStream))
                {
                    var json = streamReader.ReadToEnd();
                    var environment = JsonSerializer.Deserialize<RequestEnvironment>(json, _jsonOptions);
                    return environment;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading environment: {ex.Message}");
                return null;
            }
        }
        
        public async Task<RequestEnvironment> CreateEnvironmentAsync(RequestEnvironment environment)
        {
            // Ensure the environment has a valid ID
            if (string.IsNullOrEmpty(environment.Id))
            {
                environment.Id = Guid.NewGuid().ToString();
            }
            
            environment.CreatedAt = DateTime.Now;
            environment.UpdatedAt = DateTime.Now;
            
            // If this should be the active environment, update state
            if (environment.IsActive)
            {
                await DeactivateCurrentEnvironmentAsync();
            }
            
            // Save the environment to file
            await SaveEnvironmentToFileAsync(environment);
            
            // If this is the first environment, make it active
            var environments = await GetEnvironmentsAsync();
            if (!environments.Any() || environment.IsActive)
            {
                await SetActiveEnvironmentAsync(environment.Id);
            }
            
            return environment;
        }
        
        public async Task<bool> UpdateEnvironmentAsync(RequestEnvironment environment)
        {
            var filePath = GetEnvironmentFilePath(environment.Id);
            
            if (!File.Exists(filePath))
            {
                return false;
            }
            
            environment.UpdatedAt = DateTime.Now;
            
            try
            {
                // If this environment is being set as active, deactivate the current active environment
                if (environment.IsActive)
                {
                    await DeactivateCurrentEnvironmentAsync(environment.Id);
                }
                
                await SaveEnvironmentToFileAsync(environment);
                
                // Update active environment if this one is active
                if (environment.IsActive)
                {
                    _activeEnvironment = environment;
                    EnvironmentChanged?.Invoke(environment);
                }
                // If this was the active environment but is no longer active
                else if (_activeEnvironment != null && _activeEnvironment.Id == environment.Id)
                {
                    _activeEnvironment = null;
                    EnvironmentChanged?.Invoke(null);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating environment: {ex.Message}");
                return false;
            }
        }
        
        public async Task<bool> DeleteEnvironmentAsync(string id)
        {
            var filePath = GetEnvironmentFilePath(id);
            
            if (!File.Exists(filePath))
            {
                return false;
            }
            
            try
            {
                // Check if this was the active environment
                var wasActive = (_activeEnvironment != null && _activeEnvironment.Id == id);
                
                // Delete the file
                File.Delete(filePath);
                
                // If this was the active environment, clear the active environment
                if (wasActive)
                {
                    _activeEnvironment = null;
                    EnvironmentChanged?.Invoke(null);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting environment: {ex.Message}");
                return false;
            }
        }
        
        public async Task<RequestEnvironment?> GetActiveEnvironmentAsync()
        {
            // Return cached active environment if available
            if (_activeEnvironment != null)
            {
                return _activeEnvironment;
            }
            
            // Otherwise look for an environment with IsActive=true
            var environments = await GetEnvironmentsAsync();
            _activeEnvironment = environments.FirstOrDefault(e => e.IsActive);
            return _activeEnvironment;
        }
        
        public async Task SetActiveEnvironmentAsync(string id)
        {
            // Get the environment first to make sure it exists
            var environment = await GetEnvironmentAsync(id);
            if (environment == null)
            {
                throw new ArgumentException($"Environment with ID {id} not found");
            }
            
            try
            {
                // Deactivate the current active environment
                await DeactivateCurrentEnvironmentAsync(id);
                
                // Set this environment as active
                environment.IsActive = true;
                await SaveEnvironmentToFileAsync(environment);
                
                // Update the active environment cache
                _activeEnvironment = environment;
                
                // Notify that the environment has changed
                EnvironmentChanged?.Invoke(environment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting active environment: {ex.Message}");
                throw;
            }
        }
        
        public Task SetCurrentEnvironmentAsync(string id)
        {
            // This is just an alias for SetActiveEnvironmentAsync
            return SetActiveEnvironmentAsync(id);
        }
        
        private async Task DeactivateCurrentEnvironmentAsync(string? exceptId = null)
        {
            var environments = await GetEnvironmentsAsync();
            
            foreach (var env in environments.Where(e => e.IsActive && e.Id != exceptId))
            {
                env.IsActive = false;
                await SaveEnvironmentToFileAsync(env);
            }
        }
        
        public string ProcessString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            
            // Find all {{variable}} patterns
            var regex = new Regex(@"\{\{(.*?)\}\}", RegexOptions.Compiled);
            var matches = regex.Matches(input);
            
            // No variables to replace
            if (matches.Count == 0)
            {
                return input;
            }
            
            var result = input;
            foreach (Match match in matches)
            {
                var variableName = match.Groups[1].Value.Trim();
                var variableValue = GetVariableValue(variableName);
                
                if (variableValue != null)
                {
                    result = result.Replace(match.Value, variableValue);
                }
            }
            
            return result;
        }
        
        public ApiRequest ProcessRequest(ApiRequest request)
        {
            // Create a deep copy of the request
            //var newRequest = request.Clone();
            var serializedRequest = JsonSerializer.Serialize(request);
            var newRequest = JsonSerializer.Deserialize<ApiRequest>(serializedRequest);

            // Process URL
            newRequest.Url = ProcessString(newRequest.Url);
            
            // Process headers
            foreach (var header in newRequest.Headers)
            {
                header.Value = ProcessString(header.Value);
            }
            
            // Process query parameters
            foreach (var param in newRequest.QueryParameters)
            {
                param.Value = ProcessString(param.Value);
            }
            
            // Process path variables
            foreach (var param in newRequest.PathVariables)
            {
                param.Value = ProcessString(param.Value);
            }
            
            // Process body
            switch (newRequest.Body.Type)
            {
                case RequestBody.BodyType.Raw:
                    newRequest.Body.RawContent = ProcessString(newRequest.Body.RawContent);
                    break;
                case RequestBody.BodyType.FormData:
                    foreach (var param in newRequest.Body.FormData)
                    {
                        param.Value = ProcessString(param.Value);
                    }
                    break;
                case RequestBody.BodyType.UrlEncoded:
                    foreach (var param in newRequest.Body.UrlEncodedData)
                    {
                        param.Value = ProcessString(param.Value);
                    }
                    break;
                case RequestBody.BodyType.GraphQL:
                    newRequest.Body.GraphQLQuery = ProcessString(newRequest.Body.GraphQLQuery);
                    newRequest.Body.GraphQLVariables = ProcessString(newRequest.Body.GraphQLVariables);
                    break;
            }
            
            // Process authentication
            if (newRequest.Authentication.Enabled)
            {
                switch (newRequest.Authentication.Type)
                {
                    case Authentication.AuthType.Basic:
                        newRequest.Authentication.Username = ProcessString(newRequest.Authentication.Username);
                        newRequest.Authentication.Password = ProcessString(newRequest.Authentication.Password);
                        break;
                    case Authentication.AuthType.Bearer:
                        newRequest.Authentication.Token = ProcessString(newRequest.Authentication.Token);
                        break;
                    case Authentication.AuthType.ApiKey:
                        newRequest.Authentication.ApiKey = ProcessString(newRequest.Authentication.ApiKey);
                        break;
                }
            }
            
            return newRequest;
        }
        
        public async Task<RequestEnvironment> ImportPostmanEnvironmentAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Environment file not found: {filePath}");
            }
            
            string json = await File.ReadAllTextAsync(filePath);
            return await ImportPostmanEnvironmentFromJsonAsync(json);
        }
        
        public async Task<RequestEnvironment> ImportPostmanEnvironmentFromJsonAsync(string json)
        {
            try
            {
                // Parse the Postman environment JSON
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
                
                // Extract the environment details
                var environment = new RequestEnvironment
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = jsonElement.GetProperty("name").GetString() ?? "Imported Environment",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                
                // Extract variables
                if (jsonElement.TryGetProperty("values", out var valuesElement) && valuesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var valueElement in valuesElement.EnumerateArray())
                    {
                        var key = valueElement.GetProperty("key").GetString() ?? "";
                        var value = valueElement.TryGetProperty("value", out var valueProperty) ? 
                            valueProperty.ToString() : "";
                        var enabled = !valueElement.TryGetProperty("enabled", out var enabledProperty) || 
                            enabledProperty.GetBoolean();
                        
                        if (!string.IsNullOrEmpty(key))
                        {
                            environment.Variables.Add(new Variable
                            {
                                Key = key,
                                Value = value,
                                Enabled = enabled
                            });
                        }
                    }
                }
                
                // Save the imported environment
                return await CreateEnvironmentAsync(environment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing Postman environment: {ex.Message}");
                throw;
            }
        }
        
        private string? GetVariableValue(string name)
        {
            // First try to get from cached active environment
            if (_activeEnvironment != null)
            {
                var variable = _activeEnvironment.Variables.FirstOrDefault(v => 
                    v.Key.Equals(name, StringComparison.OrdinalIgnoreCase) && v.Enabled);
                if (variable != null)
                {
                    return variable.Value;
                }
            }
            
            // Fall back to database lookup if cached value not found
            var activeEnvironment = GetActiveEnvironmentAsync().Result;
            if (activeEnvironment == null)
            {
                return null;
            }
            
            var variableFromDb = activeEnvironment.Variables.FirstOrDefault(v => 
                v.Key.Equals(name, StringComparison.OrdinalIgnoreCase) && v.Enabled);
            return variableFromDb?.Value;
        }
        
        private string GetEnvironmentFilePath(string id)
        {
            return Path.Combine(_environmentDirectory, $"{id}.json");
        }
        
        private async Task SaveEnvironmentToFileAsync(RequestEnvironment environment)
        {
            var filePath = GetEnvironmentFilePath(environment.Id);
            var json = JsonSerializer.Serialize(environment, _jsonOptions);
            
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    await streamWriter.WriteAsync(json);
                    await streamWriter.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving environment to file: {ex.Message}");
                throw;
            }
        }
    }
}