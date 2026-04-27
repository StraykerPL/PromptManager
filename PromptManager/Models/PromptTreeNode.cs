namespace PromptManager.Models
{

    public sealed class PromptTreeNode
    {
        public string Key { get; init; } = string.Empty;
        public PromptFolder? Folder { get; init; }
        public PromptItem? Prompt { get; init; }
        public int Depth { get; init; }
        public bool IsExpanded { get; set; }
        public bool IsFolder => Folder is not null;
        public bool IsPrompt => Prompt is not null;
        public Thickness RowMargin => new(Depth * 18, 0, 0, 0);
        public string Icon => IsFolder ? (IsExpanded ? "v" : ">") : ".";
        public string Name => Folder?.Name ?? Prompt?.Name ?? string.Empty;
        public string Description => Folder?.Description ?? Prompt?.Description ?? string.Empty;
        public string Tags => Prompt?.TagsText ?? string.Empty;
        public string UpdatedAt => (Folder?.UpdatedAt ?? Prompt?.UpdatedAt ?? DateTime.UtcNow).ToLocalTime().ToString("g");
    }
}
