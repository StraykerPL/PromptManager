using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using PromptManager.Models;

namespace PromptManager.UI.Services
{
    public sealed class AvaloniaClipboardService(Window window) : IClipboardService
    {
        public async Task SetTextAsync(string text)
        {
            if (window.Clipboard is not null)
            {
                await window.Clipboard.SetTextAsync(text);
            }
        }
    }

    public sealed class AvaloniaFileDialogService(Window window) : IFileDialogService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private static readonly FilePickerFileType JsonFileType = new("JSON file")
        {
            Patterns = ["*.json"],
            MimeTypes = ["application/json"]
        };

        public async Task<PromptDataDocument?> OpenJsonAsync()
        {
            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import Prompt Manager data",
                AllowMultiple = false,
                FileTypeFilter = [JsonFileType]
            });

            var file = files.FirstOrDefault();
            if (file is null)
            {
                return null;
            }

            await using var stream = await file.OpenReadAsync();
            return await JsonSerializer.DeserializeAsync<PromptDataDocument>(stream, JsonOptions);
        }

        public async Task<bool> SaveJsonAsync(string suggestedFileName, string json)
        {
            var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Prompt Manager data",
                SuggestedFileName = suggestedFileName,
                DefaultExtension = "json",
                FileTypeChoices = [JsonFileType]
            });

            if (file is null)
            {
                return false;
            }

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(json);
            return true;
        }

        public async Task<string?> SelectDatabaseDirectoryAsync(string? suggestedStartDirectory)
        {
            IStorageFolder? start = null;
            if (!string.IsNullOrWhiteSpace(suggestedStartDirectory))
            {
                try { start = await window.StorageProvider.TryGetFolderFromPathAsync(suggestedStartDirectory); }
                catch { }
            }

            var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Choose database storage directory",
                AllowMultiple = false,
                SuggestedStartLocation = start
            });
            return folders.FirstOrDefault()?.TryGetLocalPath();
        }
    }

    public sealed class AvaloniaLauncherService : ILauncherService
    {
        public Task OpenAsync(string uri)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(uri);
            if (!Uri.TryCreate(uri, UriKind.Absolute, out _))
            {
                throw new ArgumentException("A valid absolute URI is required.", nameof(uri));
            }

            Process? process;
            try
            {
                process = Process.Start(new ProcessStartInfo
                {
                    FileName = uri,
                    UseShellExecute = true
                });
            }
            catch (Exception exception) when (OperatingSystem.IsLinux() &&
                                              exception is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    UseShellExecute = false
                };
                startInfo.ArgumentList.Add(uri);
                process = Process.Start(startInfo);
            }

            if (process is null)
            {
                throw new InvalidOperationException("The default browser could not be started.");
            }

            return Task.CompletedTask;
        }
    }

    public sealed class AvaloniaDialogService(Window window) : IDialogService
    {
        public async Task ShowMessageAsync(string title, string message)
        {
            var dialog = CreateDialog(title, message, ("OK", true));
            await dialog.ShowDialog<bool>(window);
        }

        public async Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        {
            var dialog = CreateDialog(title, message, (accept, true), (cancel, false));
            return await dialog.ShowDialog<bool>(window);
        }

        public async Task<int?> ChooseAsync(string title, string message, params string[] choices)
        {
            var dialog = CreateChoiceDialog(title, message, choices);
            return await dialog.ShowDialog<int?>(window);
        }

        private static Window CreateChoiceDialog(string title, string message, string[] choices)
        {
            var panel = CreateDialogPanel(title, message, out var buttonPanel);
            var dialog = CreateWindow(panel);
            for (var index = 0; index < choices.Length; index++)
            {
                var result = index;
                var button = new Button { Content = choices[index] };
                button.Click += (_, _) => dialog.Close((int?)result);
                buttonPanel.Children.Add(button);
            }
            dialog.Closed += (_, _) => { };
            return dialog;
        }

        private static Window CreateDialog(string title, string message, params (string Label, bool Result)[] buttons)
        {
            var panel = CreateDialogPanel(title, message, out var buttonPanel);
            var dialog = CreateWindow(panel);

            foreach (var button in buttons)
            {
                var control = new Button { Content = button.Label };
                control.Click += (_, _) => dialog.Close(button.Result);
                buttonPanel.Children.Add(control);
            }

            return dialog;
        }

        private static StackPanel CreateDialogPanel(string title, string message, out StackPanel buttonPanel)
        {
            var panel = new StackPanel
            {
                Margin = new global::Avalonia.Thickness(18),
                Spacing = 14
            };
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = global::Avalonia.Media.FontWeight.Bold
            });
            panel.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                Foreground = global::Avalonia.Media.Brush.Parse("#CBD5E1")
            });

            buttonPanel = new StackPanel
            {
                Orientation = global::Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 10
            };
            panel.Children.Add(buttonPanel);
            return panel;
        }

        private static Window CreateWindow(StackPanel panel) => new()
            {
                MinWidth = 420,
                SizeToContent = SizeToContent.WidthAndHeight,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = global::Avalonia.Media.Brush.Parse("#161A1E"),
                Content = panel
            };
    }

    public sealed class AvaloniaAppInfoService : IAppInfoService
    {
        public string Version { get; } =
            Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0";

        public string DotNetVersion { get; } = Environment.Version.ToString();
    }
}
