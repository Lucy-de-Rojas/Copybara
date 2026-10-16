# Copybara 🦫

A free, local clipboard manager for Windows, with a friendly capybara mascot.

Save text and images, keep favourites pinned, search your history and personalise your clipboard with colourful themes.

## Features

- **Text and images** — save clipboard content while Copybara is running.
- **More pins** — keep favourites without Windows’ 25-item pin limit.
- **Search and filters** — find saved text, images or pinned items.
- **Clear selection** — selected items are highlighted in bright orange.
- **Pinned favourites** — highlighted in yellow, below unpinned items.
- **Themes** — choose Light, Dark or Random. Your choice is remembered.
- **Keyboard shortcut** — open Copybara with `Ctrl + Alt + V`.
- **Automatic startup** — optionally start when signing in to Windows.
- **Backup and restore** — create portable, password-protected backups.
- **Capybara scrolling** — drag the capybara handle to browse your collection.

## Getting started

Copybara is a Windows desktop application requiring .NET Framework 4.8 or later.

1. Download and extract the project ZIP, or clone the repository.
2. Keep the extracted files in a permanent folder.
3. Double-click `Start-Copybara.cmd`.
4. The launcher builds `Copybara.exe` from the included source and opens it.

No third-party packages or administrator privileges are required.

After the first successful build, you can open `Copybara.exe` directly.

## Using Copybara

1. Copy text or an image normally.
2. Open Copybara using `Ctrl + Alt + V` or its notification-area icon.
3. Select an item to preview it.
4. Click **Copy selected**, or double-click the item.
5. Return to your document and press `Ctrl + V` to paste.

Copybara must be running to save new clipboard content.

### Search and filters

Use the sidebar to display:

- **All items**
- **Text**, shown by the document icon
- **Images**, shown by the capybara icon
- **Pinned items**

Hover over an icon to see its name.

Search and filters work together. Image search uses the saved label; it does not read text inside images.

### Managing items

- **Pin / Unpin** changes an item's pin status.
- **Delete** removes the selected saved item after confirmation.
- **Clear** removes unpinned history after confirmation. Pinned items are kept.
- Duplicate content is saved once.

### Themes

Under **Theme selector**, choose:

- **Light**
- **Dark**
- **Random**

Random creates a colour palette that stays in place until you choose another theme, including after restarting the app.

Windows High Contrast colours take priority over custom selection colours.

### Running in the background

- **Pause saving** stops new captures.
- **Resume saving** starts capturing again.
- Closing the window with **X** hides it while collection continues.
- **Quit app** stops collection and exits.
- **Start with Windows** controls automatic startup at sign-in.

The `Ctrl + Alt + V` shortcut can also launch Copybara when it is closed, once its Start menu shortcut has been created.

## Backup and restore

The **Backup** and **Restore** buttons are at the bottom right.

### Create a backup

1. Click **Backup**.
2. Choose where to save the file.
3. Enter a password twice, using at least 10 characters.
4. Keep the resulting `.copybara` file somewhere safe.

The backup includes saved text, images, pins, dates and theme.

A backup is a snapshot. Create a new one to include newer clips.

**Keep your password: there is no password-reset or recovery service.**

### Restore a backup

1. Click **Restore**.
2. Select a `.copybara` backup file.
3. Enter its password.

Missing clips are added without duplicating matching content. Backed-up pins and theme are restored, and other current clips are kept.

Backups can be restored on another Windows PC or account.

### Backup protection

Backups use:

- AES-256-CBC encryption
- HMAC-SHA256 authentication
- Separate keys derived from the password using PBKDF2
- A fresh random salt and IV for each backup

The format supports up to 100,000 items. Large histories may take time to process. Clipboard collection pauses during backup and restore dialogs.

## Privacy and storage

Copybara stores its history locally. The supplied code contains no network requests, accounts, analytics or upload functionality.

Local history is encrypted using Windows DPAPI for the current Windows account.

| Data | Location |
| --- | --- |
| Saved history | `%LOCALAPPDATA%\Copybara\History` |
| Theme preference | `%LOCALAPPDATA%\Copybara\theme.txt` |

Use **Backup** to transfer history to another computer. Copying the History folder alone does not create a portable backup.

Other applications running under your Windows account may access clipboard data. Pause saving before copying information you do not want retained.

Deleting an item from Copybara does not remove it from the current Windows clipboard or older backups. Windows clipboard sync is managed separately.

## Updating

1. Choose **Quit app** before replacing application files.
2. Replace the project files with the newer version.
3. Run `Start-Copybara.cmd` to rebuild and launch.

Keep the application folder in a permanent location. If you move it, open Copybara manually once to refresh its startup and shortcut paths.

## Sharing

Share the project ZIP or a link to this repository.

Each recipient runs Copybara on their own Windows PC, with separate local history.

**Do not upload personal `.copybara` backups or `.clip` history files to GitHub.**

## Supported content

Copybara saves plain text and bitmap images.

Rich text formatting, copied files, HTML formatting and animation are not preserved. Images take precedence when a clipboard item contains both text and an image.

Individual items over 25 MB and images over 40 megapixels are skipped. Large histories use more disk space and may slow startup.

Existing Windows clipboard history cannot be imported automatically. Copy those entries again while Copybara is running.

## Source files

| File | Purpose |
| --- | --- |
| `Copybara.cs` | Interface, clipboard collection, themes and local storage |
| `BackupSupport.cs` | Password dialogs and encrypted backup and restore |
| `ScrollSupport.cs` | Capybara scroll handle and list scrolling |
| `Start-Copybara.cmd` | Build and launch script |
| `Copybara.svg`, `Copybara.png`, `Copybara.ico` | Mascot and application icons |

## Reporting problems

Please include:

- Your Windows version
- Steps to reproduce the problem
- The exact error message, if any

Remove sensitive clipboard content from screenshots before sharing them.

Copybara has not undergone an independent security audit.

## Uninstalling

1. Untick **Start with Windows**.
2. Choose **Quit app**.
3. Delete the application folder.
4. Press `Windows + R`, enter `shell:programs`, and delete `Copybara.lnk`.
5. To remove saved history and preferences, delete `%LOCALAPPDATA%\Copybara`.

Delete backup files separately if you no longer need them.