using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;

namespace BndzFinder.App;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Unpackaged WinUI requires self-contained Windows App SDK bootstrap before XAML loads.
        if (!Bootstrap.TryInitialize(0x00010006, out _))
            Bootstrap.Initialize(0x00010006);

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ =>
        {
            var syncContext = new DispatcherQueueSynchronizationContext(
                DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(syncContext);
            _ = new App();
        });
    }
}
