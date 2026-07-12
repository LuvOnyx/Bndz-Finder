namespace BndzFinder.Theming.Editors;

public interface IThemeEditor
{
    void SetDockBackground(string? imagePath, double blur, double opacity, double margin);
    void SetProgressBarStyle(string borderStyle, string backgroundStyle);
    void SetRunIndicatorStyle(string style, int offset);
}

public sealed class ThemeEditor : IThemeEditor
{
    public void SetDockBackground(string? imagePath, double blur, double opacity, double margin)
    {
        _ = imagePath; _ = blur; _ = opacity; _ = margin;
    }

    public void SetProgressBarStyle(string borderStyle, string backgroundStyle)
    {
        _ = borderStyle; _ = backgroundStyle;
    }

    public void SetRunIndicatorStyle(string style, int offset)
    {
        _ = style; _ = offset;
    }
}

public interface IIconDesigner
{
    void SetSize(int size);
    void SetOffset(int x, int y);
    void SetColors(string primary, string? gradientEnd);
    void SetShadow(double blur, double opacity);
    void SetCornerRadius(double radius);
}

public sealed class IconDesigner : IIconDesigner
{
    private int _size = 48;
    public void SetSize(int size) => _size = size;
    public void SetOffset(int x, int y) { _ = x; _ = y; }
    public void SetColors(string primary, string? gradientEnd) { _ = primary; _ = gradientEnd; }
    public void SetShadow(double blur, double opacity) { _ = blur; _ = opacity; }
    public void SetCornerRadius(double radius) => _ = radius;
    public int Size => _size;
}

public interface ICalendarEditor
{
    void SetMonthFont(string family, double size, string color);
    void SetDayFont(string family, double size, string color);
    void SetRotation(double degrees);
}

public sealed class CalendarEditor : ICalendarEditor
{
    public void SetMonthFont(string family, double size, string color) { _ = family; _ = size; _ = color; }
    public void SetDayFont(string family, double size, string color) { _ = family; _ = size; _ = color; }
    public void SetRotation(double degrees) => _ = degrees;
}
