using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PromptManager.UI.Services;
using PromptManager.UI.ViewModels;
using PromptManager.UI.Views;
using PromptManager.Services;

namespace PromptManager.UI
{
    public partial class App : Application
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = new MainWindow();
                var systemPathProvider = new AvaloniaAppDataPathProvider();
                var settingsStore = new DatabaseLocationSettingsStore(systemPathProvider.AppDataDirectory);
                var customDirectory = settingsStore.Load(out var settingsWarning);
                var resolver = new DatabaseLocationResolver(
                    systemPathProvider.AppDataDirectory,
                    AppContext.BaseDirectory,
                    IsDebugBuild);
                var databaseLocation = resolver.Resolve(customDirectory, settingsWarning);
                var defaultLocation = resolver.Resolve(null);
                var changeService = new DatabaseLocationChangeService(settingsStore);
                string? availabilityWarning = null;
                if (databaseLocation.HasCustomDirectory)
                {
                    try
                    {
                        changeService.NormalizeAndValidate(databaseLocation.Directory, false);
                    }
                    catch (Exception ex)
                    {
                        availabilityWarning = $"The configured database directory is unavailable: {ex.Message} The default location is being used temporarily; your saved choice was retained.";
                        databaseLocation = defaultLocation with { Warning = availabilityWarning };
                    }
                }

                var startupDirectory = databaseLocation.Directory;
                var repository = CreateRepository(startupDirectory, out var startupException);
                var dialogService = new AvaloniaDialogService(window);
                var viewModel = new MainWindowViewModel(
                    repository,
                    () => CreateRepository(startupDirectory, out _),
                    new AvaloniaClipboardService(window),
                    new AvaloniaFileDialogService(window),
                    new AvaloniaLauncherService(),
                    dialogService,
                    new AvaloniaAppInfoService(),
                    changeService,
                    databaseLocation,
                    defaultLocation.Directory);
                window.DataContext = viewModel;

                if (startupException is not null)
                {
                    window.Opened += async (_, _) =>
                        await dialogService.ShowMessageAsync(
                            "Startup failed",
                            $"Prompt storage could not be opened: {startupException.Message}");
                }
                else if (databaseLocation.Warning is not null)
                {
                    window.Opened += async (_, _) =>
                        await dialogService.ShowMessageAsync("Database location warning", databaseLocation.Warning);
                }

                desktop.MainWindow = window;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static IPromptRepository? CreateRepository(
            string databaseDirectory,
            out Exception? exception)
        {
            try
            {
                exception = null;
                return new PromptRepository(databaseDirectory);
            }
            catch (Exception ex)
            {
                exception = ex;
                return null;
            }
        }

        private static bool IsDebugBuild
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }
    }
}
