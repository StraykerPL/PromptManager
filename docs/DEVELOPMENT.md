# Development Guide

This guide covers the local development workflow for Prompt Manager.

## Primary Workflow

Windows and Linux are supported desktop development targets.

Install the .NET 10 SDK feature band selected by `global.json` and Git. Windows 10 or later is supported. Linux development is validated on Debian 13 and Ubuntu 24.04 LTS; install `libx11-6`, `libice6`, `libsm6`, and `libfontconfig1` there. Other distributions may use different package names.

Running the Linux GUI requires an active X11 or Wayland desktop session. Builds and tests can run headlessly, while GUI smoke tests need a desktop session or Xvfb. NuGet access is required for package restore.

Run commands from the repository root:

```sh
dotnet --info
dotnet restore PromptManager.slnx
dotnet run --project PromptManager.UI/PromptManager.UI.csproj
```

Use `dotnet run` for manual UI testing and `dotnet build` for faster compile validation.

```sh
dotnet build PromptManager.UI/PromptManager.UI.csproj
```

## Tests

The repository includes an xUnit test project:

```sh
dotnet test PromptManager.UnitTests/PromptManager.UnitTests.csproj
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

```sh
dotnet build PromptManager.slnx
```

The solution contains the Avalonia desktop app, shared Core library, and unit tests.

## Important Project Files

- `PromptManager.UI/PromptManager.UI.csproj` - desktop app project and package references.
- `PromptManager.UI/Views/MainWindow.axaml` - main UI layout.
- `PromptManager.UI/ViewModels/MainWindowViewModel.cs` - screen state and commands.
- `PromptManager.Core/Services/PromptRepository.cs` - LiteDB persistence.
- `PromptManager.Core/Services/PromptTreeService.cs` - tree, search, folder path, and descendant logic.
- `PromptManager.UnitTests/` - unit tests.

## Style Notes

Keep new code consistent with the Avalonia/Core separation:

- Put reusable storage and data access code in `PromptManager.Core/Services/`.
- Put simple data objects in `PromptManager.Core/Models/`.
- Keep UI layout in AXAML and application behavior in view models where practical.
- Keep code-behind limited to UI integration.
- Validate desktop behavior on Windows and Linux when affected.

## Manual Smoke Test

After UI or persistence changes, run the app and verify:

1. The app starts without a storage error.
2. A new folder can be created.
3. A new prompt can be created inside that folder.
4. The folder expands and collapses.
5. Search finds the prompt by name, description, content, tag, and model where applicable.
6. The prompt can be copied from the list and from the editor.
7. Editing and deleting prompts or folders works as expected.
8. Import and export dialogs filter JSON files and complete successfully.
9. The repository link opens in the default browser.

For platform-sensitive changes, run the applicable smoke test on both Windows and Linux and report the tested platforms in the pull request.

## Packaging

`installer/PromptManager.iss` is the Windows-only Inno Setup packaging path and is not part of the cross-platform development build.

On Debian and Debian-derived Linux systems, run the interactive deployment script from the repository root:

```sh
./linux-deploy.sh
```

The script asks for the application version, Debian package version, target architecture, and maintainer metadata. It then restores, tests, builds, publishes, assembles and checks the `.deb`, and writes a SHA-256 checksum under `artifacts/deb/`. GUI smoke testing and local installation are optional prompts. Install the .NET 10 SDK plus `dpkg-dev`, `lintian`, and `imagemagick` before running it. See the [Linux Deployment Guide](LINUX_DEPLOYMENT.md) for prerequisites, architecture mappings, output, validation, installation, upgrades, and removal.

Keep publish and installer output out of source control.

## Generated Output

Do not commit generated output:

- `bin/`
- `obj/`
- test result folders
- local IDE metadata
