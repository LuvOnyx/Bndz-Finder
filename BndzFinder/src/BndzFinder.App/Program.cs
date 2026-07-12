using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;

namespace BndzFinder.App;

public static class Program
{
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
            if (!Bootstrap.TryInitialize(0x00010006, out _))
                Bootstrap.Initialize(0x00010006);

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
