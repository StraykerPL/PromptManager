using PromptManager.Models;

namespace PromptManager.Services
{
    public interface IPromptRepository : IDisposable
    {
        IReadOnlyList<PromptItem> GetPrompts();
        IReadOnlyList<PromptFolder> GetFolders();
        IReadOnlyList<string> GetAvailableTags();
        void SaveAvailableTags(IEnumerable<string> tags);
        void SavePrompt(PromptItem prompt);
        void SaveFolder(PromptFolder folder);
        void DeletePrompt(int promptId);
        void DeleteFolder(int folderId);
    }
}
