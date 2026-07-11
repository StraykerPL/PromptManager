using System.Text.Json;
using System.Text.Json.Serialization;
using PromptManager.Models;
using PromptManager.Services;

namespace PromptManager.UI.Services
{
    public enum DatabaseLocationKind
    {
        Custom,
        DebugDefault,
        SystemDefault
    }

    public sealed record DatabaseLocationResolution(
        string Directory,
        DatabaseLocationKind Kind,
        bool HasCustomDirectory,
        string? Warning = null);

    public sealed class DatabaseLocationSettingsStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly string settingsPath;

        public DatabaseLocationSettingsStore(string systemAppDataDirectory)
        {
            settingsPath = Path.Combine(systemAppDataDirectory, "settings.json");
        }

        public string? Load(out string? warning)
        {
            warning = null;
            if (!File.Exists(settingsPath))
            {
                return null;
            }

            try
            {
                var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsPath), JsonOptions);
                if (string.IsNullOrWhiteSpace(settings?.DatabaseDirectory))
                {
                    return null;
                }

                if (!Path.IsPathFullyQualified(settings.DatabaseDirectory))
                {
                    warning = "The saved database directory is not an absolute path. The default location will be used.";
                    return null;
                }

                return Path.GetFullPath(settings.DatabaseDirectory);
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                warning = $"The database location setting could not be read. The default location will be used. {ex.Message}";
                return null;
            }
        }

        public void Save(string? databaseDirectory)
        {
            var directory = Path.GetDirectoryName(settingsPath)!;
            Directory.CreateDirectory(directory);
            var temporaryPath = Path.Combine(directory, $".settings-{Guid.NewGuid():N}.tmp");
            try
            {
                var json = JsonSerializer.Serialize(new Settings { DatabaseDirectory = databaseDirectory }, JsonOptions);
                File.WriteAllText(temporaryPath, json);
                File.Move(temporaryPath, settingsPath, true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        private sealed class Settings
        {
            public string? DatabaseDirectory { get; set; }
        }
    }

    public sealed class DatabaseLocationResolver(
        string systemAppDataDirectory,
        string executableDirectory,
        bool isDebugBuild)
    {
        public DatabaseLocationResolution Resolve(string? customDirectory, string? warning = null)
        {
            if (!string.IsNullOrWhiteSpace(customDirectory))
            {
                return new DatabaseLocationResolution(Path.GetFullPath(customDirectory), DatabaseLocationKind.Custom, true, warning);
            }

            var path = isDebugBuild ? executableDirectory : systemAppDataDirectory;
            return new DatabaseLocationResolution(
                Path.GetFullPath(path),
                isDebugBuild ? DatabaseLocationKind.DebugDefault : DatabaseLocationKind.SystemDefault,
                false,
                warning);
        }
    }

    public enum NewDatabaseAction { CopyCurrent, StartEmpty }
    public enum ExistingDatabaseAction { UseExisting, ReplaceCurrent }

    public sealed record DatabaseLocationChangeResult(bool Changed, string? BackupPath = null, string? Message = null);

    public sealed class DatabaseLocationChangeService(DatabaseLocationSettingsStore settingsStore)
    {
        public bool IsSameLocation(string first, string second) =>
            string.Equals(Normalize(first), Normalize(second), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        public string NormalizeAndValidate(string selectedDirectory, bool createIfMissing)
        {
            if (string.IsNullOrWhiteSpace(selectedDirectory) || !Path.IsPathFullyQualified(selectedDirectory))
            {
                throw new ArgumentException("Select a fully qualified directory path.");
            }

            var directory = Normalize(selectedDirectory);
            if (File.Exists(directory))
            {
                throw new ArgumentException("The selected path is a file. Select a directory instead.");
            }

            if (!Directory.Exists(directory))
            {
                if (!createIfMissing)
                {
                    throw new DirectoryNotFoundException("The selected directory does not exist.");
                }
                Directory.CreateDirectory(directory);
            }

            var probe = Path.Combine(directory, $".promptmanager-write-{Guid.NewGuid():N}.tmp");
            try { using (File.Create(probe)) { } }
            finally { if (File.Exists(probe)) File.Delete(probe); }
            return directory;
        }

        public DatabaseLocationChangeResult Apply(
            string destinationDirectory,
            IPromptRepository currentRepository,
            NewDatabaseAction newAction,
            ExistingDatabaseAction existingAction,
            bool clearSetting = false)
        {
            var directory = NormalizeAndValidate(destinationDirectory, true);
            var databasePath = Path.Combine(directory, "prompts.db");
            var existed = File.Exists(databasePath);
            string? backupPath = null;
            PromptDataDocument? snapshot = null;

            if ((!existed && newAction == NewDatabaseAction.CopyCurrent) ||
                (existed && existingAction == ExistingDatabaseAction.ReplaceCurrent))
            {
                snapshot = currentRepository.ExportData();
            }

            using (var validationRepository = new PromptRepository(directory))
            {
                ReadAll(validationRepository);
            }

            if (existed && existingAction == ExistingDatabaseAction.ReplaceCurrent)
            {
                backupPath = CreateBackupPath(directory);
                File.Copy(databasePath, backupPath);
            }

            if (snapshot is not null)
            {
                using var destination = new PromptRepository(directory);
                destination.ImportData(snapshot);
                ReadAll(destination);
            }

            using (var reopened = new PromptRepository(directory))
            {
                ReadAll(reopened);
            }

            settingsStore.Save(clearSetting ? null : directory);
            return new DatabaseLocationChangeResult(true, backupPath);
        }

        private static void ReadAll(IPromptRepository repository)
        {
            _ = repository.GetPrompts();
            _ = repository.GetFolders();
            _ = repository.GetAvailableTags();
            _ = repository.GetAvailableModels();
        }

        private static string Normalize(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

        private static string CreateBackupPath(string directory)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var path = Path.Combine(directory, $"prompts.location-backup-{timestamp}.db");
            for (var index = 1; File.Exists(path); index++)
            {
                path = Path.Combine(directory, $"prompts.location-backup-{timestamp}-{index}.db");
            }
            return path;
        }
    }

    public sealed class StaticDatabaseDirectoryProvider(string directory) : IDatabaseDirectoryProvider
    {
        public string DatabaseDirectory { get; } = directory;
    }
}
