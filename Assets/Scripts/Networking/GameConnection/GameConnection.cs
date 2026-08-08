using System;
using System.Threading;
using System.Threading.Tasks;
using Netcode.Transports;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace Unity.MP_FPS
{
    /// <summary>
    /// How this session was established. Replaces the old Relay/Direct <c>NetworkType</c>.
    /// </summary>
    public enum SessionTransport
    {
        /// <summary>Steam P2P through a Steam lobby. The shipping path.</summary>
        Steam = 0,

        /// <summary>Raw UDP over <see cref="UnityTransport"/>. Debug only — see <see cref="GameConnection"/>.</summary>
        Direct = 1,
    }

    /// <summary>
    /// Owns the act of getting into a session: create or join the Steam lobby, point the transport at
    /// the host, and start NGO.
    /// </summary>
    /// <remarks>
    /// The predecessor of this class wrapped UGS <c>ISession</c> and existed mainly to extract Relay
    /// endpoints. None of that survives. What replaces it is smaller because Steam P2P needs no
    /// endpoint negotiation: the host's SteamID64 <i>is</i> the address.
    /// <para>
    /// <b>The direct-connect path is deliberately retained behind a debug flag.</b> It is how you tell
    /// a transport failure apart from a Steam failure, and it keeps offline iteration possible when
    /// Steam is down or you are working without a network.
    /// </para>
    /// </remarks>
    public class GameConnection
    {
        /// <summary>How this connection was made.</summary>
        public SessionTransport Transport { get; private set; }

        /// <summary>True when this peer is the host (server + local player).</summary>
        public bool IsHost { get; private set; }

        /// <summary>The host's SteamID64. Zero on the direct-connect debug path.</summary>
        public ulong HostSteamId { get; private set; }

        /// <summary>The Steam lobby backing this session, as a SteamID64. Zero when not on Steam.</summary>
        public ulong LobbyId { get; private set; }

        /// <summary>
        /// Creates a friends-only Steam lobby and starts hosting.
        /// </summary>
        public static async Task<GameConnection> HostSteamAsync(CancellationToken cancellationToken = default)
        {
#if DISABLESTEAMWORKS
            throw new InvalidOperationException("This build was compiled with DISABLESTEAMWORKS.");
#else
            RequireSteam();

            var connection = new GameConnection
            {
                Transport = SessionTransport.Steam,
                IsHost = true,
                HostSteamId = SteamManager.LocalSteamId,
            };

            var lobby = await SteamLobby.CreateAsync(GameManager.MaxPlayer, cancellationToken);
            connection.LobbyId = lobby.m_SteamID;

            cancellationToken.ThrowIfCancellationRequested();

            ConfigureSteamTransport(connection.HostSteamId);

            if (!NetworkManager.Singleton.StartHost())
            {
                SteamLobby.Leave();
                throw new InvalidOperationException("Netcode for GameObjects refused to start hosting.");
            }

            return connection;
#endif
        }

        /// <summary>
        /// Joins an existing session through its Steam lobby.
        /// </summary>
        /// <param name="lobbyId">The lobby to join, typically from an overlay invite.</param>
        public static async Task<GameConnection> JoinSteamAsync(ulong lobbyId, CancellationToken cancellationToken = default)
        {
#if DISABLESTEAMWORKS
            throw new InvalidOperationException("This build was compiled with DISABLESTEAMWORKS.");
#else
            RequireSteam();

            var connection = new GameConnection
            {
                Transport = SessionTransport.Steam,
                IsHost = false,
            };

            var lobby = await SteamLobby.JoinAsync(new CSteamID(lobbyId), cancellationToken);
            connection.LobbyId = lobby.m_SteamID;

            cancellationToken.ThrowIfCancellationRequested();

            // The host publishes its SteamID64 into lobby metadata on creation. Metadata is delivered
            // with the lobby on entry, so it is readable immediately.
            var hostSteamId = SteamLobby.GetHostSteamId(lobby);
            if (hostSteamId == 0ul)
            {
                SteamLobby.Leave();
                throw new InvalidOperationException(
                    "That session did not advertise a host. It may have already shut down.");
            }

            connection.HostSteamId = hostSteamId;
            ConfigureSteamTransport(hostSteamId);

            if (!NetworkManager.Singleton.StartClient())
            {
                SteamLobby.Leave();
                throw new InvalidOperationException("Netcode for GameObjects refused to start the client.");
            }

            return connection;
#endif
        }

        /// <summary>
        /// Starts a host on <see cref="UnityTransport"/> over plain UDP, with no Steam involvement.
        /// </summary>
        /// <remarks>Debug path. See the class remarks for why it exists.</remarks>
        public static Task<GameConnection> HostDirectAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var port = ParsePort(ConnectionSettings.Instance.Port);
            var utp = ConfigureDirectTransport();
            utp.SetConnectionData("0.0.0.0", port);

            if (!NetworkManager.Singleton.StartHost())
                throw new InvalidOperationException("Netcode for GameObjects refused to start hosting.");

            return Task.FromResult(new GameConnection
            {
                Transport = SessionTransport.Direct,
                IsHost = true,
            });
        }

        /// <summary>Connects to a direct-connect host by address and port. Debug path.</summary>
        public static Task<GameConnection> JoinDirectAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var settings = ConnectionSettings.Instance;
            var port = ParsePort(settings.Port);
            var utp = ConfigureDirectTransport();
            utp.SetConnectionData(settings.IPAddress, port);

            if (!NetworkManager.Singleton.StartClient())
                throw new InvalidOperationException("Netcode for GameObjects refused to start the client.");

            return Task.FromResult(new GameConnection
            {
                Transport = SessionTransport.Direct,
                IsHost = false,
            });
        }

        /// <summary>
        /// Tears the session down: stops NGO and leaves the Steam lobby.
        /// </summary>
        /// <remarks>
        /// Leaving the lobby is not optional housekeeping. A member Steam still believes is present
        /// holds a slot and, at full crew, blocks that same player's own rejoin.
        /// </remarks>
        public static void Shutdown()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();

#if !DISABLESTEAMWORKS
            SteamLobby.Leave();
#endif
        }

#if !DISABLESTEAMWORKS
        /// <summary>
        /// Selects the vendored Steam transport on the <see cref="NetworkManager"/> and points it at
        /// <paramref name="hostSteamId"/>.
        /// </summary>
        static void ConfigureSteamTransport(ulong hostSteamId)
        {
            var manager = RequireNetworkManager();
            var transport = manager.GetComponent<SteamNetworkingSocketsTransport>();
            if (transport == null)
                throw new InvalidOperationException(
                    $"The NetworkManager has no {nameof(SteamNetworkingSocketsTransport)} component. " +
                    "Add it alongside NetworkManager in the Persistents scene.");

            // Only meaningful for a client; harmless on a host, which never dials out.
            transport.ConnectToSteamID = hostSteamId;
            manager.NetworkConfig.NetworkTransport = transport;
        }
#endif

        static UnityTransport ConfigureDirectTransport()
        {
            var manager = RequireNetworkManager();
            var transport = manager.GetComponent<UnityTransport>();
            if (transport == null)
                throw new InvalidOperationException(
                    $"The NetworkManager has no {nameof(UnityTransport)} component. " +
                    "The direct-connect debug path needs it alongside the Steam transport.");

            manager.NetworkConfig.NetworkTransport = transport;
            return transport;
        }

        static NetworkManager RequireNetworkManager()
        {
            var manager = NetworkManager.Singleton;
            if (manager == null)
                throw new InvalidOperationException(
                    "There is no NetworkManager in the scene. It belongs in Persistents.");

            if (manager.IsListening)
                throw new InvalidOperationException(
                    "Netcode is already running. Shut the current session down before starting another.");

            return manager;
        }

        static ushort ParsePort(string port) =>
            ushort.TryParse(port, out var parsed) ? parsed : ConnectionSettings.DefaultServerPort;

        static void RequireSteam()
        {
            if (!SteamManager.Initialized)
                throw new InvalidOperationException(
                    SteamManager.FailureReason ??
                    "Steam is not running. Start Steam and log in, then try again.");
        }
    }
}
