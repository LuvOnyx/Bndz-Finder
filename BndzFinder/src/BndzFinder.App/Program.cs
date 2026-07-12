using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using Microsoft.WindowsAppSDK;

namespace BndzFinder.App;

public static class Program
{
    private const string RuntimeBaseDirectoryVar = "MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY";

    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                StartupErrorReporter.Report(ex, "AppDomain");
        };

        try
        {
            // Self-contained unpackaged WinUI must point bootstrap at bundled runtime DLLs.
            Environment.SetEnvironmentVariable(RuntimeBaseDirectoryVar, AppContext.BaseDirectory);

            if (!Bootstrap.TryInitialize(Release.MajorMinor, Release.VersionTag, out var hr))
            {
                var runtimeDll = Path.Combine(AppContext.BaseDirectory, "Microsoft.ui.xaml.dll");
                var message = File.Exists(runtimeDll)
                    ? $"Windows App SDK bootstrap failed (0x{hr:X8}). Rebuild with .\\run.cmd or install the Windows App SDK 1.6 runtime."
                    : $"Windows App SDK runtime DLLs are missing next to the app exe.{Environment.NewLine}" +
                      $"Expected: {runtimeDll}{Environment.NewLine}" +
                      $"Delete src\\BndzFinder.App\\bin and obj, then run .\\run.cmd to rebuild.";
                throw new InvalidOperationException(message);
            }

            WinRT.ComWrappersSupport.InitializeComWrappers();
            Application.Start(p =>
            {
                var syncContext = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(syncContext);
                new App();
            });
        }
        catch (Exception ex)
        {
            StartupErrorReporter.Report(ex, "Main");
            Environment.Exit(1);
        }
    }
}
