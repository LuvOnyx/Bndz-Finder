using BndzFinder.Interop;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;

namespace BndzFinder.Dock.Controls;

public sealed partial class DwmPreviewHost : UserControl
{
    private IWindowPreviewService? _preview;
    private nint _destHwnd;
    private nint _thumbnail;
    private long _pendingSourceHwnd;
    private int _previewWidth = 320;
    private int _previewHeight = 200;

    public DwmPreviewHost()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public void Configure(IWindowPreviewService preview, int width, int height)
    {
        _preview = preview;
        _previewWidth = Math.Max(160, width);
        _previewHeight = Math.Max(120, height);
        Width = _previewWidth;
        Height = _previewHeight;
        HostSurface.Width = _previewWidth;
        HostSurface.Height = _previewHeight;
    }

    public void ShowPreview(long sourceHwnd)
    {
        _pendingSourceHwnd = sourceHwnd;
        if (_destHwnd == nint.Zero || _preview is null || sourceHwnd == 0)
            return;

        HidePreview();
        _thumbnail = _preview.RegisterThumbnail(_destHwnd, (nint)sourceHwnd);
        if (_thumbnail != nint.Zero)
            _preview.UpdateThumbnail(_thumbnail, 0, 0, _previewWidth, _previewHeight, 255);
    }

    public void HidePreview()
    {
        if (_thumbnail == nint.Zero || _preview is null)
            return;
        _preview.UnregisterThumbnail(_thumbnail);
        _thumbnail = nint.Zero;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _destHwnd = WindowNative.GetWindowHandle(this);
        if (_pendingSourceHwnd != 0)
            ShowPreview(_pendingSourceHwnd);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => HidePreview();
}
