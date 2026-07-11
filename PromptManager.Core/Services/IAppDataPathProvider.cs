namespace PromptManager.Services
{
    public interface IDatabaseDirectoryProvider
    {
        string DatabaseDirectory { get; }
    }

    public interface IAppDataPathProvider
    {
        string AppDataDirectory { get; }
    }
}
