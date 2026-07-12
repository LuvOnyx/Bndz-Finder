using BndzFinder.Core.Models;
using BndzFinder.Core.Settings;

namespace BndzFinder.Core.Services;

public interface IHotkeySyncService
{
    void ApplyFromSettings(BndzFinderSettings settings, IHotkeyBindingRegistrar registrar);
}

public interface IHotkeyBindingRegistrar
{
    void Register(HotkeyBinding binding, Action handler);
    void Unregister(string bindingId);
}

public sealed class HotkeySyncService : IHotkeySyncService
{
    public void ApplyFromSettings(BndzFinderSettings settings, IHotkeyBindingRegistrar registrar)
    {
        ApplyBinding(registrar, settings.DockHotkey, () => { });
        ApplyBinding(registrar, settings.FinderHotkey, () => { });
        ApplyBinding(registrar, settings.LaunchpadHotkey, () => { });
        ApplyBinding(registrar, settings.StageManagerHotkey, () => { });
    }

    private static void ApplyBinding(IHotkeyBindingRegistrar registrar, HotkeyBinding binding, Action handler)
    {
        if (string.IsNullOrWhiteSpace(binding.Key))
        {
            registrar.Unregister(binding.Id);
            return;
        }
        registrar.Register(binding, handler);
    }
}
