using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows;
using Spexts.Models;

namespace Spexts.ViewModels;

public class NetworkViewModel : BaseViewModel
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
            var iface = GetActiveInterface();
            if (iface != null)
            {
                var props = iface.GetIPProperties();
                var ipv4 = iface.GetIPv4Statistics();

                rows.Add(new InfoRow("Adapter", iface.Name));
                rows.Add(new InfoRow("Type",
                    iface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                        ? "Wi-Fi" : iface.NetworkInterfaceType.ToString()));
                rows.Add(new InfoRow("Status", iface.OperationalStatus.ToString()));

                // IPv4
                foreach (var addr in props.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        rows.Add(new InfoRow("IPv4 Address", addr.Address.ToString()));
                        break;
                    }
                }

                // MAC
                var mac = iface.GetPhysicalAddress();
                var macStr = string.Join(":", mac.GetAddressBytes().Select(b => b.ToString("X2")));
                rows.Add(new InfoRow("MAC Address", macStr));

                // Link Speed
                long speedMbps = iface.Speed / 1_000_000;
                rows.Add(new InfoRow("Link Speed", $"{speedMbps} Mbps"));

                // DNS
                var dnsServers = props.DnsAddresses
                    .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => a.ToString())
                    .ToList();
                rows.Add(new InfoRow("DNS Servers",
                    dnsServers.Count > 0 ? string.Join(", ", dnsServers) : "N/A"));

                // MTU
                if (props.GetIPv4Properties() is { } ipv4Props)
                    rows.Add(new InfoRow("MTU", ipv4Props.Mtu.ToString()));

                // Gateway
                var gateways = props.GatewayAddresses
                    .Select(g => g.Address.ToString())
                    .Where(g => g != "0.0.0.0")
                    .ToList();
                if (gateways.Count > 0)
                    rows.Add(new InfoRow("Gateway", string.Join(", ", gateways)));
            }
            else
            {
                rows.Add(new InfoRow("Network", "No active adapter found"));
            }
        }
        catch
        {
            rows.Add(new InfoRow("Network", "N/A"));
        }

        // Ping tests
        rows.Add(new InfoRow("Ping 8.8.8.8", PingHost("8.8.8.8")));
        rows.Add(new InfoRow("Ping 1.1.1.1", PingHost("1.1.1.1")));

        Application.Current.Dispatcher.Invoke(() => Rows = rows);
    }

    private static NetworkInterface? GetActiveInterface()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up
                        && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                        && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .OrderByDescending(n =>
                n.NetworkInterfaceType == NetworkInterfaceType.Ethernet ? 2 :
                n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? 1 : 0)
            .FirstOrDefault();
    }

    private static string PingHost(string host)
    {
        try
        {
            using var ping = new Ping();
            var reply = ping.Send(host, 3000);
            return reply.Status == IPStatus.Success
                ? $"{reply.RoundtripTime} ms"
                : reply.Status.ToString();
        }
        catch
        {
            return "Timeout";
        }
    }
}
