namespace BndzFinder.App;

internal static class StartupErrorReporter
{
    public static void Report(Exception ex, string source = "startup")
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BndzFinder");
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, "startup.log");
        File.AppendAllText(logPath, $"[{DateTime.Now:O}] [{source}]{Environment.NewLine}{ex}{Environment.NewLine}");

        if (OperatingSystem.IsWindows())
        {
            _ = MessageBox(
                nint.Zero,
                $"{ex.Message}{Environment.NewLine}{Environment.NewLine}Details: {logPath}",
                "Bndz-Finder could not start",
                0x10);
        }
    }

    public static void ReportMessage(string message, string source = "startup")
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BndzFinder");
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, "startup.log");
        File.AppendAllText(logPath, $"[{DateTime.Now:O}] [{source}] {message}{Environment.NewLine}");
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int MessageBox(nint hWnd, string text, string caption, uint type);
}
