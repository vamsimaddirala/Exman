using System.Collections.ObjectModel;

namespace Exman.Models
{
    /// <summary>
    /// Represents an environment with variables for API requests
    /// </summary>
    public class RequestEnvironment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "New Environment";
        public string Description { get; set; } = string.Empty;
        public ObservableCollection<Variable> Variables { get; set; } = new();
        public bool IsActive { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}