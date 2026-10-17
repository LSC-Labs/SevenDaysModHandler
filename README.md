# 7 Days to Die – Mod Handler

Windows-Anwendung mit deutscher Windows-Forms-Oberfläche auf .NET 10 (modernes .NET, Nachfolger von .NET Core), ohne zusätzliche NuGet-Pakete.

## Starten

`Start.cmd` startet die fertige Anwendung aus `artifacts/app`. Voraussetzung: .NET 10 Desktop Runtime für Windows x64. Alternativ mit installiertem SDK:

```
dotnet run --project SevenDaysModManager
```

## Bedienung

- Unter Einstellungen Spielordner, Spielstandordner und Backupordner auswählen und speichern. Beim ersten Start wird Steam automatisch durchsucht. Mehrere Installationen können ausgewählt werden.
- Standard-Spielstände: `%APPDATA%\7DaysToDie\Saves`. Bei abweichenden Server-/Benutzerprofilen diesen Ordner anpassen.
- Standard-Backups: `%LOCALAPPDATA%\7Days-ModManager\Backups`. Der Zugriff wird durch Schreiben und Umbenennen eines temporären Testordners geprüft. Ein gesperrter alter Documents-Standardpfad wird beim Start automatisch auf den neuen Standard umgestellt und gespeichert; vorhandene Backups bleiben am alten Ort. Benutzerdefinierte Pfade werden nicht automatisch geändert.
- Backup erstellen kopiert den gesamten Spielstandordner, einschließlich aller Welten und Sitzungen. Datum und Uhrzeit stehen in UTC im ISO-8601-abgeleiteten, Windows-kompatiblen Format `2026-10-17T12-30-00.1234567Z`. Doppelpunkte sind in Windows-Ordnernamen unzulässig, deshalb werden Bindestriche verwendet.
- Nur vollständig erstellte Backups erscheinen in der Liste. Wiederherstellen ersetzt den gesamten konfigurierten Spielstandordner, nach einer Rückfrage und einem automatischen Sicherheitsbackup. Kopieren erfolgt zunächst in einen temporären Ordner; erst danach wird der Spielstand ausgetauscht.
- Entpackte Mods unter `<Spiel>\Mods-Store\<Modname>\ModInfo.xml` ablegen. Die Liste zeigt aktive und inaktive Mods mit Name, Version, Beschreibung und anklickbarer Webseite. Der Aktionsbutton verschiebt den vollständigen Ordner nach `Mods` bzw. zurück nach `Mods-Store`.
- Vorhandene Zielordner werden niemals überschrieben. Fehlende oder fehlerhafte ModInfo-Dateien werden in der Liste kenntlich gemacht. Alte und neue ModInfo-Strukturen werden gelesen. Nur HTTP-/HTTPS-Links werden im Standardbrowser geöffnet.
- Spiel und Dedicated Server vor Backup, Wiederherstellung oder Modwechsel schließen. Die Anwendung prüft die üblichen Prozessnamen; das Spiel während der Vorgänge nicht starten.

Gesichert werden die Inhalte des gewählten Saves-Ordners. Externe generierte Weltdaten, Spielinstallation und Mods sind nicht Teil dieser Sicherung. Benutzerprofil-Mods unter `%APPDATA%\7DaysToDie\Mods` werden nicht verwaltet. Ordnerverknüpfungen werden bei Dateioperationen abgewiesen.

## Gespeicherte Einstellungen

`%LOCALAPPDATA%\7Days-ModManager\settings.json` enthält GamePath, SavesPath und BackupPath. Änderungen mit „Einstellungen speichern“ bestätigen. Ein automatisch gefundenes Spiel wird ebenfalls gespeichert. Defekte Einstellungsdateien erzeugen einen Hinweis und können durch erneutes Speichern ersetzt werden.

## Entwicklung und Prüfung

```
dotnet build SevenDaysModManager -c Release
dotnet run --project Tests -c Release
dotnet publish SevenDaysModManager -c Release -o artifacts/app --no-self-contained
```

Die Integrationstests arbeiten ausschließlich mit temporären Dateien. Sie prüfen Backup, vollständige Wiederherstellung, Sicherheitsbackup, Pfadüberlappung, Backupauswahl, ModInfo-Versionen, Modwechsel und Kollisionsschutz.

Steam-Suche: Registry SteamPath, übliche Steam-/SteamLibrary-Verzeichnisse auf allen festen verfügbaren Laufwerken, Bibliotheken direkt im Laufwerksstamm sowie in libraryfolders.vdf eingetragene Bibliotheken. Sonderinstallationen können manuell ausgewählt werden.

Metadatenformat: https://community.thefunpimps.com/threads/what-is-the-new-layout-for-modinfo-xml-v2.32514/


Zum erneuten Bauen im Projektordner `build.cmd` ausführen. Das Skript benötigt das .NET 10 SDK und aktualisiert die Release-Ausgabe in `artifacts\app`, die von `Start.cmd` gestartet wird. Die Anwendung vorher schließen. Bei Fehlern liefert das Skript einen Fehlercode zurück.


## Modgruppen

Auf der Seite „Modgruppen“ mit „Neue Gruppe“ beginnen, einen Namen eingeben, Mods anhaken und „Gruppe speichern“ wählen. Eine gespeicherte Gruppe kann zum Bearbeiten ausgewählt, umbenannt oder gelöscht werden. Löschen ändert keine Moddateien. Ein Mod darf mehreren Gruppen angehören.

„Gruppe aktivieren“ aktiviert genau die gespeicherten Mitglieder und deaktiviert alle anderen Mods im Spielordner. „Gruppe deaktivieren“ deaktiviert nur deren Mitglieder und lässt andere Mods unverändert. Änderungen an Name und Auswahl vor dem Umschalten speichern. Eine leere Gruppe deaktiviert nach Rückfrage alle Mods.

Gruppen sind an die jeweilige Spielinstallation gebunden und werden mit ihren Modordnernamen in `%LOCALAPPDATA%\7Days-ModManager\settings.json` gespeichert. Fehlende Gruppenmods bleiben im Editor als „fehlt“ sichtbar. Aktivieren wird bei fehlenden Mitgliedern oder Zielkollisionen vor dem ersten Verschieben abgebrochen. Bei Fehlern während des Wechsels werden bereits verschobene Mods zurückgesetzt; schlägt auch das fehl, erscheint ein ausdrücklicher Hinweis.

In der Modliste wird die Statuszelle „Aktiv“ hellgrün hinterlegt, auch bei ausgewählter Zeile.

Die Modliste hebt ausgewählte Zeilen nicht farblich hervor. Aktivieren/Deaktivieren wird als umrandete Schaltfläche angezeigt; der aktive Status bleibt hellgrün.

Die Modliste lässt sich über jeden Spaltenkopf auf- und absteigend sortieren. Ein Pfeil zeigt die Richtung; die Sortierung bleibt beim Aktualisieren und Modwechsel während der Sitzung erhalten. Numerische Versionsangaben werden nach Versionsnummer sortiert. Die Aktionsbuttons verwenden die native Windows-Darstellung für Hover- und Druckzustände.

Die Anwendung verwendet das Symbol der lokal installierten 7DaysToDie.exe (The Fun Pimps); es ist als game.ico im Projekt enthalten. Status und Aktion sind in der Modliste zentriert.
