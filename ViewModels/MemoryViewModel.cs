using System.Collections.ObjectModel;
using System.Management;
using System.Windows;
using Spexts.Helpers;
using Spexts.Models;

namespace Spexts.ViewModels;

public class MemoryViewModel : BaseViewModel
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
            var sticks = WmiHelper.QueryMultiple("Win32_PhysicalMemory",
                ["Capacity", "Speed", "FormFactor", "Manufacturer", "PartNumber"]);

            ulong totalCapacity = 0;
            int slotCount = sticks.Count;

            foreach (var stick in sticks)
            {
                if (ulong.TryParse(stick.GetValueOrDefault("Capacity", "0"), out ulong cap))
                    totalCapacity += cap;
            }

            double totalGB = totalCapacity / (1024.0 * 1024.0 * 1024.0);
            rows.Add(new InfoRow("Total Capacity", $"{totalGB:F0} GB"));
            rows.Add(new InfoRow("Slots Used", $"{slotCount}"));

            if (sticks.Count > 0)
            {
                var speed = sticks[0].GetValueOrDefault("Speed", "N/A");

                // Apply RAM Speed color rule via TweakAdvisor
                string? speedColor = null;
                if (speed != "N/A" && int.TryParse(speed, out int speedMhz))
                {
                    speedColor = TweakAdvisor.ColorForRamSpeed(speedMhz);
                    rows.Add(new InfoRow("Speed", $"{speed} MHz", speedColor));
                }
                else
                {
                    rows.Add(new InfoRow("Speed", speed != "N/A" ? $"{speed} MHz" : "N/A"));
                }

                var formFactor = sticks[0].GetValueOrDefault("FormFactor", "0");
                string ff = formFactor switch
                {
                    "8"  => "DIMM",
                    "12" => "SO-DIMM",
                    "9"  => "RIMM",
                    _    => $"Type {formFactor}"
                };
                rows.Add(new InfoRow("Form Factor", ff));

                var mfr = sticks[0].GetValueOrDefault("Manufacturer", "N/A");
                if (!string.IsNullOrWhiteSpace(mfr) && mfr != "N/A")
                    rows.Add(new InfoRow("Manufacturer", mfr));

                var part = sticks[0].GetValueOrDefault("PartNumber", "N/A");
                if (!string.IsNullOrWhiteSpace(part) && part != "N/A")
                    rows.Add(new InfoRow("Part Number", part.Trim()));
            }
        }
        catch
        {
            rows.Add(new InfoRow("Total Capacity", "N/A"));
        }

        // Get RAM usage
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
            using var results = searcher.Get();
            foreach (ManagementObject obj in results)
            {
                using (obj)
                {
                    if (ulong.TryParse(obj["TotalVisibleMemorySize"]?.ToString(), out ulong totalKB) &&
                        ulong.TryParse(obj["FreePhysicalMemory"]?.ToString(), out ulong freeKB))
                    {
                        double totalGb = totalKB / (1024.0 * 1024.0);
                        double freeGb = freeKB / (1024.0 * 1024.0);
                        double usedGb = totalGb - freeGb;

                        rows.Add(new InfoRow("Used / Free",
                            $"{usedGb:F1} GB used / {freeGb:F1} GB free"));
                    }
                }
                break;
            }
        }
        catch { }

        Application.Current.Dispatcher.Invoke(() => Rows = rows);
    }
}
