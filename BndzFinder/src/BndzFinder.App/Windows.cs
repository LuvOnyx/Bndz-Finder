using BndzFinder.Core.Services;
using BndzFinder.Dock.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace BndzFinder.App;

public sealed class DockWindow : Window
{
    public DockWindow()
    {
        Title = "Bndz-Finder Dock";
        var root = new Microsoft.UI.Xaml.Controls.Grid();
        Content = root;

        var presenter = AppWindow.Presenter as OverlappedPresenter;
        if (presenter is not null)
        {
            presenter.IsResizable = false;
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsAlwaysOnTop = true;
        }

        AppWindow.IsShownInSwitchers = false;
        if (AppWindow.TitleBar is not null)
        {
            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        }

        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();

        var hwnd = WindowNative.GetWindowHandle(this);
        _ = hwnd;
    }
}

public sealed class FinderWindow : Window
{
    public FinderWindow()
    {
        Title = "Bndz-Finder Finder";
        Content = new Microsoft.UI.Xaml.Controls.Grid { Height = 28 };
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        AppWindow.IsShownInSwitchers = false;
    }
}
