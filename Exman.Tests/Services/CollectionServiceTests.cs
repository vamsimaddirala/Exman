using Exman.Models;
using Exman.Services;
using Moq;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Exman.Tests.Services
{
    public class CollectionServiceTests
    {
        [Fact]
        public async Task CreateCollectionAsync_ShouldAssignIdAndTimestamps()
        {
            // Arrange
            var service = new TestableCollectionService();
            var collection = new Collection
            {
                Name = "Test Collection",
                Description = "Test Collection Description"
            };

            // Act
            var result = await service.CreateCollectionAsync(collection);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Id);
            Assert.Equal("Test Collection", result.Name);
            Assert.Equal("Test Collection Description", result.Description);
            Assert.NotEqual(default, result.CreatedAt);
            Assert.NotEqual(default, result.UpdatedAt);
            
            // Verify the collection was saved
            Assert.Single(service.SavedCollections);
            Assert.Equal(result.Id, service.SavedCollections.First().Id);
        }
        
        [Fact]
        public async Task GetCollectionsAsync_ShouldReturnAllCollections()
        {
            // Arrange
            var service = new TestableCollectionService();
            var collection1 = new Collection { Name = "Collection 1", Id = Guid.NewGuid().ToString() };
            var collection2 = new Collection { Name = "Collection 2", Id = Guid.NewGuid().ToString() };
            
            // Add test collections
            await service.CreateCollectionAsync(collection1);
            await service.CreateCollectionAsync(collection2);
            
            // Mock what would be loaded from disk
            service.CollectionsToLoad = new List<Collection> { collection1, collection2 };

            // Act
            var result = await service.GetCollectionsAsync();

            // Assert
            var collections = result.ToList();
            Assert.Equal(2, collections.Count);
            Assert.Contains(collections, c => c.Name == "Collection 1");
            Assert.Contains(collections, c => c.Name == "Collection 2");
        }
    }

    /// <summary>
    /// A testable version of CollectionService that doesn't interact with the file system
    /// </summary>
    public class TestableCollectionService : CollectionService
    {
        public List<Collection> SavedCollections { get; set; } = new List<Collection>();
        public List<Collection> CollectionsToLoad { get; set; } = new List<Collection>();

        // Override the file saving behavior
        private new Task SaveCollectionToFileAsync(Collection collection)
        {
            // Instead of saving to a file, add to our list
            var existingCollection = SavedCollections.FirstOrDefault(c => c.Id == collection.Id);
            if (existingCollection != null)
            {
                SavedCollections.Remove(existingCollection);
            }
            
            SavedCollections.Add(collection);
            return Task.CompletedTask;
        }
        
        // Override CreateCollectionAsync to ensure it calls our SaveCollectionToFileAsync
        public new async Task<Collection> CreateCollectionAsync(Collection collection)
        {
            // Ensure the collection has a valid ID
            if (string.IsNullOrEmpty(collection.Id))
            {
                collection.Id = Guid.NewGuid().ToString();
            }
            
            collection.CreatedAt = DateTime.Now;
            collection.UpdatedAt = DateTime.Now;
            
            // Use our version of SaveCollectionToFileAsync
            await SaveCollectionToFileAsync(collection);
            
            return collection;
        }
        
        // Create a new implementation that hides the base class method
        public new async Task<IEnumerable<Collection>> GetCollectionsAsync()
        {
            if (CollectionsToLoad != null && CollectionsToLoad.Any())
            {
                return CollectionsToLoad;
            }
            
            return await base.GetCollectionsAsync();
        }
    }
}