using SevenDaysModManager;
var root = Path.Combine(Path.GetTempPath(), "SevenDaysTests-" + Guid.NewGuid().ToString("N"));
void Check(bool condition, string message) { if (!condition) throw new Exception(message); Console.WriteLine("OK: " + message); }
void Reject(Action action, string message) { try { action(); } catch (Exception e) when (e is IOException or InvalidDataException) { Console.WriteLine("OK: " + message); return; } throw new Exception(message); }
try
{
    L.ValidateCatalog();
    Check(Updates.ParseVersion("v1.1.0") == Updates.ParseVersion("1.1.0.0"), "Releaseversionen mit drei und vier Stellen sind gleichwertig");
    Check(Updates.ParseVersion("v1.10.0") > Updates.ParseVersion("v1.9.0"), "Releaseversionen werden numerisch verglichen");
    Reject(() => Updates.ParseVersion("v1.2.0-beta"), "Unbekannte Releaseformate werden abgewiesen");
    var releaseJson = """{"tag_name":"v2.0.0","draft":false,"prerelease":false,"html_url":"https://github.com/LSC-Labs/SevenDaysModHandler/releases/tag/v2.0.0"}""";
    var release = Updates.ParseRelease(releaseJson);
    Check(release.Version == new Version(2, 0, 0, 0) && release.DownloadUrl.EndsWith("/v2.0.0"), "Releaseversion und Downloadseite werden gelesen");
    Reject(() => Updates.ParseRelease(releaseJson.Replace("\"draft\":false", "\"draft\":true")), "Releaseentwürfe werden abgewiesen");
    Reject(() => Updates.ParseRelease(releaseJson.Replace("\"prerelease\":false", "\"prerelease\":true")), "Vorabversionen werden abgewiesen");
    Reject(() => Updates.ParseRelease(releaseJson.Replace("https://github.com/", "https://example.com/")), "Fremde Downloadseiten werden abgewiesen");
    Check(System.Text.Json.JsonSerializer.Deserialize<Settings>("{}")!.Theme == "light", "Bestehende Einstellungen verwenden den hellen Modus");
    foreach (var mode in new[] { "light", "dark" })
        Check(System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(new Settings { Theme = mode }))!.Theme == mode, "Darstellung wird gespeichert: " + mode);
    Check(new Settings().Language == "auto", "Neue Einstellungen verwenden die Systemsprache");
    foreach (var code in L.Languages)
    {
        Check(L.Resolve("auto", System.Globalization.CultureInfo.GetCultureInfo(code + (code == "en" ? "-US" : code == "de" ? "-DE" : code == "es" ? "-MX" : code == "it" ? "-IT" : "-CA"))) == code, "Regionale Systemsprache: " + code);
        L.Select(code);
        Check(L.T("Einstellungen") != "" && L.T("Gruppe {0} {1}", "Test", L.T("aktiviert.")).Contains("Test"), "Übersetzung und Platzhalter: " + code);
        Check(System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(new Settings { Language = code }))!.Language == code, "Sprachwahl serialisiert: " + code);
    }
    Check(L.Resolve("auto", System.Globalization.CultureInfo.GetCultureInfo("ja-JP")) == "en" && L.Resolve("invalid") == "en", "Unbekannte Sprachen verwenden Englisch");
    Check(L.Resolve("fr", System.Globalization.CultureInfo.GetCultureInfo("de-DE")) == "fr", "Manuelle Auswahl hat Vorrang");
    L.Select("en");
    Check(L.T("Einstellungen") == "Settings", "Englische Standardübersetzung");
    L.Select("de");
    var s = new Settings { SavesPath = Path.Combine(root, "Saves"), BackupPath = Path.Combine(root, "Backups"), GamePath = Path.Combine(root, "Game") };
    Check(new Settings().BackupPath == Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "7Days-ModManager", "Backups"), "Backupstandard liegt im lokalen Anwendungsordner");
    Manager.EnsureBackupAccess(s);
    Check(Directory.GetFileSystemEntries(s.BackupPath).Length == 0, "Zugriffstest prüft Schreiben und Umbenennen ohne Rückstände");
    var blocked = Path.Combine(root, "blocked"); File.WriteAllText(blocked, "not a directory");
    var invalid = new Settings { SavesPath = s.SavesPath, BackupPath = blocked };
    Reject(() => Manager.EnsureBackupAccess(invalid), "Unbeschreibbarer Backupordner wird vor dem Backup erkannt");
    Reject(() => Manager.PrepareBackupLocation(invalid), "Benutzerdefinierter gesperrter Pfad wird nicht automatisch ersetzt");
    Check(invalid.BackupPath == blocked, "Benutzerdefinierter Backupordner bleibt erhalten");
    Directory.CreateDirectory(Path.Combine(s.SavesPath, "World", "Session"));
    var save = Path.Combine(s.SavesPath, "World", "Session", "main.ttw");
    File.WriteAllText(save, "original");
    var first = Manager.Backup(s, "  Vor dem Modwechsel / Test  ");
    Check(Manager.BackupDescription(first) == "Vor dem Modwechsel / Test", "Backupbeschreibung wird gespeichert und getrimmt");
    var originalMetadata = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.Combine(first, "backup.json")))!;
    Manager.SetBackupDescription(s, first, "Neuer Name: äöü");
    var updatedMetadata = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.Combine(first, "backup.json")))!;
    Check(Manager.BackupDescription(first) == "Neuer Name: äöü" && originalMetadata["CreatedUtc"]!.ToJsonString() == updatedMetadata["CreatedUtc"]!.ToJsonString() && originalMetadata["Source"]!.ToJsonString() == updatedMetadata["Source"]!.ToJsonString(), "Nachträgliche Beschreibung erhält Datum und Quelle");
    Manager.SetBackupDescription(s, first, " ");
    Check(Manager.BackupDescription(first) == "", "Beschreibung lässt sich entfernen");
    updatedMetadata.AsObject().Remove("Description");
    File.WriteAllText(Path.Combine(first, "backup.json"), updatedMetadata.ToJsonString());
    Check(Manager.BackupDescription(first) == "", "Alte Backups ohne Beschreibung bleiben lesbar");
    Manager.SetBackupDescription(s, first, "Altes Backup benannt");
    Check(Manager.BackupDescription(first) == "Altes Backup benannt", "Alte Backups lassen sich nachträglich benennen");
    Reject(() => Manager.SetBackupDescription(s, root, "Fremd"), "Fremde Ordner lassen sich nicht umbenennen");
    Check(File.ReadAllText(Path.Combine(first, "Saves", "World", "Session", "main.ttw")) == "original", "Vollständiges verschachteltes Backup");
    File.WriteAllText(save, "changed"); File.WriteAllText(Path.Combine(s.SavesPath, "obsolete"), "extra");
    Manager.Restore(s, first);
    Check(File.ReadAllText(save) == "original" && !File.Exists(Path.Combine(s.SavesPath, "obsolete")), "Wiederherstellung ersetzt aktuellen Stand vollständig");
    Check(Manager.Backups(s).Any(p => File.ReadAllText(Path.Combine(p, "Saves", "World", "Session", "main.ttw")) == "changed"), "Sicherheitsbackup vor Wiederherstellung");
    Directory.CreateDirectory(Path.Combine(s.BackupPath, "broken.partial"));
    Check(Manager.Backups(s).Length == 2, "Unvollständige Backups werden ignoriert");
    Reject(() => Manager.Restore(s, root), "Fremde Ordner werden nicht wiederhergestellt");
    Reject(() => Manager.DeleteBackup(s, root), "Fremde Ordner werden nicht gelöscht");
    Reject(() => Manager.DeleteBackup(s, Path.Combine(s.BackupPath, "broken.partial")), "Unvollständige Backups werden nicht gelöscht");
    var disposable = Manager.Backup(s, "Zum Löschen");
    Manager.DeleteBackup(s, disposable);
    Check(!Directory.Exists(disposable) && Manager.Backups(s).Length == 2, "Nur die ausgewählte Sicherung wird vollständig gelöscht");
    Check(File.ReadAllText(save) == "original" && Directory.Exists(first), "Löschen erhält aktuelle Spielstände und andere Sicherungen");
    Reject(() => Manager.DeleteBackup(s, disposable), "Bereits gelöschte Sicherung wird abgewiesen");
    Reject(() => Manager.Backup(new Settings { SavesPath = s.SavesPath, BackupPath = Path.Combine(s.SavesPath, "Backups") }), "Überlappende Pfade werden abgewiesen");
    Directory.CreateDirectory(s.GamePath); File.WriteAllText(Path.Combine(s.GamePath, "7DaysToDie.exe"), "test");
    Reject(() => Manager.GameStartInfo("", false), "Spielstart ohne Spielordner wird abgelehnt");
    Reject(() => Manager.GameStartInfo(s.GamePath, true), "Fehlendes EAC-Startprogramm wird erkannt");
    File.WriteAllText(Path.Combine(s.GamePath, "7DaysToDie_EAC.exe"), "test");
    foreach (var withEac in new[] { false, true })
    {
        var start = Manager.GameStartInfo(s.GamePath, withEac);
        Check(start.FileName == Path.Combine(s.GamePath, withEac ? "7DaysToDie_EAC.exe" : "7DaysToDie.exe") && start.WorkingDirectory == s.GamePath && start.UseShellExecute && start.Arguments == "", "Spielstart verwendet passendes Programm und Arbeitsverzeichnis: EAC=" + withEac);
    }
    var mod = Path.Combine(s.GamePath, "Mods-Store", "Example"); Directory.CreateDirectory(mod);
    File.WriteAllText(Path.Combine(mod, "ModInfo.xml"), "<xml><Name value='Example'/><DisplayName value='Beispiel'/><Version value='1.2'/><Description value='Beschreibung'/><Website value='https://example.com'/></xml>");
    var m = Manager.Mods(s.GamePath).Single();
    Check(m.Name == "Beispiel" && m.Version == "1.2" && m.Website == "https://example.com" && !m.Active, "ModInfo V2 lesen");
    foreach (var code in L.Languages)
    {
        L.Select(code);
        var localizedMod = Manager.Mods(s.GamePath).Single();
        Check(localizedMod.Name == "Beispiel" && localizedMod.Version == "1.2", "Metadaten bleiben sprachunabhängig: " + code);
        Manager.Toggle(s.GamePath, localizedMod);
        Check(Directory.Exists(Path.Combine(s.GamePath, "Mods", "Example")), "Aktiver Modpfad bleibt sprachunabhängig: " + code);
        Manager.Toggle(s.GamePath, Manager.Mods(s.GamePath).Single());
    }
    L.Select("de");
    Manager.Toggle(s.GamePath, m); m = Manager.Mods(s.GamePath).Single(); Check(m.Active, "Mod aktivieren");
    Directory.CreateDirectory(mod); Reject(() => Manager.Toggle(s.GamePath, m), "Namenskollision überschreibt nichts"); Directory.Delete(mod);
    Manager.Toggle(s.GamePath, m); Check(!Manager.Mods(s.GamePath).Single().Active, "Mod deaktivieren");
    File.WriteAllText(Path.Combine(mod, "ModInfo.xml"), "<xml><ModInfo><Name value='Legacy'/><Version value='0.9'/></ModInfo></xml>");
    Check(Manager.Mods(s.GamePath).Single().Name == "Legacy", "Legacy-Metadaten lesen");
    File.WriteAllText(Path.Combine(mod, "ModInfo.xml"), "broken");
    Check(Manager.Mods(s.GamePath).Single().Description.Contains("nicht gelesen"), "Defekte Metadaten blockieren die Liste nicht");
    var second = Path.Combine(s.GamePath, "Mods-Store", "Second"); Directory.CreateDirectory(second);
    File.WriteAllText(Path.Combine(second, "payload"), "second-content");
    var groupA = new ModGroup { Name = "Gruppe A", GamePath = s.GamePath, Folders = ["Example"] };
    var groupB = new ModGroup { Name = "Gruppe B", GamePath = s.GamePath, Folders = ["Second"] };
    var persisted = System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(new Settings { Groups = [groupA, groupB] }))!;
    Check(persisted.Groups.Count == 2 && persisted.Groups[0].Folders.Single() == "Example", "Gruppennamen und Mitgliedschaften werden serialisiert");
    Manager.SetGroup(s.GamePath, groupA, true);
    Check(Manager.Mods(s.GamePath).Single(m => m.Active).Folder.EndsWith("Example"), "Gruppe A aktiviert nur ihre Mitglieder");
    Manager.SetGroup(s.GamePath, groupB, true);
    Check(Manager.Mods(s.GamePath).Single(m => m.Active).Folder.EndsWith("Second") && Directory.Exists(mod), "Wechsel zu B deaktiviert Mods außerhalb der Gruppe");
    Manager.SetGroup(s.GamePath, groupB, true);
    Check(Manager.Mods(s.GamePath).Count(m => m.Active) == 1, "Erneutes Aktivieren einer Gruppe ist möglich");
    Manager.SetGroup(s.GamePath, groupA, false);
    Check(Manager.Mods(s.GamePath).Single(m => m.Active).Folder.EndsWith("Second"), "Deaktivierung lässt fremde aktive Mods unverändert");
    Manager.SetGroup(s.GamePath, groupB, false);
    Check(Manager.Mods(s.GamePath).All(m => !m.Active), "Gruppe deaktivieren");
    Manager.SetGroup(s.GamePath, groupA, true);
    Reject(() => Manager.SetGroup(s.GamePath, new ModGroup { Name = "Fehlend", GamePath = s.GamePath, Folders = ["Missing"] }, true), "Fehlendes Gruppenmitglied verhindert Wechsel");
    Check(Manager.Mods(s.GamePath).Single(m => m.Active).Folder.EndsWith("Example"), "Fehlgeschlagene Vorprüfung lässt aktiven Stand unverändert");
    Directory.CreateDirectory(Path.Combine(s.GamePath, "Mods", "Second"));
    Reject(() => Manager.SetGroup(s.GamePath, groupB, true), "Gruppenwechsel prüft Kollisionen vor dem ersten Verschieben");
    Check(Directory.Exists(Path.Combine(s.GamePath, "Mods", "Example")), "Kollision deaktiviert keine bisherigen Mods");
    Directory.Delete(Path.Combine(s.GamePath, "Mods", "Second"));
    Manager.SetGroup(s.GamePath, new ModGroup { Name = "Leer", GamePath = s.GamePath }, true);
    Check(Manager.Mods(s.GamePath).All(m => !m.Active), "Leere Gruppe deaktiviert alle Mods");
    Check(File.ReadAllText(Path.Combine(second, "payload")) == "second-content", "Gruppenwechsel erhält Moddateien");
    Reject(() => Manager.SetGroup(s.GamePath, new ModGroup { GamePath = root }, true), "Gruppe einer anderen Installation wird abgewiesen");
    Console.WriteLine("Alle Integrationstests erfolgreich.");
}
finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
