using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.MP_FPS
{
    /// <summary>
    /// The authoritative host-side mapping between a player's <b>stable</b> identity (SteamID64) and
    /// their <b>ephemeral</b> NGO client id.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Key all persisted player state on SteamID64, never on <see cref="NetworkManager.LocalClientId"/>.</b>
    /// NGO reassigns client ids after a disconnect, so a returning player can be handed the id — and
    /// therefore the state — of somebody else. That is the bug this type exists to make impossible,
    /// and it is what makes reconnection tractable at all.
    /// </para>
    /// <para>
    /// Only the host maintains this. Clients learn about the crew through replicated state, not from here.
    /// </para>
    /// </remarks>
    public class CrewRegistry
    {
        /// <summary>What the host remembers about one intern across disconnects.</summary>
        public class CrewMember
        {
            /// <summary>Stable identity. Never changes for a given Steam account.</summary>
            public ulong SteamId;

            /// <summary>Steam persona name, free from the lobby — see plans 19 and 80.</summary>
            public string PersonaName;

            /// <summary>NGO's current client id for this player, or <see cref="k_NotConnected"/>.</summary>
            public ulong ClientId = k_NotConnected;

            /// <summary>True while NGO has a live connection for this player.</summary>
            public bool IsConnected => ClientId != k_NotConnected;

            /// <summary>Realtime clock reading of the last disconnect, for reconnect-window policy.</summary>
            public float DisconnectedAt;
        }

        public const ulong k_NotConnected = ulong.MaxValue;

        readonly Dictionary<ulong, CrewMember> m_BySteamId = new();
        readonly Dictionary<ulong, ulong> m_ClientIdToSteamId = new();

        public IReadOnlyCollection<CrewMember> Members => m_BySteamId.Values;

        public int ConnectedCount
        {
            get
            {
                var n = 0;
                foreach (var m in m_BySteamId.Values)
                {
                    if (m.IsConnected)
                        n++;
                }
                return n;
            }
        }

        /// <summary>
        /// Binds an NGO client id to a SteamID64, creating the crew record on first sight and
        /// <i>reusing</i> it on every subsequent reconnect.
        /// </summary>
        /// <returns>
        /// The crew record, and whether this was a reconnection rather than a first join. Callers use
        /// the flag to decide between "spawn fresh" and "restore what they had".
        /// </returns>
        public CrewMember Bind(ulong steamId, ulong clientId, string personaName, out bool isReconnect)
        {
            if (m_BySteamId.TryGetValue(steamId, out var member))
            {
                isReconnect = true;

                // Drop the stale mapping; the old client id is dead and must never resolve again.
                if (member.ClientId != k_NotConnected)
                    m_ClientIdToSteamId.Remove(member.ClientId);
            }
            else
            {
                isReconnect = false;
                member = new CrewMember { SteamId = steamId };
                m_BySteamId.Add(steamId, member);
            }

            member.ClientId = clientId;
            if (!string.IsNullOrEmpty(personaName))
                member.PersonaName = personaName;

            m_ClientIdToSteamId[clientId] = steamId;
            return member;
        }

        /// <summary>
        /// Marks a player as disconnected while <b>keeping</b> their record, so their state survives
        /// until they rejoin. Their client id mapping is dropped immediately.
        /// </summary>
        public CrewMember Unbind(ulong clientId)
        {
            if (!m_ClientIdToSteamId.TryGetValue(clientId, out var steamId))
                return null;

            m_ClientIdToSteamId.Remove(clientId);

            if (!m_BySteamId.TryGetValue(steamId, out var member))
                return null;

            member.ClientId = k_NotConnected;
            member.DisconnectedAt = Time.realtimeSinceStartup;
            return member;
        }

        /// <summary>Resolves an NGO client id to a stable SteamID64. Zero when unknown.</summary>
        public ulong GetSteamId(ulong clientId) =>
            m_ClientIdToSteamId.TryGetValue(clientId, out var steamId) ? steamId : 0ul;

        public bool TryGet(ulong steamId, out CrewMember member) =>
            m_BySteamId.TryGetValue(steamId, out member);

        /// <summary>Resolves an NGO client id straight to a crew record. Null when unknown.</summary>
        public CrewMember GetByClientId(ulong clientId)
        {
            var steamId = GetSteamId(clientId);
            return steamId != 0ul && m_BySteamId.TryGetValue(steamId, out var member) ? member : null;
        }

        /// <summary>Forgets a player entirely. Use when they leave deliberately, not on a dropout.</summary>
        public void Forget(ulong steamId)
        {
            if (!m_BySteamId.TryGetValue(steamId, out var member))
                return;

            if (member.ClientId != k_NotConnected)
                m_ClientIdToSteamId.Remove(member.ClientId);

            m_BySteamId.Remove(steamId);
        }

        public void Clear()
        {
            m_BySteamId.Clear();
            m_ClientIdToSteamId.Clear();
        }
    }
}
