namespace BndzFinder.Core.Services;

public interface ISystemMetricsService
{
    Task<double> GetCpuUsageAsync();
    Task<double> GetMemoryUsageAsync();
    Task<double> GetGpuUsageAsync();
    Task<double> GetDiskUsageAsync();
    Task<double> GetNetworkKbpsAsync();
    Task<int> GetBatteryPercentAsync();
    Task<string> GetBatteryTimeRemainingAsync();
}
