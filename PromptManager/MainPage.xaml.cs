using System.Collections.ObjectModel;
using PromptManager.Models;
using PromptManager.Services;

namespace PromptManager
{

    public partial class MainPage : ContentPage
    {
        private IPromptRepository? repository;
        private readonly PromptTreeService treeService = new();
        private readonly ObservableCollection<PromptTreeNode> treeNodes = [];
        private readonly List<int> expandedFolderIds = [];
        private readonly HashSet<string> selectedPromptTags = new(StringComparer.OrdinalIgnoreCase);
        private List<PromptFolder> folders = [];
        private List<PromptItem> prompts = [];
        private List<string> availableTags = [];
        private List<string> availableModels = [];
        private PromptItem? selectedPrompt;
        private PromptFolder? selectedFolder;
        private bool editingFolder;
        private bool showAllPrompts;

        public MainPage()
        {
            InitializeComponent();
            TreeList.ItemsSource = treeNodes;

            try
            {
                repository = new PromptRepository();
                LoadData();
            }
            catch (Exception ex)
            {
                repository?.Dispose();
                repository = null;
                LoadData();
                ShowStartupError(ex);
            }

            StartNewPrompt();
        }

        protected override void OnDisappearing()
        {
            repository?.Dispose();
            base.OnDisappearing();
        }

        private void LoadData()
        {
            if (repository is null)
            {
                folders = [];
                prompts = [];
                availableTags = [];
                availableModels = [];
                RefreshFolderPickers();
                RefreshModelPicker();
                RenderPromptTagChips();
                RefreshTree();
                return;
            }

            folders = repository.GetFolders().ToList();
            prompts = repository.GetPrompts().ToList();
            availableTags = repository.GetAvailableTags().ToList();
            availableModels = repository.GetAvailableModels().ToList();
            RefreshFolderPickers();
            RefreshModelPicker();
            RenderPromptTagChips();
            RefreshTree();
        }

        private void ShowStartupError(Exception exception)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlertAsync("Startup failed", $"Prompt storage could not be opened: {exception.Message}", "OK");
            });
        }

        private async Task<bool> EnsureRepositoryAvailable()
        {
            if (repository is not null)
            {
                return true;
            }

            try
            {
                repository = new PromptRepository();
                LoadData();
                return true;
            }
            catch (Exception ex)
            {
                repository?.Dispose();
                repository = null;
                LoadData();
                await DisplayAlertAsync("Storage unavailable", $"Prompt storage could not be opened: {ex.Message}", "OK");
                return false;
            }
        }

        private void RefreshFolderPickers()
        {
            var choices = new List<FolderChoice> { new(null, "No folder") };
            choices.AddRange(folders.Select(folder => new FolderChoice(folder.Id, treeService.BuildFolderPath(folder, folders))));

            PromptFolderPicker.ItemsSource = choices;
            ParentFolderPicker.ItemsSource = choices;
        }

        private void RefreshModelPicker()
        {
            var choices = new List<ModelChoice> { new(null, "No model") };
            choices.AddRange(availableModels.Select(model => new ModelChoice(model, model)));
            PromptModelPicker.ItemsSource = choices;
        }

        private void RefreshTree()
        {
            treeNodes.Clear();
            foreach (var node in treeService.BuildTree(folders, prompts, expandedFolderIds, showAllPrompts, SearchInput.Text))
            {
                treeNodes.Add(node);
            }
        }

        private async void OnTreeNodeTapped(object? sender, TappedEventArgs e)
        {
            if ((sender as VisualElement)?.BindingContext is not PromptTreeNode node)
            {
                return;
            }

            if (node.Folder is not null)
            {
                EditFolder(node.Folder);
                if (sender is VisualElement row)
                {
                    await AnimateFolderToggle(row);
                }

                ToggleFolderNode(node);
                return;
            }

            if (node.Prompt is not null)
            {
                EditPrompt(node.Prompt);
            }
        }

        private void ToggleFolderNode(PromptTreeNode node)
        {
            var index = treeNodes.IndexOf(node);
            if (index < 0 || node.Folder is null)
            {
                return;
            }

            if (node.IsExpanded)
            {
                expandedFolderIds.Remove(node.Folder.Id);
                treeNodes[index] = CloneNode(node, isExpanded: false);

                while (index + 1 < treeNodes.Count && treeNodes[index + 1].Depth > node.Depth)
                {
                    treeNodes.RemoveAt(index + 1);
                }

                return;
            }

            expandedFolderIds.Add(node.Folder.Id);
            treeNodes[index] = CloneNode(node, isExpanded: true);

            var insertIndex = index + 1;
            foreach (var child in treeService.BuildChildNodes(node.Folder, folders, prompts, expandedFolderIds, node.Depth + 1))
            {
                treeNodes.Insert(insertIndex++, child);
            }
        }

        private static PromptTreeNode CloneNode(PromptTreeNode node, bool isExpanded) => new()
        {
            Key = node.Key,
            Folder = node.Folder,
            Prompt = node.Prompt,
            Depth = node.Depth,
            IsExpanded = isExpanded
        };

        private static async Task AnimateFolderToggle(VisualElement row)
        {
            await row.FadeToAsync(0.82, 70, Easing.SinInOut);
            await row.FadeToAsync(1, 90, Easing.SinInOut);
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            showAllPrompts = false;
            UpdateBrowseModeButton();
            RefreshTree();
        }

        private void OnShowAllClicked(object? sender, EventArgs e)
        {
            var nextShowAllPrompts = !showAllPrompts;

            if (!string.IsNullOrEmpty(SearchInput.Text))
            {
                SearchInput.Text = string.Empty;
            }

            showAllPrompts = nextShowAllPrompts;
            UpdateBrowseModeButton();
            RefreshTree();
        }

        private void UpdateBrowseModeButton() =>
            BrowseModeButton.Text = showAllPrompts ? "Groups tree" : "All prompts";

        private async void OnTagsClicked(object? sender, EventArgs e)
        {
            if (!await EnsureRepositoryAvailable())
            {
                return;
            }

            TagNameInput.Text = string.Empty;
            RenderTagsDialog();
            TagsDialogOverlay.IsVisible = true;
            TagNameInput.Focus();
        }

        private async void OnAddTagClicked(object? sender, EventArgs e)
        {
            var tag = TagNameInput.Text?.Trim();
            if (string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            if (!availableTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                availableTags.Add(tag);
                SortAvailableTags();
                await SaveAvailableTags();
            }

            TagNameInput.Text = string.Empty;
            RenderTagsDialog();
            RenderPromptTagChips();
        }

        private async void OnRemoveTagClicked(object? sender, EventArgs e)
        {
            if ((sender as Button)?.ClassId is not string tag)
            {
                return;
            }

            availableTags.RemoveAll(candidate => string.Equals(candidate, tag, StringComparison.OrdinalIgnoreCase));
            selectedPromptTags.Remove(tag);

            await SaveAvailableTags();
            RenderTagsDialog();
            RenderPromptTagChips();
        }

        private void OnCloseTagsClicked(object? sender, EventArgs e)
        {
            TagsDialogOverlay.IsVisible = false;
        }

        private async Task SaveAvailableTags()
        {
            if (!await EnsureRepositoryAvailable())
            {
                return;
            }

            repository!.SaveAvailableTags(availableTags);
        }

        private async void OnModelsClicked(object? sender, EventArgs e)
        {
            if (!await EnsureRepositoryAvailable())
            {
                return;
            }

            ModelNameInput.Text = string.Empty;
            RenderModelsDialog();
            ModelsDialogOverlay.IsVisible = true;
            ModelNameInput.Focus();
        }

        private async void OnAddModelClicked(object? sender, EventArgs e)
        {
            var model = ModelNameInput.Text?.Trim();
            if (string.IsNullOrWhiteSpace(model))
            {
                return;
            }

            if (!availableModels.Contains(model, StringComparer.OrdinalIgnoreCase))
            {
                availableModels.Add(model);
                SortAvailableModels();
                await SaveAvailableModels();
                RefreshModelPicker();
            }

            ModelNameInput.Text = string.Empty;
            RenderModelsDialog();
        }

        private async void OnRemoveModelClicked(object? sender, EventArgs e)
        {
            if ((sender as Button)?.ClassId is not string model)
            {
                return;
            }

            availableModels.RemoveAll(candidate => string.Equals(candidate, model, StringComparison.OrdinalIgnoreCase));

            await SaveAvailableModels();
            RefreshModelPicker();
            RenderModelsDialog();
        }

        private void OnCloseModelsClicked(object? sender, EventArgs e)
        {
            ModelsDialogOverlay.IsVisible = false;
        }

        private async Task SaveAvailableModels()
        {
            if (!await EnsureRepositoryAvailable())
            {
                return;
            }

            repository!.SaveAvailableModels(availableModels);
        }

        private void RenderTagsDialog()
        {
            TagsDialogList.Children.Clear();

            foreach (var tag in availableTags)
            {
                var row = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Auto)
                    },
                    ColumnSpacing = 10,
                    Padding = new Thickness(10, 6),
                    BackgroundColor = Color.FromArgb("#20252B")
                };

                row.Add(new Label
                {
                    Text = tag,
                    TextColor = Color.FromArgb("#F2F4F8"),
                    VerticalTextAlignment = TextAlignment.Center
                });

                var removeButton = new Button
                {
                    Text = "Remove",
                    ClassId = tag,
                    FontSize = 12,
                    Padding = new Thickness(10, 4),
                    MinimumHeightRequest = 30,
                    BackgroundColor = Color.FromArgb("#FF3333"),
                    TextColor = Colors.White
                };
                removeButton.Clicked += OnRemoveTagClicked;

                row.Add(removeButton, 1);
                TagsDialogList.Children.Add(row);
            }
        }

        private void RenderModelsDialog()
        {
            ModelsDialogList.Children.Clear();

            foreach (var model in availableModels)
            {
                var row = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Auto)
                    },
                    ColumnSpacing = 10,
                    Padding = new Thickness(10, 6),
                    BackgroundColor = Color.FromArgb("#20252B")
                };

                row.Add(new Label
                {
                    Text = model,
                    TextColor = Color.FromArgb("#F2F4F8"),
                    VerticalTextAlignment = TextAlignment.Center
                });

                var removeButton = new Button
                {
                    Text = "Remove",
                    ClassId = model,
                    FontSize = 12,
                    Padding = new Thickness(10, 4),
                    MinimumHeightRequest = 30,
                    BackgroundColor = Color.FromArgb("#FF3333"),
                    TextColor = Colors.White
                };
                removeButton.Clicked += OnRemoveModelClicked;

                row.Add(removeButton, 1);
                ModelsDialogList.Children.Add(row);
            }
        }

        private void RenderPromptTagChips()
        {
            PromptTagsChips.Children.Clear();

            foreach (var tag in availableTags)
            {
                var selected = selectedPromptTags.Contains(tag);
                var chip = new Button
                {
                    Text = tag,
                    ClassId = tag,
                    FontSize = 12,
                    CornerRadius = 16,
                    Padding = new Thickness(12, 4),
                    Margin = new Thickness(0, 0, 8, 8),
                    MinimumHeightRequest = 32,
                    BorderWidth = 1,
                    BorderColor = Color.FromArgb("#0099FF"),
                    BackgroundColor = selected ? Color.FromArgb("#0099FF") : Color.FromArgb("#20252B"),
                    TextColor = selected ? Colors.White : Color.FromArgb("#CBD5E1")
                };
                chip.Clicked += OnPromptTagChipClicked;
                PromptTagsChips.Children.Add(chip);
            }
        }

        private void OnPromptTagChipClicked(object? sender, EventArgs e)
        {
            if ((sender as Button)?.ClassId is not string tag)
            {
                return;
            }

            if (!selectedPromptTags.Add(tag))
            {
                selectedPromptTags.Remove(tag);
            }

            RenderPromptTagChips();
        }

        private void SortAvailableTags()
        {
            availableTags = availableTags
                .Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void SortAvailableModels()
        {
            availableModels = availableModels
                .Select(model => model.Trim())
                .Where(model => !string.IsNullOrWhiteSpace(model))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void OnPromptQualityChanged(object? sender, ValueChangedEventArgs e)
        {
            var quality = (int)Math.Round(e.NewValue);
            PromptQualitySlider.Value = quality;
            PromptQualityLabel.Text = quality switch
            {
                1 => "Quality: 1/10 - does not work",
                10 => "Quality: 10/10 - works perfectly",
                _ => $"Quality: {quality}/10"
            };
        }

        private void OnNewPromptClicked(object? sender, EventArgs e) => StartNewPrompt();

        private void OnNewFolderClicked(object? sender, EventArgs e) => StartNewFolder();

        private void StartNewPrompt()
        {
            editingFolder = false;
            selectedPrompt = new PromptItem();
            selectedFolder = null;

            CurrentModeLabel.Text = "New prompt";
            PromptEditor.IsVisible = true;
            FolderEditor.IsVisible = false;
            CopyPromptButton.IsVisible = false;
            DeleteButton.IsVisible = false;

            PromptNameInput.Text = string.Empty;
            PromptDescriptionInput.Text = string.Empty;
            selectedPromptTags.Clear();
            RenderPromptTagChips();
            SetPromptQuality(5);
            SelectModel(null);
            PromptContentInput.Text = string.Empty;
            SelectFirstPickerItem(PromptFolderPicker);
        }

        private void StartNewFolder()
        {
            editingFolder = true;
            selectedPrompt = null;
            selectedFolder = new PromptFolder();

            CurrentModeLabel.Text = "New folder";
            PromptEditor.IsVisible = false;
            FolderEditor.IsVisible = true;
            CopyPromptButton.IsVisible = false;
            DeleteButton.IsVisible = false;

            FolderNameInput.Text = string.Empty;
            FolderDescriptionInput.Text = string.Empty;
            SelectFirstPickerItem(ParentFolderPicker);
        }

        private void EditPrompt(PromptItem prompt)
        {
            editingFolder = false;
            selectedPrompt = ClonePrompt(prompt);
            selectedFolder = null;

            CurrentModeLabel.Text = "Edit prompt";
            PromptEditor.IsVisible = true;
            FolderEditor.IsVisible = false;
            CopyPromptButton.IsVisible = true;
            DeleteButton.IsVisible = true;

            PromptNameInput.Text = selectedPrompt.Name;
            PromptDescriptionInput.Text = selectedPrompt.Description;
            selectedPromptTags.Clear();
            foreach (var tag in selectedPrompt.Tags)
            {
                selectedPromptTags.Add(tag);
            }

            RenderPromptTagChips();
            SetPromptQuality(selectedPrompt.Quality);
            SelectModel(selectedPrompt.AiModel);
            PromptContentInput.Text = selectedPrompt.Content;
            SelectFolder(PromptFolderPicker, selectedPrompt.FolderId);
        }

        private void EditFolder(PromptFolder folder)
        {
            editingFolder = true;
            selectedPrompt = null;
            selectedFolder = CloneFolder(folder);

            CurrentModeLabel.Text = "Edit folder";
            PromptEditor.IsVisible = false;
            FolderEditor.IsVisible = true;
            CopyPromptButton.IsVisible = false;
            DeleteButton.IsVisible = true;

            FolderNameInput.Text = selectedFolder.Name;
            FolderDescriptionInput.Text = selectedFolder.Description;
            SelectFolder(ParentFolderPicker, selectedFolder.ParentFolderId);
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            if (editingFolder)
            {
                await SaveFolder();
                return;
            }

            await SavePrompt();
        }

        private async Task SavePrompt()
        {
            if (selectedPrompt is null)
            {
                selectedPrompt = new PromptItem();
            }

            var name = PromptNameInput.Text?.Trim();
            var content = PromptContentInput.Text?.Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(content))
            {
                await DisplayAlertAsync("Missing prompt", "Name and prompt text are required.", "OK");
                return;
            }

            selectedPrompt.Name = name;
            selectedPrompt.Description = PromptDescriptionInput.Text?.Trim() ?? string.Empty;
            selectedPrompt.Tags = selectedPromptTags
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();
            selectedPrompt.Quality = (int)Math.Round(PromptQualitySlider.Value);
            selectedPrompt.AiModel = (PromptModelPicker.SelectedItem as ModelChoice)?.Name ?? string.Empty;
            selectedPrompt.Content = content;
            selectedPrompt.FolderId = (PromptFolderPicker.SelectedItem as FolderChoice)?.Id;

            if (!await EnsureRepositoryAvailable())
            {
                return;
            }

            repository!.SavePrompt(selectedPrompt);
            LoadData();

            var savedPrompt = prompts.FirstOrDefault(prompt => prompt.Id == selectedPrompt.Id);
            if (savedPrompt is null)
            {
                await DisplayAlertAsync("Save failed", "The prompt was saved, but it could not be reloaded.", "OK");
                StartNewPrompt();
                return;
            }

            EditPrompt(savedPrompt);
        }

        private async Task SaveFolder()
        {
            if (selectedFolder is null)
            {
                selectedFolder = new PromptFolder();
            }

            var name = FolderNameInput.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                await DisplayAlertAsync("Missing folder", "Folder name is required.", "OK");
                return;
            }

            selectedFolder.Name = name;
            selectedFolder.Description = FolderDescriptionInput.Text?.Trim() ?? string.Empty;
            selectedFolder.ParentFolderId = (ParentFolderPicker.SelectedItem as FolderChoice)?.Id;

            if (selectedFolder.ParentFolderId == selectedFolder.Id ||
                (selectedFolder.Id > 0 && selectedFolder.ParentFolderId is int parentId && treeService.IsDescendantFolder(parentId, selectedFolder.Id, folders)))
            {
                await DisplayAlertAsync("Invalid folder", "A folder cannot be moved inside itself or one of its child folders.", "OK");
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
                await DisplayAlertAsync("Save failed", "The folder was saved, but it could not be reloaded.", "OK");
                StartNewPrompt();
                return;
            }

            EditFolder(savedFolder);
        }

        private async void OnDeleteClicked(object? sender, EventArgs e)
        {
            if (editingFolder && selectedFolder?.Id > 0)
            {
                if (!await EnsureRepositoryAvailable())
                {
                    return;
                }

                var confirm = await DisplayAlertAsync("Delete folder", "Delete this folder and all prompts inside it?", "Delete", "Cancel");
                if (!confirm)
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

                var confirm = await DisplayAlertAsync("Delete prompt", "Delete this prompt?", "Delete", "Cancel");
                if (!confirm)
                {
                    return;
                }

                repository!.DeletePrompt(selectedPrompt.Id);
                LoadData();
                StartNewPrompt();
            }
        }

        private async void OnCopyPromptClicked(object? sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(PromptContentInput.Text))
            {
                await Clipboard.Default.SetTextAsync(PromptContentInput.Text);
            }
        }

        private async void OnCopyFromListClicked(object? sender, EventArgs e)
        {
            if ((sender as Button)?.BindingContext is PromptTreeNode { Prompt: not null } node)
            {
                await Clipboard.Default.SetTextAsync(node.Prompt.Content);
            }
        }

        private static void SelectFolder(Picker picker, int? folderId)
        {
            for (var i = 0; i < picker.ItemsSource?.Count; i++)
            {
                if (picker.ItemsSource[i] is FolderChoice choice && choice.Id == folderId)
                {
                    picker.SelectedIndex = i;
                    return;
                }
            }

            SelectFirstPickerItem(picker);
        }

        private void SelectModel(string? modelName)
        {
            for (var i = 0; i < PromptModelPicker.ItemsSource?.Count; i++)
            {
                if (PromptModelPicker.ItemsSource[i] is ModelChoice choice &&
                    string.Equals(choice.Name, modelName, StringComparison.OrdinalIgnoreCase))
                {
                    PromptModelPicker.SelectedIndex = i;
                    return;
                }
            }

            SelectFirstPickerItem(PromptModelPicker);
        }

        private void SetPromptQuality(int quality)
        {
            PromptQualitySlider.Value = Math.Clamp(quality, 1, 10);
            OnPromptQualityChanged(PromptQualitySlider, new ValueChangedEventArgs(PromptQualitySlider.Value, PromptQualitySlider.Value));
        }

        private static void SelectFirstPickerItem(Picker picker)
        {
            picker.SelectedIndex = picker.ItemsSource?.Count > 0 ? 0 : -1;
        }

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

        public sealed record FolderChoice(int? Id, string Name);
        public sealed record ModelChoice(string? Name, string DisplayName);
    }
}
