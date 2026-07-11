# Prompt Manager Documentation

<img src="../PromptManager.UI/Assets/icon.ico" alt="Prompt Manager icon" width="96" height="96">

This folder contains the project documentation that can be used directly in the repository or copied into a wiki.

## Start Here

- [User Guide](USER_GUIDE.md) - how to use Prompt Manager from the main app window.
- [Development Guide](DEVELOPMENT.md) - local setup, build, test, and contribution workflow.
- [Linux Deployment](LINUX_DEPLOYMENT.md) - build, verify, install, upgrade, and remove a local `.deb` package.
- [Architecture](ARCHITECTURE.md) - app structure, services, models, and UI flow.
- [Data Storage](DATA_STORAGE.md) - LiteDB collections, normalization, backup behavior, and data ownership.
- [Troubleshooting](TROUBLESHOOTING.md) - common build, runtime, and storage issues.

## Project Summary

Prompt Manager is a local-first Avalonia app for managing reusable prompts on Windows and Linux. It stores data locally in LiteDB and exposes a simple two-pane UI:

- Left pane: search, folder tree, flat prompt list, and quick copy actions.
- Right pane: prompt or folder editor.
- Top bar: settings, browse mode toggle, and create actions.

## Documentation Rules

When updating docs:

- Keep command examples directly usable on Windows and Linux. Clearly label alternatives when a command genuinely differs by platform.
- Update the user guide for visible UI or workflow changes.
- Update architecture or storage docs for model, repository, service, or persistence changes.
- Update troubleshooting when a recurring setup or runtime issue is discovered.
- Keep examples concrete and avoid documenting features that are not implemented.
