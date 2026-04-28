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
        public string Details
        {
            get
            {
                if (Folder is not null)
                {
                    return Folder.Description ?? string.Empty;
                }

                if (Prompt is null)
                {
                    return string.Empty;
                }

                var parts = new List<string> { $"Quality {Prompt.Quality}/10" };
                if (!string.IsNullOrWhiteSpace(Prompt.AiModel))
                {
                    parts.Add(Prompt.AiModel);
                }

                if (!string.IsNullOrWhiteSpace(Prompt.Description))
                {
                    parts.Add(Prompt.Description);
                }

                return string.Join(" | ", parts);
            }
        }
        public string Tags => Prompt?.TagsText ?? string.Empty;
        public string Quality => Prompt is null ? string.Empty : $"{Prompt.Quality}/10";
        public string AiModel => Prompt?.AiModel ?? string.Empty;
        public string UpdatedAt => (Folder?.UpdatedAt ?? Prompt?.UpdatedAt ?? DateTime.UtcNow).ToLocalTime().ToString("g");
    }
}
