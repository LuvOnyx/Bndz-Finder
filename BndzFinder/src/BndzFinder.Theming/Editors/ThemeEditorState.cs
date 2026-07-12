using BndzFinder.Core.Settings;

namespace BndzFinder.Theming.Editors;

public interface IThemeEditor
{
    void SetDockBackground(string? imagePath, double blur, double opacity, double margin);
    void SetProgressBarStyle(string borderStyle, string backgroundStyle);
    void SetRunIndicatorStyle(string style, int offset);
}

public interface IIconDesigner
{
    void SetSize(int size);
    void SetOffset(int x, int y);
    void SetColors(string primary, string? gradientEnd);
    void SetShadow(double blur, double opacity);
    void SetCornerRadius(double radius);
}

public interface ICalendarEditor
{
    void SetMonthFont(string family, double size, string color);
    void SetDayFont(string family, double size, string color);
    void SetRotation(double degrees);
}

public sealed class ThemeEditorState
{
    public string? DockBackgroundImage { get; set; }
    public double DockBackgroundBlur { get; set; }
    public double DockBackgroundOpacity { get; set; } = 1.0;
    public double DockBackgroundMargin { get; set; }
    public string ProgressBarBorderStyle { get; set; } = "rounded";
    public string ProgressBarBackgroundStyle { get; set; } = "glass";
    public string RunIndicatorStyle { get; set; } = "dot";
    public int RunIndicatorOffset { get; set; }
}

public interface IThemeApplyService
{
    ThemeEditorState State { get; }
    void ApplyToSettings(BndzFinderSettings settings);
    void LoadFromSettings(BndzFinderSettings settings);
}

public sealed class ThemeApplyService : IThemeApplyService
{
    public ThemeEditorState State { get; } = new();

    public void LoadFromSettings(BndzFinderSettings settings)
    {
        State.DockBackgroundOpacity = settings.DockOpacity;
        State.DockBackgroundBlur = settings.GlobalBlurValue;
        State.RunIndicatorOffset = 4;
    }

    public void ApplyToSettings(BndzFinderSettings settings)
    {
        settings.DockOpacity = State.DockBackgroundOpacity;
        settings.GlobalBlurValue = State.DockBackgroundBlur;
        settings.ActiveDockSkin = State.ProgressBarBackgroundStyle;
        settings.ActiveIconTheme = State.RunIndicatorStyle;
    }
}

public sealed class ThemeEditor : IThemeEditor
{
    private readonly ThemeEditorState _state;

    public ThemeEditor(ThemeEditorState? state = null) => _state = state ?? new ThemeEditorState();

    public void SetDockBackground(string? imagePath, double blur, double opacity, double margin)
    {
        _state.DockBackgroundImage = imagePath;
        _state.DockBackgroundBlur = blur;
        _state.DockBackgroundOpacity = opacity;
        _state.DockBackgroundMargin = margin;
    }

    public void SetProgressBarStyle(string borderStyle, string backgroundStyle)
    {
        _state.ProgressBarBorderStyle = borderStyle;
        _state.ProgressBarBackgroundStyle = backgroundStyle;
    }

    public void SetRunIndicatorStyle(string style, int offset)
    {
        _state.RunIndicatorStyle = style;
        _state.RunIndicatorOffset = offset;
    }
}

public sealed class IconDesigner : IIconDesigner
{
    private int _size = 48;
    private string _primary = "#0078D4";
    private string? _gradientEnd;
    private double _shadowBlur = 8;
    private double _shadowOpacity = 0.4;
    private double _cornerRadius = 12;

    public void SetSize(int size) => _size = size;
    public void SetOffset(int x, int y) { _ = x; _ = y; }
    public void SetColors(string primary, string? gradientEnd) { _primary = primary; _gradientEnd = gradientEnd; }
    public void SetShadow(double blur, double opacity) { _shadowBlur = blur; _shadowOpacity = opacity; }
    public void SetCornerRadius(double radius) => _cornerRadius = radius;
    public int Size => _size;
    public string PrimaryColor => _primary;
    public string? GradientEnd => _gradientEnd;
    public double ShadowBlur => _shadowBlur;
    public double ShadowOpacity => _shadowOpacity;
    public double CornerRadius => _cornerRadius;
}

public sealed class CalendarEditor : ICalendarEditor
{
    public string MonthFontFamily { get; private set; } = "Segoe UI";
    public double MonthFontSize { get; private set; } = 11;
    public string MonthColor { get; private set; } = "#FFFFFF";
    public string DayFontFamily { get; private set; } = "Segoe UI";
    public double DayFontSize { get; private set; } = 24;
    public string DayColor { get; private set; } = "#FFFFFF";
    public double Rotation { get; private set; }

    public void SetMonthFont(string family, double size, string color)
    {
        MonthFontFamily = family;
        MonthFontSize = size;
        MonthColor = color;
    }

    public void SetDayFont(string family, double size, string color)
    {
        DayFontFamily = family;
        DayFontSize = size;
        DayColor = color;
    }

    public void SetRotation(double degrees) => Rotation = degrees;
}
