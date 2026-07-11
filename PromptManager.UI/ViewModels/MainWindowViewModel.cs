using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using Avalonia.Media;
using PromptManager.UI.Services;
using PromptManager.Models;
using PromptManager.Services;

namespace PromptManager.UI.ViewModels
{
    public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        private static readonly JsonSerializerOptions ExportJsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private readonly Func<IPromptRepository?> repositoryFactory;
        private readonly IClipboardService clipboardService;
        private readonly IFileDialogService fileDialogService;
        private readonly ILauncherService launcherService;
        private readonly IDialogService dialogService;
        private readonly DatabaseLocationChangeService databaseLocationChangeService;
        private readonly string defaultDatabaseDirectory;
        private readonly PromptTreeService treeService = new();
        private readonly List<int> expandedFolderIds = [];
        private readonly HashSet<string> selectedPromptTags = new(StringComparer.OrdinalIgnoreCase);

        private IPromptRepository? repository;
        private List<PromptFolder> folders = [];
        private List<PromptItem> prompts = [];
        private List<string> availableTags = [];
        private List<string> availableModels = [];
        private PromptItem? selectedPrompt;
        private PromptFolder? selectedFolder;
        private bool editingFolder;
        private bool showAllPrompts;
        private string searchText = string.Empty;
        private string promptName = string.Empty;
        private string promptDescription = string.Empty;
        private string promptContent = string.Empty;
        private int promptQuality = 5;
        private string folderName = string.Empty;
        private string folderDescription = string.Empty;
        private string newTagName = string.Empty;
        private string newModelName = string.Empty;
        private FolderChoice? selectedPromptFolder;
        private FolderChoice? selectedParentFolder;
        private ModelChoice? selectedModel;
        private bool isSettingsVisible;
        private bool isAboutVisible;
        private string databaseDirectory;
        private string databaseLocationKind;
        private bool hasCustomDatabaseDirectory;

        public MainWindowViewModel(
            IPromptRepository? repository,
            Func<IPromptRepository?> repositoryFactory,
            IClipboardService clipboardService,
            IFileDialogService fileDialogService,
            ILauncherService launcherService,
            IDialogService dialogService,
            IAppInfoService appInfoService,
            DatabaseLocationChangeService databaseLocationChangeService,
            DatabaseLocationResolution databaseLocation,
            string defaultDatabaseDirectory)
        {
            this.repository = repository;
            this.repositoryFactory = repositoryFactory;
            this.clipboardService = clipboardService;
            this.fileDialogService = fileDialogService;
            this.launcherService = launcherService;
            this.dialogService = dialogService;
            this.databaseLocationChangeService = databaseLocationChangeService;
            this.defaultDatabaseDirectory = defaultDatabaseDirectory;
            databaseDirectory = databaseLocation.Directory;
            databaseLocationKind = FormatLocationKind(databaseLocation.Kind);
            hasCustomDatabaseDirectory = databaseLocation.HasCustomDirectory;
            AppVersion = appInfoService.Version;
            DotNetVersion = appInfoService.DotNetVersion;

            ShowSettingsCommand = new RelayCommand(_ => ShowSettings());
            CloseSettingsCommand = new RelayCommand(_ => IsSettingsVisible = false);
            ShowAboutCommand = new RelayCommand(_ => IsAboutVisible = true);
            CloseAboutCommand = new RelayCommand(_ => IsAboutVisible = false);
            ToggleBrowseModeCommand = new RelayCommand(_ => ToggleBrowseMode());
            NewPromptCommand = new RelayCommand(_ => StartNewPrompt());
            NewFolderCommand = new RelayCommand(_ => StartNewFolder());
            SaveCommand = new AsyncRelayCommand(_ => SaveAsync());
            DeleteCommand = new AsyncRelayCommand(_ => DeleteAsync());
            CopyPromptCommand = new AsyncRelayCommand(_ => CopyPromptAsync());
            CopyFromListCommand = new AsyncRelayCommand(CopyFromListAsync);
            ImportCommand = new AsyncRelayCommand(_ => ImportAsync());
            ExportCommand = new AsyncRelayCommand(_ => ExportAsync());
            AddTagCommand = new AsyncRelayCommand(_ => AddTagAsync());
            RemoveTagCommand = new AsyncRelayCommand(RemoveTagAsync);
            ToggleTagCommand = new RelayCommand(ToggleTag);
            AddModelCommand = new AsyncRelayCommand(_ => AddModelAsync());
            RemoveModelCommand = new AsyncRelayCommand(RemoveModelAsync);
            OpenRepositoryCommand = new AsyncRelayCommand(_ => OpenRepositoryAsync());
            ChooseDatabaseDirectoryCommand = new AsyncRelayCommand(_ => ChooseDatabaseDirectoryAsync());
            RestoreDefaultDatabaseDirectoryCommand = new AsyncRelayCommand(_ => RestoreDefaultDatabaseDirectoryAsync(), _ => HasCustomDatabaseDirectory);

            LoadData();
            StartNewPrompt();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<PromptTreeNode> TreeNodes { get; } = [];
        public ObservableCollection<FolderChoice> FolderChoices { get; } = [];
        public ObservableCollection<ModelChoice> ModelChoices { get; } = [];
        public ObservableCollection<TagChipView> PromptTagChips { get; } = [];
        public ObservableCollection<string> TagsDialogRows { get; } = [];
        public ObservableCollection<string> ModelsDialogRows { get; } = [];

        public ICommand ShowSettingsCommand { get; }
        public ICommand CloseSettingsCommand { get; }
        public ICommand ShowAboutCommand { get; }
        public ICommand CloseAboutCommand { get; }
        public ICommand ToggleBrowseModeCommand { get; }
        public ICommand NewPromptCommand { get; }
        public ICommand NewFolderCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CopyPromptCommand { get; }
        public ICommand CopyFromListCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand AddTagCommand { get; }
        public ICommand RemoveTagCommand { get; }
        public ICommand ToggleTagCommand { get; }
        public ICommand AddModelCommand { get; }
        public ICommand RemoveModelCommand { get; }
        public ICommand OpenRepositoryCommand { get; }
        public ICommand ChooseDatabaseDirectoryCommand { get; }
        public ICommand RestoreDefaultDatabaseDirectoryCommand { get; }

        public string AppVersion { get; }
        public string DotNetVersion { get; }
        public string BrowseModeText => showAllPrompts ? "Groups tree" : "All prompts";
        public string CurrentModeText => editingFolder ? selectedFolder?.Id > 0 ? "Edit folder" : "New folder" : selectedPrompt?.Id > 0 ? "Edit prompt" : "New prompt";
        public bool IsPromptEditorVisible => !editingFolder;
        public bool IsFolderEditorVisible => editingFolder;
        public bool CanCopyPrompt => selectedPrompt?.Id > 0 && !editingFolder;
        public bool CanDelete => editingFolder ? selectedFolder?.Id > 0 : selectedPrompt?.Id > 0;
        public string PromptQualityText => PromptQuality switch
        {
            1 => "Quality: 1/10 - does not work",
            10 => "Quality: 10/10 - works perfectly",
            _ => $"Quality: {PromptQuality}/10"
        };

        public string SearchText
        {
            get => searchText;
            set
            {
                if (SetProperty(ref searchText, value ?? string.Empty))
                {
                    showAllPrompts = false;
                    OnPropertyChanged(nameof(BrowseModeText));
                    RefreshTree();
                }
            }
        }

        public string PromptName { get => promptName; set => SetProperty(ref promptName, value ?? string.Empty); }
        public string PromptDescription { get => promptDescription; set => SetProperty(ref promptDescription, value ?? string.Empty); }
        public string PromptContent { get => promptContent; set => SetProperty(ref promptContent, value ?? string.Empty); }
        public int PromptQuality
        {
            get => promptQuality;
            set
            {
                if (SetProperty(ref promptQuality, Math.Clamp(value, 1, 10)))
                {
                    OnPropertyChanged(nameof(PromptQualityText));
                }
            }
        }

        public string FolderName { get => folderName; set => SetProperty(ref folderName, value ?? string.Empty); }
        public string FolderDescription { get => folderDescription; set => SetProperty(ref folderDescription, value ?? string.Empty); }
        public string NewTagName { get => newTagName; set => SetProperty(ref newTagName, value ?? string.Empty); }
        public string NewModelName { get => newModelName; set => SetProperty(ref newModelName, value ?? string.Empty); }
        public FolderChoice? SelectedPromptFolder { get => selectedPromptFolder; set => SetProperty(ref selectedPromptFolder, value); }
        public FolderChoice? SelectedParentFolder { get => selectedParentFolder; set => SetProperty(ref selectedParentFolder, value); }
        public ModelChoice? SelectedModel { get => selectedModel; set => SetProperty(ref selectedModel, value); }
        public bool IsSettingsVisible { get => isSettingsVisible; set => SetProperty(ref isSettingsVisible, value); }
        public bool IsAboutVisible { get => isAboutVisible; set => SetProperty(ref isAboutVisible, value); }
        public string DatabaseDirectory { get => databaseDirectory; private set => SetProperty(ref databaseDirectory, value); }
        public string DatabaseLocationKind { get => databaseLocationKind; private set => SetProperty(ref databaseLocationKind, value); }
        public bool HasCustomDatabaseDirectory
        {
            get => hasCustomDatabaseDirectory;
            private set
            {
                if (SetProperty(ref hasCustomDatabaseDirectory, value) && RestoreDefaultDatabaseDirectoryCommand is AsyncRelayCommand command)
                {
                    command.RaiseCanExecuteChanged();
                }
            }
        }

        public void SelectTreeNode(PromptTreeNode node)
        {
            if (node.Folder is not null)
            {
                EditFolder(node.Folder);
                ToggleFolderNode(node);
                return;
            }

            if (node.Prompt is not null)
            {
                EditPrompt(node.Prompt);
            }
        }

        private void LoadData()
        {
            if (repository is null)
            {
                folders = [];
                prompts = [];
                availableTags = [];
                availableModels = [];
            }
            else
            {
                folders = repository.GetFolders().ToList();
                prompts = repository.GetPrompts().ToList();
                availableTags = repository.GetAvailableTags().ToList();
                availableModels = repository.GetAvailableModels().ToList();
            }

            RefreshFolderChoices();
            RefreshModelChoices();
            RenderPromptTagChips();
            RenderSettingsDialog();
            RefreshTree();
        }

        private async Task<bool> EnsureRepositoryAvailable()
        {
            if (repository is not null)
            {
                return true;
            }

            repository = repositoryFactory();
            if (repository is not null)
            {
                LoadData();
                return true;
            }

            await dialogService.ShowMessageAsync("Storage unavailable", "Prompt storage could not be opened.");
            return false;
        }

        private void RefreshFolderChoices()
        {
            var promptFolderId = SelectedPromptFolder?.Id;
            var parentFolderId = SelectedParentFolder?.Id;
            FolderChoices.Clear();
            FolderChoices.Add(new FolderChoice(null, "No folder"));

            foreach (var folder in folders.Select(folder => new FolderChoice(folder.Id, treeService.BuildFolderPath(folder, folders))))
            {
                FolderChoices.Add(folder);
            }

            SelectedPromptFolder = FolderChoices.FirstOrDefault(choice => choice.Id == promptFolderId) ?? FolderChoices.FirstOrDefault();
            SelectedParentFolder = FolderChoices.FirstOrDefault(choice => choice.Id == parentFolderId) ?? FolderChoices.FirstOrDefault();
        }

        private void RefreshModelChoices()
        {
            var modelName = SelectedModel?.Name;
            ModelChoices.Clear();
            ModelChoices.Add(new ModelChoice(null, "No model"));

            foreach (var model in availableModels.Select(model => new ModelChoice(model, model)))
            {
                ModelChoices.Add(model);
            }

            SelectedModel = ModelChoices.FirstOrDefault(choice => string.Equals(choice.Name, modelName, StringComparison.OrdinalIgnoreCase)) ?? ModelChoices.FirstOrDefault();
        }

        private void RefreshTree()
        {
            TreeNodes.Clear();
            foreach (var node in treeService.BuildTree(folders, prompts, expandedFolderIds, showAllPrompts, SearchText))
            {
                TreeNodes.Add(node);
            }
        }

        private void ToggleFolderNode(PromptTreeNode node)
        {
            if (node.Folder is null)
            {
                return;
            }

            if (!expandedFolderIds.Remove(node.Folder.Id))
            {
                expandedFolderIds.Add(node.Folder.Id);
            }

            RefreshTree();
        }

        private void ToggleBrowseMode()
        {
            var nextShowAllPrompts = !showAllPrompts;
            if (!string.IsNullOrEmpty(SearchText))
            {
                SearchText = string.Empty;
            }

            showAllPrompts = nextShowAllPrompts;
            OnPropertyChanged(nameof(BrowseModeText));
            RefreshTree();
        }

        private void ShowSettings()
        {
            NewTagName = string.Empty;
            NewModelName = string.Empty;
            RenderSettingsDialog();
            IsSettingsVisible = true;
        }

        private async Task OpenRepositoryAsync()
        {
            try
            {
                await launcherService.OpenAsync("https://github.com/StraykerPL/PromptManager");
            }
            catch (Exception ex)
            {
                await dialogService.ShowMessageAsync("Could not open browser", ex.Message);
            }
        }

        private async Task ChooseDatabaseDirectoryAsync()
        {
            var selected = await fileDialogService.SelectDatabaseDirectoryAsync(DatabaseDirectory);
            if (selected is null || repository is null)
            {
                return;
            }

            await ChangeDatabaseDirectoryAsync(selected, clearSetting: false);
        }

        private async Task RestoreDefaultDatabaseDirectoryAsync()
        {
            if (!HasCustomDatabaseDirectory || repository is null)
            {
                return;
            }

            await ChangeDatabaseDirectoryAsync(defaultDatabaseDirectory, clearSetting: true);
        }

        private async Task ChangeDatabaseDirectoryAsync(string selected, bool clearSetting)
        {
            try
            {
                var normalized = databaseLocationChangeService.NormalizeAndValidate(selected, false);
                if (databaseLocationChangeService.IsSameLocation(normalized, DatabaseDirectory))
                {
                    return;
                }

                var databasePath = Path.Combine(normalized, "prompts.db");
                var exists = File.Exists(databasePath);
                var newAction = NewDatabaseAction.StartEmpty;
                var existingAction = ExistingDatabaseAction.UseExisting;

                if (!exists)
                {
                    var choice = await dialogService.ChooseAsync(
                        "Move database storage",
                        "This directory has no prompts.db. Copy the current data, start with an empty database, or cancel?",
                        "Copy current data", "Start empty", "Cancel");
                    if (choice is null or 2) return;
                    newAction = choice == 0 ? NewDatabaseAction.CopyCurrent : NewDatabaseAction.StartEmpty;
                }
                else
                {
                    var choice = await dialogService.ChooseAsync(
                        "Database already exists",
                        "Use the existing prompts.db, replace it with current data, or cancel?",
                        "Use existing", "Replace with current", "Cancel");
                    if (choice is null or 2) return;
                    existingAction = choice == 0 ? ExistingDatabaseAction.UseExisting : ExistingDatabaseAction.ReplaceCurrent;
                    if (existingAction == ExistingDatabaseAction.ReplaceCurrent &&
                        !await dialogService.ConfirmAsync(
                            "Confirm database replacement",
                            "Replace the destination database? A timestamped backup will be created first.",
                            "Replace and back up", "Cancel"))
                    {
                        return;
                    }
                }

                var result = databaseLocationChangeService.Apply(normalized, repository!, newAction, existingAction, clearSetting);
                DatabaseDirectory = normalized;
                HasCustomDatabaseDirectory = !clearSetting;
                DatabaseLocationKind = clearSetting
#if DEBUG
                    ? "Debug default"
#else
                    ? "System default"
#endif
                    : "Custom";
                var backup = result.BackupPath is null ? string.Empty : $" A backup was created at {result.BackupPath}.";
                await dialogService.ShowMessageAsync("Database location saved", $"The new location will be used after restart.{backup}");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                await dialogService.ShowMessageAsync("Database location not changed", ex.Message);
            }
        }

        private static string FormatLocationKind(DatabaseLocationKind kind) => kind switch
        {
            Services.DatabaseLocationKind.Custom => "Custom",
            Services.DatabaseLocationKind.DebugDefault => "Debug default",
            _ => "System default"
        };

        private async Task ImportAsync()
        {
            if (!await EnsureRepositoryAvailable())
            {
                return;
            }

            var confirm = await dialogService.ConfirmAsync(
                "Import data",
                "Importing a JSON file will replace the current prompts, folders, tags, and models. Continue?",
                "Import",
                "Cancel");
            if (!confirm)
            {
                return;
            }

            try
            {
                var document = await fileDialogService.OpenJsonAsync();
                if (document is null)
                {
                    return;
                }

                repository!.ImportData(document);
                expandedFolderIds.Clear();
                showAllPrompts = false;
                SearchText = string.Empty;
                LoadData();
                StartNewPrompt();
                await dialogService.ShowMessageAsync("Import complete", "Prompt Manager data was imported.");
            }
            catch (JsonException)
            {
                await dialogService.ShowMessageAsync("Import failed", "The selected file is not valid Prompt Manager JSON data.");
            }
            catch (Exception ex)
            {
                await dialogService.ShowMessageAsync("Import failed", ex.Message);
            }
        }

        private async Task ExportAsync()
        {
            if (!await EnsureRepositoryAvailable())
            {
                return;
            }

            try
            {
                var json = JsonSerializer.Serialize(repository!.ExportData(), ExportJsonOptions);
                var fileName = $"prompt-manager-export-{DateTime.Now:yyyyMMdd-HHmmss}.json";
                if (await fileDialogService.SaveJsonAsync(fileName, json))
                {
                    await dialogService.ShowMessageAsync("Export complete", "Prompt Manager data was exported.");
                }
            }
            catch (Exception ex)
            {
                await dialogService.ShowMessageAsync("Export failed", ex.Message);
            }
        }

        private async Task AddTagAsync()
        {
            if (!await EnsureRepositoryAvailable())
            {
                return;
            }

            var tag = NewTagName.Trim();
            if (string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            if (!availableTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                availableTags.Add(tag);
                SortAvailableTags();
                repository!.SaveAvailableTags(availableTags);
            }

            NewTagName = string.Empty;
            RenderSettingsDialog();
            RenderPromptTagChips();
        }

        private async Task RemoveTagAsync(object? parameter)
        {
            if (!await EnsureRepositoryAvailable() || parameter is not string tag)
            {
                return;
            }

            availableTags.RemoveAll(candidate => string.Equals(candidate, tag, StringComparison.OrdinalIgnoreCase));
            selectedPromptTags.Remove(tag);
            repository!.SaveAvailableTags(availableTags);
            RenderSettingsDialog();
            RenderPromptTagChips();
        }

        private async Task AddModelAsync()
        {
            if (!await EnsureRepositoryAvailable())
            {
                return;
            }

            var model = NewModelName.Trim();
            if (string.IsNullOrWhiteSpace(model))
            {
                return;
            }

            if (!availableModels.Contains(model, StringComparer.OrdinalIgnoreCase))
            {
                availableModels.Add(model);
                SortAvailableModels();
                repository!.SaveAvailableModels(availableModels);
                RefreshModelChoices();
            }

            NewModelName = string.Empty;
            RenderSettingsDialog();
        }

        private async Task RemoveModelAsync(object? parameter)
        {
            if (!await EnsureRepositoryAvailable() || parameter is not string model)
            {
                return;
            }

            availableModels.RemoveAll(candidate => string.Equals(candidate, model, StringComparison.OrdinalIgnoreCase));
            repository!.SaveAvailableModels(availableModels);
            RefreshModelChoices();
            RenderSettingsDialog();
        }

        private void RenderSettingsDialog()
        {
            TagsDialogRows.Clear();
            foreach (var tag in availableTags)
            {
                TagsDialogRows.Add(tag);
            }

            ModelsDialogRows.Clear();
            foreach (var model in availableModels)
            {
                ModelsDialogRows.Add(model);
            }
        }

        private void RenderPromptTagChips()
        {
            PromptTagChips.Clear();
            foreach (var tag in availableTags)
            {
                PromptTagChips.Add(new TagChipView(tag, selectedPromptTags.Contains(tag)));
            }
        }

        private void ToggleTag(object? parameter)
        {
            var tag = parameter switch
            {
                TagChipView chip => chip.Name,
                string value => value,
                _ => null
            };

            if (string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            if (!selectedPromptTags.Add(tag))
            {
                selectedPromptTags.Remove(tag);
            }

            RenderPromptTagChips();
        }

        private void SortAvailableTags() =>
            availableTags = availableTags
                .Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();

        private void SortAvailableModels() =>
            availableModels = availableModels
                .Select(model => model.Trim())
                .Where(model => !string.IsNullOrWhiteSpace(model))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();

        private void StartNewPrompt()
        {
            editingFolder = false;
            selectedPrompt = new PromptItem();
            selectedFolder = null;
            PromptName = string.Empty;
            PromptDescription = string.Empty;
            selectedPromptTags.Clear();
            RenderPromptTagChips();
            PromptQuality = 5;
            SelectedModel = ModelChoices.FirstOrDefault();
            PromptContent = string.Empty;
            SelectedPromptFolder = FolderChoices.FirstOrDefault();
            NotifyEditorState();
        }

        private void StartNewFolder()
        {
            editingFolder = true;
            selectedPrompt = null;
            selectedFolder = new PromptFolder();
            FolderName = string.Empty;
            FolderDescription = string.Empty;
            SelectedParentFolder = FolderChoices.FirstOrDefault();
            NotifyEditorState();
        }

        private void EditPrompt(PromptItem prompt)
        {
            editingFolder = false;
            selectedPrompt = ClonePrompt(prompt);
            selectedFolder = null;
            PromptName = selectedPrompt.Name;
            PromptDescription = selectedPrompt.Description;
            selectedPromptTags.Clear();
            foreach (var tag in selectedPrompt.Tags)
            {
                selectedPromptTags.Add(tag);
            }

            RenderPromptTagChips();
            PromptQuality = selectedPrompt.Quality;
            SelectedModel = ModelChoices.FirstOrDefault(choice => string.Equals(choice.Name, selectedPrompt.AiModel, StringComparison.OrdinalIgnoreCase)) ?? ModelChoices.FirstOrDefault();
            PromptContent = selectedPrompt.Content;
            SelectedPromptFolder = FolderChoices.FirstOrDefault(choice => choice.Id == selectedPrompt.FolderId) ?? FolderChoices.FirstOrDefault();
            NotifyEditorState();
        }

        private void EditFolder(PromptFolder folder)
        {
            editingFolder = true;
            selectedPrompt = null;
            selectedFolder = CloneFolder(folder);
            FolderName = selectedFolder.Name;
            FolderDescription = selectedFolder.Description;
            SelectedParentFolder = FolderChoices.FirstOrDefault(choice => choice.Id == selectedFolder.ParentFolderId) ?? FolderChoices.FirstOrDefault();
            NotifyEditorState();
        }

        private async Task SaveAsync()
        {
            if (editingFolder)
            {
                await SaveFolderAsync();
                return;
            }

            await SavePromptAsync();
        }

        private async Task SavePromptAsync()
        {
            selectedPrompt ??= new PromptItem();
            var name = PromptName.Trim();
            var content = PromptContent.Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(content))
            {
                await dialogService.ShowMessageAsync("Missing prompt", "Name and prompt text are required.");
                return;
            }

            selectedPrompt.Name = name;
            selectedPrompt.Description = PromptDescription.Trim();
            selectedPrompt.Tags = selectedPromptTags.Order(StringComparer.OrdinalIgnoreCase).ToList();
            selectedPrompt.Quality = PromptQuality;
            selectedPrompt.AiModel = SelectedModel?.Name ?? string.Empty;
            selectedPrompt.Content = content;
            selectedPrompt.FolderId = SelectedPromptFolder?.Id;

            if (!await EnsureRepositoryAvailable())
            {
                return;
            }

            repository!.SavePrompt(selectedPrompt);
            LoadData();
            var savedPrompt = prompts.FirstOrDefault(prompt => prompt.Id == selectedPrompt.Id);
            if (savedPrompt is null)
            {
                await dialogService.ShowMessageAsync("Save failed", "The prompt was saved, but it could not be reloaded.");
                StartNewPrompt();
                return;
            }

            EditPrompt(savedPrompt);
        }

        private async Task SaveFolderAsync()
        {
            selectedFolder ??= new PromptFolder();
            var name = FolderName.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await dialogService.ShowMessageAsync("Missing folder", "Folder name is required.");
                return;
            }

            selectedFolder.Name = name;
            selectedFolder.Description = FolderDescription.Trim();
            selectedFolder.ParentFolderId = SelectedParentFolder?.Id;

            if (selectedFolder.ParentFolderId == selectedFolder.Id ||
                (selectedFolder.Id > 0 && selectedFolder.ParentFolderId is int parentId && treeService.IsDescendantFolder(parentId, selectedFolder.Id, folders)))
            {
                await dialogService.ShowMessageAsync("Invalid folder", "A folder cannot be moved inside itself or one of its child folders.");
                return;
            }

            if (!await EnsureRepositoryAvailable())
            {
                return;
            }

            repository!.SaveFolder(selectedFolder);
            if (!expandedFolderIds.Contains(selectedFolder.Id))
            {
                expandedFolderIds.Add(selectedFolder.Id);
            }

            LoadData();
            var savedFolder = folders.FirstOrDefault(folder => folder.Id == selectedFolder.Id);
            if (savedFolder is null)
            {
                await dialogService.ShowMessageAsync("Save failed", "The folder was saved, but it could not be reloaded.");
                StartNewPrompt();
                return;
            }

            EditFolder(savedFolder);
        }

        private async Task DeleteAsync()
        {
            if (editingFolder && selectedFolder?.Id > 0)
            {
                if (!await EnsureRepositoryAvailable())
                {
                    return;
                }

                if (!await dialogService.ConfirmAsync("Delete folder", "Delete this folder and all prompts inside it?", "Delete", "Cancel"))
                {
                    return;
                }

                repository!.DeleteFolder(selectedFolder.Id);
                expandedFolderIds.Remove(selectedFolder.Id);
                LoadData();
                StartNewPrompt();
                return;
            }

            if (selectedPrompt?.Id > 0)
            {
                if (!await EnsureRepositoryAvailable())
                {
                    return;
                }

                if (!await dialogService.ConfirmAsync("Delete prompt", "Delete this prompt?", "Delete", "Cancel"))
                {
                    return;
                }

                repository!.DeletePrompt(selectedPrompt.Id);
                LoadData();
                StartNewPrompt();
            }
        }

        private async Task CopyPromptAsync()
        {
            if (!string.IsNullOrWhiteSpace(PromptContent))
            {
                await clipboardService.SetTextAsync(PromptContent);
            }
        }

        private async Task CopyFromListAsync(object? parameter)
        {
            if (parameter is PromptTreeNode { Prompt: not null } node)
            {
                await clipboardService.SetTextAsync(node.Prompt.Content);
            }
        }

        private void NotifyEditorState()
        {
            OnPropertyChanged(nameof(CurrentModeText));
            OnPropertyChanged(nameof(IsPromptEditorVisible));
            OnPropertyChanged(nameof(IsFolderEditorVisible));
            OnPropertyChanged(nameof(CanCopyPrompt));
            OnPropertyChanged(nameof(CanDelete));
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private static PromptItem ClonePrompt(PromptItem prompt) => new()
        {
            Id = prompt.Id,
            FolderId = prompt.FolderId,
            Name = prompt.Name ?? string.Empty,
            Description = prompt.Description ?? string.Empty,
            Content = prompt.Content ?? string.Empty,
            Tags = [.. prompt.Tags ?? []],
            Quality = prompt.Quality,
            AiModel = prompt.AiModel ?? string.Empty,
            CreatedAt = prompt.CreatedAt,
            UpdatedAt = prompt.UpdatedAt
        };

        private static PromptFolder CloneFolder(PromptFolder folder) => new()
        {
            Id = folder.Id,
            ParentFolderId = folder.ParentFolderId,
            Name = folder.Name ?? string.Empty,
            Description = folder.Description ?? string.Empty,
            CreatedAt = folder.CreatedAt,
            UpdatedAt = folder.UpdatedAt
        };

        public void Dispose() => repository?.Dispose();
    }

    public sealed record FolderChoice(int? Id, string Name)
    {
        public override string ToString() => Name;
    }

    public sealed record ModelChoice(string? Name, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    public sealed class TagChipView(string name, bool isSelected)
    {
        public string Name { get; } = name;
        public IBrush Background { get; } = Brush.Parse(isSelected ? "#0099FF" : "#20252B");
        public IBrush Foreground { get; } = Brush.Parse(isSelected ? "#FFFFFF" : "#CBD5E1");
    }
}
