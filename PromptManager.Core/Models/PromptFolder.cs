namespace PromptManager.Models
{

    public sealed class PromptFolder
    {
        public int Id { get; set; }
        public int? ParentFolderId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
