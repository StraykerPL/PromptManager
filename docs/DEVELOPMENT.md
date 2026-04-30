# Development Guide

This guide covers the local development workflow for Prompt Manager.

## Primary Workflow

Windows is the primary development and validation target.

```powershell
dotnet restore "PromptManager\PromptManager.csproj"
dotnet run --project "PromptManager\PromptManager.csproj" -f net10.0-windows10.0.19041.0
```

Use `dotnet run` when you need to manually test the MAUI app. Use `dotnet build` for faster compile validation.

```powershell
dotnet build "PromptManager\PromptManager.csproj" -f net10.0-windows10.0.19041.0
```

## Tests

The repository includes an xUnit test project:

```powershell
dotnet test "PromptManager.UnitTests\PromptManager.UnitTests.csproj"
```

The tests currently cover:

- Prompt repository normalization.
- Tag and model cleanup.
- Prompt and folder save behavior.
- Prompt and folder delete behavior.
- Search matching.
- Folder tree construction.
- Folder path and descendant checks.

## Solution Build

Build the solution when you change project files or target framework configuration:

```powershell
dotnet build "PromptManager.slnx"
```

The solution includes platform targets beyond Windows. Android builds require a valid JDK. iOS and MacCatalyst builds require the normal Apple platform tooling.

## Important Project Files

- `PromptManager/PromptManager.csproj` - MAUI app project, target frameworks, package references, resources, and app metadata.
- `PromptManager/Resources/Images/icon.ico` - main Windows app icon referenced by the project metadata.
- `PromptManager/App.xaml` - app-level resources.
- `PromptManager/AppShell.xaml` - shell setup.
- `PromptManager/MainPage.xaml` - main UI layout.
- `PromptManager/MainPage.xaml.cs` - UI event handling and screen state.
- `PromptManager/Services/PromptRepository.cs` - LiteDB persistence.
- `PromptManager/Services/PromptTreeService.cs` - tree, search, folder path, and descendant logic.
- `PromptManager.UnitTests/` - unit tests.

## Style Notes

The app follows the existing MAUI template structure with project-specific services and models. Keep new code consistent with the current layout:

- Put storage and data access code in `Services/`.
- Put simple data objects in `Models/`.
- Keep UI layout in XAML where practical.
- Keep event handlers small and delegate reusable logic to services.
- Preserve Windows behavior as the primary validation path.

## Manual Smoke Test

After UI or persistence changes, run the app and verify:

1. The app starts without a storage error.
2. A new folder can be created.
3. A new prompt can be created inside that folder.
4. The folder expands and collapses.
5. Search finds the prompt by name, description, content, tag, and model where applicable.
6. The prompt can be copied from the list and from the editor.
7. Editing and deleting prompts or folders works as expected.

## Generated Output

Do not commit generated output:

- `bin/`
- `obj/`
- test result folders
- local IDE metadata
