using System.Web;
using Microsoft.Maui.Controls;
using System;

namespace Exman.Utilities
{
    public static class NavigationExtensions
    {
        /// <summary>
        /// Extension method to get query parameters from ShellNavigationState
        /// </summary>
        /// <param name="navigationState">The ShellNavigationState from which to extract parameters</param>
        /// <returns>A dictionary of parameter names and values</returns>
        public static Dictionary<string, string> GetQueryParameters(this ShellNavigationState navigationState)
        {
            var result = new Dictionary<string, string>();
            
            if (navigationState == null)
                return result;
                
            try
            {
                // Get the raw URI string and parse it manually
                string uriString = navigationState.Location.ToString();
                
                // Check if there is a query section (contains a ?)
                int queryIndex = uriString.IndexOf('?');
                if (queryIndex < 0)
                    return result;
                    
                // Extract the query string part
                string queryString = uriString.Substring(queryIndex + 1);
                
                // Parse the query string
                foreach (string pair in queryString.Split('&'))
                {
                    if (string.IsNullOrEmpty(pair))
                        continue;
                        
                    string[] keyValue = pair.Split('=');
                    if (keyValue.Length != 2)
                        continue;
                        
                    string key = HttpUtility.UrlDecode(keyValue[0]);
                    string value = HttpUtility.UrlDecode(keyValue[1]);
                    
                    result[key] = value;
                }
            }
            catch (Exception)
            {
                // Handle any parsing exceptions gracefully
                // Return empty dictionary rather than crashing
            }
            
            return result;
        }
    }
}