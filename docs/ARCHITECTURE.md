# Architecture

Prompt Manager is a single-project .NET MAUI app with a small service layer and local LiteDB persistence.

## High-Level Flow

`MainPage` owns the screen state and UI event handlers. On startup it creates a `PromptRepository`, loads folders, prompts, tag options, and model options, then asks `PromptTreeService` to build the left-side tree/list.

The main runtime flow is:

1. Repository loads normalized data from LiteDB.
2. `MainPage` stores the current in-memory lists.
3. `PromptTreeService` creates display nodes for the tree or flat list.
4. User actions update the selected prompt or folder.
5. Save/delete actions go through `PromptRepository`.
6. The page reloads data and refreshes the tree/list.

## UI Layer

Main files:

- `PromptManager/MainPage.xaml`
- `PromptManager/MainPage.xaml.cs`

The UI is split into:

- Top command bar for tag management, model management, browse mode, and create actions.
- Left panel for search and prompt navigation.
- Right panel for prompt or folder editing.
- Modal overlays for tags and AI models.

The left navigation is implemented with a MAUI `CollectionView` bound to `ObservableCollection<PromptTreeNode>`. Folder expansion is controlled by `expandedFolderIds` in `MainPage`.

## Services

### PromptRepository

`PromptRepository` is responsible for persistence through LiteDB. It manages:

- `prompts`
- `folders`
- `tagOptions`
- `modelOptions`

It normalizes data before returning and saving it, creates indexes, and backs up a corrupt database file before creating a fresh one.

It also owns import/export at the data level. Export creates a `PromptDataDocument`; import replaces all repository collections with normalized data from that document.

### PromptTreeService

`PromptTreeService` is pure tree/search logic. It:

- Builds collapsed and expanded folder trees.
- Builds flat prompt lists for search or "All prompts" mode.
- Filters prompts by name, description, content, AI model, and tags.
- Builds folder paths for picker display.
- Checks whether a folder is a descendant of another folder.

This service is unit-tested and should remain free of UI dependencies.

## Models

### PromptItem

Represents a reusable prompt:

- `Id`
- `FolderId`
- `Name`
- `Description`
- `Content`
- `Tags`
- `Quality`
- `AiModel`
- `CreatedAt`
- `UpdatedAt`

`TagsText` converts the tag list to and from comma-separated text.

### PromptFolder

Represents a folder:

- `Id`
- `ParentFolderId`
- `Name`
- `Description`
- `CreatedAt`
- `UpdatedAt`

Folders can be nested by assigning `ParentFolderId`.

### PromptTreeNode

Represents one row in the navigation list. It can wrap either a folder or a prompt and exposes display properties such as icon, name, details, margin depth, tags, quality, model, and updated date.

### PromptTagOption And PromptModelOption

Represent shared selectable tag and AI model names.

### PromptDataDocument

Represents the JSON import/export payload. It contains document version, export timestamp, folders, prompts, shared tags, and shared AI model names.

## Sorting And Search

Repository methods return prompts and folders sorted by name. `PromptTreeService` also sorts folders and prompts by name when building navigation nodes.

Search is case-insensitive and matches:

- Prompt name
- Prompt description
- Prompt content
- AI model
- Tags

Search results are shown as a flat prompt list.

## Platform Notes

Windows is the primary app target. The project also contains Android, iOS, and MacCatalyst platform folders from the MAUI single-project structure.

Windows-specific UI behavior should be guarded with `#if WINDOWS` in code-behind or platform-specific files.
