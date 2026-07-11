namespace PromptManager.Models
{
    public sealed class PromptDataDocument
    {
        public int Version { get; set; } = 1;
        public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;
        public List<PromptFolder> Folders { get; set; } = [];
        public List<PromptItem> Prompts { get; set; } = [];
        public List<string> Tags { get; set; } = [];
        public List<string> Models { get; set; } = [];
    }
}
