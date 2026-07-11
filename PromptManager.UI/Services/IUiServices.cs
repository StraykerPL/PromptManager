using PromptManager.Models;

namespace PromptManager.UI.Services
{
    public interface IClipboardService
    {
        Task SetTextAsync(string text);
    }

    public interface IFileDialogService
    {
        Task<PromptDataDocument?> OpenJsonAsync();
        Task<bool> SaveJsonAsync(string suggestedFileName, string json);
        Task<string?> SelectDatabaseDirectoryAsync(string? suggestedStartDirectory);
    }

    public interface ILauncherService
    {
        Task OpenAsync(string uri);
    }

    public interface IDialogService
    {
        Task ShowMessageAsync(string title, string message);
        Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
        Task<int?> ChooseAsync(string title, string message, params string[] choices);
    }

    public interface IAppInfoService
    {
        string Version { get; }
        string DotNetVersion { get; }
    }
}
