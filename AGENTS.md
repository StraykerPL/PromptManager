# Repository Guidelines

## Project Structure & Module Organization

This repository contains a single .NET MAUI app with Windows as the primary development and validation target. The solution entry point is `PromptManager.slnx`, and the app project is `PromptManager/PromptManager.csproj`.

- `PromptManager/`: application source, XAML views, and MAUI startup code.
- `PromptManager/Models/`: data models such as prompt, folder, tag, and tree node types.
- `PromptManager/Services/`: persistence and application services, including the LiteDB repository.
- `PromptManager/Platforms/Windows/`: Windows-specific app entry point and manifests.
- `PromptManager/Platforms/Android/`, `iOS/`, `MacCatalyst/`: secondary platform targets.
- `PromptManager/Resources/`: app icons, splash screen, fonts, images, and styles.
- `docs/`: project documentation.

There is currently no dedicated test project.

## Build, Test, and Development Commands

Run app/debug commands from the project folder. Prefer the Windows target unless a change explicitly affects another platform.

```powershell
cd PromptManager
dotnet restore "PromptManager.csproj"
dotnet run "PromptManager.csproj" -f net10.0-windows10.0.19041.0
dotnet build "PromptManager.csproj" -f net10.0-windows10.0.19041.0
dotnet build "..\PromptManager.slnx"
```

`dotnet run "PromptManager.csproj" -f net10.0-windows10.0.19041.0` is the primary local debug workflow. Use the Windows `dotnet build` command for fast compile-only validation. Build the solution when checking cross-platform project configuration; Android builds require a valid JDK, not only a JRE.

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

Use `PromptManager` as the root namespace. Use PascalCase for types, methods, properties, and XAML class names; use camelCase for local variables and private fields. Keep project and folder names free of spaces. Preserve 4-space C# indentation and concise XAML formatting. Keep Windows XAML namespace references aligned with `PromptManager.WinUI`.

## Testing Guidelines

No automated test suite is configured yet. When adding tests, create `PromptManager.Tests`, use xUnit or NUnit consistently, and name test files after the type under test, for example `PromptRepositoryTests.cs`. Until tests exist, validate with the Windows build command and manual Windows MAUI smoke testing.

## Commit & Pull Request Guidelines

Recent commits use short imperative summaries, such as `Rename project to follow conventions...`. Keep commit subjects clear and action-oriented. Pull requests should include a short description, Windows verification steps, and screenshots for visible UI changes. Call out Android, iOS, or MacCatalyst impact only when touched.

## Agent-Specific Instructions

Do not commit generated `bin/` or `obj/` output. Avoid broad refactors unless needed for the requested change. Keep namespace, XAML `x:Class`, and project metadata changes synchronized.
