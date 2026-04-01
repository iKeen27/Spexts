using System.Collections.ObjectModel;
using System.Windows;
using Spexts.Helpers;
using Spexts.Models;

namespace Spexts.ViewModels;

public class StorageViewModel : BaseViewModel
{
    private ObservableCollection<InfoRow> _rows = new();
    public ObservableCollection<InfoRow> Rows
    {
        get => _rows;
        set => SetProperty(ref _rows, value);
    }

    public void Load()
    {
        var rows = new ObservableCollection<InfoRow>();

        try
        {
            // Get physical disks for media type
            var physicalDisks = WmiHelper.QueryMultiple("MSFT_PhysicalDisk",
                ["FriendlyName", "MediaType", "Size"],
                @"root\Microsoft\Windows\Storage");

            var mediaTypes = new Dictionary<string, string>();
            foreach (var disk in physicalDisks)
            {
                var name = disk.GetValueOrDefault("FriendlyName", "");
                var mediaType = disk.GetValueOrDefault("MediaType", "0") switch
                {
                    "3" => "HDD",
                    "4" => "SSD",
                    "5" => "SCM",
                    _ => "Unknown"
                };
                if (!string.IsNullOrEmpty(name))
                    mediaTypes[name] = mediaType;
            }

            // Get logical disks
            var logicalDisks = WmiHelper.QueryMultiple("Win32_LogicalDisk",
                ["DeviceID", "VolumeName", "Size", "FreeSpace", "DriveType"]);

            foreach (var disk in logicalDisks)
            {
                var driveType = disk.GetValueOrDefault("DriveType", "0");
                if (driveType != "3") continue; // Only local disks

                var letter = disk.GetValueOrDefault("DeviceID", "?");
                var name = disk.GetValueOrDefault("VolumeName", "Local Disk");
                if (string.IsNullOrWhiteSpace(name)) name = "Local Disk";

                var sizeStr = disk.GetValueOrDefault("Size", "0");
                var freeStr = disk.GetValueOrDefault("FreeSpace", "0");

                ulong.TryParse(sizeStr, out ulong size);
                ulong.TryParse(freeStr, out ulong free);

                double sizeGB = size / (1024.0 * 1024.0 * 1024.0);
                double freeGB = free / (1024.0 * 1024.0 * 1024.0);
                double usedPct = size > 0 ? ((size - free) * 100.0 / size) : 0;

                // Find media type
                string type = "Unknown";
                foreach (var kvp in mediaTypes)
                {
                    type = kvp.Value; // Use first/best match
                    break;
                }

                rows.Add(new InfoRow($"{letter} ({name})",
                    $"{type} — {sizeGB:F0} GB total, {freeGB:F0} GB free ({usedPct:F0}% used)"));
            }

            if (rows.Count == 0)
                rows.Add(new InfoRow("Storage", "No local drives detected"));
        }
        catch
        {
            rows.Add(new InfoRow("Storage", "N/A"));
        }

        Application.Current.Dispatcher.Invoke(() => Rows = rows);
    }
}
