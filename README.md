# Prompt Manager

<img src="PromptManager.UI/Assets/icon.ico" alt="Prompt Manager icon" width="96" height="96">

Prompt Manager is an Avalonia desktop app for saving, organizing, searching, rating, and copying reusable prompts on Windows and Linux.

The app is useful when you maintain a personal library of prompts for different tools, models, projects, or quality levels. Prompts can be grouped into nested folders, tagged, associated with an AI model, scored from 1 to 10, searched, and copied directly from the list or editor.

> [!CAUTION]
> This project is 100% vibe-coded, even end user's documentation is fully generated. I didin't read even single line of text in this repo (project's logo was also generated). Please, follow your common sense when interacting with this repo.

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
- Configurable database directory with guarded copy, replacement, and restore-default flows. Custom choices apply after restart; without one, Debug builds use the executable directory and Release builds use platform app-data. `settings.json` always remains in platform app-data.

## Tech Stack

- .NET 10
- Avalonia UI
- C#
- XAML
- LiteDB
- xUnit
- Moq

## Repository Layout

- `PromptManager.slnx` - solution entry point.
- `PromptManager.UI/` - Avalonia UI, view model, desktop services, resources, and startup code.
- `PromptManager.Core/` - shared models, LiteDB repository, and prompt tree/search service.
- `PromptManager.UnitTests/` - xUnit tests for repository normalization/deletion behavior and tree/search behavior.
- `docs/` - user, developer, architecture, storage, and troubleshooting documentation.

## Requirements

- The .NET 10 SDK feature band pinned by `global.json` and Git. Check it with `dotnet --info`.
- Windows 10 or later with a desktop session.
- Debian 13 or Ubuntu 24.04 LTS with `libx11-6`, `libice6`, `libsm6`, and `libfontconfig1`. Package names differ on other distributions, which are not currently validated by CI.
- Linux requires an active X11 or Wayland desktop session to run the GUI. Headless GUI tests need a graphical session such as Xvfb.

Restoring dependencies requires access to NuGet.

## Getting Started

Restore packages:

```sh
dotnet --info
dotnet restore PromptManager.UI/PromptManager.UI.csproj
```

Run the desktop app:

```sh
dotnet run --project PromptManager.UI/PromptManager.UI.csproj
```

Build the desktop app:

```sh
dotnet build PromptManager.UI/PromptManager.UI.csproj
```

Run unit tests:

```sh
dotnet test PromptManager.UnitTests/PromptManager.UnitTests.csproj
```

Build the solution:

```sh
dotnet build PromptManager.slnx
```

## Linux Deployment

On Debian and Debian-derived systems, create a self-contained `.deb` package with the interactive repository script:

```sh
./linux-deploy.sh
```

Install the .NET 10 SDK, `dpkg-dev`, `lintian`, and `imagemagick` first. The script asks for version, architecture, and maintainer metadata, runs the Release tests and build, creates the package and SHA-256 checksum under `artifacts/deb/`, and optionally performs a GUI smoke test and local installation. See the [Linux Deployment Guide](docs/LINUX_DEPLOYMENT.md) for the complete procedure.

## Documentation

- [Documentation Index](docs/README.md)
- [Linux Deployment](docs/LINUX_DEPLOYMENT.md)
- [User Guide](docs/USER_GUIDE.md)
- [Development Guide](docs/DEVELOPMENT.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Data Storage](docs/DATA_STORAGE.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md)

## Current Scope

Prompt Manager currently focuses on local personal prompt management. It does not include cloud sync, accounts, encryption settings, or collaboration workflows yet.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for contribution workflow, validation commands, and documentation expectations.

## Licensing

This project is licensed under the MIT/X11 license. See [LICENSE.md](LICENSE.md).

## Contact

For suggestions or questions, use the official [Strayker Software Discord Server](https://discord.gg/ytdkCVD).
