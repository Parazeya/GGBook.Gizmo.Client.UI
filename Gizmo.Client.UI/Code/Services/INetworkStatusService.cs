using System;

namespace Gizmo.Client.UI.Services
{
    public interface INetworkStatusService
    {
        bool IsConnected { get; }
        NetworkConnectionType ConnectionType { get; }
        /// <summary>Primary IPv4 address of active interface, null if no connection.</summary>
        string? IpAddress { get; }

        event EventHandler StatusChanged;
    }

    public enum NetworkConnectionType
    {
        None,
        Ethernet,
        WiFi,
        Other,
    }
}
