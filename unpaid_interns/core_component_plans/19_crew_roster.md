# 19 — Crew Roster

**Source:** [`core_components.md`](../core_components.md) §3 — Multiplayer & Team
**Status:** ⚠️ Connection tracking exists, crew state does not · **[MVP]**
**Depends on:** Run Manager
**Blocks:** Death & Body System, Spectator Mode, Mid-Round Disconnect Handling, Reconnection, end-of-round summary

## Summary

The authoritative list of who is on this crew and what state each of them is in.

The project tracks *connections*: `ClientsMap` is a dynamic buffer indexed by `NetworkId` holding a connection entity, a player entity, and a character-controller entity, and `JoinedClient` carries a name and character index on the connection. That is enough for a deathmatch, where a player is either connected or not.

A quota game needs more, because **failure is collective**. Who is alive, who is dead, who made it back to the extraction point, and who dropped out are four different things, and the Day Cycle Controller, the death penalty, the end-of-round summary, and the round-end check all read them. Today none of that exists, so every one of those systems would invent its own answer.

This component is small and boring, and almost every other multiplayer component in the plan depends on getting it right. Build it early.

**Scope boundary:** this component owns the *data* — identity and per-player state. The *policies* that read and write it live elsewhere: who may join and when is [`08_late_join_rejoin_policy.md`](08_late_join_rejoin_policy.md), what a drop costs is [`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md), and how a returning player is restored is [`25_reconnection.md`](25_reconnection.md). Keep those decisions out of this file and this class.

## How to Build

**Pick a stable identity — before anything else**

- `NetworkId` is **not** an identity. Netcode assigns it per connection and reassigns it on reconnect, so a player who drops and returns is a different `NetworkId` and will not match their previous slot. Every rejoin, penalty, and stat feature breaks if built on it.
- Use the UGS anonymous-authentication player id, which is already available: `GameConnection.StartServicesAsync` initializes `AuthenticationService.Instance` before any session work. Pass it from client to server in `ClientJoinRequestRpc` alongside the existing `PlayerName` and `CharacterIndex`.
- Handle the direct-connect and dedicated-server paths, which never touch Unity Services (`HostGameAsync`, `ConnectGameAsync`, `ServerBootstrap`). Those sessions need a fallback id — a locally generated GUID persisted in player prefs is sufficient for LAN play and must not crash the roster when UGS is absent.
- Keep `NetworkId` as the *routing* key for the current connection. Identity and routing are two different lookups and conflating them is the bug this section exists to prevent.

**Define the state**

- Build the roster as a ghost dynamic buffer on a manager singleton, exactly as `LeaderboardManager.PlayerScoreEntry` does — that class is the working reference for a replicated per-player table, including the `_pendingPlayers` queue pattern that defers writes until `GhostGameObject.IsGhostLinked()` is true.
- Put it on the Run Manager ghost rather than a new one unless it grows large. One fewer manager prefab to register in `ManagerGhostsSpawner.ManagersToSpawn`, and the two are always read together.
- Per entry: stable player id, display name, current `NetworkId` (or an invalid sentinel when disconnected), a `CrewState` enum, and per-round stats — items banked, value banked, deaths this run.
- `CrewState` values: `InHub`, `Deployed`, `Extracted`, `Dead`, `Spectating`, `Disconnected`. These are not all mutually exclusive in the abstract, so define precedence explicitly — a player who dies and then disconnects is `Disconnected`, and their death still counts.
- Every field must be blittable. Use `FixedString64Bytes` for names, as `PlayerScoreEntry` already does.

**Own the transitions**

- Expose one server-only mutator per transition — `MarkDeployed`, `MarkExtracted`, `MarkDead`, `MarkDisconnected`, `MarkReconnected` — rather than a public setter. Guard each with the `Role != MultiplayerRole.Server` check and the `IsGhostLinked()` check, following `LeaderboardManager.AddKill`.
- Provide the derived queries the rest of the game actually asks: `AnyAliveInField()`, `AllAccountedFor()`, `LiveCrewCount()`. The Day Cycle Controller's total-crew-loss check is one of these; do not let it re-derive the answer from raw entries.
- Reset per-round state at round start — everyone returns to `InHub` then `Deployed`, dead players come back alive per [`14_death_and_body_system.md`](14_death_and_body_system.md). Per-*run* state (deaths this run, stable id) survives.

**Wire it to the existing systems**

- `ServerGameSystem.HandleJoinRequests` currently calls `AddPlayerToLeaderboard` on join and `RefreshClientsMap` calls `RemovePlayerFromLeaderboard` on disconnect. These are the two hook points; add roster calls beside them.
- **Do not remove a player from the roster on disconnect.** `RemovePlayer` deletes the leaderboard row today, which is correct for a deathmatch and fatal here — the roster entry is what a reconnect matches against, and what the disconnect penalty is applied to. Mark them `Disconnected` and keep the row for the life of the run.
- Publish roster changes through the shared EventBus so the HUD roster, action feed, and summary screens react without holding a reference.

**Fix the crew size while here**

- `GameManager.MaxPlayer` is 32, and `SessionOptions.MaxPlayers` reads it. That is a deathmatch number, and §16 flags it as an open question. The roster is the component that makes it concrete: size the buffer and the UI for the real crew size (4 is the genre default) and let monster power budgets and quota scaling be tuned against a known number.

## Acceptance Criteria

- [ ] Crew entries are keyed on a stable player id, not `NetworkId`, and a reconnecting player matches their existing entry.
- [ ] A fallback identity works on the direct-connect and dedicated-server paths where UGS is unavailable.
- [ ] The roster replicates to all clients and shows identical entries and states on host and every client.
- [ ] A client joining mid-session receives the full current roster, not an empty or partial one.
- [ ] Every `CrewState` transition is server-only; a client calling a mutator is rejected with a warning and changes nothing.
- [ ] State precedence is implemented as documented — a player who dies then disconnects reads `Disconnected` and still counts as a death.
- [ ] A disconnected player's entry is retained, not deleted, for the remainder of the run.
- [ ] `AnyAliveInField()` is the single source used by the Day Cycle Controller's total-crew-loss check.
- [ ] Per-round state resets at round start; per-run state does not.
- [ ] Banked-value and death stats per player are accurate at end of round and match the settlement total.
- [ ] The roster is correct after a full round with a death, an extraction, and a disconnect in the same round.
- [ ] Crew size is set from a single configured value, and `SessionOptions.MaxPlayers` no longer reports 32.
- [ ] Two players disconnecting on the same tick corrupts neither the roster nor `ClientsMap`.
