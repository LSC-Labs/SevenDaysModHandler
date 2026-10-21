# Version 1.2.0.0

Windows x64 release of SevenDaysModHandler.

## What's new

- Optional backup names/descriptions displayed beside the timestamp; descriptions can also be edited for existing backups.
- Delete a selected backup after confirmation, with validation to protect other folders and saves.
- Configurable light and dark appearance, applied immediately and saved for future launches.
- About page with application description, author LSC Labs, version, and GitHub release checks. Newer versions link to their release downloads.
- Application version displayed in the window title and About page.
- Readable mod website links in normal, active, and visited states, with colors adapted to the selected appearance.

## Download and run

Extract `SevenDaysModHandler-1.2.0.0-win-x64.zip` completely and run `SevenDaysModManager.exe`. The .NET runtime is included. Close the game and server before backing up, restoring, or switching mods.

`SHA256SUMS.txt` contains the SHA-256 checksum of the ZIP archive.

## Validation and scope

Release build and integration tests passed, including backup descriptions and deletion, appearance persistence, release parsing and version comparison, translation coverage, backups, restores, and mod/group operations. Tests do not launch the actual game; interactive GUI validation remains separate.

Backups include only the configured saves folder, not external generated worlds or mods. Launching with EAC does not automatically disable mods.
