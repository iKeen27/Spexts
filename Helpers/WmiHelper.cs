using System.Management;

namespace Spexts.Helpers;

public static class WmiHelper
{
    public static string QuerySingle(string className, string property, string scope = @"root\cimv2")
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(scope, $"SELECT {property} FROM {className}");
            using var results = searcher.Get();
            foreach (ManagementObject obj in results)
            {
                using (obj)
                {
                    var val = obj[property];
                    if (val != null)
                    {
                        string str = val.ToString()?.Trim() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(str))
                            return str;
                    }
                }
            }
            return "N/A";
        }
        catch
        {
            return "N/A";
        }
    }

    public static List<Dictionary<string, string>> QueryMultiple(
        string className, string[] properties, string scope = @"root\cimv2")
    {
        var results = new List<Dictionary<string, string>>();
        try
        {
            var propList = string.Join(",", properties);
            using var searcher = new ManagementObjectSearcher(scope, $"SELECT {propList} FROM {className}");
            using var queryResults = searcher.Get();
            foreach (ManagementObject obj in queryResults)
            {
                using (obj)
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var prop in properties)
                    {
                        try
                        {
                            var val = obj[prop];
                            dict[prop] = val?.ToString()?.Trim() ?? "N/A";
                        }
                        catch
                        {
                            dict[prop] = "N/A";
                        }
                    }
                    results.Add(dict);
                }
            }
        }
        catch { }
        return results;
    }

    public static string FormatBytes(object? bytes)
    {
        try
        {
            ulong b = Convert.ToUInt64(bytes);
            string[] sizes = ["B", "KB", "MB", "GB", "TB"];
            double len = b;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:F1} {sizes[order]}";
        }
        catch { return "N/A"; }
    }

    public static string FormatCapacityGB(object? bytes)
    {
        try
        {
            ulong b = Convert.ToUInt64(bytes);
            double gb = b / (1024.0 * 1024.0 * 1024.0);
            return $"{gb:F0} GB";
        }
        catch { return "N/A"; }
    }
}
