# Contributing To Prompt Manager

Contributions should keep the app simple, local-first, and reliable on Windows and Linux. Before opening a pull request, read [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) and check the existing documentation under [docs/](docs/README.md).

## Good First Areas

- Fix bugs in prompt editing, folder tree behavior, search, or data cleanup.
- Improve documentation when behavior changes.
- Add focused unit tests for repository, tree, and validation behavior.
- Improve Avalonia desktop usability without broad visual rewrites.

## Local Setup

Restore packages:

```sh
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

Run tests:

```sh
dotnet test PromptManager.UnitTests/PromptManager.UnitTests.csproj
```

## Coding Guidelines

- Use C# nullable reference types and implicit usings.
- Use block-scoped namespaces.
- Keep `PromptManager` as the root namespace.
- Use PascalCase for public types, methods, properties, and XAML class names.
- Use camelCase for locals and private fields.
- Keep C# indentation at 4 spaces.
- Keep XAML concise and aligned with the existing style.
- Avoid broad refactors unless they are needed for the requested change.

## Testing Expectations

For code changes, run at least:

```sh
dotnet test PromptManager.UnitTests/PromptManager.UnitTests.csproj
dotnet build PromptManager.slnx
```

Add tests when changing:

- Prompt or folder normalization.
- Search behavior.
- Folder tree ordering or expansion.
- Delete behavior.
- Repository persistence rules.

For visible UI changes, include an appropriate Windows and/or Linux desktop smoke test in the pull request. Changes to paths, storage, clipboard, file dialogs, URL launching, fonts, windowing, or packaging must state which platforms were tested.

## Documentation Expectations

Update documentation in the same change when behavior changes:

- User-facing workflow changes: update [docs/USER_GUIDE.md](docs/USER_GUIDE.md).
- Build/test workflow changes: update [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md).
- Service/model/storage changes: update [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) or [docs/DATA_STORAGE.md](docs/DATA_STORAGE.md).
- Known setup failures: update [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md).
- Linux packaging changes: update [docs/LINUX_DEPLOYMENT.md](docs/LINUX_DEPLOYMENT.md) and validate `linux-deploy.sh`.

## Pull Request Checklist

- The change is scoped to the issue or task.
- CI/build validation passes on Windows and Linux.
- Unit tests pass or skipped tests are explained.
- Documentation is updated when needed.
- Screenshots are included for visible UI changes.
- `bin/`, `obj/`, and other generated output are not committed.
