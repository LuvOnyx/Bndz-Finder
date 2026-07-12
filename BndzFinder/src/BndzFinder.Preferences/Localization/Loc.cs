namespace BndzFinder.Preferences.Localization;

public static class Loc
{
    private static string _language = "en";

    public static string Language
    {
        get => _language;
        set => _language = Normalize(value);
    }

    public static bool IsRightToLeft => _language is "ar" or "he" or "fa";

    public static IReadOnlyList<string> SupportedLanguages { get; } =
    [
        "en", "es", "pt-BR", "zh", "ru", "ja", "ko", "fr", "de", "it",
        "uk", "pl", "tr", "ar", "hi", "th", "vi", "id", "nl", "sv",
        "da", "no", "fi", "cs", "hu"
    ];

    public static string Get(string key) =>
        LocCatalog.TryGet(_language, key) ?? LocCatalog.TryGet("en", key) ?? key;

    private static string Normalize(string lang) =>
        SupportedLanguages.Contains(lang) ? lang : lang.Split('-')[0] is var baseLang && SupportedLanguages.Contains(baseLang) ? baseLang : "en";
}

internal static class LocCatalog
{
    private static readonly Dictionary<string, Dictionary<string, string>> Tables = Build();

    public static string? TryGet(string language, string key) =>
        Tables.TryGetValue(language, out var table) && table.TryGetValue(key, out var value) ? value : null;

    private static Dictionary<string, Dictionary<string, string>> Build()
    {
        var keys = new Dictionary<string, string>
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
            ["Dock.DisplayMode"] = "Dock visibility",
            ["Dock.MinimizeEffect"] = "Minimize windows using",
            ["Dock.AutoHide"] = "Automatically hide and show the Dock",
            ["Dock.GlassEffect"] = "Glass effect",
            ["Dock.IconEffect"] = "Icon hover effect",
            ["Dock.AccentColor"] = "Accent color",
            ["Dock.GlassTint"] = "Glass tint color",
            ["Dock.Opacity"] = "Dock opacity",
            ["Dock.CornerRadius"] = "Corner radius",
            ["Finder.Enable"] = "Show menu bar",
            ["Finder.Widgets"] = "Menu bar widgets",
            ["Finder.ShowCpu"] = "CPU usage",
            ["Finder.ShowMemory"] = "Memory usage",
            ["Finder.ShowGpu"] = "GPU usage",
            ["Finder.ShowDisk"] = "Disk usage",
            ["Finder.ShowNetwork"] = "Network",
            ["Finder.ShowBattery"] = "Battery",
            ["Finder.ShowWeather"] = "Weather",
            ["Finder.ShowAudio"] = "Sound",
            ["Finder.ShowBluetooth"] = "Bluetooth",
            ["Finder.ShowDisplay"] = "Display",
            ["Finder.ShowKeyboard"] = "Keyboard layout",
            ["Finder.ShowMedia"] = "Media controls",
            ["Finder.ShowNotifications"] = "Notifications",
            ["Finder.Wifi"] = "Wi-Fi",
            ["Launchpad.Search"] = "Search",
            ["StageManager.Windows"] = "Open windows",
            ["Save"] = "Save",
            ["Reset"] = "Reset section",
            ["Backup"] = "Backup settings",
            ["Restore"] = "Restore settings",
            ["Language"] = "Language"
        };

        var translations = new Dictionary<string, Dictionary<string, string>>
        {
            ["en"] = keys,
            ["es"] = Translate(keys, "Preferencias del Dock", "General", "Apariencia", "Guardar"),
            ["pt-BR"] = Translate(keys, "Preferências do Dock", "Geral", "Aparência", "Salvar"),
            ["zh"] = Translate(keys, "程序坞偏好设置", "通用", "外观", "保存"),
            ["ru"] = Translate(keys, "Настройки Dock", "Общие", "Внешний вид", "Сохранить"),
            ["ja"] = Translate(keys, "Dock設定", "一般", "外観", "保存"),
            ["ko"] = Translate(keys, "Dock 환경설정", "일반", "모양", "저장"),
            ["fr"] = Translate(keys, "Préférences du Dock", "Général", "Apparence", "Enregistrer"),
            ["de"] = Translate(keys, "Dock-Einstellungen", "Allgemein", "Erscheinungsbild", "Speichern"),
            ["it"] = Translate(keys, "Preferenze Dock", "Generale", "Aspetto", "Salva"),
            ["uk"] = Translate(keys, "Налаштування Dock", "Загальні", "Вигляд", "Зберегти"),
            ["pl"] = Translate(keys, "Ustawienia Dock", "Ogólne", "Wygląd", "Zapisz"),
            ["tr"] = Translate(keys, "Dock Tercihleri", "Genel", "Görünüm", "Kaydet"),
            ["ar"] = Translate(keys, "تفضيلات Dock", "عام", "المظهر", "حفظ"),
            ["hi"] = Translate(keys, "Dock प्राथमिकताएँ", "सामान्य", "रूप", "सहेजें"),
            ["th"] = Translate(keys, "การตั้งค่า Dock", "ทั่วไป", "รูปลักษณ์", "บันทึก"),
            ["vi"] = Translate(keys, "Tùy chọn Dock", "Chung", "Giao diện", "Lưu"),
            ["id"] = Translate(keys, "Preferensi Dock", "Umum", "Tampilan", "Simpan"),
            ["nl"] = Translate(keys, "Dock-voorkeuren", "Algemeen", "Uiterlijk", "Opslaan"),
            ["sv"] = Translate(keys, "Dock-inställningar", "Allmänt", "Utseende", "Spara"),
            ["da"] = Translate(keys, "Dock-indstillinger", "Generelt", "Udseende", "Gem"),
            ["no"] = Translate(keys, "Dock-innstillinger", "Generelt", "Utseende", "Lagre"),
            ["fi"] = Translate(keys, "Dock-asetukset", "Yleinen", "Ulkoasu", "Tallenna"),
            ["cs"] = Translate(keys, "Předvolby Docku", "Obecné", "Vzhled", "Uložit"),
            ["hu"] = Translate(keys, "Dock beállítások", "Általános", "Megjelenés", "Mentés")
        };

        foreach (var lang in Loc.SupportedLanguages)
        {
            if (!translations.ContainsKey(lang))
                translations[lang] = new Dictionary<string, string>(keys);
        }
        return translations;
    }

    private static Dictionary<string, string> Translate(
        Dictionary<string, string> en, string title, string general, string appearance, string save)
    {
        var copy = new Dictionary<string, string>(en);
        copy["Preferences.Title"] = title;
        copy["Section.General"] = general;
        copy["Section.Appearance"] = appearance;
        copy["Save"] = save;
        return copy;
    }
}
