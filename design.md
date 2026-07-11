# Configurable Database Location Design

## Goal

Allow a user to choose the directory containing `prompts.db` from the Settings dialog. A saved user choice must be used on subsequent launches on Windows and Linux.

When no user choice has been saved, the default depends on the build configuration:

- Debug builds use the directory containing the running executable (`AppContext.BaseDirectory`).
- Release builds retain the existing platform data directory: `%APPDATA%\PromptManager` on Windows and `${XDG_DATA_HOME:-$HOME/.local/share}/PromptManager` on Linux.

The Debug behavior must be selected at compile time with `#if DEBUG`. Runtime environment names such as `DOTNET_ENVIRONMENT` must not change a packaged Release build's storage location.

## User Experience

Add a **Database storage** section to Settings containing:

- A read-only text field showing the resolved directory.
- A **Choose folder** button that opens the native Avalonia folder picker.
- A **Restore default** button, enabled when a custom location is configured.
- A short note that the database filename is always `prompts.db`.

The displayed value is the effective location, including the Debug or platform default when no override exists. The UI should label a custom value as **Custom**, an executable-directory value as **Debug default**, and the normal app-data value as **System default**.

Changing the location follows this flow:

1. The user selects a directory rather than a database file.
2. The app normalizes it to an absolute path and rejects relative, missing, or unwritable locations with a user-facing error.
3. If the directory resolves to the current directory, no change is made.
4. If the destination has no `prompts.db`, the app asks whether to copy the current data to the new location or start with an empty database. Copying current data should be the primary action.
5. If the destination already has `prompts.db`, the app asks whether to use that database or replace its contents with the current data. Replacement requires a second explicit confirmation and creates a timestamped backup first.
6. Once the destination has been opened and validated successfully, the custom setting is saved atomically. The app then tells the user that the change takes effect after restart.

Using a restart boundary is intentional. LiteDB owns an open file handle, while the current view model and repository factory assume one repository for the lifetime of the window. Deferring activation avoids partially switching repositories or losing unsaved editor state. The currently open database remains active until normal application shutdown.

**Restore default** uses the same safety flow. It clears the custom setting only after the default destination is validated, and takes effect after restart. It must not delete the database at the former custom location.

## Location Resolution

Introduce a resolver that distinguishes the persistent configuration location from the selected database location:

```text
DatabaseLocationSettingsStore
    reads/writes the optional custom directory

DatabaseLocationResolver
    custom directory, if configured
    otherwise AppContext.BaseDirectory in DEBUG
    otherwise AvaloniaAppDataPathProvider.AppDataDirectory
```

The configuration file must always live in the platform application-data directory, not beside `prompts.db`. Otherwise the app would need to know the selected database location before it could find the setting that identifies that location.

Suggested settings paths:

```text
Windows: %APPDATA%\PromptManager\settings.json
Linux:  ${XDG_DATA_HOME:-$HOME/.local/share}/PromptManager/settings.json
```

Suggested schema:

```json
{
  "databaseDirectory": "/absolute/custom/path"
}
```

An absent file, absent property, or blank property means “use the build/platform default.” Invalid JSON or a relative saved path must produce a visible warning and fall back to the default; it must never create a relative database directory.

Write settings through a temporary file in the same directory followed by an atomic replace/rename. Do not store machine-specific paths in the prompt database or JSON prompt exports.

## Proposed Types and Responsibilities

### Core

Keep `PromptRepository` responsible for LiteDB and continue passing it an `IAppDataPathProvider`. No Avalonia dependency should enter Core.

Add or rename the abstraction to make its meaning explicit:

```csharp
public interface IDatabaseDirectoryProvider
{
    string DatabaseDirectory { get; }
}
```

`PromptRepository` continues to append the fixed filename `prompts.db`. The existing `IAppDataPathProvider` can remain during a small migration, but it should not represent both the system configuration path and a user-selected database path long term.

### UI services

- `SystemAppDataPathProvider`: retains the current Windows/XDG resolution and is used for `settings.json` and the Release default.
- `DatabaseLocationSettingsStore`: loads, validates, and atomically saves the optional custom directory.
- `DatabaseLocationResolver`: applies custom → Debug executable → Release system-app-data precedence.
- `DatabaseLocationChangeService`: validates a selected destination, opens it with `PromptRepository`, handles copy/replace preparation, creates backups, and persists the choice only on success.
- `AvaloniaFileDialogService`: add `Task<string?> SelectDatabaseDirectoryAsync(string? suggestedStartDirectory)` using `StorageProvider.OpenFolderPickerAsync` with single selection.

Pass these services into `MainWindowViewModel`. Add bindable properties such as `DatabaseDirectory`, `DatabaseLocationKind`, and `HasCustomDatabaseDirectory`, plus `ChooseDatabaseDirectoryCommand` and `RestoreDefaultDatabaseDirectoryCommand`.

At startup, `App.axaml.cs` should load the settings, resolve one effective directory, and create both the initial repository and retry factory from that immutable startup result. A setting changed during the session must not make the retry factory unexpectedly point at a different database before restart.

## Safe Data Transfer

Do not copy an open LiteDB file directly. To copy current data while the source repository is open:

1. Call `ExportData()` on the current repository to obtain an in-memory `PromptDataDocument` snapshot.
2. Create and validate a repository at the destination.
3. If replacement was approved and a destination file exists, close the validation repository, create `prompts.location-backup-yyyyMMddHHmmss.db`, and reopen a fresh destination repository.
4. Call `ImportData(snapshot)` on the destination repository.
5. Read back the destination data to ensure the database can be reopened.
6. Dispose the destination repository and atomically save the new directory setting.

If any step fails, retain the current setting and current repository. Leave any backup intact and report the error. Do not delete the source database automatically after a successful copy; this makes changing the location reversible and avoids destructive behavior.

Starting empty should create and validate an empty destination repository. Selecting an existing database without replacement should open and read all collections before saving the setting. The existing corrupt-database recovery policy applies, but the location-change dialog must report when a corrupt destination was backed up and replaced rather than doing so silently.

## Validation and Security

- Require a fully qualified path after `Path.GetFullPath` normalization.
- Reject a selected file path; selection is directory-only.
- Create the directory when it does not exist only after user confirmation.
- Verify write access by creating and deleting a uniquely named temporary file in the directory. Avoid fixed probe filenames.
- Handle unauthorized access, read-only media, unavailable network shares, invalid path characters, and destinations that disappear between selection and restart.
- On every startup, validate the configured directory again. If it is unavailable, show a choice to retry or temporarily use the default. A temporary fallback must not erase the saved custom setting.
- Do not expand shell expressions such as `%APPDATA%`, `$HOME`, or `~` in user-selected values; persist the absolute path returned by the native picker.
- Do not allow the configuration file itself to be selected as the database location input because the UI selects directories only.

## Debug Default Details

Use `AppContext.BaseDirectory`, normalized with `Path.GetFullPath`, for the Debug default. Do not use the process working directory because IDEs, tests, and terminal launches can assign different working directories.

The expected development path is typically:

```text
PromptManager.UI/bin/Debug/net10.0/prompts.db
```

This database is generated output and remains ignored through the existing `bin/` rule. Unit tests must inject paths and must not read or write the real executable directory.

Published or Release builds must never default beside the executable, including portable-looking deployments. A user may still explicitly select that directory if it is writable.

## Tests

Add unit tests for:

- A custom absolute directory taking precedence in Debug and Release.
- Debug without a custom setting resolving to `AppContext.BaseDirectory` through an injected executable directory.
- Release without a custom setting resolving to the Windows or Linux system app-data directory.
- Blank, relative, malformed, and inaccessible configured paths falling back with a warning.
- Settings serialization and atomic replacement.
- Restoring the default removing only the override, without deleting either database.
- Selecting the current location being a no-op.
- New destinations supporting copy-current-data and start-empty flows.
- Existing destinations supporting use-existing, confirmed replacement, and cancellation.
- A failed validation or import leaving the saved setting unchanged.
- Destination replacement creating a backup.
- The startup retry factory retaining the startup-resolved directory until restart.

Keep platform cases host-independent by injecting the build mode, system app-data directory, executable directory, filesystem operations where practical, and dialog decisions. Do not mutate process-wide environment variables in parallel tests.

Manual smoke tests on both Windows and Linux should verify:

1. The native folder picker opens and allows one directory.
2. Current data can be copied to a new directory and appears after restart.
3. An existing database can be selected and appears after restart.
4. Restore default returns to the expected Debug or Release location.
5. Unwritable and disconnected destinations show actionable errors.
6. Import, export, corrupt-database backup, and normal shutdown still work at a custom location.

## Documentation Updates During Implementation

Update `README.md`, `docs/USER_GUIDE.md`, `docs/DATA_STORAGE.md`, `docs/ARCHITECTURE.md`, and `docs/TROUBLESHOOTING.md`. Document the location precedence, the compile-time Debug exception, where `settings.json` remains stored, restart behavior, retained source databases, and recovery steps for an unavailable custom directory.

## Acceptance Criteria

- A user can select and persist an absolute database directory through Settings on Windows and Linux.
- The active database remains named `prompts.db`.
- A saved user selection always wins over defaults.
- With no saved selection, Debug uses `AppContext.BaseDirectory` and Release uses the existing platform app-data directory.
- Changing locations cannot silently overwrite or delete prompt data.
- A failed destination or settings write leaves the current location operational and configured.
- The selected location is active after restart and is clearly displayed in Settings.
- Automated tests cover path precedence and all data-switch decisions without depending on the host operating system.
