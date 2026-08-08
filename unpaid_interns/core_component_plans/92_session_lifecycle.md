# 92 — Session Lifecycle for a Round-Based Game

**Source:** [`core_components.md`](../core_components.md) §12 — Build & Release Readiness
**Status:** ⚠️ Session works; its lifecycle assumes a deathmatch
**Depends on:** [Late Join / Rejoin Policy](08_late_join_rejoin_policy.md), [Crew Roster](19_crew_roster.md), [Join by Code](91_join_by_code.md)
**Blocks:** a session behaving sensibly across a multi-day run

## Summary

How long a session lives, who can be in it, and what happens to it at the boundaries of a run.

The connection layer works. What it lacks is any notion that the game it hosts has **rounds** — a session is created, players join whenever, and it ends when the host leaves. That is correct for a deathmatch and wrong for a contract that spans several days with distinct safe and dangerous phases.

The most visible symptom is a single number. `GameManager.MaxPlayer` is **32**, consumed by `SessionOptions.MaxPlayers` in three places (`GameConnection.cs:48`, `:130`, `UGS_ServerBootstrap.cs:73`). §16 flags it as an open question; [`19_crew_roster.md`](19_crew_roster.md) makes it concrete by requiring the roster and UI to be sized against a real crew size. A co-op extraction game is usually four, and every downstream system — monster power budgets, map size, quota scaling, loot density — is tuned against that number.

**Scope boundary:** [`08_late_join_rejoin_policy.md`](08_late_join_rejoin_policy.md) owns the *gameplay* join gate — what a new arrival gets and when they may spawn. This component owns the *session-level* answer: whether the session accepts connections at all, and how it starts, persists, and ends.

## How to Build

**Set the crew size, once, in one place**

- One configured value, read by all three `MaxPlayers` call sites and by the roster's buffer sizing. `GameManager.MaxPlayer = 32` becomes that value.
- Fix the incidental sizings that were derived from it. `ServerGameSystem` allocates `_overlapColliders = new Collider[16]` for spawn-point overlap testing against up to 32 players; [`31_entry_point_extraction_zone.md`](31_entry_point_extraction_zone.md) already requires that buffer to be sized to the real crew size.
- Record the number and its consequences somewhere the balance work can find it — monster budgets ([`26_location_catalogue.md`](26_location_catalogue.md)), quota scaling ([`64_quota_system.md`](64_quota_system.md), which deliberately does *not* scale with crew size), and loot density ([`39_loot_spawner.md`](39_loot_spawner.md)) are all tuned against it.

**Decide whether the session locks on deploy**

This is the decision `core_components.md` leaves open, and it follows directly from whichever join policy [`08_late_join_rejoin_policy.md`](08_late_join_rejoin_policy.md) selects:

- **Locked once deployed** — the session refuses new connections while a round is in progress and reopens in the hub. Simplest, and it means a mid-round arrival is impossible rather than merely handled. Bad for a friend who drops in late.
- **Open, with a gameplay gate** — the session always accepts connections; component 08's policy decides whether the arrival spectates or waits. More work, better experience.
- **Recommended: open at the session level, gated at the gameplay level.** Locking the session forecloses reconnection too, and [`25_reconnection.md`](25_reconnection.md) needs returning players admitted mid-round. A locked session and a grace window are contradictory.
- Whichever is chosen, a refused connection needs a **specific reason** the client can display, not a generic failure ([`91_join_by_code.md`](91_join_by_code.md) requires distinguishable messages for full, deployed, and not-found).

**Keep the session alive across rounds**

- A run spans multiple rounds and returns to the hub between them. The session must survive every location load and unload — nothing in [`05_location_load_unload_flow.md`](05_location_load_unload_flow.md) should touch the connection.
- Verify this explicitly. Repeated subscene load and unload is exactly the kind of operation that can disturb world state, and a session that silently drops on the second deploy is a bug that only appears after ten minutes of play.
- The session should outlive a failed run too, so the crew can start a new contract without reconnecting ([`07_game_over_win_resolution.md`](07_game_over_win_resolution.md) returns to the main menu or offers a fresh contract).

**Handle host departure as a run-ending event**

- With Relay + Lobby the host is a player's machine, so a host leaving ends the session for everyone. [`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md) calls this *"not a disconnect rule, it is a run-ending event"*.
- Combined with the host-owns-the-save decision ([`86_savesystem_integration.md`](86_savesystem_integration.md)), the run itself is lost. Detect host departure distinctly and return clients to the main menu with a message that says so, rather than a transport error.
- Tell players **before** they invest six in-game days. Surface it once in the lobby, not only at the moment it happens.
- Host migration is out of scope and should be stated as such. It is a substantial piece of work — the server world holds all authoritative state — and pretending it might arrive later shapes decisions badly.

**Clean the session up**

- A session whose crew has all left should terminate rather than linger. This matters most for the dedicated-server path, where [`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md) already requires that everyone disconnecting settles the round and returns to the hub *"without hanging"* — a permanently occupied session is the failure that produces.
- Add an idle timeout for a session with no connected players.
- Verify the dedicated-server build (`ServerBootstrap.cs`, guarded by `UNITY_SERVER`, reading `port=` and defaulting to 7979) handles the full lifecycle with no client ever attached.

**Keep the fallback paths working**

- Direct connect (`HostGameAsync`, `ConnectGameAsync`, `GetServerConnectionSettings`) never touches Unity Services and is how transport-level failures are distinguished from service-level ones ([`90_relay_and_lobby_service_enablement.md`](90_relay_and_lobby_service_enablement.md)).
- Those paths have no session and no session code, so anything keyed on session identity needs a fallback — the same requirement [`19_crew_roster.md`](19_crew_roster.md) places on stable player ids where UGS is unavailable.
- Test the lifecycle on all three paths: Relay session, direct connect, and dedicated server.

## Acceptance Criteria

- [ ] Crew size is a single configured value, read by all three `MaxPlayers` call sites and by the roster.
- [ ] `GameManager.MaxPlayer` no longer reports 32.
- [ ] The spawn-point overlap buffer is sized to the real crew size.
- [ ] The crew size and its balance consequences are documented where the tuning work will find them.
- [ ] The session lock-on-deploy decision is implemented and documented in this file.
- [ ] A refused connection reports a specific reason — full, deployed, or not found — distinguishable by the client.
- [ ] The session survives repeated location loads and unloads across at least five deploy cycles.
- [ ] The session survives a failed run, and a new contract can start without reconnecting.
- [ ] Host departure is detected distinctly and returns clients to the main menu with a message explaining the run's fate.
- [ ] Players are told before committing to a run that host departure ends it.
- [ ] Host migration is documented as out of scope.
- [ ] A session with no connected players terminates on an idle timeout rather than lingering.
- [ ] A dedicated-server build completes a full round lifecycle with no client attached and does not hang.
- [ ] The lifecycle works identically on the Relay, direct-connect, and dedicated-server paths.
- [ ] Anything keyed on session identity has a working fallback where no UGS session exists.
