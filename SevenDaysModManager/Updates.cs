using System.Net;
using System.Text.Json;

namespace SevenDaysModManager;

public sealed record ReleaseInfo(Version Version, string Tag, string DownloadUrl);

public static class Updates
{
    public const string ReleasesUrl = "https://github.com/LSC-Labs/SevenDaysModHandler/releases";
    public static Version CurrentVersion => typeof(Updates).Assembly.GetName().Version ?? new Version(1, 0, 0, 0);
    public static string VersionText => CurrentVersion.ToString(3);
    public static Version ParseVersion(string tag)
    {
        if (!Version.TryParse(tag.Trim().TrimStart('v', 'V'), out var version))
            throw new InvalidDataException(L.T("Die Release-Version konnte nicht gelesen werden."));
        return new Version(version.Major, version.Minor, Math.Max(0, version.Build), Math.Max(0, version.Revision));
    }
    public static ReleaseInfo ParseRelease(string json)
    {
        using var document = JsonDocument.Parse(json);
        var release = document.RootElement;
        if (release.GetProperty("draft").GetBoolean() || release.GetProperty("prerelease").GetBoolean())
            throw new InvalidDataException(L.T("Kein stabiles Release verfügbar."));
        var tag = release.GetProperty("tag_name").GetString() ?? "";
        var url = release.GetProperty("html_url").GetString() ?? "";
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host != "github.com" || !uri.AbsolutePath.StartsWith("/LSC-Labs/SevenDaysModHandler/releases/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(L.T("Ungültige GitHub-Release-Adresse."));
        return new ReleaseInfo(ParseVersion(tag), tag, uri.AbsoluteUri);
    }
    public static async Task<ReleaseInfo?> LatestAsync()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SevenDaysModHandler/" + VersionText);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        using var response = await client.GetAsync("https://api.github.com/repos/LSC-Labs/SevenDaysModHandler/releases/latest");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return ParseRelease(await response.Content.ReadAsStringAsync());
    }
}
