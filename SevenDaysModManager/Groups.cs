namespace SevenDaysModManager;

public sealed partial class MainForm
{
    readonly ListBox groupList = new() { Dock = DockStyle.Fill };
    readonly TextBox groupName = new() { Dock = DockStyle.Fill };
    readonly CheckedListBox groupMods = new() { Dock = DockStyle.Fill, CheckOnClick = true, HorizontalScrollbar = true };
    sealed record GroupItem(string Folder, string Label)
    {
        public override string ToString() => Label;
    }
    void CreateGroupsPage()
    {
        var page = new TabPage(L.T("Modgruppen")) { Padding = new Padding(16) };
        tabs.TabPages.Insert(2, page);
        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 260, FixedPanel = FixedPanel.Panel1 };
        split.Panel1.Controls.Add(groupList);
        split.Panel1.Controls.Add(new Label { Dock = DockStyle.Top, Height = 32, Text = L.T("Gespeicherte Gruppen") });
        var editor = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        editor.ColumnStyles.Add(new(SizeType.Percent, 100));
        editor.RowStyles.Add(new(SizeType.Absolute, 28)); editor.RowStyles.Add(new(SizeType.Absolute, 38));
        editor.RowStyles.Add(new(SizeType.Absolute, 32)); editor.RowStyles.Add(new(SizeType.Percent, 100));
        editor.Controls.Add(new Label { Text = L.T("Gruppenname"), AutoSize = true }, 0, 0);
        editor.Controls.Add(groupName, 0, 1);
        editor.Controls.Add(new Label { Text = L.T("Zugehörige Mods auswählen (Mehrfachzuordnung möglich)"), AutoSize = true }, 0, 2);
        editor.Controls.Add(groupMods, 0, 3); split.Panel2.Controls.Add(editor);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 90, WrapContents = true };
        actions.Controls.Add(Button(L.T("Neue Gruppe"), () => GroupEdit(() => { groupList.ClearSelected(); groupName.Clear(); PopulateGroup(null); groupName.Focus(); })));
        actions.Controls.Add(Button(L.T("Gruppe speichern"), () => GroupEdit(SaveGroup)));
        actions.Controls.Add(Button(L.T("Gruppe löschen"), () => GroupEdit(() =>
        {
            if (groupList.SelectedItem is not ModGroup selected) throw new IOException(L.T("Bitte eine Gruppe auswählen."));
            if (MessageBox.Show(this, L.T("Gruppe löschen? Die Mods bleiben unverändert."), L.T("Gruppe löschen"), MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            var previous = settings.Groups; settings.Groups = previous.Where(g => g != selected).ToList();
            try { settings.Save(); } catch { settings.Groups = previous; throw; }
            RefreshGroups();
        })));
        actions.Controls.Add(Button(L.T("Gruppe aktivieren"), () => SwitchGroup(true)));
        actions.Controls.Add(Button(L.T("Gruppe deaktivieren"), () => SwitchGroup(false)));
        actions.Controls.Add(Button(L.T("Aktualisieren"), () => GroupEdit(() => { Apply(); RefreshLists(); })));
        page.Controls.Add(split); page.Controls.Add(actions);
        page.Controls.Add(new Label { Dock = DockStyle.Bottom, Height = 65, Text = L.T("Aktivieren: Genau die gespeicherten Gruppenmods werden aktiviert; alle anderen Mods werden deaktiviert.\nDeaktivieren: Nur die Mods der gewählten Gruppe werden deaktiviert. Änderungen vorher speichern.\nGruppen gelten für die jeweilige Spielinstallation. Spiel und Server vorher beenden.") });
        groupList.SelectedIndexChanged += (_, _) => PopulateGroup(groupList.SelectedItem as ModGroup);
    }
    Task GroupEdit(Action action)
    {
        try { action(); } catch (Exception e) { ShowError(e); }
        return Task.CompletedTask;
    }
    void RefreshGroups()
    {
        var selected = groupList.SelectedItem as ModGroup;
        groupList.Items.Clear();
        foreach (var group in settings.Groups.Where(g => string.Equals(g.GamePath.TrimEnd('\\', '/'), settings.GamePath.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase))) groupList.Items.Add(group);
        if (selected != null && groupList.Items.Contains(selected)) groupList.SelectedItem = selected;
        else if (groupList.Items.Count > 0) groupList.SelectedIndex = 0;
        else PopulateGroup(null);
    }
    void PopulateGroup(ModGroup? group)
    {
        groupName.Text = group?.Name ?? "";
        groupMods.Items.Clear();
        try
        {
            var all = Manager.Mods(settings.GamePath);
            var folders = all.Select(m => Path.GetFileName(m.Folder)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var missing in group?.Folders ?? []) if (!folders.Contains(missing, StringComparer.OrdinalIgnoreCase)) folders.Add(missing);
            foreach (var folder in folders)
            {
                var mod = all.FirstOrDefault(m => string.Equals(Path.GetFileName(m.Folder), folder, StringComparison.OrdinalIgnoreCase));
                var label = mod == null ? folder + L.T(" (fehlt)") : $"{mod.Name} — {folder}";
                groupMods.Items.Add(new GroupItem(folder, label), group?.Folders.Contains(folder, StringComparer.OrdinalIgnoreCase) ?? false);
            }
        }
        catch (Exception e) { ShowError(e); }
    }
    void SaveGroup()
    {
        Apply();
        if (!File.Exists(Path.Combine(settings.GamePath, "7DaysToDie.exe"))) throw new IOException(L.T("Bitte zuerst einen gültigen Spielordner einstellen."));
        var name = groupName.Text.Trim();
        if (name.Length == 0) throw new IOException(L.T("Bitte einen Gruppennamen eingeben."));
        var selected = groupList.SelectedItem as ModGroup;
        if (settings.Groups.Any(g => g != selected && string.Equals(g.GamePath, settings.GamePath, StringComparison.OrdinalIgnoreCase) && string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase))) throw new IOException(L.T("Eine Gruppe mit diesem Namen existiert bereits."));
        var replacement = new ModGroup { Name = name, GamePath = Path.GetFullPath(settings.GamePath).TrimEnd('\\', '/'), Folders = groupMods.CheckedItems.Cast<GroupItem>().Select(i => i.Folder).ToList() };
        var previous = settings.Groups;
        settings.Groups = previous.Where(g => g != selected).Append(replacement).ToList();
        try { settings.Save(); } catch { settings.Groups = previous; throw; }
        RefreshGroups(); groupList.SelectedItem = replacement; status.Text = L.T("Gruppe gespeichert: ") + name;
    }
    async Task SwitchGroup(bool activate)
    {
        if (groupList.SelectedItem is not ModGroup selected) { MessageBox.Show(this, L.T("Bitte eine gespeicherte Gruppe auswählen.")); return; }
        if (activate && selected.Folders.Count == 0 && MessageBox.Show(this, L.T("Diese Gruppe ist leer. Dadurch werden alle Mods deaktiviert. Fortfahren?"), L.T("Leere Gruppe aktivieren"), MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        await Run(L.T("Modgruppe wird umgeschaltet …"), () => { Manager.SetGroup(settings.GamePath, selected, activate); return L.T("Gruppe {0} {1}", selected.Name, activate ? L.T("aktiviert.") : L.T("deaktiviert.")); });
    }
}
