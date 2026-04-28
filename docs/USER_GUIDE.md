# Prompt Manager User Guide

Prompt Manager is a desktop app for saving, organizing, searching, and copying reusable prompts. The main screen is split into a prompt list on the left and an editor on the right.

## Main Screen

The top bar contains the main actions:

- **Tags** opens the tag manager.
- **All prompts** switches the left list from folder tree mode to a flat list of every prompt.
- **New folder** opens the folder editor.
- **New prompt** opens the prompt editor.

The left panel contains:

- A search field for finding prompts.
- A tree or prompt list.
- A **Copy** button on each prompt row.

The right panel contains the editor for the currently selected prompt or folder.

## Creating a Prompt

1. Select **New prompt**.
2. Enter a prompt **Name**.
3. Optionally enter a **Description**.
4. Select any existing tags you want to apply.
5. Choose a folder from the **Folder** list, or keep **No folder**.
6. Enter the prompt text in the **Prompt** field.
7. Select **Save**.

The app requires both a prompt name and prompt text. If either is missing, the prompt will not be saved.

## Editing a Prompt

1. Select a prompt from the left list.
2. Update its name, description, tags, folder, or prompt text.
3. Select **Save**.

When editing an existing prompt, the **Copy prompt** and **Delete** buttons are shown above the editor.

## Copying a Prompt

There are two ways to copy prompt text:

- Select **Copy** on a prompt row in the left list.
- Open a prompt and select **Copy prompt** above the editor.

Only the prompt text is copied.

## Deleting a Prompt

1. Select a prompt from the left list.
2. Select **Delete**.
3. Confirm the delete action.

Deleted prompts are removed from the app.

## Creating a Folder

1. Select **New folder**.
2. Enter a **Folder name**.
3. Optionally enter a **Description**.
4. Choose a **Parent folder**, or keep **No parent folder**.
5. Select **Save**.

Folders can be nested inside other folders. A folder cannot be moved inside itself or inside one of its child folders.

## Editing a Folder

1. Select a folder from the left tree.
2. Update its name, description, or parent folder.
3. Select **Save**.

Selecting a folder also expands or collapses it in the tree.

## Deleting a Folder

1. Select a folder from the left tree.
2. Select **Delete**.
3. Confirm the delete action.

Deleting a folder also deletes its child folders and all prompts stored inside those folders.

## Browsing Prompts

By default, prompts are shown in a folder tree:

- Folders are listed first.
- Prompts without a folder appear at the root level.
- Selecting a folder expands or collapses it.

Select **All prompts** to show every prompt in a flat list. In this mode, folders are hidden and prompts are sorted by name.

When flat list mode is active, the button changes to **Groups tree**. Select it to return to the folder tree.

## Searching

Use the search field in the left panel to find prompts. Search matches:

- Prompt name
- Description
- Prompt text
- Tags

Search results are shown as a flat list of prompts. Clearing the search field returns to the normal folder tree.

## Managing Tags

Select **Tags** to open the tag manager.

To add a tag:

1. Type the tag name in the **New tag** field.
2. Select **Add**, or press Enter.

To remove a tag:

1. Find the tag in the tag manager.
2. Select **Remove**.

Tags are stored as a shared list. After a tag is added, it appears as a selectable chip in the prompt editor. Select a tag chip to add it to the current prompt; select it again to remove it from that prompt.

Tags are cleaned up automatically:

- Extra spaces are removed.
- Blank tags are ignored.
- Duplicate tags are ignored without matching letter case.
- Tags are sorted alphabetically.

## Storage And Startup

Prompt Manager stores prompts, folders, and tags locally on the device. If the storage file cannot be opened at startup, the app shows a startup error and opens with an empty workspace. Saving or loading data may remain unavailable until storage can be opened again.

If a damaged storage file is detected, the app attempts to move it aside as a backup and create a fresh database.
