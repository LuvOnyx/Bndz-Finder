using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;
using BndzFinder.Core.Services;

namespace BndzFinder.Interop;

/// <summary>
/// Live WMI / performance-counter metrics for MyFinder widgets.
/// </summary>
public sealed class WindowsSystemMetricsService : ISystemMetricsService
{
    private static PerformanceCounter? _cpuCounter;
    private static long _lastNetBytes;
    private static DateTimeOffset _lastNetSample = DateTimeOffset.MinValue;

    public Task<double> GetCpuUsageAsync()
    {
        if (!OperatingSystem.IsWindows()) return Task.FromResult(0.0);
        try
        {
            _cpuCounter ??= new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue();
            return Task.FromResult((double)Math.Clamp(_cpuCounter.NextValue(), 0, 100));
        }
        catch
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                    return Task.FromResult(Convert.ToDouble(obj["LoadPercentage"]));
            }
            catch { }
            return Task.FromResult(0.0);
        }
    }

    public Task<double> GetMemoryUsageAsync()
    {
        if (!OperatingSystem.IsWindows()) return Task.FromResult(0.0);
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT TotalVisibleMemorySize,FreePhysicalMemory FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                var total = Convert.ToDouble(obj["TotalVisibleMemorySize"]);
                var free = Convert.ToDouble(obj["FreePhysicalMemory"]);
                return Task.FromResult(total > 0 ? (total - free) / total * 100 : 0);
            }
        }
        catch { }
        return Task.FromResult(0.0);
    }

    public Task<double> GetGpuUsageAsync()
    {
        if (!OperatingSystem.IsWindows()) return Task.FromResult(0.0);
        try
        {
            using var gpuSearcher = new ManagementObjectSearcher(
                "SELECT AdapterRAM FROM Win32_VideoController WHERE AdapterRAM IS NOT NULL");
            var adapters = 0;
            foreach (ManagementObject obj in gpuSearcher.Get())
            {
                if (obj["AdapterRAM"] is not null) adapters++;
            }

            try
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var total = 0.0;
                var count = 0;
                foreach (var instance in category.GetInstanceNames())
                {
                    if (!instance.Contains("engtype_3D", StringComparison.OrdinalIgnoreCase)) continue;
                    using var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instance);
                    total += (double)Math.Clamp(counter.NextValue(), 0, 100);
                    count++;
                }

                if (count > 0) return Task.FromResult(total / count);
            }
            catch { }

            return Task.FromResult(0.0);
        }
        catch
        {
            return Task.FromResult(0.0);
        }
    }

    public Task<double> GetDiskUsageAsync()
    {
        if (!OperatingSystem.IsWindows()) return Task.FromResult(0.0);
        try
        {
            var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase))
                ?? DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady);
            if (drive is null || drive.TotalSize <= 0) return Task.FromResult(0.0);
            var used = drive.TotalSize - drive.AvailableFreeSpace;
            return Task.FromResult(used / (double)drive.TotalSize * 100);
        }
        catch
        {
            return Task.FromResult(0.0);
        }
    }

    public Task<double> GetNetworkKbpsAsync()
    {
        if (!OperatingSystem.IsWindows()) return Task.FromResult(0.0);
        try
        {
            var bytes = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                            && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(n => n.GetIPv4Statistics())
                .Sum(s => s.BytesReceived + s.BytesSent);

            var now = DateTimeOffset.UtcNow;
            if (_lastNetSample == DateTimeOffset.MinValue)
            {
                _lastNetBytes = bytes;
                _lastNetSample = now;
                return Task.FromResult(0.0);
            }

            var elapsed = (now - _lastNetSample).TotalSeconds;
            if (elapsed < 0.5) return Task.FromResult(0.0);

            var delta = Math.Max(0, bytes - _lastNetBytes);
            _lastNetBytes = bytes;
            _lastNetSample = now;
            return Task.FromResult(delta * 8 / elapsed / 1000.0);
        }
        catch
        {
            return Task.FromResult(0.0);
        }
    }

    public Task<int> GetBatteryPercentAsync()
    {
        if (!OperatingSystem.IsWindows()) return Task.FromResult(100);
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT EstimatedChargeRemaining FROM Win32_Battery");
            foreach (ManagementObject obj in searcher.Get())
                return Task.FromResult(Convert.ToInt32(obj["EstimatedChargeRemaining"]));
        }
        catch { }
        return Task.FromResult(100);
    }

    public Task<string> GetBatteryTimeRemainingAsync()
    {
        if (!OperatingSystem.IsWindows()) return Task.FromResult(string.Empty);
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT EstimatedRunTime,BatteryStatus FROM Win32_Battery");
            foreach (ManagementObject obj in searcher.Get())
            {
                var minutes = Convert.ToInt32(obj["EstimatedRunTime"]);
                if (minutes is <= 0 or >= 71582788) return Task.FromResult(string.Empty);
                if (minutes < 60) return Task.FromResult($"{minutes}m");
                return Task.FromResult($"{minutes / 60}h {minutes % 60}m");
            }
        }
        catch { }
        return Task.FromResult(string.Empty);
    }
}
