using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management;
using System.Windows;
using Microsoft.Win32;
using Spexts.Helpers;
using Spexts.Models;

namespace Spexts.ViewModels;

public class SecurityViewModel : BaseViewModel
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

        // ═══════════════════════════════════════════════════════════
        // TOP 4: Unified color-coded rows (no checkmarks/symbols)
        // ═══════════════════════════════════════════════════════════

        // ═══════════ Secure Boot ═══════════
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
            if (key != null)
            {
                var val = key.GetValue("UEFISecureBootEnabled");
                string status = val != null && (int)val == 1 ? "Enabled" : "Disabled";
                rows.Add(new InfoRow("Secure Boot", status,
                    TweakAdvisor.ColorForSecureBoot(status)));
            }
            else
            {
                rows.Add(new InfoRow("Secure Boot", "Not Supported"));
            }
        }
        catch
        {
            rows.Add(new InfoRow("Secure Boot", "N/A"));
        }

        // ═══════════ TPM ═══════════
        try
        {
            var tpmPresent = WmiHelper.QuerySingle("Win32_Tpm", "IsActivated_InitialValue",
                @"root\cimv2\Security\MicrosoftTpm");

            if (tpmPresent != "N/A")
            {
                var tpmVersion = WmiHelper.QuerySingle("Win32_Tpm", "SpecVersion",
                    @"root\cimv2\Security\MicrosoftTpm");
                string ver = tpmVersion != "N/A" && tpmVersion.Contains(",")
                    ? tpmVersion.Split(',')[0].Trim()
                    : tpmVersion;

                bool isActive = tpmPresent.Equals("True", StringComparison.OrdinalIgnoreCase);
                string status = isActive ? $"Active (v{ver})" : $"Inactive (v{ver})";
                rows.Add(new InfoRow("TPM Status", status,
                    TweakAdvisor.ColorForTpm(status)));
            }
            else
            {
                string status = "Not Detected";
                rows.Add(new InfoRow("TPM Status", status,
                    TweakAdvisor.ColorForTpm(status)));
            }
        }
        catch
        {
            rows.Add(new InfoRow("TPM Status", "N/A"));
        }

        // ═══════════ Virtualization ═══════════
        try
        {
            var vtEnabled = WmiHelper.QuerySingle("Win32_Processor",
                "VirtualizationFirmwareEnabled");
            string status = vtEnabled.Equals("True", StringComparison.OrdinalIgnoreCase)
                ? "Enabled" : "Disabled";
            rows.Add(new InfoRow("Virtualization (VT-x/AMD-V)", status,
                TweakAdvisor.ColorForVirtualization(status)));
        }
        catch
        {
            rows.Add(new InfoRow("Virtualization", "N/A"));
        }

        // ═══════════ Hypervisor ═══════════
        try
        {
            var hypervisor = WmiHelper.QuerySingle("Win32_ComputerSystem",
                "HypervisorPresent");
            string status = hypervisor.Equals("True", StringComparison.OrdinalIgnoreCase)
                ? "Yes" : "No";
            rows.Add(new InfoRow("Hypervisor Present", status,
                TweakAdvisor.ColorForHypervisor(status)));
        }
        catch
        {
            rows.Add(new InfoRow("Hypervisor Present", "N/A"));
        }

        // ═══════════════════════════════════════════════════════════
        // ADVANCED TWEAKING DIAGNOSTICS (with color-coded advisor)
        // ═══════════════════════════════════════════════════════════

        // ═══════════ VBS & Core Isolation ═══════════
        try
        {
            string vbsStatus = QueryVbs();
            rows.Add(new InfoRow("VBS / Core Isolation", vbsStatus,
                TweakAdvisor.ColorForVbs(vbsStatus)));
        }
        catch
        {
            rows.Add(new InfoRow("VBS / Core Isolation", "Unknown"));
        }

        // ═══════════ Active Power Plan ═══════════
        try
        {
            string powerPlan = QueryActivePowerPlan();
            rows.Add(new InfoRow("Active Power Plan", powerPlan,
                TweakAdvisor.ColorForPowerPlan(powerPlan)));
        }
        catch
        {
            rows.Add(new InfoRow("Active Power Plan", "Unknown"));
        }

        // ═══════════ HPET Status ═══════════
        try
        {
            string hpet = QueryHpetStatus();
            rows.Add(new InfoRow("HPET (Timer)", hpet,
                TweakAdvisor.ColorForHpet(hpet)));
        }
        catch
        {
            rows.Add(new InfoRow("HPET (Timer)", "Unknown"));
        }

        // ═══════════ Fast Startup ═══════════
        try
        {
            string fastStartup = QueryFastStartup();
            rows.Add(new InfoRow("Fast Startup", fastStartup,
                TweakAdvisor.ColorForFastStartup(fastStartup)));
        }
        catch
        {
            rows.Add(new InfoRow("Fast Startup", "Unknown"));
        }

        // ═══════════ Resizable BAR (ReBAR) ═══════════
        try
        {
            string rebar = QueryResizableBar();
            rows.Add(new InfoRow("Resizable BAR (ReBAR)", rebar,
                TweakAdvisor.ColorForRebar(rebar)));
        }
        catch
        {
            rows.Add(new InfoRow("Resizable BAR (ReBAR)",
                "Requires Manual BIOS Check", TweakAdvisor.Yellow));
        }

        Application.Current.Dispatcher.Invoke(() => Rows = rows);
    }

    // ─────────────────────────────────────────────────────────
    // Query helpers with robust try/catch
    // ─────────────────────────────────────────────────────────

    private static string QueryVbs()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\Microsoft\Windows\DeviceGuard",
                "SELECT VirtualizationBasedSecurityStatus, SecurityServicesRunning FROM Win32_DeviceGuard");
            using var results = searcher.Get();

            foreach (ManagementObject obj in results)
            {
                using (obj)
                {
                    var vbsStatusVal = obj["VirtualizationBasedSecurityStatus"];
                    var servicesRunning = obj["SecurityServicesRunning"];

                    int vbsStatus = vbsStatusVal != null ? Convert.ToInt32(vbsStatusVal) : -1;

                    string statusText = vbsStatus switch
                    {
                        0 => "Disabled",
                        1 => "Enabled (Not Running)",
                        2 => "Enabled & Running",
                        _ => "Unknown"
                    };

                    if (servicesRunning is uint[] services && services.Length > 0)
                    {
                        bool hvciRunning = services.Any(s => s == 2);
                        if (hvciRunning)
                            statusText += " + HVCI Active";
                    }
                    else if (servicesRunning is int[] servicesInt && servicesInt.Length > 0)
                    {
                        bool hvciRunning = servicesInt.Any(s => s == 2);
                        if (hvciRunning)
                            statusText += " + HVCI Active";
                    }

                    return statusText;
                }
            }
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string QueryActivePowerPlan()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\cimv2\power",
                "SELECT ElementName FROM Win32_PowerPlan WHERE IsActive = True");
            using var results = searcher.Get();

            foreach (ManagementObject obj in results)
            {
                using (obj)
                {
                    var name = obj["ElementName"]?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(name))
                        return name;
                }
            }
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string QueryHpetStatus()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "bcdedit",
                Arguments = "/enum",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = ""
            };

            using var process = Process.Start(psi);
            if (process == null) return "Unknown";

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            if (string.IsNullOrWhiteSpace(output))
                return "Unknown";

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("useplatformclock", StringComparison.OrdinalIgnoreCase))
                {
                    if (line.Contains("Yes", StringComparison.OrdinalIgnoreCase))
                        return "Enabled";
                    if (line.Contains("No", StringComparison.OrdinalIgnoreCase))
                        return "Disabled";
                }
            }

            return "Disabled (Default)";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string QueryFastStartup()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Session Manager\Power");
            if (key != null)
            {
                var val = key.GetValue("HiberbootEnabled");
                if (val != null)
                {
                    return (int)val == 1 ? "Enabled" : "Disabled";
                }
            }
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string QueryResizableBar()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, Status FROM Win32_PnPEntity WHERE Name LIKE '%PCI Express%' AND Name LIKE '%Large%'");
            using var results = searcher.Get();

            foreach (ManagementObject obj in results)
            {
                using (obj)
                {
                    var name = obj["Name"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(name) &&
                        name.Contains("Large", StringComparison.OrdinalIgnoreCase))
                    {
                        return "Likely Enabled (PCI Large Memory Range detected)";
                    }
                }
            }

            return "Requires Manual BIOS Check";
        }
        catch
        {
            return "Requires Manual BIOS Check";
        }
    }
}
