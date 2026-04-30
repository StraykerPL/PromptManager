# Troubleshooting

This page lists common issues when building, testing, or running Prompt Manager.

## `dotnet` First-Run Permission Error

If `dotnet build` or `dotnet test` fails while creating a first-run sentinel or updating the user tool path, the current user may not have permission to write to the .NET profile directory.

Check that the active user can write to:

```text
%USERPROFILE%\.dotnet
```

Then rerun the command.

## Windows Build Fails

Use the explicit Windows target:

```powershell
dotnet build "PromptManager\PromptManager.csproj" -f net10.0-windows10.0.19041.0
```

If restore fails first, run:

```powershell
dotnet restore "PromptManager\PromptManager.csproj"
```

Also verify that the .NET 10 SDK and MAUI workloads are installed.

## Android Build Fails

Android builds require a valid JDK. A JRE alone is not enough. Validate Android tooling separately before treating Android failures as app regressions.

## Unit Tests Do Not Discover Tests

Run tests against the test project directly:

```powershell
dotnet test "PromptManager.UnitTests\PromptManager.UnitTests.csproj"
```

The test project targets Windows because it references the MAUI app project.

## App Opens With Empty Data

If the LiteDB file cannot be opened, Prompt Manager shows a startup error and opens with an empty workspace. If the file exists and is detected as damaged, the repository moves it aside with a `prompts.corrupt-*.db` name and creates a fresh database.

Look in the app data directory for the original backup file.

## Prompts Or Tags Appear Reordered

This is expected. The app sorts prompts, folders, tags, and model names alphabetically in several places so the UI stays predictable.

## Duplicate Tags Or Models Disappear

This is expected. Tags and model names are deduplicated case-insensitively after trimming whitespace.

## Folder Cannot Be Moved

A folder cannot be moved inside itself or inside one of its descendants. This prevents cycles in the folder tree.
