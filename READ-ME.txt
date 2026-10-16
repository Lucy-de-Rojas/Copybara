COPYBARA - VERSION 5

UPDATE / START
1. Choose Quit app in the older app before updating.
2. Extract this ZIP into a permanent folder.
3. Open its Copybara folder and double-click Start-Copybara.cmd.
The launcher uses Windows' .NET Framework compiler to build Copybara.exe.
No extra packages, internet connection or administrator rights are needed.
All source and icon assets are included. Copybara.exe works on its own afterwards.
Your existing history, pins and startup choice carry over automatically.
Keep the new app folder in place. Do not open an older version afterwards.
If startup is enabled, launching this update refreshes the registered app path.

NEW IN VERSION 5
- Copybara name and a capybara mascot on the app, tray and image-category icons.
- Light, Dark and Random theme buttons at the top right.
- Random chooses a coordinated palette that stays until you choose another theme.
  The exact palette is remembered even after quitting and signing in again.
- Selected items have a bright orange highlighter background in every theme.
  A selected pin turns orange too; when deselected it returns to yellow.
- Backup and Restore buttons for password-protected portable backups.

USE
Copy text or an image normally. Open Copybara with Ctrl+Alt+V or its tray icon.
Select an item to preview it. Copy selected (or double-click) copies it back to
the Windows clipboard; return to your document and press Ctrl+V to paste.
Sidebar: All items, document icon for text, capybara icon for images, Pinned.
Hover over an icon to see its name. Counts refer to the whole collection.
Search and filters work together. Image search uses its label, not OCR.
Pins appear below unpinned items. There is no 25-item limit. Duplicates are saved once.
Delete removes the selected saved item after confirmation.
Clear deletes unpinned history after confirmation; pinned items are kept.
Pause saving stops new captures; Resume saving starts them again.
X hides the window; it keeps saving. Quit app stops it.
In Windows High Contrast mode, system selection colours take priority.

BACKUP
Click Backup, choose the destination and enter a password twice (10+ characters).
The .copybara file contains all saved text, images, pin states, dates and theme.
It is encrypted with AES-256-CBC and authenticated with HMAC-SHA256. Separate
keys are derived from the password using PBKDF2 with a random salt. A fresh
salt and IV are generated for each backup. No cloud service is involved.
Keep the password: there is no password-reset or recovery service.
Store the file somewhere you can find later (for example, an external drive).
A backup is a snapshot: click Backup again to include newer clips.
A backup supports up to 100,000 items. Large histories can take time to process.
Copybara is busy during backup/restore, so wait before copying new items.

RESTORE
Click Restore, choose a .copybara backup and enter its password.
The file is authenticated before importing. Restoring adds missing clips,
restores backed-up pins on matching clips, and applies the backed-up theme.
Your other saved clips remain. Matching contents are not duplicated. An item
already pinned remains pinned. The startup path is not imported from a backup.
The backup can be restored in Copybara on another Windows PC or account.
Imported history is re-encrypted for that PC's current Windows account.
If a disk error occurs during the final import, already imported items are
reported; you can retry the same backup without duplicating them.

AUTOMATIC STARTUP
Start with Windows controls a startup entry for your Windows user, named
JairoClipboard (the original identifier is retained for compatible updates).
It is enabled on first run if never configured before. It opens in the tray at
sign-in; Windows may delay startup. Untick the checkbox to disable it.
If you move the app folder, launch it manually once to update the startup path.
It runs only when you are signed in and does not change other Windows accounts.

WHAT IS SAVED
Plain text, including Unicode and line breaks, and bitmap images stored as PNG.
Not Word formatting, HTML, copied files or animated images. Images take precedence
when one copy contains both text and an image. Items over 25 MB or images over
40 megapixels are skipped. Large histories use more space and take longer to open.
Copybara cannot automatically import Windows+V history: copy those items again
while it is running to save them here.

PRIVACY AND LOCAL FILES
The supplied code makes no network requests, has no account, analytics or uploads.
Local history stays in %LOCALAPPDATA%\JairoClipboard\History for compatibility.
Local history files use Windows DPAPI encryption for your Windows account.
Theme choice is in %LOCALAPPDATA%\JairoClipboard\theme.txt.
Encryption does not protect against malware, apps running as you, or someone
using your unlocked Windows account. A plain copy of the History folder alone
is not a portable backup; use the Backup button for transfer/recovery elsewhere.
Windows clipboard sync and other clipboard apps are independent. This app does
not disable them; Windows sync can be turned off in Settings > System > Clipboard.
Pause saving before copying passwords or information you do not want retained.
Two common password-manager exclusion flags are honoured, but not all sensitive
copies can be recognised. Deletion removes this app's copy, not Windows clipboard
contents, other apps' copies or old backups. Secure disk erasure is not claimed.

REMOVE
Untick Start with Windows, choose Quit app, and delete the app folder.
To remove local history too, delete %LOCALAPPDATA%\JairoClipboard after quitting.
Delete any .copybara backups separately if you do not want to keep them.

VALIDATION
The previous version was confirmed working by Jairo. Version 5 was reviewed and
packaged on Linux. Random palette text contrast and archive integrity were
checked. Windows compilation, backup/restore and the new UI still need a real
Windows test; no Windows runtime was available here.
Try Light/Dark/Random, quit and reopen to check the saved palette, select a
pinned item, and try a backup followed by Restore. Restoring the same backup
should preserve the current collection without duplicate clips.
Send any exact error or a screenshot if something fails.
