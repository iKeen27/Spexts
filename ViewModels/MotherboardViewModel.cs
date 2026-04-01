using System.Collections.ObjectModel;
using System.Management;
using System.Windows;
using Spexts.Helpers;
using Spexts.Models;

namespace Spexts.ViewModels;

public class MotherboardViewModel : BaseViewModel
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
            rows.Add(new InfoRow("Manufacturer", WmiHelper.QuerySingle("Win32_BaseBoard", "Manufacturer")));
            rows.Add(new InfoRow("Model", WmiHelper.QuerySingle("Win32_BaseBoard", "Product")));
        }
        catch
        {
            rows.Add(new InfoRow("Manufacturer", "N/A"));
            rows.Add(new InfoRow("Model", "N/A"));
        }

        try
        {
            rows.Add(new InfoRow("BIOS Version", WmiHelper.QuerySingle("Win32_BIOS", "SMBIOSBIOSVersion")));

            var dateStr = WmiHelper.QuerySingle("Win32_BIOS", "ReleaseDate");
            if (dateStr != "N/A" && dateStr.Length >= 8)
            {
                try
                {
                    var dt = ManagementDateTimeConverter.ToDateTime(dateStr);
                    rows.Add(new InfoRow("BIOS Date", dt.ToString("yyyy-MM-dd")));
                }
                catch
                {
                    rows.Add(new InfoRow("BIOS Date", dateStr));
                }
            }
            else
            {
                rows.Add(new InfoRow("BIOS Date", dateStr));
            }
        }
        catch
        {
            rows.Add(new InfoRow("BIOS Version", "N/A"));
            rows.Add(new InfoRow("BIOS Date", "N/A"));
        }

        Application.Current.Dispatcher.Invoke(() => Rows = rows);
    }
}
