namespace BndzFinder.Preferences.Localization;

public static class Loc
{
    private static string _language = "en";
    private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
    {
        ["en"] = new()
        {
            ["Preferences.Title"] = "Dock Preferences",
            ["Section.General"] = "General",
            ["Section.Appearance"] = "Appearance",
            ["Section.SystemIconTray"] = "System Icon Tray",
            ["Section.Screen"] = "Screen",
            ["Section.LookAndBehavior"] = "Look and Behavior",
            ["Section.Launchpad"] = "Launchpad",
            ["Section.WindowAnimations"] = "Window Animations",
            ["Section.AudioDisplayNetwork"] = "Audio / Display / Network",
            ["Section.Advanced"] = "Hot Corners and Hotkeys",
            ["Section.Themes"] = "Themes",
            ["Dock.Position"] = "Position on screen",
            ["Dock.MinimizeEffect"] = "Minimize windows using",
            ["Dock.AutoHide"] = "Automatically hide and show the Dock",
            ["Dock.GlassEffect"] = "Glass effect",
            ["Dock.IconEffect"] = "Icon hover effect",
            ["Finder.Wifi"] = "Wi-Fi",
            ["Finder.Bluetooth"] = "Bluetooth",
            ["Finder.Audio"] = "Sound",
            ["Finder.Display"] = "Display",
            ["Launchpad.Search"] = "Search",
            ["StageManager.Windows"] = "Open windows",
            ["Save"] = "Save"
        },
        ["es"] = new()
        {
            ["Preferences.Title"] = "Preferencias del Dock",
            ["Section.General"] = "General"
        }
    };

    public static string Language
    {
        get => _language;
        set => _language = value;
    }

    public static string Get(string key)
    {
        if (Strings.TryGetValue(_language, out var table) && table.TryGetValue(key, out var value))
        {
            return value;
        }
        if (Strings["en"].TryGetValue(key, out var fallback))
        {
            return fallback;
        }
        return key;
    }

    public static IReadOnlyList<string> SupportedLanguages { get; } =
    [
        "en", "es", "pt-BR", "zh", "ru", "ja", "ko", "fr", "de", "it",
        "uk", "pl", "tr", "ar", "hi", "th", "vi", "id", "nl", "sv",
        "da", "no", "fi", "cs", "hu"
    ];
}
