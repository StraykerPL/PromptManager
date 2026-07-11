# Linux Deployment

Prompt Manager provides an interactive repository script that builds a self-contained application and packages it as a local Debian `.deb` file. The resulting package does not require a separate .NET runtime and can be installed on Debian and Debian-derived distributions such as Ubuntu and Linux Mint.

The script supports these targets:

| Debian architecture | .NET runtime identifier |
| --- | --- |
| `amd64` | `linux-x64` |
| `arm64` | `linux-arm64` |

Run the script on the same architecture as the destination system unless the required target runtime can be restored and published from the build host.

## Prerequisites

Install the .NET 10 SDK selected by `global.json` and the Debian packaging tools:

```sh
sudo apt-get update
sudo apt-get install dpkg-dev lintian imagemagick
dotnet --info
```

The script requires `dotnet`, `dpkg-deb`, `lintian`, `sha256sum`, and standard Unix file utilities. ImageMagick may provide either the `magick` or legacy `convert` command.

Restoring and publishing dependencies requires access to NuGet. The optional GUI smoke test also requires an active X11 or Wayland desktop session.

## Build The Package

Run the executable script from the repository root:

```sh
./linux-deploy.sh
```

The script asks for:

1. Application version, as SemVer without a leading `v`, for example `1.2.0`.
2. Debian package version, normally the application version followed by a Debian revision, for example `1.2.0-1`.
3. Debian architecture, either `amd64` or `arm64`.
4. Package maintainer in `Name <email>` format. This value is local package metadata and does not contact or publish to the address.

Review the displayed values before confirming the build. For a packaging-only revision, keep the application version unchanged and increment the Debian revision, for example from `1.2.0-1` to `1.2.0-2`.

The automated workflow then:

1. Clears the previous `artifacts/publish/` and `artifacts/package/` staging directories.
2. Restores the solution.
3. Runs the Release unit tests and solution build.
4. Publishes a self-contained, runtime-specific Release application.
5. Optionally launches the published executable for a GUI smoke test.
6. Assembles the Debian filesystem under `artifacts/package/`.
7. Builds and inspects the `.deb` package.
8. Runs Lintian and asks whether to continue if Lintian fails.
9. Creates and verifies a SHA-256 checksum.
10. Optionally installs the package locally through APT and `sudo`.

Close Prompt Manager after the optional pre-package GUI test so the script can continue.

## Output

Successful builds create these files:

```text
artifacts/deb/promptmanager_<debian-version>_<architecture>.deb
artifacts/deb/promptmanager_<debian-version>_<architecture>.deb.sha256
```

The package installs application files under `/opt/promptmanager`, creates the `promptmanager` command under `/usr/bin`, and registers a desktop application entry and icon.

Do not commit `artifacts/`, `bin/`, or `obj/` output.

## Verify And Install Elsewhere

Copy both generated files to the destination machine. From the directory containing them, verify the checksum before installation:

```sh
sha256sum --check promptmanager_<debian-version>_<architecture>.deb.sha256
sudo apt install ./promptmanager_<debian-version>_<architecture>.deb
```

Continue only when checksum verification reports `OK`. The checksum detects accidental corruption but does not authenticate a package obtained from an untrusted source.

Start Prompt Manager from the desktop application menu or run:

```sh
promptmanager
```

## Smoke Test

After installation, verify that:

1. Prompt Manager appears in the desktop application menu with its icon.
2. It starts from both the menu and the `promptmanager` command.
3. Folder and prompt creation, search, clipboard copy, import, and export work.
4. The repository link opens in the default browser.
5. Existing prompts and settings remain available after an upgrade.

## Upgrade And Removal

Install a newer package with APT in the same way as the first package:

```sh
sudo apt install ./promptmanager_<new-version>_<architecture>.deb
```

Application data is stored per user under `${XDG_DATA_HOME:-$HOME/.local/share}/PromptManager` by default and is not removed or replaced during package upgrades.

Remove the application with:

```sh
sudo apt remove promptmanager
```

Package removal does not delete the user's database or settings. Back up and delete the data directory manually only when the data is no longer needed.

## Troubleshooting

If the script reports a missing command, install the prerequisite package listed above and run it again from the repository root. If restore or publish fails, verify the selected SDK with `dotnet --info` and confirm NuGet connectivity.

Review all Lintian findings before distributing a package. A missing local changelog or local maintainer address may be acceptable for private use, but errors affecting package installation, permissions, dependencies, or executable layout should be resolved.

For GUI startup, runtime library, and desktop-session problems, see [Troubleshooting](TROUBLESHOOTING.md).
