using System;
using System.Net;
using System.Runtime.CompilerServices;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.MP_FPS
{
    /// <summary>
    /// How the player asked to get into a session.
    /// </summary>
    /// <remarks>
    /// The old <c>CreateOrJoin</c> (Relay + 6-character code) is gone with UGS. Steam invites replace
    /// it — see <see cref="SteamLobby"/>.
    /// </remarks>
    public enum CreationType
    {
        /// <summary>Create a friends-only Steam lobby and host it.</summary>
        HostSteam = 0,

        /// <summary>Join a Steam lobby, normally arriving via an overlay invite.</summary>
        JoinSteam = 1,

        /// <summary>Host over plain UDP with no Steam. Debug only.</summary>
        HostDirect = 2,

        /// <summary>Connect over plain UDP with no Steam. Debug only.</summary>
        JoinDirect = 3,
    }

    /// <summary>
    /// Local connection state, replacing <c>Unity.NetCode.ConnectionState</c>.
    /// </summary>
    public static class ConnectionState
    {
        public enum State
        {
            Disconnected = 0,
            Connecting = 1,
            Connected = 2,
        }
    }

    /// <summary>
    /// Player-facing connection settings, bound into the main-menu UI.
    /// </summary>
    /// <remarks>
    /// Only the direct-connect debug path still reads the address and port fields. The Steam path
    /// needs neither: the host's SteamID64 comes from lobby metadata.
    /// </remarks>
    public class ConnectionSettings : INotifyBindablePropertyChanged
    {
        public static ConnectionSettings Instance { get; private set; } = null!;

        /// <summary>
        /// This initialization is required in the Editor to avoid the instance from a previous Playmode to stay alive in the next session.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeInitializeOnLoad() => Instance = new ConnectionSettings();

        public const string DefaultServerAddress = "127.0.0.1";
        public const ushort DefaultServerPort = 7979;

        const string k_IPAddressKey = "IPAddress";
        const string k_PortKey = "Port";

        ConnectionSettings()
        {
            IPAddress = PlayerPrefs.GetString(k_IPAddressKey, DefaultServerAddress);
            if (!IsAddressValid(IPAddress))
                IPAddress = DefaultServerAddress;

            Port = PlayerPrefs.GetString(k_PortKey, DefaultServerPort.ToString());
            if (!ushort.TryParse(Port, out _))
                Port = DefaultServerPort.ToString();
        }

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;
        void Notify([CallerMemberName] string property = "") =>
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));

        ConnectionState.State m_ConnectionState;
        public ConnectionState.State GameConnectionState
        {
            get => m_ConnectionState;
            set
            {
                if (m_ConnectionState == value)
                    return;
                m_ConnectionState = value;
                Notify(ConnectionStatusStylePropertyName);
            }
        }

        public static readonly string ConnectionStatusStylePropertyName = nameof(ConnectionStatusStyle);
        [CreateProperty]
        DisplayStyle ConnectionStatusStyle =>
            m_ConnectionState == ConnectionState.State.Connecting
                ? DisplayStyle.Flex
                : DisplayStyle.None;

        bool m_IsNetworkEndpointFormatValid;
        [CreateProperty]
        public bool IsNetworkEndpointValid
        {
            get => m_IsNetworkEndpointFormatValid;
            set
            {
                if (m_IsNetworkEndpointFormatValid == value)
                    return;
                m_IsNetworkEndpointFormatValid = value;
                Notify();
            }
        }

        string m_IPAddress;
        [CreateProperty]
        public string IPAddress
        {
            get => m_IPAddress;
            set
            {
                if (m_IPAddress == value)
                    return;

                m_IPAddress = value;
                PlayerPrefs.SetString(k_IPAddressKey, value);
                IsNetworkEndpointValid = IsAddressValid(m_IPAddress) && ushort.TryParse(m_Port, out _);
                Notify();
            }
        }

        string m_Port;
        [CreateProperty]
        public string Port
        {
            get => m_Port;
            set
            {
                if (m_Port == value)
                    return;

                m_Port = value;
                PlayerPrefs.SetString(k_PortKey, value);
                IsNetworkEndpointValid = IsAddressValid(m_IPAddress) && ushort.TryParse(m_Port, out _);
                Notify();
            }
        }

        /// <summary>
        /// The lobby this client was last asked to join, as a SteamID64.
        /// </summary>
        /// <remarks>
        /// Set by the overlay-invite callback and consumed by <see cref="GameConnection.JoinSteamAsync"/>.
        /// This is not a join code and is never shown to the player — the invite model is
        /// overlay-only, so nothing needs to be typed in.
        /// </remarks>
        public ulong PendingLobbyId { get; set; }

        static bool IsAddressValid(string address) =>
            !string.IsNullOrWhiteSpace(address) && System.Net.IPAddress.TryParse(address, out _);
    }
}
