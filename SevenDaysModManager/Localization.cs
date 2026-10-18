using System.Globalization;
using System.Text.Json;

namespace SevenDaysModManager;

public static class L
{
    public static readonly string[] Languages = ["en", "de", "es", "it", "fr"];
    static readonly CultureInfo SystemCulture = CultureInfo.CurrentUICulture;
    static readonly Dictionary<string, Dictionary<string, string>> Catalog = LoadCatalog();
    public static string Language { get; private set; } = "en";
    static Dictionary<string, Dictionary<string, string>> LoadCatalog()
    {
        using var stream = typeof(L).Assembly.GetManifestResourceStream("SevenDaysModManager.Translations.json")!;
        var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(stream)!;
        static string Decode(string s) => s.Replace("\\n", "\n").Replace("\\\\", "\\");
        return data.ToDictionary(p => Decode(p.Key), p => p.Value.ToDictionary(v => v.Key, v => Decode(v.Value)));
    }
    public static string Resolve(string? preference, CultureInfo? systemCulture = null)
    {
        var code = string.IsNullOrEmpty(preference) || preference == "auto"
            ? (systemCulture ?? SystemCulture).TwoLetterISOLanguageName
            : preference;
        return Languages.Contains(code) ? code : "en";
    }
    public static void Select(string? preference)
    {
        Language = Resolve(preference);
        var culture = CultureInfo.GetCultureInfo(Language);
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
    public static string T(string key, params object[] arguments)
    {
        var value = Catalog.TryGetValue(key, out var entry) ? entry.GetValueOrDefault(Language, entry["en"]) : key;
        return arguments.Length == 0 ? value : string.Format(CultureInfo.CurrentCulture, value, arguments);
    }
    public static string? KeyFor(string text) => Catalog.FirstOrDefault(p => p.Value.Values.Contains(text)).Key;
    public static void ValidateCatalog()
    {
        foreach (var entry in Catalog)
        {
            var argumentCount = System.Text.CompositeFormat.Parse(entry.Value["en"]).MinimumArgumentCount;
            foreach (var language in Languages)
            {
                if (!entry.Value.TryGetValue(language, out var value) || string.IsNullOrWhiteSpace(value))
                    throw new InvalidDataException($"Missing translation: {entry.Key} / {language}");
                if (System.Text.CompositeFormat.Parse(value).MinimumArgumentCount != argumentCount)
                    throw new InvalidDataException($"Translation placeholders differ: {entry.Key} / {language}");
            }
        }
    }
}
