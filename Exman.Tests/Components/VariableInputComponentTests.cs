using Bunit;
using Exman.Components;
using Exman.Models;
using Exman.Services;
using Exman.Tests.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using System.Collections.ObjectModel;
using Xunit;

namespace Exman.Tests.Components
{
    public class VariableInputComponentTests : TestContext
    {
        private readonly Mock<IEnvironmentService> _mockEnvironmentService;
        private readonly Mock<IJSRuntime> _mockJSRuntime;
        
        public VariableInputComponentTests()
        {
            _mockEnvironmentService = new Mock<IEnvironmentService>();
            _mockJSRuntime = TestHelpers.CreateMockJSRuntime();
            
            // Set up services for component testing
            Services.AddSingleton<IEnvironmentService>(_mockEnvironmentService.Object);
            Services.AddSingleton<IJSRuntime>(_mockJSRuntime.Object);
            
            // Mock environment service response
            _mockEnvironmentService
                .Setup(x => x.GetActiveEnvironmentAsync())
                .ReturnsAsync(CreateTestEnvironment());
        }
        
        [Fact]
        public void VariableInputComponent_ShouldRender()
        {
            // Arrange & Act
            var component = RenderComponent<VariableInputComponent>(parameters => parameters
                .Add(p => p.Value, "Hello World")
                .Add(p => p.Placeholder, "Enter text")
                .Add(p => p.IsMultiline, true)
                .Add(p => p.Rows, 5)
            );
            
            // Assert
            Assert.NotNull(component);
            Assert.Contains("variable-input-container", component.Markup);
            Assert.Contains("multiline", component.Markup);
        }
        
        [Fact]
        public void VariableInputComponent_ShouldHaveCorrectClass_WhenSingleLine()
        {
            // Arrange & Act
            var component = RenderComponent<VariableInputComponent>(parameters => parameters
                .Add(p => p.IsMultiline, false)
                .Add(p => p.CssClass, "custom-class")
            );
            
            // Assert
            Assert.Contains("custom-class", component.Markup);
            Assert.DoesNotContain("multiline", component.Markup);
        }
        
        [Fact]
        public async Task GetVariableValue_ShouldReturnCorrectValue()
        {
            // Arrange
            var component = RenderComponent<VariableInputComponent>();
            
            // Act - Call the method directly
            var result = component.Instance.GetVariableValue("API_KEY");
            
            // Assert
            Assert.Equal("abc123", result);
        }
        
        [Fact]
        public async Task GetMatchingVariables_ShouldReturnMatchingVariables()
        {
            // Arrange
            var component = RenderComponent<VariableInputComponent>();
            
            // Act
            var result = component.Instance.GetMatchingVariables("API");
            
            // Assert
            Assert.Contains("API_KEY", result);
            Assert.Contains("API_URL", result);
            Assert.Equal(2, result.Length);
        }
        
        private static RequestEnvironment CreateTestEnvironment()
        {
            return new RequestEnvironment
            {
                Id = "env1",
                Name = "Test Environment",
                IsActive = true,
                Variables = new ObservableCollection<Variable>
                {
                    new Variable { Key = "API_KEY", Value = "abc123", Enabled = true },
                    new Variable { Key = "API_URL", Value = "https://api.example.com", Enabled = true },
                    new Variable { Key = "USERNAME", Value = "testuser", Enabled = true },
                    new Variable { Key = "DISABLED_VAR", Value = "disabled", Enabled = false }
                }
            };
        }
    }
}