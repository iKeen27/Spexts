using System.Collections.ObjectModel;
using System.Windows;
using Spexts.Helpers;
using Spexts.Models;

namespace Spexts.ViewModels;

public class SystemInfoViewModel : BaseViewModel
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
            rows.Add(new InfoRow("OS Name", WmiHelper.QuerySingle("Win32_OperatingSystem", "Caption")));
            rows.Add(new InfoRow("Version", WmiHelper.QuerySingle("Win32_OperatingSystem", "Version")));
            rows.Add(new InfoRow("Build Number", WmiHelper.QuerySingle("Win32_OperatingSystem", "BuildNumber")));
            rows.Add(new InfoRow("Architecture", WmiHelper.QuerySingle("Win32_OperatingSystem", "OSArchitecture")));
        }
        catch
        {
            rows.Add(new InfoRow("OS Name", "N/A"));
        }

        try
        {
            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            rows.Add(new InfoRow("System Uptime", $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m"));
        }
        catch { rows.Add(new InfoRow("System Uptime", "N/A")); }

        try
        {
            rows.Add(new InfoRow("Computer Name", Environment.MachineName));
            rows.Add(new InfoRow("Username", Environment.UserName));
        }
        catch { }

        Application.Current.Dispatcher.Invoke(() => Rows = rows);
    }
}
