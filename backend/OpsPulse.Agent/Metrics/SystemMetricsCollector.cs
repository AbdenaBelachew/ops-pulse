using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace OpsPulse.Agent.Metrics;

public sealed record SystemSample(
    double CpuUsagePercent,
    long MemoryUsedMb,
    long MemoryTotalMb,
    double? DiskUsedPercent,
    double? NetworkInMbps,
    double? NetworkOutMbps);

/// <summary>
/// Cross-platform system metric collector. Windows uses performance counters;
/// Linux parses /proc; other platforms report zeros (still heartbeats fine).
/// </summary>
public sealed class SystemMetricsCollector(ILogger<SystemMetricsCollector> logger)
{
    private PerformanceCounter? _cpuCounter;
    private readonly Dictionary<string, (long TotalBytes, DateTime At)> _lastNetworkBytes = [];
    private DateTime? _lastCpuSampleUtc;
    private TimeSpan _lastCpuIdle;
    private TimeSpan _lastCpuTotal;

    public SystemSample Collect()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                return CollectWindows();
            }

            if (OperatingSystem.IsLinux())
            {
                return CollectLinux();
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Metric collection failed; reporting zeros");
        }

        return new SystemSample(0, 0, 0, null, null, null);
    }

    private SystemSample CollectWindows()
    {
        _cpuCounter ??= new PerformanceCounter("Processor", "% Processor Time", "_Total", readOnly: true);
        _cpuCounter.NextValue(); // first call always returns 0
        Thread.Sleep(250);
        var cpu = _cpuCounter.NextValue();

        var (memoryUsedMb, memoryTotalMb) = GetWindowsMemory();
        return new SystemSample(
            Math.Round(cpu, 1),
            memoryUsedMb,
            memoryTotalMb,
            GetDiskUsedPercent(),
            null,
            null);
    }

    private static (long UsedMb, long TotalMb) GetWindowsMemory()
    {
        var memStatus = new NativeMethods.MEMORYSTATUSEX { dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MEMORYSTATUSEX>() };
        NativeMethods.GlobalMemoryStatusEx(ref memStatus);
        var totalMb = (long)(memStatus.ullTotalPhys / (1024 * 1024));
        var usedMb = (long)((memStatus.ullTotalPhys - memStatus.ullAvailPhys) / (1024 * 1024));
        return (usedMb, totalMb);
    }

    private SystemSample CollectLinux()
    {
        var cpu = GetLinuxCpuPercent();
        var (memoryUsedMb, memoryTotalMb) = GetLinuxMemory();
        var (inMbps, outMbps) = GetLinuxNetworkMbps();
        return new SystemSample(cpu, memoryUsedMb, memoryTotalMb, GetDiskUsedPercent(), inMbps, outMbps);
    }

    private double GetLinuxCpuPercent()
    {
        var lines = File.ReadAllLines("/proc/stat");
        var cpuLine = lines.FirstOrDefault(l => l.StartsWith("cpu ", StringComparison.Ordinal));
        if (cpuLine is null)
        {
            return 0;
        }

        var parts = cpuLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var idle = TimeSpan.FromMilliseconds(double.Parse(parts[4]) * 1000 / Environment.TickCount64 * 0 + double.Parse(parts[4]) * 10); // jiffies ≈ 10ms
        var total = TimeSpan.FromMilliseconds(parts.Skip(1).Take(8).Sum(double.Parse) * 10);

        var now = DateTime.UtcNow;
        if (_lastCpuSampleUtc is not { } last || (now - last).TotalSeconds < 0.5)
        {
            _lastCpuSampleUtc = now;
            _lastCpuIdle = idle;
            _lastCpuTotal = total;
            Thread.Sleep(300);
            return GetLinuxCpuPercent();
        }

        var idleDelta = (idle - _lastCpuIdle).TotalMilliseconds;
        var totalDelta = (total - _lastCpuTotal).TotalMilliseconds;
        _lastCpuSampleUtc = now;
        _lastCpuIdle = idle;
        _lastCpuTotal = total;

        return totalDelta <= 0 ? 0 : Math.Round((1 - idleDelta / totalDelta) * 100, 1);
    }

    private static (long UsedMb, long TotalMb) GetLinuxMemory()
    {
        var values = File.ReadAllLines("/proc/meminfo")
            .Select(l => l.Split(':', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim(), p => long.Parse(p[1].Trim().Split(' ')[0]));

        var totalKb = values.GetValueOrDefault("MemTotal");
        var availableKb = values.GetValueOrDefault("MemAvailable", values.GetValueOrDefault("MemFree"));
        return ((totalKb - availableKb) / 1024, totalKb / 1024);
    }

    private (double? InMbps, double? OutMbps) GetLinuxNetworkMbps()
    {
        var lines = File.ReadAllLines("/proc/net/dev").Skip(2);
        long totalRx = 0;
        long totalTx = 0;
        foreach (var line in lines)
        {
            var parts = line.Split(':', 2);
            if (parts.Length < 2 || parts[0].Trim() is "lo")
            {
                continue;
            }

            var fields = parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            totalRx += long.Parse(fields[0]);
            totalTx += long.Parse(fields[8]);
        }

        var now = DateTime.UtcNow;
        double? inMbps = null;
        double? outMbps = null;

        if (_lastNetworkBytes.TryGetValue("total", out var previous) && (now - previous.At).TotalSeconds > 0.5)
        {
            var seconds = (now - previous.At).TotalSeconds;
            inMbps = Math.Max(0, (totalRx - previous.TotalBytes) * 8 / seconds / 1_000_000);
            outMbps = Math.Max(0, (totalTx - previous.TotalBytes) * 8 / seconds / 1_000_000);
        }

        _lastNetworkBytes["total"] = (totalRx + totalTx, now);
        return (inMbps, outMbps);
    }

    private static double? GetDiskUsedPercent()
    {
        try
        {
            var root = DriveInfo.GetDrives()
                .Where(d => d.IsReady && (d.DriveType == DriveType.Fixed || d.Name == "/"))
                .OrderByDescending(d => d.TotalSize)
                .FirstOrDefault();

            if (root is null || root.TotalSize == 0)
            {
                return null;
            }

            return Math.Round((root.TotalSize - root.AvailableFreeSpace) * 100.0 / root.TotalSize, 1);
        }
        catch
        {
            return null;
        }
    }

    public static string? GetPrimaryIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)?.ToString();
        }
        catch
        {
            return null;
        }
    }

    public static string GetHostName() => Environment.MachineName.ToLowerInvariant();

    public static (string OsName, string OsVersion) GetOs()
    {
        if (OperatingSystem.IsWindows())
        {
            return ("Windows", Environment.OSVersion.VersionString);
        }

        if (OperatingSystem.IsLinux())
        {
            return ("Linux", Environment.OSVersion.VersionString);
        }

        if (OperatingSystem.IsMacOS())
        {
            return ("macOS", Environment.OSVersion.VersionString);
        }

        return (Environment.OSVersion.Platform.ToString(), Environment.OSVersion.VersionString);
    }

    public static long GetTotalMemoryMbWindows()
    {
        var memStatus = new NativeMethods.MEMORYSTATUSEX { dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MEMORYSTATUSEX>() };
        NativeMethods.GlobalMemoryStatusEx(ref memStatus);
        return (long)(memStatus.ullTotalPhys / (1024 * 1024));
    }
}

internal static class NativeMethods
{
    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }
}
