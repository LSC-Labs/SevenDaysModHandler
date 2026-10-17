# Projektkontext für weitere Bearbeitung

- Nutzerwunsch: deutsche Windows-GUI für 7 Days to Die; vollständige Spielstandbackups, Wiederherstellung, Mods durch Verschieben aktivieren/deaktivieren, persistente Pfade und initiale Steam-Suche über Laufwerke.
- Technik: C#, .NET 10, Windows Forms, keine externen Pakete. Hauptprojekt SevenDaysModManager; Services.cs enthält Dateioperationen, Einstellungen, Metadaten und Steam-Suche; Program.cs enthält GUI.
- Persistenz: %LOCALAPPDATA%\7Days-ModManager\settings.json; GamePath, SavesPath, BackupPath. Standard-Saves: %APPDATA%\7DaysToDie\Saves. Backups standardmäßig unter %LOCALAPPDATA%\7Days-ModManager\Backups.
- Mods ausschließlich im gewählten Spielordner: Mods (aktiv) und Mods-Store (inaktiv). Metadaten aus ModInfo.xml, alte und neue Struktur. HTTP/HTTPS im Standardbrowser. Niemals Zielmod überschreiben.
- Backupformat: UTC-Ordner yyyy-MM-ddTHH-mm-ss.fffffffZ mit Saves/ und backup.json. .partial-Verzeichnisse bleiben unsichtbar. Wiederherstellung erstellt vorher Sicherheitsbackup und tauscht vorbereiteten Ordner mit Rollback bei fehlgeschlagenem Umbenennen aus.
- Dateioperationen bei laufendem Spiel/Server blockieren, keine überlappenden Save-/Backupordner, keine Reparse Points kopieren. Keine echten Spielstände für Tests verwenden.
- Tests: dotnet run --project Tests -c Release. Build: dotnet build SevenDaysModManager -c Release. Veröffentlichung: dotnet publish SevenDaysModManager -c Release -o artifacts/app --no-self-contained. Start.cmd verwendet diese Ausgabe.
- Die GUI wurde kompiliert; visuelle/interaktive GUI-Abnahme steht noch aus.

Validierung: Release-Build und Publish erfolgreich (0 Warnungen/Fehler); alle 12 Integrationstests erfolgreich. Tests mussten wegen verweigerter temporärer Ordnerumbenennung außerhalb der Sandbox ausgeführt werden.


Korrektur Backupzugriff: Standard auf LocalApplicationData/7Days-ModManager/Backups umgestellt. EnsureBackupAccess prüft Erstellen, Schreiben, Umbenennen, Löschen und Auflisten. PrepareBackupLocation ersetzt nur unzugängliche alte Documents-Standardpfade und speichert die Änderung mit GUI-Hinweis; keine vorhandenen Backups verschieben. Benutzerdefinierte gesperrte Pfade erhalten verständliche Fehlermeldung. Release-Publish und alle 17 Integrationstests erfolgreich. Automatische Migration des tatsächlichen gesperrten Documents-Ordners wurde nicht interaktiv geprüft.

Build-Einstieg für den Nutzer: build.cmd im Projektstamm führt dotnet publish im Release-Modus nach artifacts/app aus, prüft dotnet-Verfügbarkeit und gibt den Build-Fehlercode weiter.


Lokaler Datenordner auf Nutzerwunsch: %LOCALAPPDATA%\7Days-ModManager. Einstellungen: settings.json; Standard-Backups: Backups/. Projektname und Assembly bleiben SevenDaysModManager. Keine automatische Übernahme des bisherigen Datenordners.

Erweiterung Modgruppen:
- GUI in Groups.cs (MainForm partial), zusätzliche Seite Modgruppen mit Erstellen, Umbenennen/Bearbeiten, Löschen und Mitgliedschaft über Checkboxen. Speichern ist explizit; Umschalten verwendet die gespeicherte Gruppe. Gruppen gelten pro GamePath; Identifikation der Mitglieder über Ordnernamen, nicht Anzeigenamen.
- Settings.Groups: List<ModGroup>, jeweils Name, GamePath, Folders. Speicherung weiterhin %LOCALAPPDATA%\7Days-ModManager\settings.json. Ein Mod darf in mehreren Gruppen vorkommen.
- Manager.SetGroup: Aktivieren setzt exakt die Gruppenmitglieder aktiv und alle übrigen inaktiv. Deaktivieren betrifft nur Mitglieder. Vorprüfung auf fehlende Mitglieder (Aktivieren), Zielkollisionen und Reparse Points; Rückverschieben bereits bewegter Mods bei Laufzeitfehler, ausdrücklicher Hinweis bei fehlgeschlagenem Rollback. Spielprozesssperre gilt weiterhin.
- Aktive Statuszellen der Modliste hellgrün, auch bei Zeilenauswahl; Inaktiv neutral.
- Release-Publish erfolgreich; alle 30 Integrationstests erfolgreich, darunter 13 Prüfungen zu Gruppen/Persistenz/Wechsel/Fehlerfällen. Interaktive visuelle GUI-Prüfung weiterhin ausstehend.

Modlisten-Darstellung: Auswahlfarben entsprechen den normalen Zellfarben; kein Fokusrahmen in Datenzellen. Aktiver Status behält dasselbe helle Grün auch bei Auswahl. Aktionszellen zeichnen native Windows-Schaltflächen mit Innenabstand, Rand und Hover-/Druckzustand; Mindestzeilenhöhe 38 und Aktionsspalte 135 Pixel. Release-Build/Publish erfolgreich; rein visuelle Änderung ohne erneute Dateioperations-Tests.

Sortierung Modliste: alle Spalten Programmatic; ColumnHeaderMouseClick wechselt Richtung und BindSortedMods sortiert die gebundene Liste inklusive Status/Aktion. Sortierspalte und Richtung bleiben während der Sitzung bei Refresh erhalten, SortGlyphDirection zeigt Richtung. Versionen numerisch, nicht parsebare Angaben als separate Textgruppe. Eigene ButtonRenderer-Zeichnung entfernt; native DataGridViewButtonCell mit Padding verwaltet Hover/Pressed/Leave. Normale Buttons explizit Standard/UseVisualStyleBackColor. Auswahlfarben und Grün unverändert. Release-Build erfolgreich; interaktive Hover-Abnahme nicht durchgeführt.

Anwendungssymbol: SevenDaysModManager/game.ico aus der lokalen D:/SteamLibrary/steamapps/common/7 Days To Die/7DaysToDie.exe extrahiert. ApplicationIcon im Projekt bindet es in die EXE ein; MainForm übernimmt es via ExtractAssociatedIcon. Build benötigt keine installierte Spielkopie. Status und Aktion: Zelltexte und Spaltenüberschriften MiddleCenter, Statusfarben und Button-Padding bleiben erhalten.

Versionsverwaltung: lokales Git-Repository im Projektstamm, Hauptbranch main. Quellcode, Tests, game.ico, Skripte und Dokumentation versionieren; bin/obj, artifacts, .dotnet und lokale IDE-Einstellungen bleiben ausgeschlossen.
