using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Win32;

namespace SevenDaysModManager;

public sealed class Settings
{
    public string GamePath { get; set; } = "";
    public string Language { get; set; } = "auto";
    public string SavesPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "7DaysToDie", "Saves");
    public static string DefaultBackupPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "7Days-ModManager", "Backups");
    public string BackupPath { get; set; } = DefaultBackupPath;
    public List<ModGroup> Groups { get; set; } = [];
    public static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "7Days-ModManager", "settings.json");
    public static Settings Load() => File.Exists(FilePath) ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new() : new();
    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, FilePath, true);
    }
}

public sealed record Mod(string Name, string Version, string Description, string Website, bool Active, string Folder);
public sealed class ModGroup
{
    public string Name { get; set; } = "";
    public string GamePath { get; set; } = "";
    public List<string> Folders { get; set; } = [];
    public override string ToString() => Name;
}
public static class Manager
{
    public static ProcessStartInfo GameStartInfo(string gamePath, bool withEac)
    {
        if (string.IsNullOrWhiteSpace(gamePath) || !File.Exists(Path.Combine(gamePath, "7DaysToDie.exe")))
            throw new IOException(L.T("Bitte einen gültigen Spielordner auswählen (7DaysToDie.exe)."));
        var directory = Path.GetFullPath(gamePath);
        var executable = Path.Combine(directory, withEac ? "7DaysToDie_EAC.exe" : "7DaysToDie.exe");
        if (!File.Exists(executable)) throw new IOException(L.T("Startprogramm fehlt: {0}", executable));
        return new ProcessStartInfo(executable) { WorkingDirectory = directory, UseShellExecute = true };
    }
    public static void StartGame(string gamePath, bool withEac)
    {
        var start = GameStartInfo(gamePath, withEac);
        EnsureStopped();
        using var process = Process.Start(start);
        if (process == null) throw new IOException(L.T("Das Spiel konnte nicht gestartet werden."));
    }
    public static void SetGroup(string game, ModGroup group, bool activate)
    {
        EnsureStopped();
        if (!string.Equals(Path.GetFullPath(game), Path.GetFullPath(group.GamePath), StringComparison.OrdinalIgnoreCase))
            throw new IOException(L.T("Die Gruppe gehört zu einer anderen Spielinstallation."));
        if (!File.Exists(Path.Combine(game, "7DaysToDie.exe"))) throw new IOException(L.T("Ungültiger Spielordner."));
        var all = Mods(game);
        var wanted = group.Folders.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (activate)
        {
            var missing = wanted.Except(all.Select(m => Path.GetFileName(m.Folder)), StringComparer.OrdinalIgnoreCase).ToArray();
            if (missing.Length > 0) throw new IOException(L.T("Gruppenmods fehlen: ") + string.Join(", ", missing) + L.T(". Es wurde nichts geändert."));
        }
        var changes = all.Where(m => activate ? m.Active != wanted.Contains(Path.GetFileName(m.Folder)) : m.Active && wanted.Contains(Path.GetFileName(m.Folder))).ToList();
        // Check every move before touching the first mod.
        foreach (var mod in changes)
        {
            var targetRoot = Path.Combine(game, mod.Active ? "Mods-Store" : "Mods");
            CheckLink(mod.Folder); CheckLink(targetRoot);
            var target = Path.Combine(targetRoot, Path.GetFileName(mod.Folder));
            if (Directory.Exists(target) || File.Exists(target)) throw new IOException(L.T("Ziel bereits vorhanden: ") + target + L.T(". Es wurde nichts geändert."));
        }
        var moved = new Stack<Mod>();
        try
        {
            foreach (var mod in changes) { Toggle(game, mod); moved.Push(mod); }
        }
        catch (Exception original)
        {
            var errors = new List<Exception> { original };
            foreach (var mod in moved)
            {
                var current = Path.Combine(game, mod.Active ? "Mods-Store" : "Mods", Path.GetFileName(mod.Folder));
                try { Directory.Move(current, mod.Folder); }
                catch (Exception rollback) { errors.Add(rollback); }
            }
            if (errors.Count > 1) throw new AggregateException(L.T("Gruppenwechsel fehlgeschlagen; einige Mods konnten nicht zurückverschoben werden. Bitte Modstatus prüfen."), errors);
            throw new IOException(L.T("Gruppenwechsel fehlgeschlagen. Bereits verschobene Mods wurden zurückgesetzt. ") + original.Message, original);
        }
    }
    public static void EnsureBackupAccess(Settings s)
    {
        Validate(s);
        string? probe = null;
        try
        {
            CheckLink(s.BackupPath);
            Directory.CreateDirectory(s.BackupPath);
            // Verify the operations used to finalize a backup, not just file creation.
            probe = Path.Combine(s.BackupPath, ".access-check-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(probe);
            File.WriteAllText(Path.Combine(probe, "test"), "test");
            Directory.Move(probe, probe + ".renamed");
            probe += ".renamed";
            Directory.Delete(probe, true);
            probe = null;
            _ = Directory.GetDirectories(s.BackupPath);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new IOException(L.T("Der Backupordner ist nicht les- und beschreibbar:\n{0}\n\nBitte unter Einstellungen einen zugänglichen Backupordner auswählen, beispielsweise:\n{1}\n\nDetails: {2}", s.BackupPath, Settings.DefaultBackupPath, e.Message), e);
        }
        finally
        {
            if (probe != null)
                try { if (Directory.Exists(probe)) Directory.Delete(probe, true); }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
    }

    public static string? PrepareBackupLocation(Settings s)
    {
        try { EnsureBackupAccess(s); return null; }
        catch (IOException)
        {
            var legacy = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "7DaysToDie-Backups");
            var legacyProfile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", "7DaysToDie-Backups");
            bool Same(string p) => string.Equals(Path.GetFullPath(p).TrimEnd('\\', '/'), Path.GetFullPath(s.BackupPath).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
            if (!Same(legacy) && !Same(legacyProfile)) throw;
            var previous = s.BackupPath;
            s.BackupPath = Settings.DefaultBackupPath;
            try { EnsureBackupAccess(s); s.Save(); }
            catch { s.BackupPath = previous; throw; }
            return L.T("Der bisherige Standard-Backupordner ist nicht zugänglich:\n{0}\n\nNeue Backups werden hier gespeichert:\n{1}\n\nVorhandene Backups bleiben im bisherigen Ordner. Sie wurden nicht verschoben. Der Backupordner kann unter Einstellungen geändert werden.", previous, s.BackupPath);
        }
    }
    public static bool ContainsPath(string parent, string child) => Path.GetFullPath(child).TrimEnd(Path.DirectorySeparatorChar).Equals(Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) || Path.GetFullPath(child).StartsWith(Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    public static void Validate(Settings s)
    {
        if (string.IsNullOrWhiteSpace(s.SavesPath) || string.IsNullOrWhiteSpace(s.BackupPath)) throw new IOException(L.T("Spielstand- und Backupordner müssen angegeben sein."));
        if (ContainsPath(s.SavesPath, s.BackupPath) || ContainsPath(s.BackupPath, s.SavesPath)) throw new IOException(L.T("Spielstand- und Backupordner dürfen sich nicht überlappen."));
        if (Path.GetPathRoot(Path.GetFullPath(s.SavesPath)) == Path.GetFullPath(s.SavesPath)) throw new IOException(L.T("Ein Laufwerksstamm ist kein Spielstandordner."));
    }
    public static void EnsureStopped()
    {
        foreach (var name in new[] { "7DaysToDie", "7DaysToDieServer", "7DaysToDie_EAC" })
        {
            var processes = Process.GetProcessesByName(name);
            try { if (processes.Length > 0) throw new IOException(L.T("Bitte zuerst 7 Days to Die und den Server beenden.")); }
            finally { foreach (var p in processes) p.Dispose(); }
        }
    }
    static void CheckLink(string path)
    {
        for (var d = new DirectoryInfo(Path.GetFullPath(path)); d != null; d = d.Parent)
            if (d.Exists && (d.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException(L.T("Verknüpfte Ordner werden aus Sicherheitsgründen nicht unterstützt: ") + d.FullName);
    }
    public static void CopyTree(string source, string target)
    {
        CheckLink(source);
        CheckLink(target);
        Directory.CreateDirectory(target);
        foreach (var file in Directory.EnumerateFiles(source))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) throw new IOException(L.T("Verknüpfte Datei: ") + file);
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), false);
        }
        foreach (var dir in Directory.EnumerateDirectories(source)) CopyTree(dir, Path.Combine(target, Path.GetFileName(dir)));
    }
    public static string Backup(Settings s, string description = "")
    {
        Validate(s); EnsureStopped();
        if (!Directory.Exists(s.SavesPath)) throw new DirectoryNotFoundException(L.T("Spielstandordner wurde nicht gefunden."));
        EnsureBackupAccess(s);
        var name = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH-mm-ss.fffffff'Z'");
        var target = Path.Combine(s.BackupPath, name);
        var staging = target + ".partial";
        try
        {
            CopyTree(s.SavesPath, Path.Combine(staging, "Saves"));
            File.WriteAllText(Path.Combine(staging, "backup.json"), JsonSerializer.Serialize(new { Format = 1, CreatedUtc = DateTime.UtcNow, Source = s.SavesPath, Description = description.Trim() }));
            Directory.Move(staging, target);
            return target;
        }
        catch { if (Directory.Exists(staging)) Directory.Delete(staging, true); throw; }
    }
    public static string[] Backups(Settings s) => Directory.Exists(s.BackupPath) ? Directory.GetDirectories(s.BackupPath).Where(p => !p.EndsWith(".partial") && File.Exists(Path.Combine(p, "backup.json")) && Directory.Exists(Path.Combine(p, "Saves"))).OrderDescending().ToArray() : [];
    public static string BackupDescription(string backup)
    {
        using var metadata = JsonDocument.Parse(File.ReadAllText(Path.Combine(backup, "backup.json")));
        return metadata.RootElement.TryGetProperty("Description", out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
    }
    public static void SetBackupDescription(Settings s, string backup, string description)
    {
        if (!Backups(s).Contains(backup, StringComparer.OrdinalIgnoreCase)) throw new IOException(L.T("Kein gültiges Backup ausgewählt."));
        CheckLink(backup);
        var path = Path.Combine(backup, "backup.json");
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException(L.T("Verknüpfte Datei: ") + path);
        var metadata = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        metadata["Description"] = description.Trim();
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, metadata.ToJsonString());
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public static void Restore(Settings s, string backup)
    {
        Validate(s); EnsureStopped();
        if (!Backups(s).Contains(backup, StringComparer.OrdinalIgnoreCase)) throw new IOException(L.T("Kein gültiges Backup ausgewählt."));
        CheckLink(s.SavesPath);
        if (Directory.Exists(s.SavesPath)) Backup(s);
        var target = Path.GetFullPath(s.SavesPath).TrimEnd(Path.DirectorySeparatorChar);
        var staged = target + ".restore-" + Guid.NewGuid().ToString("N");
        var old = target + ".previous-" + Guid.NewGuid().ToString("N");
        try
        {
            CopyTree(Path.Combine(backup, "Saves"), staged);
            EnsureStopped();
            if (Directory.Exists(target)) Directory.Move(target, old);
            try { Directory.Move(staged, target); }
            catch { if (Directory.Exists(old)) Directory.Move(old, target); throw; }
        }
        finally { if (Directory.Exists(staged)) Directory.Delete(staged, true); }
        // The old folder remains recoverable if cleanup fails; the restored saves are already in place.
        if (Directory.Exists(old)) Directory.Delete(old, true);
    }
    public static List<Mod> Mods(string game)
    {
        var result = new List<Mod>();
        if (string.IsNullOrWhiteSpace(game)) return result;
        foreach (var active in new[] { true, false })
        {
            var root = Path.Combine(game, active ? "Mods" : "Mods-Store");
            if (!Directory.Exists(root)) continue;
            foreach (var folder in Directory.GetDirectories(root))
            {
                var name = Path.GetFileName(folder); string version = "–", description = L.T("Keine ModInfo.xml vorhanden"), website = "";
                try
                {
                    var info = Path.Combine(folder, "ModInfo.xml");
                    if (File.Exists(info))
                    {
                        var xml = XDocument.Load(info);
                        string Read(string key) => xml.Descendants(key).FirstOrDefault()?.Attribute("value")?.Value ?? "";
                        name = new[] { Read("DisplayName"), Read("Name"), name }.First(x => !string.IsNullOrWhiteSpace(x));
                        version = Read("Version"); description = Read("Description"); website = Read("Website");
                    }
                }
                catch (Exception e) when (e is IOException or System.Xml.XmlException or UnauthorizedAccessException) { description = L.T("Metadaten konnten nicht gelesen werden: ") + e.Message; }
                result.Add(new(name, version, description, website, active, folder));
            }
        }
        return result.OrderBy(m => m.Name).ToList();
    }
    public static void Toggle(string game, Mod mod)
    {
        EnsureStopped();
        if (!File.Exists(Path.Combine(game, "7DaysToDie.exe"))) throw new IOException(L.T("Bitte einen gültigen Spielordner auswählen (7DaysToDie.exe)."));
        var sourceRoot = Path.Combine(game, mod.Active ? "Mods" : "Mods-Store");
        if (!string.Equals(Path.GetDirectoryName(Path.GetFullPath(mod.Folder)), Path.GetFullPath(sourceRoot), StringComparison.OrdinalIgnoreCase)) throw new IOException(L.T("Ungültiger Modordner."));
        var targetRoot = Path.Combine(game, mod.Active ? "Mods-Store" : "Mods");
        CheckLink(mod.Folder); CheckLink(targetRoot);
        Directory.CreateDirectory(targetRoot);
        var target = Path.Combine(targetRoot, Path.GetFileName(mod.Folder));
        if (Directory.Exists(target) || File.Exists(target)) throw new IOException(L.T("Im Ziel existiert bereits ein Mod mit diesem Ordnernamen. Es wurde nichts überschrieben."));
        Directory.Move(mod.Folder, target);
    }
    public static string[] FindGames()
    {
        var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(string? p) { if (!string.IsNullOrWhiteSpace(p)) libraries.Add(p); }
        Add(Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string);
        Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"));
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
            foreach (var relative in new[] { "SteamLibrary", "Steam", @"Program Files (x86)\Steam", @"Program Files\Steam" }) Add(Path.Combine(drive.RootDirectory.FullName, relative));
            try { foreach (var folder in Directory.EnumerateDirectories(drive.RootDirectory.FullName)) if (Directory.Exists(Path.Combine(folder, "steamapps"))) Add(folder); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
        foreach (var library in libraries.ToArray())
        {
            try
            {
                var file = Path.Combine(library, "steamapps", "libraryfolders.vdf");
                if (File.Exists(file)) foreach (Match match in Regex.Matches(File.ReadAllText(file), "\"path\"\\s*\"([^\"]+)\"")) Add(match.Groups[1].Value.Replace(@"\\", @"\"));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var library in libraries)
        {
            var common = Path.Combine(library, "steamapps", "common");
            try { if (Directory.Exists(common)) foreach (var dir in Directory.GetDirectories(common)) if (File.Exists(Path.Combine(dir, "7DaysToDie.exe"))) found.Add(dir); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        }
        return found.Order().ToArray();
    }
}
