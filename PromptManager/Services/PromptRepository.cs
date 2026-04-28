using LiteDB;
using PromptManager.Models;

namespace PromptManager.Services
{

    public sealed class PromptRepository : IPromptRepository
    {
        private LiteDatabase database = null!;
        private ILiteCollection<PromptItem> prompts = null!;
        private ILiteCollection<PromptFolder> folders = null!;
        private ILiteCollection<PromptTagOption> tagOptions = null!;

        public PromptRepository()
        {
            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "prompts.db");
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

            Initialize(databasePath);
        }

        internal PromptRepository(
            ILiteCollection<PromptItem> prompts,
            ILiteCollection<PromptFolder> folders,
            ILiteCollection<PromptTagOption> tagOptions)
        {
            this.prompts = prompts;
            this.folders = folders;
            this.tagOptions = tagOptions;
        }

        private void Initialize(string path)
        {
            try
            {
                OpenCollections(path);
            }
            catch (Exception) when (File.Exists(path))
            {
                database?.Dispose();
                File.Move(path, CreateBackupPath(path));
                OpenCollections(path);
            }
        }

        private void OpenCollections(string path)
        {
            database = new LiteDatabase(path);
            prompts = database.GetCollection<PromptItem>("prompts");
            folders = database.GetCollection<PromptFolder>("folders");
            tagOptions = database.GetCollection<PromptTagOption>("tagOptions");

            prompts.EnsureIndex(prompt => prompt.Name);
            prompts.EnsureIndex(prompt => prompt.FolderId);
            folders.EnsureIndex(folder => folder.Name);
            folders.EnsureIndex(folder => folder.ParentFolderId);
            tagOptions.EnsureIndex(tag => tag.Name, unique: true);
        }

        private static string CreateBackupPath(string path)
        {
            var directory = Path.GetDirectoryName(path)!;
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var backupPath = Path.Combine(directory, $"prompts.corrupt-{timestamp}.db");

            for (var index = 1; File.Exists(backupPath); index++)
            {
                backupPath = Path.Combine(directory, $"prompts.corrupt-{timestamp}-{index}.db");
            }

            return backupPath;
        }

        public IReadOnlyList<PromptItem> GetPrompts() =>
            prompts.FindAll()
                .Select(NormalizePrompt)
                .OrderBy(prompt => prompt.Name)
                .ToList();

        public IReadOnlyList<PromptFolder> GetFolders() =>
            folders.FindAll()
                .Select(NormalizeFolder)
                .OrderBy(folder => folder.Name)
                .ToList();

        public IReadOnlyList<string> GetAvailableTags() =>
            tagOptions.FindAll()
                .Select(tag => tag.Name?.Trim() ?? string.Empty)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();

        public void SaveAvailableTags(IEnumerable<string> tags)
        {
            tagOptions.DeleteAll();

            foreach (var tag in NormalizeTags(tags))
            {
                tagOptions.Insert(new PromptTagOption { Name = tag });
            }
        }

        public void SavePrompt(PromptItem prompt)
        {
            NormalizePrompt(prompt);
            prompt.UpdatedAt = DateTime.UtcNow;

            if (prompt.Id == 0)
            {
                prompt.CreatedAt = prompt.UpdatedAt;
                prompt.Id = prompts.Insert(prompt).AsInt32;
                return;
            }

            prompts.Update(prompt);
        }

        public void SaveFolder(PromptFolder folder)
        {
            NormalizeFolder(folder);
            folder.UpdatedAt = DateTime.UtcNow;

            if (folder.Id == 0)
            {
                folder.CreatedAt = folder.UpdatedAt;
                folder.Id = folders.Insert(folder).AsInt32;
                return;
            }

            folders.Update(folder);
        }

        private static PromptItem NormalizePrompt(PromptItem prompt)
        {
            prompt.Name ??= string.Empty;
            prompt.Description ??= string.Empty;
            prompt.Content ??= string.Empty;
            prompt.Tags = NormalizeTags(prompt.Tags).ToList();
            return prompt;
        }

        private static PromptFolder NormalizeFolder(PromptFolder folder)
        {
            folder.Name ??= string.Empty;
            folder.Description ??= string.Empty;
            return folder;
        }

        private static IEnumerable<string> NormalizeTags(IEnumerable<string>? tags) =>
            (tags ?? [])
                .Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase);

        public void DeletePrompt(int promptId) => prompts.Delete(promptId);

        public void DeleteFolder(int folderId)
        {
            var descendants = GetFolderAndDescendantIds(folderId);

            foreach (var prompt in prompts.Find(prompt => prompt.FolderId != null && descendants.Contains(prompt.FolderId.Value)))
            {
                prompts.Delete(prompt.Id);
            }

            foreach (var id in descendants)
            {
                folders.Delete(id);
            }
        }

        private HashSet<int> GetFolderAndDescendantIds(int folderId)
        {
            var allFolders = GetFolders();
            var ids = new HashSet<int> { folderId };
            var changed = true;

            while (changed)
            {
                changed = false;

                foreach (var folder in allFolders)
                {
                    if (folder.ParentFolderId is int parentId && ids.Contains(parentId) && ids.Add(folder.Id))
                    {
                        changed = true;
                    }
                }
            }

            return ids;
        }

        public void Dispose() => database?.Dispose();
    }
}
