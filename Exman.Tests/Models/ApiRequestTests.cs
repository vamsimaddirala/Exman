using Exman.Models;
using System.Collections.ObjectModel;
using Xunit;
// Explicitly use Exman.Models.KeyValuePair to avoid ambiguity
using KVPair = Exman.Models.KeyValuePair;

namespace Exman.Tests.Models
{
    public class ApiRequestTests
    {
        [Fact]
        public void Clone_ShouldCreateDeepCopy()
        {
            // Arrange
            var original = new ApiRequest
            {
                Name = "Test Request",
                Method = ApiHttpMethod.POST,
                Url = "https://api.example.com/test",
                FollowRedirects = false
            };
            
            original.Headers.Add(new KVPair { Key = "Content-Type", Value = "application/json" });
            original.QueryParameters.Add(new KVPair { Key = "param1", Value = "value1" });
            original.Body.RawContent = "{ \"test\": \"data\" }";
            original.Body.ContentType = "application/json";

            // Act
            var clone = original.Clone();

            // Assert
            Assert.NotSame(original, clone);
            Assert.Equal(original.Id, clone.Id);
            Assert.Equal(original.Name, clone.Name);
            Assert.Equal(original.Method, clone.Method);
            Assert.Equal(original.Url, clone.Url);
            Assert.Equal(original.FollowRedirects, clone.FollowRedirects);
            
            // Check collections are deep copied
            Assert.NotSame(original.Headers, clone.Headers);
            Assert.Equal(original.Headers.Count, clone.Headers.Count);
            Assert.Equal(original.Headers[0].Key, clone.Headers[0].Key);
            Assert.Equal(original.Headers[0].Value, clone.Headers[0].Value);
            
            Assert.NotSame(original.QueryParameters, clone.QueryParameters);
            Assert.Equal(original.QueryParameters.Count, clone.QueryParameters.Count);
            
            // Check body content is copied
            Assert.NotSame(original.Body, clone.Body);
            Assert.Equal(original.Body.RawContent, clone.Body.RawContent);
            Assert.Equal(original.Body.ContentType, clone.Body.ContentType);

            // Verify modifying clone doesn't affect original
            clone.Headers[0].Value = "changed";
            Assert.NotEqual(original.Headers[0].Value, clone.Headers[0].Value);
        }
    }
}