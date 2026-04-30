# Prompt Manager

<img src="PromptManager/Resources/Images/icon.ico" alt="Prompt Manager icon" width="96" height="96">

Prompt Manager is a .NET MAUI desktop app for saving, organizing, searching, rating, and copying reusable prompts. It is built primarily for Windows development and validation, with Android, iOS, and MacCatalyst targets kept in the project for future platform work.

The app is useful when you maintain a personal library of prompts for different tools, models, projects, or quality levels. Prompts can be grouped into nested folders, tagged, associated with an AI model, scored from 1 to 10, searched, and copied directly from the list or editor.

## Features

- Local prompt library stored with LiteDB.
- Nested folders for organizing prompts.
- Flat "All prompts" mode for browsing every prompt without folder context.
- Search across prompt name, description, content, tags, and AI model.
- Prompt quality score from 1 to 10.
- Shared tag manager with duplicate cleanup and alphabetical sorting.
- Shared AI model manager with duplicate cleanup and alphabetical sorting.
- One-click prompt copying from the tree/list or editor.
- JSON import/export for backing up or moving prompt data.
- Corrupt database backup handling on startup.

## Tech Stack

- .NET 10
- .NET MAUI
- C#
- XAML
- LiteDB
- xUnit
- Moq

## Repository Layout

- `PromptManager.slnx` - solution entry point.
- `PromptManager/` - MAUI app source, XAML views, app startup, resources, and platform files.
- `PromptManager/Models/` - prompt, folder, tag, model, and tree node models.
- `PromptManager/Services/` - LiteDB repository and prompt tree/search service.
- `PromptManager.UnitTests/` - xUnit tests for repository normalization/deletion behavior and tree/search behavior.
- `docs/` - user, developer, architecture, storage, and troubleshooting documentation.

## Requirements

- Windows for the primary local workflow.
- .NET 10 SDK with MAUI workloads installed.
- Windows App SDK / MAUI Windows prerequisites available through the .NET MAUI tooling.
- Android JDK only if you build the Android target.

## Getting Started

Restore packages:

```powershell
dotnet restore "PromptManager\PromptManager.csproj"
```

Run the Windows app:

```powershell
dotnet run --project "PromptManager\PromptManager.csproj" -f net10.0-windows10.0.19041.0
```

Build the Windows app:

```powershell
dotnet build "PromptManager\PromptManager.csproj" -f net10.0-windows10.0.19041.0
```

Run unit tests:

```powershell
dotnet test "PromptManager.UnitTests\PromptManager.UnitTests.csproj"
```

Build the solution:

```powershell
dotnet build "PromptManager.slnx"
```

## Documentation

- [Documentation Index](docs/README.md)
- [User Guide](docs/USER_GUIDE.md)
- [Development Guide](docs/DEVELOPMENT.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Data Storage](docs/DATA_STORAGE.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md)

## Current Scope

Prompt Manager currently focuses on local personal prompt management. It does not include cloud sync, accounts, import/export, encryption settings, or collaboration workflows yet.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution workflow, validation commands, and documentation expectations.

## Licensing

This project is licensed under the MIT/X11 license. See [LICENSE.md](LICENSE.md).

## Contact

For suggestions or questions, use the official [Strayker Software Discord Server](https://discord.gg/ytdkCVD).
