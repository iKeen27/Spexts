using System.Collections.ObjectModel;
using System.Windows;
using LibreHardwareMonitor.Hardware;
using Spexts.Helpers;
using Spexts.Models;

namespace Spexts.ViewModels;

public class CpuViewModel : BaseViewModel
{
    private ObservableCollection<InfoRow> _rows = new();
    public ObservableCollection<InfoRow> Rows
    {
        get => _rows;
        set => SetProperty(ref _rows, value);
    }


    private InfoRow? _liveClockRow;
    private InfoRow? _liveTempRow;

    public void Load(Computer? computer)
    {
        var rows = new ObservableCollection<InfoRow>();

        try
        {
            rows.Add(new InfoRow("Processor", WmiHelper.QuerySingle("Win32_Processor", "Name")));
            rows.Add(new InfoRow("Socket", WmiHelper.QuerySingle("Win32_Processor", "SocketDesignation")));

            var cores = WmiHelper.QuerySingle("Win32_Processor", "NumberOfCores");
            var threads = WmiHelper.QuerySingle("Win32_Processor", "NumberOfLogicalProcessors");
            rows.Add(new InfoRow("Cores / Threads", $"{cores} / {threads}"));

            var baseClock = WmiHelper.QuerySingle("Win32_Processor", "MaxClockSpeed");
            if (baseClock != "N/A" && int.TryParse(baseClock, out int mhz))
                rows.Add(new InfoRow("Base Clock", $"{mhz} MHz ({mhz / 1000.0:F2} GHz)"));
            else
                rows.Add(new InfoRow("Base Clock", baseClock));
        }
        catch
        {
            rows.Add(new InfoRow("Processor", "N/A"));
        }

        _liveClockRow = new InfoRow("Current Clock", "Scanning...");
        _liveTempRow = new InfoRow("Temperature", "Scanning...");
        rows.Add(_liveClockRow);
        rows.Add(_liveTempRow);

        Application.Current.Dispatcher.Invoke(() => Rows = rows);

        // Initial live reading
        UpdateLive(computer);
    }

    public void UpdateLive(Computer? computer)
    {
        if (computer == null) return;

        try
        {
            foreach (var hw in computer.Hardware)
            {
                if (hw.HardwareType != HardwareType.Cpu) continue;

                float maxClock = 0;
                float? temp = null;

                foreach (var sensor in hw.Sensors)
                {
                    if (sensor.SensorType == SensorType.Clock && sensor.Value.HasValue)
                    {
                        if (sensor.Value.Value > maxClock)
                            maxClock = sensor.Value.Value;
                    }
                    if (sensor.SensorType == SensorType.Temperature
                        && sensor.Name.Contains("Package", StringComparison.OrdinalIgnoreCase)
                        && sensor.Value.HasValue)
                    {
                        temp = sensor.Value.Value;
                    }
                    // Fallback: take first temperature if no package found
                    if (sensor.SensorType == SensorType.Temperature
                        && sensor.Value.HasValue && temp == null)
                    {
                        temp = sensor.Value.Value;
                    }
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_liveClockRow != null)
                        _liveClockRow.Value = maxClock > 0
                            ? $"{maxClock:F0} MHz ({maxClock / 1000.0:F2} GHz)"
                            : "N/A";
                    if (_liveTempRow != null)
                        _liveTempRow.Value = temp.HasValue
                            ? $"{temp.Value:F1} °C"
                            : "N/A";
                });
                break;
            }
        }
        catch { }
    }
}
