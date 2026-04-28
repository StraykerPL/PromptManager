using PromptManager.Models;

namespace PromptManager.Services
{
    public interface IPromptRepository : IDisposable
    {
        IReadOnlyList<PromptItem> GetPrompts();
        IReadOnlyList<PromptFolder> GetFolders();
        IReadOnlyList<string> GetAvailableTags();
        IReadOnlyList<string> GetAvailableModels();
        void SaveAvailableTags(IEnumerable<string> tags);
        void SaveAvailableModels(IEnumerable<string> models);
        void SavePrompt(PromptItem prompt);
        void SaveFolder(PromptFolder folder);
        void DeletePrompt(int promptId);
        void DeleteFolder(int folderId);
    }
}
