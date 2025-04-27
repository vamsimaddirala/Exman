using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moq;
using System.Reflection;

namespace Exman.Tests.Utilities
{
    public static class TestHelpers
    {
        /// <summary>
        /// Creates a mock JSRuntime for testing components that use JavaScript interop
        /// </summary>
        public static Mock<IJSRuntime> CreateMockJSRuntime()
        {
            var mockJSRuntime = new Mock<IJSRuntime>();
            mockJSRuntime
                .Setup(j => j.InvokeAsync<object>(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object[]>()))
                .ReturnsAsync((object)null);
            
            return mockJSRuntime;
        }

        /// <summary>
        /// Creates a mock NavigationManager for testing navigation in components
        /// </summary>
        public static Mock<NavigationManager> CreateMockNavigationManager(string baseUri = "http://localhost/")
        {
            var mockNavManager = new Mock<NavigationManager>();
            
            // Need to set private read-only fields using reflection
            typeof(NavigationManager)
                .GetField("_baseUri", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(mockNavManager.Object, baseUri);
            
            typeof(NavigationManager)
                .GetField("_uri", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(mockNavManager.Object, baseUri);

            return mockNavManager;
        }

        /// <summary>
        /// Helper method to trigger an event callback
        /// </summary>
        public static Task InvokeCallbackAsync<T>(EventCallback<T> callback, T arg)
        {
            return callback.InvokeAsync(arg);
        }

        /// <summary>
        /// Helper method to trigger an event callback with no argument
        /// </summary>
        public static Task InvokeCallbackAsync(EventCallback callback)
        {
            return callback.InvokeAsync();
        }
    }
}