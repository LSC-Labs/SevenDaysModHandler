# SevenDaysModHandler

[English](README.md) | Deutsch

Eine mehrsprachige Windows-Anwendung zum Sichern von Spielständen und Verwalten von Mods für **7 Days to Die**. Entwickelt mit C# und Windows Forms auf .NET 10.

## Download und Start

Die aktuelle Anwendung steht unter **[GitHub Releases](https://github.com/LSC-Labs/SevenDaysModHandler/releases/latest)** bereit.

1. `SevenDaysModHandler-1.1.0.0-win-x64.zip` herunterladen.
2. Das ZIP vollständig in einen eigenen Ordner entpacken.
3. `SevenDaysModManager.exe` starten.
4. Unter **Einstellungen** Spiel-, Spielstand- und Backupordner prüfen und speichern.

Der Release-Download enthält die .NET-Laufzeit. Eine zusätzliche .NET-Installation ist nicht erforderlich. Benötigt wird ein von .NET 10 unterstütztes Windows auf x64. Die Anwendung benötigt keine Installation.

## Funktionen

- **Spiel starten:** Zwei große Buttons oberhalb der Registerkarten starten die gewählte Installation ohne EAC (für Mods) oder mit Easy Anti-Cheat. Der Start erfolgt über `7DaysToDie.exe` beziehungsweise `7DaysToDie_EAC.exe`. Beide Varianten behalten die aktive Modauswahl bei; EAC ist kein Schalter zum Deaktivieren von Mods. Für einen Start ohne eigene Mods diese vorher in der Modverwaltung deaktivieren. Fehlende Startprogramme und bereits laufendes Spiel/Server werden gemeldet.
- **Spielstand sichern:** Kopiert den gesamten konfigurierten Spielstandordner in einen Backupordner mit UTC-Datum und Uhrzeit. Das Format `2026-10-17T12-30-00.1234567Z` verwendet Windows-kompatible Bindestriche statt Doppelpunkten.
- **Backup wiederherstellen:** Erstellt vorher ein Sicherheitsbackup und ersetzt anschließend den aktuellen Spielstandordner durch das ausgewählte Backup. Unvollständige Backups werden nicht angeboten.
- **Mods aktivieren und deaktivieren:** Verschiebt vollständige Modordner zwischen `<Spiel>\Mods` und `<Spiel>\Mods-Store`. Vorhandene Zielordner werden nicht überschrieben.
- **Modinformationen:** Zeigt Name, Version, Beschreibung und anklickbare Webseite aus `ModInfo.xml`. Alte und neue Metadatenformate werden unterstützt.
- **Modgruppen:** Speichert benannte Zusammenstellungen für die jeweilige Spielinstallation. Beim Aktivieren werden genau deren Mods aktiviert und alle übrigen deaktiviert. Beim Deaktivieren werden nur die Mods dieser Gruppe deaktiviert.
- **Sortierbare Modliste:** Klick auf den Spaltenkopf wechselt die Sortierrichtung. Aktive Mods haben eine hellgrüne Statuszelle.
- **Steam-Suche:** Sucht beim ersten Start nach Installationen in Steam-Bibliotheken auf verfügbaren festen Laufwerken, einschließlich der Einträge in `libraryfolders.vdf`. Sonderinstallationen können manuell ausgewählt werden.

## Mods und Gruppen verwenden

Entpackte Mods in folgender Struktur ablegen:

```text
<Spiel>\Mods-Store\MeinMod\ModInfo.xml
```

Auf **Mods** schaltet der Button **Aktivieren/Deaktivieren** den jeweiligen Mod um. Webseiten werden im Standardbrowser geöffnet.

Auf **Modgruppen** eine neue Gruppe anlegen, einen Namen eingeben, Mitglieder anhaken und **Gruppe speichern** wählen. Ein Mod darf mehreren Gruppen angehören. Änderungen vor dem Aktivieren speichern. Das Aktivieren einer leeren Gruppe deaktiviert nach Rückfrage alle Mods. Das Löschen einer Gruppe verändert keine Moddateien.

Fehlende Gruppenmods und Zielkollisionen verhindern die Aktivierung vor dem ersten Verschieben. Bei Fehlern während eines Gruppenwechsels versucht die Anwendung bereits verschobene Mods zurückzusetzen.

## Einstellungen und Speicherorte

Unter **Settings → Language** (Deutsch: **Einstellungen → Sprache**) stehen **English, Deutsch, Español, Italiano und Français** sowie **Automatisch (Systemsprache)** zur Verfügung. Standardmäßig folgt die Anwendung der Windows-Anzeigesprache; regionale Varianten wie Französisch (Kanada) werden unterstützt. Bei anderen Systemsprachen wird Englisch verwendet. Eine manuelle Auswahl hat Vorrang, wirkt sofort und wird für weitere Starts gespeichert. Die Texte der Oberfläche, Hinweise und anwendungseigenen Fehlermeldungen sind übersetzt. Modnamen, Modbeschreibungen und eigene Gruppennamen bleiben im Original.

| Inhalt | Standardpfad |
| --- | --- |
| Einstellungen und Modgruppen | `%LOCALAPPDATA%\7Days-ModManager\settings.json` |
| Backups | `%LOCALAPPDATA%\7Days-ModManager\Backups` |
| Spielstände | `%APPDATA%\7DaysToDie\Saves` |

Die Pfade können unter **Einstellungen** geändert werden. Der Backupordner wird auf Lese- und Schreibzugriff geprüft. Ein nicht zugänglicher alter Documents-Standardpfad wird mit Hinweis auf den aktuellen Standard umgestellt; vorhandene Backups bleiben am bisherigen Ort.

## Umfang und Hinweise

- Spiel und Dedicated Server vor Backup, Wiederherstellung oder Modwechsel beenden und während des Vorgangs geschlossen lassen. Die Anwendung prüft die üblichen Prozessnamen.
- Ein Backup umfasst ausschließlich den gewählten Spielstandordner. Externe generierte Weltdaten, Spielinstallation und Mods sind nicht enthalten.
- Verwaltet werden Mods im gewählten Spielordner. Mods unter `%APPDATA%\7DaysToDie\Mods` bleiben unverändert.
- Wiederherstellen ersetzt den gesamten Spielstandordner, einschließlich aller enthaltenen Welten und Sitzungen.
- Verknüpfte Ordner/Dateien werden bei Backup- und Modoperationen aus Sicherheitsgründen nicht unterstützt.
- Die Anwendung prüft keine Kompatibilität von Mods untereinander oder mit der Spielversion.

## Selbst bauen

Voraussetzungen: Windows und das **.NET 10 SDK**.

```powershell
git clone https://github.com/LSC-Labs/SevenDaysModHandler.git
cd SevenDaysModHandler
.\build.cmd
.\Start.cmd
```

`build.cmd` erstellt die Release-Ausgabe unter `artifacts\app`. Diese lokale Ausgabe benötigt die .NET 10 Desktop Runtime. Die Anwendung vor einem erneuten Build schließen.

Eigenständigen Windows-x64-Download mit enthaltener Laufzeit bauen:

```powershell
dotnet publish SevenDaysModManager -c Release -r win-x64 --self-contained true -p:Version=1.1.0.0 -o artifacts/release/win-x64
```

## Tests und Projektstruktur

```powershell
dotnet run --project Tests -c Release
```

Die Integrationstests verwenden eigene temporäre Dateien und prüfen Backups, Wiederherstellung, Sicherheitsbackup, Pfadprüfungen, Modmetadaten, Modwechsel, Gruppenwechsel und Kollisionsschutz. Die interaktive GUI-Abnahme erfolgt separat.

| Datei/Ordner | Inhalt |
| --- | --- |
| `SevenDaysModManager/Program.cs` | Hauptfenster und Modliste |
| `SevenDaysModManager/Groups.cs` | Oberfläche für Modgruppen |
| `SevenDaysModManager/Services.cs` | Einstellungen, Steam-Suche und Dateioperationen |
| `Tests/` | Integrationstests ohne externe Testpakete |
| `build.cmd`, `Start.cmd` | Lokales Bauen und Starten |

## Spielbezug

Dieses Projekt ist ein unabhängiges Hilfsprogramm für 7 Days to Die. Das Anwendungssymbol wurde aus der lokal installierten `7DaysToDie.exe` übernommen; Spielname und Symbol gehören den jeweiligen Rechteinhabern (The Fun Pimps).

Das unterstützte ModInfo-Format ist im [Entwicklerforum von The Fun Pimps](https://community.thefunpimps.com/threads/what-is-the-new-layout-for-modinfo-xml-v2.32514/) beschrieben.
