# Version 1.1.0.0

Windows x64 release of SevenDaysModHandler.

## What's new

- English, German, Spanish, Italian, and French interface, notices, and application error messages.
- Automatic Windows display language detection with English fallback, plus a persistent manual language selection that applies immediately.
- Prominent game launch buttons for playing without Easy Anti-Cheat (for mods) or with EAC. Both options preserve the active mod selection.
- A distinct launch area with the EAC explanation beside the buttons and clear separation from the tabs.
- English README and a linked German version in README.de.md.

## Download and run

Extract `SevenDaysModHandler-1.1.0.0-win-x64.zip` completely and run `SevenDaysModManager.exe`. The .NET runtime is included. Close the game and server before backing up, restoring, or switching mods.

`SHA256SUMS.txt` contains the SHA-256 checksum of the ZIP archive.

## Validation and scope

Release build and integration tests passed, including language detection, translation coverage, launch configuration, backups, restores, and mod/group operations. Tests do not launch the actual game; interactive GUI validation remains separate.

Backups include only the configured saves folder, not external generated worlds or mods. Launching with EAC does not automatically disable mods.
