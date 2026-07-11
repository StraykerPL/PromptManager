# Repository Guidelines

## Project Structure & Module Organization

This repository contains an Avalonia desktop app, shared Core library, and xUnit tests. The solution entry point is `PromptManager.slnx`, and the app project is `PromptManager.UI/PromptManager.UI.csproj`.

- `PromptManager.UI/`: Avalonia views, view models, desktop services, resources, and startup code.
- `PromptManager.Core/Models/`: prompt, folder, tag, and tree node data models.
- `PromptManager.Core/Services/`: persistence and application services, including the LiteDB repository.
- `PromptManager.UnitTests/`: xUnit tests for Core and desktop path behavior.
- `docs/`: project documentation.

## Build, Test, and Development Commands

Run app/debug commands from the repository root.

```sh
dotnet restore PromptManager.UI/PromptManager.UI.csproj
dotnet run --project PromptManager.UI/PromptManager.UI.csproj
dotnet build PromptManager.UI/PromptManager.UI.csproj
dotnet test PromptManager.UnitTests/PromptManager.UnitTests.csproj
dotnet build PromptManager.slnx
```

Use the Avalonia `dotnet run` command for local debugging. Build the solution when checking project configuration across the app, Core library, and tests.

## Coding Style & Naming Conventions

Use C# with nullable reference types and implicit usings enabled. Prefer block-scoped namespaces, for example:

```csharp
namespace PromptManager.Services
{
    public sealed class PromptRepository
    {
    }
}
```

Use `PromptManager` as the root namespace. Use PascalCase for types, methods, properties, and AXAML class names; use camelCase for local variables and private fields. Keep project and folder names free of spaces. Preserve 4-space C# indentation and concise AXAML formatting.

## Testing Guidelines

Use xUnit consistently and name test files after the type under test, for example `PromptRepositoryTests.cs`. Validate with the solution build, unit tests, and manual Avalonia desktop smoke testing.

## Commit & Pull Request Guidelines

Recent commits use short imperative summaries, such as `Rename project to follow conventions...`. Keep commit subjects clear and action-oriented. Pull requests should include a short description, desktop verification steps, and screenshots for visible UI changes. Call out Windows or Linux impact when platform behavior is touched.

## Agent-Specific Instructions

Do not commit generated `bin/` or `obj/` output. Avoid broad refactors unless needed for the requested change. Keep namespace, XAML `x:Class`, and project metadata changes synchronized.
