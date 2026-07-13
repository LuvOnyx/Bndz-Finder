using System.IO.Compression;
using System.Text.Json;
using BndzFinder.Core.Models;
using BndzFinder.Core.Settings;

namespace BndzFinder.Core.Services;

public interface IBackupService
{
    Task<string> BackupAsync(CancellationToken cancellationToken = default);
    Task RestoreAsync(string backupPath, CancellationToken cancellationToken = default);
}

public sealed class BackupService : IBackupService
{
    private readonly ISettingsService _settings;

    public BackupService(ISettingsService settings) => _settings = settings;

    public async Task<string> BackupAsync(CancellationToken cancellationToken = default)
    {
        var backupDir = Path.Combine(Path.GetTempPath(), "BndzFinder", "backups");
        Directory.CreateDirectory(backupDir);
        var zipPath = Path.Combine(backupDir, $"backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip");
        var staging = Path.Combine(Path.GetTempPath(), $"bndz-staging-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(staging, "settings.json"),
                JsonSerializer.Serialize(_settings.Current, new JsonSerializerOptions { WriteIndented = true }),
                cancellationToken).ConfigureAwait(false);

            var dockIconsDir = Path.Combine(staging, "dock-icons");
            Directory.CreateDirectory(dockIconsDir);

            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(staging, zipPath);
            return zipPath;
        }
        finally
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
        }
    }

    public async Task RestoreAsync(string backupPath, CancellationToken cancellationToken = default)
    {
        var staging = Path.Combine(Path.GetTempPath(), $"bndz-restore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);
        try
        {
            ZipFile.ExtractToDirectory(backupPath, staging);
            var settingsPath = Path.Combine(staging, "settings.json");
            if (File.Exists(settingsPath))
            {
                var json = await File.ReadAllTextAsync(settingsPath, cancellationToken).ConfigureAwait(false);
                var settings = JsonSerializer.Deserialize<BndzFinderSettings>(json);
                if (settings is not null)
                {
                    await _settings.ReplaceCurrentAsync(settings, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
        }
    }
}

public interface IHotCornerService
{
    void Register(HotCornerBinding binding, Action<HotCornerAction> handler);
}

public sealed class HotCornerService : IHotCornerService
{
    public void Register(HotCornerBinding binding, Action<HotCornerAction> handler)
    {
        _ = binding;
        _ = handler;
    }
}

public interface IStartupService
{
    Task ConfigureAsync(StartupMode mode, CancellationToken cancellationToken = default);
}

public sealed class StartupService : IStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "BndzFinder";

    public Task ConfigureAsync(StartupMode mode, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows()) return Task.CompletedTask;

        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key is null) return Task.CompletedTask;

        if (mode == StartupMode.None)
        {
            key.DeleteValue(RunValueName, throwOnMissingValue: false);
            return Task.CompletedTask;
        }

        var launcher = ResolveLauncherCommand();
        if (string.IsNullOrWhiteSpace(launcher)) return Task.CompletedTask;
        key.SetValue(RunValueName, launcher);
        return Task.CompletedTask;
    }

    private static string? ResolveLauncherCommand()
    {
        var runScript = Path.Combine(AppContext.BaseDirectory, "run.cmd");
        if (File.Exists(runScript))
            return $"\"{runScript}\"";

        var shellHost = Path.Combine(AppContext.BaseDirectory, "BndzFinder.ShellHost.exe");
        var app = Path.Combine(AppContext.BaseDirectory, "BndzFinder.App.exe");
        if (File.Exists(shellHost) && File.Exists(app))
            return $"\"{shellHost}\" & \"{app}\"";

        return null;
    }
}
