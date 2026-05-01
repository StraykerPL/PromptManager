# Prompt Manager User Guide

Prompt Manager is a desktop app for saving, organizing, searching, and copying reusable prompts. The main screen is split into a prompt list on the left and an editor on the right.

## Quick Start

1. Select **New folder** if you want to organize prompts by topic or project.
2. Select **New prompt**.
3. Enter a name and prompt text.
4. Optionally choose tags, a folder, a quality score, and an AI model.
5. Select **Save**.
6. Use **Copy** from the left list whenever you need the prompt text.

## Main Screen

The top bar contains the main actions:

- **Settings** opens data import/export, tag management, AI model management, and About.
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
6. Set the prompt **Quality** from 1 to 10.
7. Choose the **AI model** used with this prompt, or keep **No model**.
8. Enter the prompt text in the **Prompt** field.
9. Select **Save**.

The app requires both a prompt name and prompt text. If either is missing, the prompt will not be saved.

## Editing a Prompt

1. Select a prompt from the left list.
2. Update its name, description, tags, folder, quality, AI model, or prompt text.
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
- AI model
- Tags

Search results are shown as a flat list of prompts. Clearing the search field returns to the normal folder tree.

## Setting Prompt Quality

Each prompt can have a quality score from 1 to 10:

- **1** means the prompt does not work.
- **10** means the prompt works perfectly.

Use the **Quality** slider in the prompt editor when creating or editing a prompt. The selected value is saved with the prompt, so you can track which prompts are reliable and which ones need improvement.

## Managing Tags

Select **Settings** to open the settings dialog, then use the **Tags** section.

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

## Managing AI Models

Select **Settings** to open the settings dialog, then use the **AI models** section.

To add a model:

1. Type the model name in the **New model** field.
2. Select **Add**, or press Enter.

To remove a model:

1. Find the model in the model manager.
2. Select **Remove**.

Models are stored as a shared list. After a model is added, it appears in the **AI model** list in the prompt editor. Select a model there to record which AI or LLM model was used with the prompt.

Model names are cleaned up automatically:

- Extra spaces are removed.
- Blank model names are ignored.
- Duplicate model names are ignored without matching letter case.
- Model names are sorted alphabetically.

## Importing And Exporting Data

Select **Settings**, then select **Export** to save the current Prompt Manager data to a JSON file. The system save dialog asks where to store the file and only offers the JSON file type.

Select **Settings**, then select **Import** to load data from a JSON file. The system open dialog is filtered to JSON files. Importing replaces the current local prompts, folders, tags, and AI models, so the app asks for confirmation before loading the file.

Imported data is cleaned up before it is saved:

- Prompt quality is clamped to 1 through 10.
- Tags and model names are trimmed, deduplicated, and sorted.
- Prompts that point to missing folders are moved to the root level.
- Folders that point to missing, invalid, or cyclic parents are moved to the root level.

## About

Select **Settings**, then select **About** to view the app name, app version, .NET version, app logo, copyright placeholder, and GitHub repository link placeholder.

## Storage And Startup

Prompt Manager stores prompts, folders, tags, model names, quality scores, and prompt model selections locally on the device. If the storage file cannot be opened at startup, the app shows a startup error and opens with an empty workspace. Saving or loading data may remain unavailable until storage can be opened again.

If a damaged storage file is detected, the app attempts to move it aside as a backup and create a fresh database.

## Current Limitations

Prompt Manager currently does not provide built-in cloud sync, accounts, collaboration, or encryption settings. Data is local to the device and app data directory.

## Recommended Workflow

Use folders for broad categories such as projects, products, or writing tasks. Use tags for qualities that cross folder boundaries, such as `drafting`, `review`, `coding`, `marketing`, or `needs-work`. Use the quality score to separate proven prompts from experiments.
