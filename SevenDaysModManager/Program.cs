using System.Diagnostics;
namespace SevenDaysModManager;

internal static class Program
{
    [STAThread] static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var mutex = new Mutex(true, @"Local\SevenDaysModManager", out var created);
        if (!created) { MessageBox.Show("Die Anwendung ist bereits geöffnet."); return; }
        Application.Run(new MainForm());
    }
}

public sealed partial class MainForm : Form
{
    Settings settings = new();
    readonly TextBox game = new(), saves = new(), backups = new();
    readonly ListBox backupList = new() { Dock = DockStyle.Fill, DisplayMember = "Name" };
    readonly DataGridView mods = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoGenerateColumns = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells, BackgroundColor = Color.White, BorderStyle = BorderStyle.None };
    readonly Label status = new() { Dock = DockStyle.Bottom, Height = 36, Padding = new Padding(10), Text = "Bereit" };
    readonly TabControl tabs = new() { Dock = DockStyle.Fill };
    bool busy;
    string modSortColumn = "Name";
    bool modSortDescending;
    public MainForm()
    {
        Text = "7 Days to Die · Spielstände & Mods"; Width = 1160; Height = 760; MinimumSize = new Size(900, 600); StartPosition = FormStartPosition.CenterScreen;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Font = new Font("Segoe UI", 10); BackColor = Color.FromArgb(244, 246, 249);
        var header = new Label { Text = "7 DAYS TO DIE   /   MOD HANDLER", Dock = DockStyle.Top, Height = 65, Padding = new Padding(18), Font = new Font("Segoe UI", 17, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(27, 40, 48) };
        Controls.Add(tabs); Controls.Add(status); Controls.Add(header);
        var backupTab = new TabPage("Spielstände") { Padding = new Padding(16) };
        var modTab = new TabPage("Mods") { Padding = new Padding(16) };
        var settingsTab = new TabPage("Einstellungen") { Padding = new Padding(16) };
        tabs.TabPages.AddRange([backupTab, modTab, settingsTab]);
        CreateGroupsPage();
        var backupActions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52 };
        backupActions.Controls.Add(Button("Backup erstellen", async () => await Run("Backup wird erstellt …", () => { var path = Manager.Backup(settings); return "Backup erstellt: " + Path.GetFileName(path); })));
        backupActions.Controls.Add(Button("Auswahl wiederherstellen", async () =>
        {
            if (backupList.SelectedItem is not DirectoryInfo selected) { MessageBox.Show(this, "Bitte ein Backup auswählen."); return; }
            if (MessageBox.Show(this, "Alle aktuellen Spielstände werden ersetzt. Vorher wird automatisch ein Sicherheitsbackup erstellt. Fortfahren?", "Backup wiederherstellen", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            await Run("Spielstände werden wiederhergestellt …", () => { Manager.Restore(settings, selected.FullName); return "Wiederherstellung abgeschlossen."; });
        }));
        backupActions.Controls.Add(Button("Aktualisieren", () => { RefreshLists(); return Task.CompletedTask; }));
        var hint = new Label { Text = "Backups des gesamten Spielstandordners · Zeitstempel in UTC (Z), mit Windows-kompatiblen Bindestrichen · Spiel vorher beenden", Dock = DockStyle.Bottom, Height = 46 };
        backupTab.Controls.Add(backupList); backupTab.Controls.Add(backupActions); backupTab.Controls.Add(hint);
        mods.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", DataPropertyName = "Name", Width = 220 });
        mods.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Version", DataPropertyName = "Version", Width = 90 });
        mods.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Beschreibung", DataPropertyName = "Description", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
        mods.Columns.Add(new DataGridViewLinkColumn { HeaderText = "Webseite", DataPropertyName = "Website", Width = 190 });
        mods.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", Name = "State", Width = 95, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        mods.DefaultCellStyle.BackColor = Color.White;
        mods.DefaultCellStyle.ForeColor = Color.Black;
        mods.DefaultCellStyle.SelectionBackColor = Color.White;
        mods.DefaultCellStyle.SelectionForeColor = Color.Black;
        mods.Columns.Add(new DataGridViewButtonColumn
        {
            HeaderText = "Aktion", Name = "Toggle", Width = 135, FlatStyle = FlatStyle.Standard,
            DefaultCellStyle = new DataGridViewCellStyle { Padding = new Padding(6, 5, 6, 5), Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        mods.Columns["State"]!.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        mods.Columns["Toggle"]!.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        foreach (DataGridViewColumn column in mods.Columns) column.SortMode = DataGridViewColumnSortMode.Programmatic;
        mods.ColumnHeaderMouseClick += (_, e) =>
        {
            if (busy || e.ColumnIndex < 0) return;
            var column = mods.Columns[e.ColumnIndex];
            var key = string.IsNullOrEmpty(column.DataPropertyName) ? column.Name : column.DataPropertyName;
            modSortDescending = key == modSortColumn && !modSortDescending;
            modSortColumn = key;
            if (mods.DataSource is List<Mod> items) BindSortedMods(items);
        };
        mods.RowTemplate.MinimumHeight = 38;
        mods.CellPainting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            // Let the native button cell manage hover, press and mouse-leave states.
            e.Paint(e.ClipBounds, e.PaintParts & ~DataGridViewPaintParts.Focus);
            e.Handled = true;
        };
        mods.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || mods.Rows[e.RowIndex].DataBoundItem is not Mod m) return;
            if (mods.Columns[e.ColumnIndex].Name == "State")
            {
                e.Value = m.Active ? "Aktiv" : "Inaktiv";
                e.CellStyle!.BackColor = m.Active ? Color.FromArgb(210, 245, 210) : Color.White;
                e.CellStyle.SelectionBackColor = e.CellStyle.BackColor;
                e.CellStyle.ForeColor = Color.Black;
                e.CellStyle.SelectionForeColor = Color.Black;
            }
            if (mods.Columns[e.ColumnIndex].Name == "Toggle") e.Value = m.Active ? "Deaktivieren" : "Aktivieren";
        };
        mods.CellContentClick += async (_, e) =>
        {
            if (busy || e.RowIndex < 0 || mods.Rows[e.RowIndex].DataBoundItem is not Mod mod) return;
            if (mods.Columns[e.ColumnIndex] is DataGridViewLinkColumn)
            {
                var address = mod.Website.Trim();
                if (address.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) address = "https://" + address;
                if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http")) { MessageBox.Show(this, "Keine gültige HTTP/HTTPS-Webseite hinterlegt."); return; }
                try { Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); } catch (Exception ex) { ShowError(ex); }
            }
            if (mods.Columns[e.ColumnIndex].Name == "Toggle") await Run("Mod wird verschoben …", () => { Manager.Toggle(settings.GamePath, mod); return mod.Name + (mod.Active ? " deaktiviert." : " aktiviert."); });
        };
        var modActions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 50 };
        modActions.Controls.Add(Button("Mods aktualisieren", () => { RefreshLists(); return Task.CompletedTask; }));
        var modHint = new Label { Dock = DockStyle.Bottom, Height = 50, Text = "Entpackte Modordner in <Spiel>\\Mods-Store ablegen. Aktivieren verschiebt sie nach <Spiel>\\Mods.\nDiese Anwendung verwaltet die Mods im Spielordner; Mods im Benutzerprofil werden nicht verändert." };
        modTab.Controls.Add(mods); modTab.Controls.Add(modActions); modTab.Controls.Add(modHint);
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, Padding = new Padding(4) };
        layout.ColumnStyles.Add(new(SizeType.Absolute, 165)); layout.ColumnStyles.Add(new(SizeType.Percent, 100)); layout.ColumnStyles.Add(new(SizeType.Absolute, 130));
        AddPath(layout, "Spielordner", game); AddPath(layout, "Spielstandordner", saves); AddPath(layout, "Backupordner", backups);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        actions.Controls.Add(Button("Einstellungen speichern", async () => await Run("Speichern …", () => { Manager.EnsureBackupAccess(settings); if (!File.Exists(Path.Combine(settings.GamePath, "7DaysToDie.exe"))) throw new IOException("Im Spielordner fehlt 7DaysToDie.exe."); settings.Save(); return "Einstellungen gespeichert."; })));
        actions.Controls.Add(Button("Spiel automatisch suchen", FindGame));
        settingsTab.Controls.Add(actions); settingsTab.Controls.Add(layout);
        Shown += async (_, _) =>
        {
            try { settings = Settings.Load(); } catch (Exception e) { ShowError(new IOException("Einstellungen konnten nicht geladen werden. Bitte neu speichern.", e)); }
            game.Text = settings.GamePath; saves.Text = settings.SavesPath; backups.Text = settings.BackupPath;
            try
            {
                var notice = await Task.Run(() => Manager.PrepareBackupLocation(settings));
                backups.Text = settings.BackupPath;
                if (notice != null) MessageBox.Show(this, notice, "Backupordner aktualisiert", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception e) { tabs.SelectedTab = settingsTab; ShowError(e); return; }
            if (string.IsNullOrWhiteSpace(settings.GamePath)) { tabs.SelectedTab = settingsTab; await FindGame(); } else RefreshLists();
        };
        FormClosing += (_, e) => { if (busy) { e.Cancel = true; MessageBox.Show(this, "Bitte den laufenden Vorgang abwarten."); } };
    }
    static Button Button(string title, Func<Task> action)
    {
        var button = new Button { Text = title, AutoSize = true, Height = 34, Padding = new Padding(8, 3, 8, 3), Margin = new Padding(0, 0, 10, 8), FlatStyle = FlatStyle.Standard, UseVisualStyleBackColor = true };
        button.Click += async (_, _) => await action(); return button;
    }
    void AddPath(TableLayoutPanel layout, string title, TextBox input)
    {
        int row = layout.RowCount++;
        layout.RowStyles.Add(new(SizeType.Absolute, 48));
        layout.Controls.Add(new Label { Text = title, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        input.Dock = DockStyle.Fill; input.Margin = new Padding(3, 8, 12, 3); layout.Controls.Add(input, 1, row);
        layout.Controls.Add(Button("Durchsuchen …", () => { using var dialog = new FolderBrowserDialog { Description = title, UseDescriptionForTitle = true, InitialDirectory = input.Text }; if (dialog.ShowDialog(this) == DialogResult.OK) input.Text = dialog.SelectedPath; return Task.CompletedTask; }), 2, row);
    }
    void Apply() { settings.GamePath = game.Text.Trim(); settings.SavesPath = saves.Text.Trim(); settings.BackupPath = backups.Text.Trim(); }
    async Task FindGame()
    {
        await Run("Steam-Bibliotheken werden durchsucht …", () =>
        {
            var found = Manager.FindGames();
            return found.Length == 0 ? "Spiel nicht gefunden. Bitte den Spielordner manuell auswählen." : string.Join("\n", found);
        }, result =>
        {
            if (result.StartsWith("Spiel nicht")) return;
            var found = result.Split('\n');
            if (found.Length == 1) game.Text = found[0];
            else
            {
                using var picker = new Form { Text = "Spielinstallation auswählen", Width = 760, Height = 300, StartPosition = FormStartPosition.CenterParent };
                var list = new ListBox { Dock = DockStyle.Fill, DataSource = found };
                var ok = new Button { Text = "Auswählen", Dock = DockStyle.Bottom, DialogResult = DialogResult.OK };
                picker.Controls.Add(list); picker.Controls.Add(ok); picker.AcceptButton = ok;
                if (picker.ShowDialog(this) != DialogResult.OK) return;
                game.Text = list.SelectedItem?.ToString() ?? "";
            }
            Apply(); settings.Save(); status.Text = "Spiel gefunden und Einstellungen gespeichert.";
        });
    }
    async Task Run(string message, Func<string> work, Action<string>? completed = null)
    {
        if (busy) return;
        busy = true; tabs.Enabled = false; UseWaitCursor = true; status.Text = message;
        try
        {
            // Read controls on the UI thread; disk operations run in the worker.
            Apply();
            var result = await Task.Run(work); status.Text = result; completed?.Invoke(result); RefreshLists();
        }
        catch (Exception e) { ShowError(e); RefreshLists(); }
        finally { busy = false; tabs.Enabled = true; UseWaitCursor = false; }
    }
    void RefreshLists()
    {
        try { backupList.DataSource = Manager.Backups(settings).Select(p => new DirectoryInfo(p)).ToList(); }
        catch (Exception e) { backupList.DataSource = null; ShowError(e); }
        try { BindSortedMods(Manager.Mods(settings.GamePath)); }
        catch (Exception e) { ShowError(e); }
        RefreshGroups();
    }
    void BindSortedMods(List<Mod> items)
    {
        string Key(Mod mod) => modSortColumn switch
        {
            "Version" => mod.Version,
            "Description" => mod.Description,
            "Website" => mod.Website,
            "State" => mod.Active ? "Aktiv" : "Inaktiv",
            "Toggle" => mod.Active ? "Deaktivieren" : "Aktivieren",
            _ => mod.Name
        };
        var comparer = Comparer<Mod>.Create((a, b) =>
        {
            if (modSortColumn == "Version")
            {
                var aNumeric = Version.TryParse(a.Version, out var av);
                var bNumeric = Version.TryParse(b.Version, out var bv);
                if (aNumeric && bNumeric) return av!.CompareTo(bv);
                if (aNumeric != bNumeric) return aNumeric ? -1 : 1;
            }
            return StringComparer.CurrentCultureIgnoreCase.Compare(Key(a), Key(b));
        });
        var sorted = modSortDescending ? items.OrderByDescending(m => m, comparer) : items.OrderBy(m => m, comparer);
        mods.DataSource = sorted.ThenBy(m => m.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(m => m.Folder, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (DataGridViewColumn column in mods.Columns)
        {
            var key = string.IsNullOrEmpty(column.DataPropertyName) ? column.Name : column.DataPropertyName;
            column.HeaderCell.SortGlyphDirection = key == modSortColumn ? (modSortDescending ? SortOrder.Descending : SortOrder.Ascending) : SortOrder.None;
        }
    }
    void ShowError(Exception e) { status.Text = "Vorgang fehlgeschlagen."; MessageBox.Show(this, e.Message, "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
}
