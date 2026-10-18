# SevenDaysModHandler

English | [Deutsch](README.de.md)

A multilingual Windows application for backing up saves and managing mods for **7 Days to Die**. Built with C# and Windows Forms on .NET 10.

## Download and launch

The latest application is available from **[GitHub Releases](https://github.com/LSC-Labs/SevenDaysModHandler/releases/latest)**.

1. Download `SevenDaysModHandler-1.0.0-win-x64.zip`.
2. Extract the entire ZIP into its own folder.
3. Run `SevenDaysModManager.exe`.
4. In **Settings**, check and save the game, saves, and backup folders.

The release download includes the .NET runtime. No separate .NET installation is required. You need an x64 version of Windows supported by .NET 10. The application requires no installation.

## Features

- **Launch the game:** Two large buttons above the tabs launch the selected installation without EAC (for mods) or with Easy Anti-Cheat, using `7DaysToDie.exe` or `7DaysToDie_EAC.exe`, respectively. Both options preserve the active mod selection; EAC does not disable mods. To play without your own mods, deactivate them in the mod manager first. Missing executables and an already running game or server are reported.
- **Back up saves:** Copies the entire configured saves folder into a backup folder named with the UTC date and time. The format `2026-10-17T12-30-00.1234567Z` uses Windows-compatible hyphens instead of colons.
- **Restore a backup:** Creates a safety backup first, then replaces the current saves folder with the selected backup. Incomplete backups are excluded from the list.
- **Activate and deactivate mods:** Moves entire mod folders between `<Game>\Mods` and `<Game>\Mods-Store`. Existing destination folders are never overwritten.
- **Mod information:** Displays the name, version, description, and clickable website from `ModInfo.xml`. Both old and new metadata formats are supported.
- **Mod groups:** Saves named mod selections for each game installation. Activating a group activates exactly its mods and deactivates all others. Deactivating a group affects only that group's mods.
- **Sortable mod list:** Click a column header to change the sort direction. Active mods have a light green status cell.
- **Steam search:** On first launch, searches for installations in Steam libraries on available fixed drives, including entries in `libraryfolders.vdf`. Other installations can be selected manually.

## Using mods and groups

Place extracted mods in this structure:

```text
<Game>\Mods-Store\MyMod\ModInfo.xml
```

On the **Mods** tab, use **Activate/Deactivate** to toggle a mod. Websites open in the default browser.

On the **Mod groups** tab, create a new group, enter a name, check its members, and select **Save group**. A mod can belong to multiple groups. Save changes before activating a group. Activating an empty group deactivates all mods after confirmation. Deleting a group does not change any mod files.

Missing group mods and destination conflicts prevent activation before any mods are moved. If switching groups fails, the application attempts to move any affected mods back.

## Settings and storage locations

Under **Settings → Language**, choose **English, Deutsch, Español, Italiano, or Français**, or **Automatic (system language)**. By default, the application follows the Windows display language, including regional variants such as French (Canada). Other system languages fall back to English. A manual selection takes priority, applies immediately, and is saved for future launches. The interface, notices, and application-defined error messages are translated. Mod names, mod descriptions, and custom group names remain in their original language.

| Content | Default path |
| --- | --- |
| Settings and mod groups | `%LOCALAPPDATA%\7Days-ModManager\settings.json` |
| Backups | `%LOCALAPPDATA%\7Days-ModManager\Backups` |
| Saves | `%APPDATA%\7DaysToDie\Saves` |

You can change these paths in **Settings**. The backup folder is checked for read and write access. If an old default backup path under Documents is inaccessible, the application switches to the current default and displays a notice; existing backups remain in their original location.

## Scope and notes

- Close the game and dedicated server before backing up, restoring, or switching mods, and keep them closed throughout the operation. The application checks the usual process names.
- A backup includes only the selected saves folder. External generated world data, the game installation, and mods are not included.
- The application manages mods in the selected game folder. Mods under `%APPDATA%\7DaysToDie\Mods` remain unchanged.
- Restoring replaces the entire saves folder, including all worlds and sessions it contains.
- Linked folders and files are not supported for backup and mod operations for safety reasons.
- The application does not check compatibility between mods or with the game version.

## Building from source

Requirements: Windows and the **.NET 10 SDK**.

```powershell
git clone https://github.com/LSC-Labs/SevenDaysModHandler.git
cd SevenDaysModHandler
.\build.cmd
.\Start.cmd
```

`build.cmd` creates the Release output in `artifacts\app`. This local build requires the .NET 10 Desktop Runtime. Close the application before rebuilding.

To build a standalone Windows x64 download with the runtime included:

```powershell
dotnet publish SevenDaysModManager -c Release -r win-x64 --self-contained true -p:Version=1.0.0 -o artifacts/release/win-x64
```

## Tests and project structure

```powershell
dotnet run --project Tests -c Release
```

The integration tests use their own temporary files and cover backups, restoration, safety backups, path validation, mod metadata, mod switching, group switching, and conflict protection. Interactive GUI validation is performed separately.

| File/folder | Content |
| --- | --- |
| `SevenDaysModManager/Program.cs` | Main window and mod list |
| `SevenDaysModManager/Groups.cs` | Mod group interface |
| `SevenDaysModManager/Services.cs` | Settings, Steam search, and file operations |
| `Tests/` | Integration tests without external test packages |
| `build.cmd`, `Start.cmd` | Local build and launch scripts |

## Game attribution

This project is an independent utility for 7 Days to Die. The application icon was taken from the locally installed `7DaysToDie.exe`; the game name and icon belong to their respective rights holders (The Fun Pimps).

The supported ModInfo format is described in [The Fun Pimps developer forum](https://community.thefunpimps.com/threads/what-is-the-new-layout-for-modinfo-xml-v2.32514/).
