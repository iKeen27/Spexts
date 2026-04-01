using System.Collections.ObjectModel;
using System.Windows;
using LibreHardwareMonitor.Hardware;
using Spexts.Helpers;
using Spexts.Models;

namespace Spexts.ViewModels;

public class GpuViewModel : BaseViewModel
{
    private ObservableCollection<InfoRow> _rows = new();
    public ObservableCollection<InfoRow> Rows
    {
        get => _rows;
        set => SetProperty(ref _rows, value);
    }


    private InfoRow? _liveTempRow;

    public void Load(Computer? computer)
    {
        var rows = new ObservableCollection<InfoRow>();

        try
        {
            var gpus = WmiHelper.QueryMultiple("Win32_VideoController",
                ["Name", "AdapterRAM", "DriverVersion", "VideoProcessor"]);

            if (gpus.Count > 0)
            {
                var gpu = gpus[0];
                rows.Add(new InfoRow("GPU Name", gpu.GetValueOrDefault("Name", "N/A")));

                var vramStr = gpu.GetValueOrDefault("AdapterRAM", "0");
                if (long.TryParse(vramStr, out long vramBytes) && vramBytes > 0)
                {
                    double vramGB = vramBytes / (1024.0 * 1024.0 * 1024.0);
                    // WMI caps at 4GB for AdapterRAM, try LHM for accurate value
                    rows.Add(new InfoRow("VRAM", vramGB >= 4 ? $"{vramGB:F0} GB (WMI limit — may be higher)" : $"{vramGB:F1} GB"));
                }
                else
                    rows.Add(new InfoRow("VRAM", "N/A"));

                rows.Add(new InfoRow("Driver Version", gpu.GetValueOrDefault("DriverVersion", "N/A")));
            }
            else
            {
                rows.Add(new InfoRow("GPU Name", "N/A"));
            }
        }
        catch
        {
            rows.Add(new InfoRow("GPU Name", "N/A"));
        }

        _liveTempRow = new InfoRow("GPU Temperature", "Scanning...");
        rows.Add(_liveTempRow);

        // Try to get accurate VRAM from LHM
        TryGetLhmGpuInfo(computer, rows);

        Application.Current.Dispatcher.Invoke(() => Rows = rows);
        UpdateLive(computer);
    }

    private void TryGetLhmGpuInfo(Computer? computer, ObservableCollection<InfoRow> rows)
    {
        if (computer == null) return;
        try
        {
            foreach (var hw in computer.Hardware)
            {
                if (hw.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel)
                {
                    foreach (var sensor in hw.Sensors)
                    {
                        if (sensor.SensorType == SensorType.SmallData
                            && sensor.Name.Contains("Memory Total", StringComparison.OrdinalIgnoreCase)
                            && sensor.Value.HasValue)
                        {
                            double gb = sensor.Value.Value / 1024.0;
                            // Update the VRAM row if we already added one
                            foreach (var row in rows)
                            {
                                if (row.Label == "VRAM")
                                {
                                    row.Value = $"{gb:F0} GB";
                                    break;
                                }
                            }
                            break;
                        }
                    }
                    break;
                }
            }
        }
        catch { }
    }

    public void UpdateLive(Computer? computer)
    {
        if (computer == null) return;

        try
        {
            foreach (var hw in computer.Hardware)
            {
                if (hw.HardwareType is not (HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel))
                    continue;

                foreach (var sensor in hw.Sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (_liveTempRow != null)
                                _liveTempRow.Value = $"{sensor.Value.Value:F1} °C";
                        });
                        return;
                    }
                }
            }
        }
        catch { }

        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_liveTempRow != null && _liveTempRow.Value == "Scanning...")
                _liveTempRow.Value = "N/A";
        });
    }
}
