# Troubleshooting

This page lists common issues when building, testing, or running Prompt Manager.

## Custom Database Directory Is Unavailable

The app warns and temporarily uses the build/platform default without erasing the custom setting. Reconnect or make the directory writable and restart, or select another folder. Former source databases are deliberately retained. After replacement, recover from a timestamped `prompts.location-backup-*.db` by selecting its directory or preserving and renaming the desired file while the app is closed.

## `dotnet` First-Run Permission Error

If `dotnet build` or `dotnet test` cannot create its first-run files, verify that the current user can write to `%USERPROFILE%\.dotnet` on Windows or `$HOME/.dotnet` on Linux. If `DOTNET_CLI_HOME` is set, the CLI uses that directory instead; make sure it exists and is writable.

## Restore Or Build Fails

From the repository root, verify the SDK selected by `global.json`, restore packages, and build the solution:

```sh
dotnet --info
dotnet restore PromptManager.slnx
dotnet build PromptManager.slnx --no-restore
```

Restore needs network access to NuGet. Check proxy, certificate, and package-source settings if it cannot download dependencies. On Linux, also check filename casing because paths are case-sensitive.

### Linux Desktop Libraries

On Debian 13 or Ubuntu 24.04 LTS, install the validated runtime libraries:

```sh
sudo apt-get update
sudo apt-get install libx11-6 libice6 libsm6 libfontconfig1
```

Other distributions use different package names. Find packages providing X11, ICE, SM, and Fontconfig runtime libraries for that distribution.

## Linux Deployment Script Fails

Run `./linux-deploy.sh` from the repository root. If it reports a missing command, install the packaging prerequisites:

```sh
sudo apt-get update
sudo apt-get install dpkg-dev lintian imagemagick
```

Use an application version without a leading `v`, choose only `amd64` or `arm64`, and enter the maintainer as `Name <email>`. Restore and publish require NuGet access. A GUI smoke test requires an active desktop session; answer `n` when packaging in a headless environment.

If Lintian fails, review its findings before choosing whether to create the checksum. Installation or executable-layout errors should not be ignored. See the [Linux Deployment Guide](LINUX_DEPLOYMENT.md) for output paths, checksum verification, and installation steps.

## Unit Tests Do Not Discover Tests

Run the test project directly:

```sh
dotnet test PromptManager.UnitTests/PromptManager.UnitTests.csproj
```

The test project targets .NET 10 and references the Avalonia and Core projects.

## Linux Window Does Not Open

The GUI requires an active X11 or Wayland desktop session. Check `DISPLAY` for X11 and `WAYLAND_DISPLAY`/`XDG_RUNTIME_DIR` for Wayland. A remote shell or CI runner is usually headless; use Xvfb for an intentional GUI smoke test, or limit that environment to build and unit-test validation.

## Clipboard, File Picker, Or Browser Problems

- Clipboard access requires a running desktop session and clipboard provider. Try the same action in another desktop application.
- File pickers are supplied by the active Avalonia platform backend. Verify the desktop portal/provider is running and that the selected directory is writable.
- Opening the repository link uses the default browser and falls back to `xdg-open` on Linux. Verify a default browser is configured and `xdg-open` is installed and available on `PATH`.
- Permission errors during import/export or startup usually mean the app-data or selected destination directory is not writable by the current user.

## App Opens With Empty Data

If the LiteDB file cannot be opened, Prompt Manager shows a startup error and opens with an empty workspace. If the file exists and is detected as damaged, the repository moves it aside with a `prompts.corrupt-*.db` name and creates a fresh database. Look in the platform app-data directory for the backup.

## Prompts Or Tags Appear Reordered

This is expected. The app sorts prompts, folders, tags, and model names alphabetically in several places so the UI stays predictable.

## Duplicate Tags Or Models Disappear

This is expected. Tags and model names are deduplicated case-insensitively after trimming whitespace.

## Folder Cannot Be Moved

A folder cannot be moved inside itself or inside one of its descendants. This prevents cycles in the folder tree.
