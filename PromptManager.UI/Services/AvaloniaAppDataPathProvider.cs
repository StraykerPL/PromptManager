using PromptManager.Services;

namespace PromptManager.UI.Services
{
    public enum DesktopPlatform
    {
        Windows,
        Linux
    }

    public sealed class AvaloniaAppDataPathProvider : IAppDataPathProvider
    {
        private readonly DesktopPlatform platform;
        private readonly Func<Environment.SpecialFolder, string> getFolderPath;
        private readonly Func<string, string?> getEnvironmentVariable;

        public AvaloniaAppDataPathProvider()
            : this(
                OperatingSystem.IsWindows() ? DesktopPlatform.Windows : DesktopPlatform.Linux,
                Environment.GetFolderPath,
                Environment.GetEnvironmentVariable)
        {
        }

        public AvaloniaAppDataPathProvider(
            DesktopPlatform platform,
            Func<Environment.SpecialFolder, string> getFolderPath,
            Func<string, string?> getEnvironmentVariable)
        {
            this.platform = platform;
            this.getFolderPath = getFolderPath ?? throw new ArgumentNullException(nameof(getFolderPath));
            this.getEnvironmentVariable = getEnvironmentVariable ?? throw new ArgumentNullException(nameof(getEnvironmentVariable));
        }

        public string AppDataDirectory
        {
            get
            {
                string basePath;
                if (platform == DesktopPlatform.Windows)
                {
                    basePath = getFolderPath(Environment.SpecialFolder.ApplicationData);
                }

                else
                {
                    var xdgDataHome = getEnvironmentVariable("XDG_DATA_HOME");
                    if (!string.IsNullOrWhiteSpace(xdgDataHome) && Path.IsPathFullyQualified(xdgDataHome))
                    {
                        basePath = xdgDataHome;
                    }
                    else
                    {
                        var homePath = getFolderPath(Environment.SpecialFolder.UserProfile);
                        basePath = string.IsNullOrWhiteSpace(homePath)
                            ? string.Empty
                            : Path.Combine(homePath, ".local", "share");
                    }
                }

                if (string.IsNullOrWhiteSpace(basePath) || !Path.IsPathFullyQualified(basePath))
                {
                    throw new InvalidOperationException(
                        "Prompt Manager could not determine an absolute application-data directory for the current user.");
                }

                return Path.Combine(basePath, "PromptManager");
            }
        }
    }
}
