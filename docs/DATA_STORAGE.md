# Data Storage

Prompt Manager stores data locally with LiteDB.

## Database Location

A saved custom directory wins over all defaults. Without one, compile-time Debug builds use `AppContext.BaseDirectory`; Release builds use the platform locations below. Runtime environment names do not change a packaged Release build. The database filename is always `prompts.db`.

The database file is created at:

```text
Windows: %APPDATA%\PromptManager\prompts.db
Linux:  ${XDG_DATA_HOME:-$HOME/.local/share}/PromptManager/prompts.db
```

On Linux, an absolute `XDG_DATA_HOME` is honored. When it is unset, blank, or relative, the app follows the XDG fallback and uses `$HOME/.local/share/PromptManager/prompts.db`. If the required user directory cannot be determined, startup reports an error instead of creating a relative data directory.

The optional override is stored in `settings.json` in platform app-data even when the database is elsewhere. It is written with a same-directory temporary file and atomic replacement. Transfers use repository export/import instead of copying an open LiteDB file. Sources are retained; replaced destinations receive `prompts.location-backup-<timestamp>.db`. Location changes activate after restart.

## Collections

The database uses four collections:

- `prompts` - prompt records.
- `folders` - folder records.
- `tagOptions` - shared tag names.
- `modelOptions` - shared AI model names.

## Indexes

The repository ensures these indexes when opening the database:

- `prompts.Name`
- `prompts.FolderId`
- `prompts.AiModel`
- `folders.Name`
- `folders.ParentFolderId`
- `tagOptions.Name`, unique
- `modelOptions.Name`, unique

## Prompt Data

Each prompt stores:

- Name
- Description
- Prompt content
- Optional folder id
- Tags
- Quality score from 1 to 10
- Optional AI model name
- Created timestamp
- Updated timestamp

Quality is clamped to the `1..10` range before saving.

## Folder Data

Each folder stores:

- Name
- Description
- Optional parent folder id
- Created timestamp
- Updated timestamp

Deleting a folder deletes that folder, all descendant folders, and prompts stored in those folders.

## Normalization

The repository normalizes data when reading and saving:

- Null prompt strings become empty strings.
- Null folder strings become empty strings.
- AI model names are trimmed.
- Tags are trimmed.
- Blank tags are removed.
- Duplicate tags are removed case-insensitively.
- Tags are sorted alphabetically.
- Shared tag and model option lists are trimmed, deduplicated case-insensitively, and sorted alphabetically.

## Corrupt Database Handling

If the app cannot open the database and the database file exists, the repository moves the file aside and creates a new database.

Backup files use this pattern:

```text
prompts.corrupt-yyyyMMddHHmmss.db
```

If a backup with the same timestamp already exists, a numeric suffix is added.

## Data Ownership

Prompt Manager is local-first. There is no account system, cloud sync, or remote storage in the current app. Users are responsible for backing up their local app data if they need migration or recovery outside the built-in corrupt-file backup behavior.

## JSON Import And Export

The app can export all managed data to a JSON file:

- Folders
- Prompts
- Shared tags
- Shared AI models
- Export document version
- Export timestamp in UTC

Importing a JSON file replaces the current LiteDB contents after user confirmation. Import uses the same normalization rules as repository saves and also protects folder references:

- Prompt `FolderId` values that do not exist in the imported folder set are cleared.
- Folder `ParentFolderId` values that are missing, self-referencing, or cyclic are cleared.

Avalonia passes the `.json` file filter to the native storage provider on Windows and Linux. The exact appearance is controlled by the active desktop environment, but import and export request JSON files on both platforms.
