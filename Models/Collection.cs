using System.Collections.ObjectModel;

namespace Exman.Models
{
    /// <summary>
    /// Represents a collection of API requests
    /// </summary>
    public class Collection
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "New Collection";
        public string Description { get; set; } = string.Empty;
        public ObservableCollection<ApiRequest> Requests { get; set; } = new();
        public ObservableCollection<Folder> Folders { get; set; } = new();
        public ObservableCollection<Variable> Variables { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Represents a folder within a collection that can contain API requests
        /// </summary>
        public class Folder
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Name { get; set; } = "New Folder";
            public string Description { get; set; } = string.Empty;
            public ObservableCollection<ApiRequest> Requests { get; set; } = new();
            public ObservableCollection<Folder> Folders { get; set; } = new();
            public string ParentId { get; set; } = string.Empty;
        }
    }
}