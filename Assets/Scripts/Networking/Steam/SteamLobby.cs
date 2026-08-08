#if !DISABLESTEAMWORKS
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Steamworks;
using UnityEngine;

namespace Unity.MP_FPS
{
    /// <summary>
    /// Thin wrapper over <see cref="SteamMatchmaking"/> that owns exactly one lobby at a time.
    /// </summary>
    /// <remarks>
    /// This replaces the UGS Sessions layer. There is deliberately no server browser and no join
    /// code: the friends list <i>is</i> the matchmaking system, exactly as in Lethal Company and
    /// R.E.P.O. Lobbies are created <see cref="ELobbyType.k_ELobbyTypeFriendsOnly"/> and joined
    /// through the Steam overlay.
    /// <para>
    /// The lobby's job is discovery only. Once a joiner knows the host's SteamID64 — published in
    /// lobby metadata under <see cref="HostSteamIdKey"/> — the actual game traffic runs over
    /// <c>SteamNetworkingSockets</c> via the transport, not through the lobby.
    /// </para>
    /// </remarks>
    public static class SteamLobby
    {
        /// <summary>Lobby metadata key carrying the host's SteamID64 as a decimal string.</summary>
        /// <remarks>
        /// Deliberately not derived from <see cref="SteamMatchmaking.GetLobbyOwner"/> at join time.
        /// The lobby owner and the netcode host are the same player today, but Steam reassigns lobby
        /// ownership when the owner leaves, and we do not want a client to silently retarget its
        /// transport at a new "owner" who is not running a server. Host migration is out of scope.
        /// </remarks>
        public const string HostSteamIdKey = "host_steam_id";

        /// <summary>Lobby metadata key carrying the host's persona name, for UI before connection.</summary>
        public const string HostNameKey = "host_name";

        static Callback<GameLobbyJoinRequested_t> s_JoinRequested;
        static Callback<LobbyChatUpdate_t> s_ChatUpdate;
        static Callback<LobbyEnter_t> s_LobbyEnter;

        static CallResult<LobbyCreated_t> s_LobbyCreated;
        static CallResult<LobbyEnter_t> s_LobbyEnterResult;

        static TaskCompletionSource<CSteamID> s_PendingCreate;
        static TaskCompletionSource<CSteamID> s_PendingJoin;

        /// <summary>The lobby we are currently in, or <see cref="CSteamID.Nil"/>.</summary>
        public static CSteamID CurrentLobby { get; private set; } = CSteamID.Nil;

        public static bool InLobby => CurrentLobby != CSteamID.Nil;

        /// <summary>Raised when the player accepts an invite from the Steam overlay.</summary>
        /// <remarks>This is the callback that makes overlay invites work at all.</remarks>
        public static event Action<CSteamID> JoinRequested;

        /// <summary>Raised when another player enters the current lobby.</summary>
        public static event Action<CSteamID> MemberJoined;

        /// <summary>Raised when another player leaves, disconnects, or is kicked.</summary>
        public static event Action<CSteamID> MemberLeft;

        /// <summary>
        /// Registers the Steam callbacks. Safe to call more than once; only the first call binds.
        /// Must run after <see cref="SteamManager"/> has initialised.
        /// </summary>
        public static void Initialize()
        {
            if (!SteamManager.Initialized || s_JoinRequested != null)
                return;

            s_JoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            s_ChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
            s_LobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
        }

        /// <summary>
        /// Creates a friends-only lobby and publishes this player as its host.
        /// </summary>
        /// <param name="maxMembers">Crew size. Steam enforces this as the lobby member limit.</param>
        /// <returns>The created lobby's id.</returns>
        public static async Task<CSteamID> CreateAsync(int maxMembers, CancellationToken cancellationToken = default)
        {
            RequireSteam();
            Initialize();

            if (InLobby)
                Leave();

            s_LobbyCreated ??= CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
            s_PendingCreate = new TaskCompletionSource<CSteamID>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (cancellationToken.Register(() => s_PendingCreate?.TrySetCanceled(cancellationToken)))
            {
                var call = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxMembers);
                s_LobbyCreated.Set(call);

                var lobby = await s_PendingCreate.Task;

                CurrentLobby = lobby;
                var localId = SteamUser.GetSteamID();
                SteamMatchmaking.SetLobbyData(lobby, HostSteamIdKey, localId.m_SteamID.ToString());
                SteamMatchmaking.SetLobbyData(lobby, HostNameKey, SteamFriends.GetPersonaName());
                SteamMatchmaking.SetLobbyMemberLimit(lobby, maxMembers);

                Debug.Log($"[{nameof(SteamLobby)}] Created lobby {lobby.m_SteamID} for up to {maxMembers} interns.");
                return lobby;
            }
        }

        /// <summary>Joins an existing lobby by id.</summary>
        public static async Task<CSteamID> JoinAsync(CSteamID lobbyId, CancellationToken cancellationToken = default)
        {
            RequireSteam();
            Initialize();

            if (InLobby && CurrentLobby != lobbyId)
                Leave();

            s_LobbyEnterResult ??= CallResult<LobbyEnter_t>.Create(OnLobbyEnterResult);
            s_PendingJoin = new TaskCompletionSource<CSteamID>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (cancellationToken.Register(() => s_PendingJoin?.TrySetCanceled(cancellationToken)))
            {
                var call = SteamMatchmaking.JoinLobby(lobbyId);
                s_LobbyEnterResult.Set(call);

                var lobby = await s_PendingJoin.Task;
                CurrentLobby = lobby;

                Debug.Log($"[{nameof(SteamLobby)}] Joined lobby {lobby.m_SteamID}.");
                return lobby;
            }
        }

        /// <summary>
        /// Leaves the current lobby, if any.
        /// </summary>
        /// <remarks>
        /// Call this promptly on every exit path. A member that Steam still believes is in the lobby
        /// occupies a slot and, at full crew, blocks that same player's rejoin.
        /// </remarks>
        public static void Leave()
        {
            if (!SteamManager.Initialized || !InLobby)
                return;

            var lobby = CurrentLobby;
            CurrentLobby = CSteamID.Nil;
            SteamMatchmaking.LeaveLobby(lobby);
            Debug.Log($"[{nameof(SteamLobby)}] Left lobby {lobby.m_SteamID}.");
        }

        /// <summary>Reads the host's SteamID64 out of lobby metadata. Zero if unset or malformed.</summary>
        public static ulong GetHostSteamId(CSteamID lobbyId)
        {
            if (!SteamManager.Initialized)
                return 0ul;

            var raw = SteamMatchmaking.GetLobbyData(lobbyId, HostSteamIdKey);
            return ulong.TryParse(raw, out var id) ? id : 0ul;
        }

        /// <summary>Reads the host's persona name out of lobby metadata.</summary>
        public static string GetHostName(CSteamID lobbyId)
        {
            if (!SteamManager.Initialized)
                return string.Empty;

            return SteamMatchmaking.GetLobbyData(lobbyId, HostNameKey);
        }

        /// <summary>Opens the Steam overlay's invite dialog for the current lobby.</summary>
        public static bool OpenInviteOverlay()
        {
            if (!SteamManager.Initialized || !InLobby)
                return false;

            SteamFriends.ActivateGameOverlayInviteDialog(CurrentLobby);
            return true;
        }

        /// <summary>Current lobby members, by SteamID64.</summary>
        public static IReadOnlyList<CSteamID> GetMembers()
        {
            if (!SteamManager.Initialized || !InLobby)
                return Array.Empty<CSteamID>();

            var count = SteamMatchmaking.GetNumLobbyMembers(CurrentLobby);
            var members = new List<CSteamID>(count);
            for (var i = 0; i < count; i++)
                members.Add(SteamMatchmaking.GetLobbyMemberByIndex(CurrentLobby, i));

            return members;
        }

        /// <summary>
        /// Stops new players entering the lobby. Used when the crew deploys, so a late invitee does
        /// not land mid-round into a world they cannot be spawned into.
        /// </summary>
        public static void SetJoinable(bool joinable)
        {
            if (SteamManager.Initialized && InLobby)
                SteamMatchmaking.SetLobbyJoinable(CurrentLobby, joinable);
        }

        static void RequireSteam()
        {
            if (!SteamManager.Initialized)
                throw new InvalidOperationException(
                    SteamManager.FailureReason ?? "Steam is not initialised.");
        }

        static void OnLobbyCreated(LobbyCreated_t param, bool ioFailure)
        {
            var pending = s_PendingCreate;
            s_PendingCreate = null;
            if (pending == null)
                return;

            if (ioFailure || param.m_eResult != EResult.k_EResultOK)
            {
                pending.TrySetException(new InvalidOperationException(
                    ioFailure
                        ? "Could not reach Steam to create the lobby."
                        : $"Steam refused to create the lobby ({param.m_eResult})."));
                return;
            }

            pending.TrySetResult(new CSteamID(param.m_ulSteamIDLobby));
        }

        static void OnLobbyEnterResult(LobbyEnter_t param, bool ioFailure)
        {
            var pending = s_PendingJoin;
            s_PendingJoin = null;
            if (pending == null)
                return;

            var response = (EChatRoomEnterResponse)param.m_EChatRoomEnterResponse;
            if (ioFailure || response != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                pending.TrySetException(new InvalidOperationException(
                    ioFailure
                        ? "Could not reach Steam to join the lobby."
                        : DescribeEnterFailure(response)));
                return;
            }

            pending.TrySetResult(new CSteamID(param.m_ulSteamIDLobby));
        }

        static string DescribeEnterFailure(EChatRoomEnterResponse response) => response switch
        {
            EChatRoomEnterResponse.k_EChatRoomEnterResponseFull =>
                "That crew is full.",
            EChatRoomEnterResponse.k_EChatRoomEnterResponseDoesntExist =>
                "That session no longer exists.",
            EChatRoomEnterResponse.k_EChatRoomEnterResponseNotAllowed or
            EChatRoomEnterResponse.k_EChatRoomEnterResponseBanned =>
                "You are not allowed to join that session.",
            _ => $"Could not join that session ({response}).",
        };

        static void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t param)
        {
            Debug.Log($"[{nameof(SteamLobby)}] Overlay invite accepted for lobby {param.m_steamIDLobby.m_SteamID}.");
            JoinRequested?.Invoke(param.m_steamIDLobby);
        }

        static void OnLobbyEnter(LobbyEnter_t param)
        {
            // Fires for every entry, including ones we did not initiate through JoinAsync
            // (notably a command-line +connect_lobby launch from a cold start).
            CurrentLobby = new CSteamID(param.m_ulSteamIDLobby);
        }

        static void OnLobbyChatUpdate(LobbyChatUpdate_t param)
        {
            if (param.m_ulSteamIDLobby != CurrentLobby.m_SteamID)
                return;

            var who = new CSteamID(param.m_ulSteamIDUserChanged);
            var change = (EChatMemberStateChange)param.m_rgfChatMemberStateChange;

            if ((change & EChatMemberStateChange.k_EChatMemberStateChangeEntered) != 0)
            {
                MemberJoined?.Invoke(who);
                return;
            }

            const EChatMemberStateChange left =
                EChatMemberStateChange.k_EChatMemberStateChangeLeft |
                EChatMemberStateChange.k_EChatMemberStateChangeDisconnected |
                EChatMemberStateChange.k_EChatMemberStateChangeKicked |
                EChatMemberStateChange.k_EChatMemberStateChangeBanned;

            if ((change & left) != 0)
                MemberLeft?.Invoke(who);
        }
    }
}
#endif
