using Gizmo.Client.UI.Services;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Gizmo.Client.UI.Host.WPF.Services
{
    public sealed class NetworkStatusService : INetworkStatusService, IDisposable
    {
        public bool IsConnected { get; private set; }
        public NetworkConnectionType ConnectionType { get; private set; }
        public string? IpAddress { get; private set; }

        public event EventHandler? StatusChanged;

        public NetworkStatusService()
        {
            Refresh();
            NetworkChange.NetworkAvailabilityChanged += OnAvailabilityChanged;
            NetworkChange.NetworkAddressChanged += OnAddressChanged;
        }

        private void OnAvailabilityChanged(object? s, NetworkAvailabilityEventArgs e) => Refresh();
        private void OnAddressChanged(object? s, EventArgs e) => Refresh();

        private void Refresh()
        {
            IsConnected = NetworkInterface.GetIsNetworkAvailable();

            var active = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up
                          && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback
                          && ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .OrderByDescending(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                .FirstOrDefault();

            if (active != null)
            {
                ConnectionType = active.NetworkInterfaceType switch
                {
                    NetworkInterfaceType.Ethernet => NetworkConnectionType.Ethernet,
                    NetworkInterfaceType.Wireless80211 => NetworkConnectionType.WiFi,
                    _ => NetworkConnectionType.Other,
                };
                IpAddress = active.GetIPProperties().UnicastAddresses
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => a.Address.ToString())
                    .FirstOrDefault();
            }
            else
            {
                ConnectionType = NetworkConnectionType.None;
                IpAddress = null;
            }

            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            NetworkChange.NetworkAvailabilityChanged -= OnAvailabilityChanged;
            NetworkChange.NetworkAddressChanged -= OnAddressChanged;
        }
    }
}
