using System.Diagnostics;
namespace SevenDaysModManager;

internal static class Program
{
    [STAThread] static void Main()
    {
        ApplicationConfiguration.Initialize();
        try { L.Select(Settings.Load().Language); }
        catch { L.Select("auto"); } // The form reports settings errors after opening.
        using var mutex = new Mutex(true, @"Local\SevenDaysModManager", out var created);
        if (!created) { MessageBox.Show(L.T("Die Anwendung ist bereits geöffnet.")); return; }
        Application.Run(new MainForm());
    }
}

public sealed partial class MainForm : Form
{
    Settings settings = new();
    readonly TextBox game = new(), saves = new(), backups = new();
    readonly ListBox backupList = new() { Dock = DockStyle.Fill, DisplayMember = "Name" };
    readonly DataGridView mods = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoGenerateColumns = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells, BackgroundColor = Color.White, BorderStyle = BorderStyle.None };
    readonly Label status = new() { Dock = DockStyle.Bottom, Height = 36, Padding = new Padding(10), Text = L.T("Bereit") };
    readonly TabControl tabs = new() { Dock = DockStyle.Fill };
    readonly TableLayoutPanel launchActions = new() { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, RowCount = 1, Padding = new Padding(16, 14, 16, 8), BackColor = Color.FromArgb(224, 235, 240), ForeColor = Color.FromArgb(38, 58, 69) };
    bool busy;
    string modSortColumn = "Name";
    bool modSortDescending;
    readonly ComboBox language = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    readonly Dictionary<Control, string> localizedControls = new();
    readonly Dictionary<DataGridViewColumn, string> localizedColumns = new();
    bool updatingLanguage;
    sealed record LanguageOption(string Code, string Name)
    {
        public override string ToString() => Code == "auto" ? L.T("Automatisch (Systemsprache)") : Name;
    }
    public MainForm()
    {
        Text = L.T("7 Days to Die · Spielstände & Mods"); Width = 1160; Height = 760; MinimumSize = new Size(900, 600); StartPosition = FormStartPosition.CenterScreen;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Font = new Font("Segoe UI", 10); BackColor = Color.FromArgb(244, 246, 249);
        var header = new Label { Text = "7 DAYS TO DIE   /   MOD HANDLER", Dock = DockStyle.Top, Height = 65, Padding = new Padding(18), Font = new Font("Segoe UI", 17, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(27, 40, 48) };
        var playMods = Button(L.T("▶ Spielen · ohne EAC (Mods)"), () => LaunchGame(false));
        var playEac = Button(L.T("▶ Spielen · mit EAC"), () => LaunchGame(true));
        foreach (var button in new[] { playMods, playEac })
        {
            button.Font = new Font(Font, FontStyle.Bold);
            button.MinimumSize = new Size(270, 48);
            button.Padding = new Padding(16, 10, 16, 10);
        }
        playMods.UseVisualStyleBackColor = false;
        playMods.BackColor = Color.FromArgb(30, 112, 70);
        playMods.ForeColor = Color.White;
        launchActions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        launchActions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        launchActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        launchActions.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        launchActions.Controls.Add(playMods, 0, 0); launchActions.Controls.Add(playEac, 1, 0);
        launchActions.Controls.Add(new Label { Text = L.T("EAC ist der Anti-Cheat-Schutz. Aktive Mods bleiben bei beiden Startarten unverändert."), AutoSize = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(12, 0, 0, 8) }, 2, 0);
        var launchDivider = new Panel { Dock = DockStyle.Top, Height = 12, BackColor = BackColor };
        launchDivider.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 2, BackColor = Color.FromArgb(170, 192, 203) });
        Controls.Add(tabs); Controls.Add(status); Controls.Add(launchDivider); Controls.Add(launchActions); Controls.Add(header);
        var backupTab = new TabPage(L.T("Spielstände")) { Padding = new Padding(16) };
        var modTab = new TabPage(L.T("Mods")) { Padding = new Padding(16) };
        var settingsTab = new TabPage(L.T("Einstellungen")) { Padding = new Padding(16) };
        tabs.TabPages.AddRange([backupTab, modTab, settingsTab]);
        CreateGroupsPage();
        var backupActions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52 };
        backupActions.Controls.Add(Button(L.T("Backup erstellen"), async () => await Run(L.T("Backup wird erstellt …"), () => { var path = Manager.Backup(settings); return L.T("Backup erstellt: ") + Path.GetFileName(path); })));
        backupActions.Controls.Add(Button(L.T("Auswahl wiederherstellen"), async () =>
        {
            if (backupList.SelectedItem is not DirectoryInfo selected) { MessageBox.Show(this, L.T("Bitte ein Backup auswählen.")); return; }
            if (MessageBox.Show(this, L.T("Alle aktuellen Spielstände werden ersetzt. Vorher wird automatisch ein Sicherheitsbackup erstellt. Fortfahren?"), L.T("Backup wiederherstellen"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            await Run(L.T("Spielstände werden wiederhergestellt …"), () => { Manager.Restore(settings, selected.FullName); return L.T("Wiederherstellung abgeschlossen."); });
        }));
        backupActions.Controls.Add(Button(L.T("Aktualisieren"), () => { RefreshLists(); return Task.CompletedTask; }));
        var hint = new Label { Text = L.T("Backups des gesamten Spielstandordners · Zeitstempel in UTC (Z), mit Windows-kompatiblen Bindestrichen · Spiel vorher beenden"), Dock = DockStyle.Bottom, Height = 46 };
        backupTab.Controls.Add(backupList); backupTab.Controls.Add(backupActions); backupTab.Controls.Add(hint);
        mods.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L.T("Name"), DataPropertyName = "Name", Width = 220 });
        mods.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L.T("Version"), DataPropertyName = "Version", Width = 90 });
        mods.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L.T("Beschreibung"), DataPropertyName = "Description", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
        mods.Columns.Add(new DataGridViewLinkColumn { HeaderText = L.T("Webseite"), DataPropertyName = "Website", Width = 190 });
        mods.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L.T("Status"), Name = "State", Width = 95, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        mods.DefaultCellStyle.BackColor = Color.White;
        mods.DefaultCellStyle.ForeColor = Color.Black;
        mods.DefaultCellStyle.SelectionBackColor = Color.White;
        mods.DefaultCellStyle.SelectionForeColor = Color.Black;
        mods.Columns.Add(new DataGridViewButtonColumn
        {
            HeaderText = L.T("Aktion"), Name = "Toggle", Width = 135, FlatStyle = FlatStyle.Standard,
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
                e.Value = m.Active ? L.T("Aktiv") : L.T("Inaktiv");
                e.CellStyle!.BackColor = m.Active ? Color.FromArgb(210, 245, 210) : Color.White;
                e.CellStyle.SelectionBackColor = e.CellStyle.BackColor;
                e.CellStyle.ForeColor = Color.Black;
                e.CellStyle.SelectionForeColor = Color.Black;
            }
            if (mods.Columns[e.ColumnIndex].Name == "Toggle") e.Value = m.Active ? L.T("Deaktivieren") : L.T("Aktivieren");
        };
        mods.CellContentClick += async (_, e) =>
        {
            if (busy || e.RowIndex < 0 || mods.Rows[e.RowIndex].DataBoundItem is not Mod mod) return;
            if (mods.Columns[e.ColumnIndex] is DataGridViewLinkColumn)
            {
                var address = mod.Website.Trim();
                if (address.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) address = "https://" + address;
                if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http")) { MessageBox.Show(this, L.T("Keine gültige HTTP/HTTPS-Webseite hinterlegt.")); return; }
                try { Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); } catch (Exception ex) { ShowError(ex); }
            }
            if (mods.Columns[e.ColumnIndex].Name == "Toggle") await Run(L.T("Mod wird verschoben …"), () => { Manager.Toggle(settings.GamePath, mod); return mod.Name + (mod.Active ? L.T(" deaktiviert.") : L.T(" aktiviert.")); });
        };
        var modActions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 50 };
        modActions.Controls.Add(Button(L.T("Mods aktualisieren"), () => { RefreshLists(); return Task.CompletedTask; }));
        var modHint = new Label { Dock = DockStyle.Bottom, Height = 50, Text = L.T("Entpackte Modordner in <Spiel>\\Mods-Store ablegen. Aktivieren verschiebt sie nach <Spiel>\\Mods.\nDiese Anwendung verwaltet die Mods im Spielordner; Mods im Benutzerprofil werden nicht verändert.") };
        modTab.Controls.Add(mods); modTab.Controls.Add(modActions); modTab.Controls.Add(modHint);
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, Padding = new Padding(4) };
        layout.ColumnStyles.Add(new(SizeType.Absolute, 200)); layout.ColumnStyles.Add(new(SizeType.Percent, 100)); layout.ColumnStyles.Add(new(SizeType.Absolute, 160));
        AddPath(layout, L.T("Spielordner"), game); AddPath(layout, L.T("Spielstandordner"), saves); AddPath(layout, L.T("Backupordner"), backups);
        var languageRow = layout.RowCount++;
        layout.RowStyles.Add(new(SizeType.Absolute, 48));
        layout.Controls.Add(new Label { Text = L.T("Sprache"), AutoSize = true, Anchor = AnchorStyles.Left }, 0, languageRow);
        language.Items.AddRange(new object[] { new LanguageOption("auto", ""), new LanguageOption("en", "English"), new LanguageOption("de", "Deutsch"), new LanguageOption("es", "Español"), new LanguageOption("it", "Italiano"), new LanguageOption("fr", "Français") });
        layout.Controls.Add(language, 1, languageRow);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        actions.Controls.Add(Button(L.T("Einstellungen speichern"), async () => await Run(L.T("Speichern …"), () => { Manager.EnsureBackupAccess(settings); if (!File.Exists(Path.Combine(settings.GamePath, "7DaysToDie.exe"))) throw new IOException(L.T("Im Spielordner fehlt 7DaysToDie.exe.")); settings.Save(); return L.T("Einstellungen gespeichert."); })));
        actions.Controls.Add(Button(L.T("Spiel automatisch suchen"), FindGame));
        settingsTab.Controls.Add(actions); settingsTab.Controls.Add(layout);
        Shown += async (_, _) =>
        {
            try { settings = Settings.Load(); } catch (Exception e) { ShowError(new IOException(L.T("Einstellungen konnten nicht geladen werden. Bitte neu speichern."), e)); }
            language.SelectedIndex = Math.Max(0, language.Items.Cast<LanguageOption>().ToList().FindIndex(o => o.Code == settings.Language));
            language.SelectedIndexChanged += (_, _) => ChangeLanguage();
            game.Text = settings.GamePath; saves.Text = settings.SavesPath; backups.Text = settings.BackupPath;
            try
            {
                var notice = await Task.Run(() => Manager.PrepareBackupLocation(settings));
                backups.Text = settings.BackupPath;
                if (notice != null) MessageBox.Show(this, notice, L.T("Backupordner aktualisiert"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception e) { tabs.SelectedTab = settingsTab; ShowError(e); return; }
            if (string.IsNullOrWhiteSpace(settings.GamePath)) { tabs.SelectedTab = settingsTab; await FindGame(); } else RefreshLists();
        };
        FormClosing += (_, e) => { if (busy) { e.Cancel = true; MessageBox.Show(this, L.T("Bitte den laufenden Vorgang abwarten.")); } };
        CaptureTranslations(this);
        foreach (DataGridViewColumn column in mods.Columns)
            if (L.KeyFor(column.HeaderText) is string key) localizedColumns[column] = key;
    }
    void CaptureTranslations(Control control)
    {
        // Keep resource identities separately from editable text and user-provided data.
        if (control is Form or TabPage or Label or System.Windows.Forms.Button)
            if (L.KeyFor(control.Text) is string key) localizedControls[control] = key;
        foreach (Control child in control.Controls) CaptureTranslations(child);
    }
    void ChangeLanguage()
    {
        if (updatingLanguage || language.SelectedItem is not LanguageOption option) return;
        var previous = settings.Language;
        settings.Language = option.Code;
        try { settings.Save(); }
        catch (Exception e)
        {
            settings.Language = previous;
            updatingLanguage = true;
            language.SelectedItem = language.Items.Cast<LanguageOption>().First(o => o.Code == previous || (o.Code == "auto" && !L.Languages.Contains(previous)));
            updatingLanguage = false;
            ShowError(e); return;
        }
        L.Select(option.Code);
        SuspendLayout();
        foreach (var pair in localizedControls) pair.Key.Text = L.T(pair.Value);
        foreach (var pair in localizedColumns) pair.Key.HeaderText = L.T(pair.Value);
        // Repaint the automatic option using its translated display name.
        updatingLanguage = true;
        var selectedIndex = language.SelectedIndex;
        language.Items[0] = new LanguageOption("auto", "");
        language.SelectedIndex = selectedIndex;
        updatingLanguage = false;
        status.Text = L.T("Bereit");
        try
        {
            var items = Manager.Mods(settings.GamePath);
            BindSortedMods(items);
            for (int i = 0; i < groupMods.Items.Count; i++)
                if (groupMods.Items[i] is GroupItem item && !items.Any(m => string.Equals(Path.GetFileName(m.Folder), item.Folder, StringComparison.OrdinalIgnoreCase)))
                    groupMods.Items[i] = new GroupItem(item.Folder, item.Folder + L.T(" (fehlt)"));
        }
        catch (Exception e) { ShowError(e); }
        mods.Invalidate();
        ResumeLayout(true);
    }
    Task LaunchGame(bool withEac) => Run(L.T("Spiel wird gestartet …"), () =>
    {
        Manager.StartGame(settings.GamePath, withEac);
        return L.T("Spielstart angefordert.");
    });
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
        var titleKey = L.KeyFor(title) ?? title;
        layout.Controls.Add(Button(L.T("Durchsuchen …"), () => { using var dialog = new FolderBrowserDialog { Description = L.T(titleKey), UseDescriptionForTitle = true, InitialDirectory = input.Text }; if (dialog.ShowDialog(this) == DialogResult.OK) input.Text = dialog.SelectedPath; return Task.CompletedTask; }), 2, row);
    }
    void Apply() { settings.GamePath = game.Text.Trim(); settings.SavesPath = saves.Text.Trim(); settings.BackupPath = backups.Text.Trim(); }
    async Task FindGame()
    {
        await Run(L.T("Steam-Bibliotheken werden durchsucht …"), () =>
        {
            var found = Manager.FindGames();
            return found.Length == 0 ? L.T("Spiel nicht gefunden. Bitte den Spielordner manuell auswählen.") : string.Join("\n", found);
        }, result =>
        {
            if (result == L.T("Spiel nicht gefunden. Bitte den Spielordner manuell auswählen.")) return;
            var found = result.Split('\n');
            if (found.Length == 1) game.Text = found[0];
            else
            {
                using var picker = new Form { Text = L.T("Spielinstallation auswählen"), Width = 760, Height = 300, StartPosition = FormStartPosition.CenterParent };
                var list = new ListBox { Dock = DockStyle.Fill, DataSource = found };
                var ok = new Button { Text = L.T("Auswählen"), Dock = DockStyle.Bottom, DialogResult = DialogResult.OK };
                picker.Controls.Add(list); picker.Controls.Add(ok); picker.AcceptButton = ok;
                if (picker.ShowDialog(this) != DialogResult.OK) return;
                game.Text = list.SelectedItem?.ToString() ?? "";
            }
            Apply(); settings.Save(); status.Text = L.T("Spiel gefunden und Einstellungen gespeichert.");
        });
    }
    async Task Run(string message, Func<string> work, Action<string>? completed = null)
    {
        if (busy) return;
        busy = true; tabs.Enabled = false; launchActions.Enabled = false; UseWaitCursor = true; status.Text = message;
        try
        {
            // Read controls on the UI thread; disk operations run in the worker.
            Apply();
            var result = await Task.Run(work); status.Text = result; completed?.Invoke(result); RefreshLists();
        }
        catch (Exception e) { ShowError(e); RefreshLists(); }
        finally { busy = false; tabs.Enabled = true; launchActions.Enabled = true; UseWaitCursor = false; }
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
            "State" => mod.Active ? L.T("Aktiv") : L.T("Inaktiv"),
            "Toggle" => mod.Active ? L.T("Deaktivieren") : L.T("Aktivieren"),
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
    void ShowError(Exception e) { status.Text = L.T("Vorgang fehlgeschlagen."); MessageBox.Show(this, e.Message, L.T("Hinweis"), MessageBoxButtons.OK, MessageBoxIcon.Warning); }
}
