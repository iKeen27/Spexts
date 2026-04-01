using System.Collections.ObjectModel;
using System.Windows;
using LibreHardwareMonitor.Hardware;
using Spexts.Helpers;
using Spexts.Models;

namespace Spexts.ViewModels;

public class BatteryViewModel : BaseViewModel
{
    private ObservableCollection<InfoRow> _rows = new();
    public ObservableCollection<InfoRow> Rows
    {
        get => _rows;
        set => SetProperty(ref _rows, value);
    }

    private bool _hasBattery;
    public bool HasBattery
    {
        get => _hasBattery;
        set => SetProperty(ref _hasBattery, value);
    }

    public void Load(Computer? computer)
    {
        var rows = new ObservableCollection<InfoRow>();

        // Check WMI for battery presence
        bool batteryFound = false;
        try
        {
            var status = WmiHelper.QuerySingle("Win32_Battery", "Status");
            batteryFound = status != "N/A";
        }
        catch { }

        if (!batteryFound)
        {
            rows.Add(new InfoRow("Status", "No Battery Detected (Desktop)"));
            Application.Current.Dispatcher.Invoke(() =>
            {
                HasBattery = false;
                Rows = rows;
            });
            return;
        }

        try
        {
            rows.Add(new InfoRow("Status", WmiHelper.QuerySingle("Win32_Battery", "Status")));

            var estCharge = WmiHelper.QuerySingle("Win32_Battery", "EstimatedChargeRemaining");
            if (estCharge != "N/A")
                rows.Add(new InfoRow("Charge Level", $"{estCharge}%"));
        }
        catch { }

        // Get detailed battery info from LHM
        if (computer != null)
        {
            try
            {
                foreach (var hw in computer.Hardware)
                {
                    if (hw.HardwareType != HardwareType.Battery) continue;

                    float? designCap = null, fullCap = null, currentCap = null;
                    float? voltage = null, chargeRate = null;

                    foreach (var sensor in hw.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Energy)
                        {
                            if (sensor.Name.Contains("Designed", StringComparison.OrdinalIgnoreCase))
                                designCap = sensor.Value;
                            else if (sensor.Name.Contains("Full", StringComparison.OrdinalIgnoreCase))
                                fullCap = sensor.Value;
                            else if (sensor.Name.Contains("Remaining", StringComparison.OrdinalIgnoreCase))
                                currentCap = sensor.Value;
                        }
                        if (sensor.SensorType == SensorType.Voltage && sensor.Value.HasValue)
                            voltage = sensor.Value;
                        if (sensor.SensorType == SensorType.Power && sensor.Value.HasValue)
                            chargeRate = sensor.Value;
                    }

                    if (designCap.HasValue)
                        rows.Add(new InfoRow("Design Capacity", $"{designCap.Value:F1} Wh"));
                    if (fullCap.HasValue)
                        rows.Add(new InfoRow("Full Charge Capacity", $"{fullCap.Value:F1} Wh"));
                    if (designCap.HasValue && fullCap.HasValue && designCap.Value > 0)
                    {
                        double health = (fullCap.Value / designCap.Value) * 100;
                        double wear = 100 - health;
                        rows.Add(new InfoRow("Battery Health", $"{health:F1}%"));
                        rows.Add(new InfoRow("Wear Level", $"{wear:F1}%"));
                    }
                    if (voltage.HasValue)
                        rows.Add(new InfoRow("Voltage", $"{voltage.Value:F2} V"));
                    if (chargeRate.HasValue)
                        rows.Add(new InfoRow("Charge/Discharge Rate", $"{chargeRate.Value:F1} W"));

                    break;
                }
            }
            catch { }
        }

        if (rows.Count == 0)
            rows.Add(new InfoRow("Battery", "N/A"));

        Application.Current.Dispatcher.Invoke(() =>
        {
            HasBattery = true;
            Rows = rows;
        });
    }
}
