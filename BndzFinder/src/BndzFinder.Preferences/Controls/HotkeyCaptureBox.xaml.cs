using BndzFinder.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace BndzFinder.Preferences.Controls;

public sealed partial class HotkeyCaptureBox : UserControl
{
    public static readonly DependencyProperty BindingProperty =
        DependencyProperty.Register(nameof(Binding), typeof(HotkeyBinding), typeof(HotkeyCaptureBox),
            new PropertyMetadata(null, (_, e) => ((HotkeyCaptureBox)_).UpdateDisplay()));

    public static readonly DependencyProperty IsRecordingProperty =
        DependencyProperty.Register(nameof(IsRecording), typeof(bool), typeof(HotkeyCaptureBox),
            new PropertyMetadata(false, (_, e) => ((HotkeyCaptureBox)_).UpdateRecordingState()));

    public HotkeyBinding? Binding
    {
        get => (HotkeyBinding?)GetValue(BindingProperty);
        set => SetValue(BindingProperty, value);
    }

    public bool IsRecording
    {
        get => (bool)GetValue(IsRecordingProperty);
        set => SetValue(IsRecordingProperty, value);
    }

    public event EventHandler<HotkeyBinding>? BindingCaptured;

    public HotkeyCaptureBox()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
        LostFocus += (_, _) => IsRecording = false;
    }

    private void OnRecordClick(object sender, RoutedEventArgs e)
    {
        IsRecording = true;
        DisplayBox.Text = "Press keys…";
        Focus(FocusState.Programmatic);
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        if (Binding is null) return;
        Binding.Modifiers = null;
        Binding.Key = null;
        UpdateDisplay();
        BindingCaptured?.Invoke(this, Binding);
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (!IsRecording || Binding is null) return;

        e.Handled = true;
        if (e.Key is VirtualKey.Escape)
        {
            IsRecording = false;
            UpdateDisplay();
            return;
        }

        if (e.Key is VirtualKey.Control or VirtualKey.Shift or VirtualKey.Menu
            or VirtualKey.LeftWindows or VirtualKey.RightWindows)
            return;

        var mods = new List<string>();
        var state = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        if (state.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down)) mods.Add("Ctrl");
        state = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
        if (state.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down)) mods.Add("Shift");
        state = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu);
        if (state.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down)) mods.Add("Alt");
        state = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.LeftWindows);
        if (state.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down)) mods.Add("Win");
        else
        {
            state = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.RightWindows);
            if (state.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down)) mods.Add("Win");
        }

        Binding.Modifiers = mods.Count == 0 ? null : string.Join("+", mods);
        Binding.Key = e.Key.ToString();
        IsRecording = false;
        UpdateDisplay();
        BindingCaptured?.Invoke(this, Binding);
    }

    private void UpdateDisplay()
    {
        if (Binding is null)
        {
            DisplayBox.Text = "(none)";
            return;
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(Binding.Modifiers)) parts.Add(Binding.Modifiers);
        if (!string.IsNullOrWhiteSpace(Binding.Key)) parts.Add(Binding.Key);
        DisplayBox.Text = parts.Count == 0 ? "(none)" : string.Join(" + ", parts);
    }

    private void UpdateRecordingState() =>
        RecordButton.Content = IsRecording ? "Listening…" : "Record";
}
