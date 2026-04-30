# Contributing To Prompt Manager

Contributions should keep the app simple, local-first, and reliable on Windows. Before opening a pull request, read [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) and check the existing documentation under [docs/](docs/README.md).

## Good First Areas

- Fix bugs in prompt editing, folder tree behavior, search, or data cleanup.
- Improve documentation when behavior changes.
- Add focused unit tests for repository, tree, and validation behavior.
- Improve Windows MAUI usability without broad visual rewrites.

## Local Setup

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

Run tests:

```powershell
dotnet test "PromptManager.UnitTests\PromptManager.UnitTests.csproj"
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

```powershell
dotnet test "PromptManager.UnitTests\PromptManager.UnitTests.csproj"
dotnet build "PromptManager\PromptManager.csproj" -f net10.0-windows10.0.19041.0
```

Add tests when changing:

- Prompt or folder normalization.
- Search behavior.
- Folder tree ordering or expansion.
- Delete behavior.
- Repository persistence rules.

For visible UI changes, include a short manual Windows smoke test in the pull request.

## Documentation Expectations

Update documentation in the same change when behavior changes:

- User-facing workflow changes: update [docs/USER_GUIDE.md](docs/USER_GUIDE.md).
- Build/test workflow changes: update [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md).
- Service/model/storage changes: update [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) or [docs/DATA_STORAGE.md](docs/DATA_STORAGE.md).
- Known setup failures: update [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md).

## Pull Request Checklist

- The change is scoped to the issue or task.
- Windows build passes.
- Unit tests pass or skipped tests are explained.
- Documentation is updated when needed.
- Screenshots are included for visible UI changes.
- `bin/`, `obj/`, and other generated output are not committed.
