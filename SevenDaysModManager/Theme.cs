namespace SevenDaysModManager;

public sealed partial class MainForm
{
    readonly ComboBox theme = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    bool updatingTheme;
    bool DarkMode => settings.Theme == "dark";
    Color SurfaceColor => DarkMode ? Color.FromArgb(36, 40, 47) : Color.White;
    Color TextColor => DarkMode ? Color.FromArgb(235, 239, 245) : Color.FromArgb(25, 32, 40);
    Color ActiveModBackground => DarkMode ? Color.FromArgb(35, 83, 54) : Color.FromArgb(210, 245, 210);

    void AddThemeSetting(TableLayoutPanel layout)
    {
        int row = layout.RowCount++;
        layout.RowStyles.Add(new(SizeType.Absolute, 48));
        layout.Controls.Add(new Label { Text = L.T("Darstellung"), AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        layout.Controls.Add(theme, 1, row);
        RefreshThemeOptions();
        tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabs.DrawItem += (_, e) =>
        {
            bool selected = e.Index == tabs.SelectedIndex;
            using var brush = new SolidBrush(selected ? SurfaceColor : BackColor);
            e.Graphics.FillRectangle(brush, e.Bounds);
            TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, Font, e.Bounds, TextColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };
    }
    void RefreshThemeOptions()
    {
        updatingTheme = true;
        theme.Items.Clear();
        theme.Items.AddRange(new object[] { L.T("Hell"), L.T("Dunkel") });
        theme.SelectedIndex = DarkMode ? 1 : 0;
        updatingTheme = false;
    }
    void InitializeTheme()
    {
        RefreshThemeOptions();
        ApplyTheme(this);
        theme.SelectedIndexChanged += (_, _) =>
        {
            if (updatingTheme) return;
            var previous = settings.Theme;
            settings.Theme = theme.SelectedIndex == 1 ? "dark" : "light";
            try { settings.Save(); }
            catch (Exception e) { settings.Theme = previous; RefreshThemeOptions(); ShowError(e); return; }
            ApplyTheme(this);
        };
    }
    void ApplyTheme(Control root)
    {
        var background = DarkMode ? Color.FromArgb(27, 30, 36) : Color.FromArgb(244, 246, 249);
        var buttonColor = DarkMode ? Color.FromArgb(53, 60, 70) : Color.FromArgb(232, 237, 243);
        void Paint(Control control)
        {
            control.BackColor = control is TextBoxBase or ListBox or ComboBox or TabPage ? SurfaceColor : background;
            control.ForeColor = TextColor;
            if (control is TabPage page) page.UseVisualStyleBackColor = false;
            if (control is Button button)
            {
                button.UseVisualStyleBackColor = false;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = DarkMode ? Color.FromArgb(92, 104, 120) : Color.FromArgb(160, 173, 187);
                button.FlatAppearance.MouseOverBackColor = DarkMode ? Color.FromArgb(70, 81, 96) : Color.FromArgb(213, 225, 238);
                button.FlatAppearance.MouseDownBackColor = DarkMode ? Color.FromArgb(83, 97, 115) : Color.FromArgb(194, 212, 233);
                button.BackColor = buttonColor;
            }
            foreach (Control child in control.Controls) Paint(child);
        }
        root.SuspendLayout();
        Paint(root);
        if (root == this)
        {
            launchActions.BackColor = DarkMode ? Color.FromArgb(34, 48, 59) : Color.FromArgb(224, 235, 240);
            foreach (Control child in launchActions.Controls) if (child is Label) child.BackColor = launchActions.BackColor;
            if (launchActions.GetControlFromPosition(0, 0) is Button play)
            {
                play.BackColor = Color.FromArgb(30, 112, 70); play.ForeColor = Color.White;
                play.FlatAppearance.MouseOverBackColor = Color.FromArgb(38, 134, 84);
                play.FlatAppearance.MouseDownBackColor = Color.FromArgb(24, 90, 56);
            }
            mods.BackgroundColor = SurfaceColor;
            mods.GridColor = DarkMode ? Color.FromArgb(65, 73, 85) : Color.FromArgb(214, 222, 231);
            mods.EnableHeadersVisualStyles = false;
            foreach (var style in new[] { mods.DefaultCellStyle, mods.ColumnHeadersDefaultCellStyle })
            {
                style.BackColor = SurfaceColor; style.ForeColor = TextColor;
                style.SelectionBackColor = SurfaceColor; style.SelectionForeColor = TextColor;
            }
            foreach (DataGridViewColumn column in mods.Columns)
            {
                if (column is DataGridViewLinkColumn link)
                {
                    link.LinkColor = DarkMode ? Color.FromArgb(116, 190, 255) : Color.FromArgb(0, 80, 160);
                    link.ActiveLinkColor = DarkMode ? Color.FromArgb(255, 191, 130) : Color.FromArgb(160, 40, 0);
                    link.VisitedLinkColor = DarkMode ? Color.FromArgb(202, 170, 255) : Color.FromArgb(100, 40, 140);
                }
                if (column is DataGridViewButtonColumn action)
                {
                    action.FlatStyle = DarkMode ? FlatStyle.Flat : FlatStyle.Standard;
                    action.DefaultCellStyle.BackColor = buttonColor;
                    action.DefaultCellStyle.ForeColor = TextColor;
                    action.DefaultCellStyle.SelectionBackColor = buttonColor;
                    action.DefaultCellStyle.SelectionForeColor = TextColor;
                }
            }
        }
        root.ResumeLayout(true);
        root.Invalidate(true);
    }
}
