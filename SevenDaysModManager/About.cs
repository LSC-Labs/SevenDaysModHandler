using System.Diagnostics;

namespace SevenDaysModManager;

public sealed partial class MainForm
{
    readonly Label updateStatus = new() { AutoSize = true, MaximumSize = new Size(760, 0), Margin = new Padding(0, 16, 0, 12) };
    Button checkUpdates = null!, downloadUpdate = null!;
    ReleaseInfo? latestRelease;
    bool checkingUpdates, updateAttempted;
    string updateMessage = "Noch nicht auf Updates geprüft.";

    void UpdateWindowTitle() => Text = L.T("7 Days to Die · Spielstände & Mods") + " · v" + Updates.VersionText;
    void CreateAboutPage()
    {
        var page = new TabPage(L.T("Über")) { Padding = new Padding(24), AutoScroll = true };
        var content = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        content.Controls.Add(new Label { Text = "7 Days Mod Handler", AutoSize = true, Font = new Font(Font.FontFamily, 20, FontStyle.Bold), Margin = new Padding(0, 0, 0, 16) });
        content.Controls.Add(new Label { Text = L.T("Verwaltet Spielstandsicherungen, Mods und Modgruppen für 7 Days to Die."), AutoSize = true, MaximumSize = new Size(760, 0), Margin = new Padding(0, 0, 0, 16) });
        content.Controls.Add(new Label { Text = L.T("Autor: LSC Labs"), AutoSize = true });
        content.Controls.Add(new Label { Text = "Version: " + Updates.VersionText, AutoSize = true });
        checkUpdates = Button(L.T("Auf Updates prüfen"), CheckUpdates);
        downloadUpdate = Button(L.T("Neue Version herunterladen"), () => OpenRelease(latestRelease?.DownloadUrl ?? Updates.ReleasesUrl));
        downloadUpdate.Visible = false;
        content.Controls.Add(updateStatus);
        content.Controls.Add(checkUpdates);
        content.Controls.Add(downloadUpdate);
        content.Controls.Add(Button(L.T("GitHub-Releases öffnen"), () => OpenRelease(Updates.ReleasesUrl)));
        page.Controls.Add(content); tabs.TabPages.Add(page);
        RefreshAboutText();
        tabs.SelectedIndexChanged += async (_, _) =>
        {
            if (tabs.SelectedTab == page && !updateAttempted) await CheckUpdates();
        };
    }
    void RefreshAboutText()
    {
        UpdateWindowTitle();
        updateStatus.Text = latestRelease != null && latestRelease.Version > Updates.CurrentVersion
            ? L.T("Neue Version verfügbar: {0}", latestRelease.Tag) : L.T(updateMessage);
    }
    async Task CheckUpdates()
    {
        if (checkingUpdates) return;
        checkingUpdates = true; updateAttempted = true; checkUpdates.Enabled = false;
        latestRelease = null; downloadUpdate.Visible = false;
        updateMessage = "Updates werden geprüft …"; RefreshAboutText();
        try
        {
            var release = await Updates.LatestAsync();
            if (IsDisposed) return;
            latestRelease = release;
            updateMessage = release == null ? "Kein stabiles Release verfügbar." : "Die installierte Version ist aktuell.";
            downloadUpdate.Visible = release != null && release.Version > Updates.CurrentVersion;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or InvalidDataException or InvalidOperationException or KeyNotFoundException)
        {
            updateMessage = "Updateprüfung fehlgeschlagen. Bitte Internetverbindung prüfen oder die GitHub-Releases öffnen.";
        }
        finally
        {
            checkingUpdates = false;
            if (!IsDisposed) { checkUpdates.Enabled = true; RefreshAboutText(); }
        }
    }
    Task OpenRelease(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception e) { ShowError(e); }
        return Task.CompletedTask;
    }
}
