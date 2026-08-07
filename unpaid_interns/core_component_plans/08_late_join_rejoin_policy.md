# 08 — Late Join / Rejoin Policy

**Source:** [`core_components.md`](../core_components.md) §1 — Game Loop & Session State
**Status:** ⚠️ Partial — plumbing exists, policy does not · **[MVP]**
**Depends on:** Run Manager, Day Cycle Controller, Hub State, Crew Roster
**Blocks:** any playtest longer than one uninterrupted round

## Summary

Who can join, when, and what happens to someone who drops.

The project currently has an answer, and it is the wrong one for this game: `ServerGameSystem.HandleJoinRequests` spawns a character for **anyone who connects, at any moment**. In a deathmatch that is correct. In a round-based co-op extraction game it means a stranger can materialize inside a location mid-round, fully alive, next to a monster.

The disconnect side is equally undefined. `ServerGameSystem.RefreshClientsMap` destroys the dropped player's character and input entities on `ConnectionState.State.Disconnected` — correct cleanup, zero gameplay meaning. Nothing decides whether their carried loot falls to the floor, whether they count as a death against the crew's credits, or whether they can come back.

This matters more than it sounds. The quota is collective and the penalties are percentage-based, so **one person's router hiccup can cost everyone the run.** That needs a deliberate rule, not emergent behavior.

## Scope — Read This Before Building

The connection lifecycle is covered by four components, and they were split after this file was written. **This component owns the join gate only: who may connect, when, and what they get on arrival.** Everything else has moved:

| Concern | Owner |
| --- | --- |
| Who may connect and when; what a new arrival gets | **this file** |
| Stable player identity and per-player crew state | [`19_crew_roster.md`](19_crew_roster.md) |
| What a mid-round drop costs — loot, penalty, grace window | [`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md) |
| Restoring a returning player to the right state | [`25_reconnection.md`](25_reconnection.md) |

The sections below on identity, disconnect consequences, and rejoin are retained as **requirements this component places on those files**, not as work to do here. Implement the join gate; hold the rest to the linked plans so the rules exist in exactly one place.

## How to Build

**Define the join policy**

- Choose one and implement it explicitly in `HandleJoinRequests`:
  - **Hub-only join** — connections during a round are held until the crew returns. Simplest and safest; the joiner waits.
  - **Join as spectator** — mid-round arrivals spectate and deploy with the crew next round. Better experience, needs Spectator Mode first.
  - **Locked session** — the session refuses new connections once deployed. Simplest of all, worst for friends dropping in.
- Whichever is chosen, the joiner must receive a clear explanation rather than an unexplained wait.
- Gate the spawn on the Day Cycle Controller's phase, not on connection state — connection and readiness-to-play are now different things.

**Fix identity for rejoin** *(requirement on [`19_crew_roster.md`](19_crew_roster.md) — build it there)*

- `ClientsMap` is indexed by `NetworkId`, which netcode reassigns on reconnect. A returning player is a **different `NetworkId`** and will not match their previous slot, so identity cannot be built on it.
- Use a stable identifier instead. UGS anonymous authentication (`AuthenticationService.Instance`, already initialized in `GameConnection.StartServicesAsync`) provides a persistent player id — pass it in `ClientJoinRequestRpc` alongside the existing `PlayerName` and key crew state on that.
- Keep a server-side record of departed players for the duration of the run so a returning id can be recognized and reattached.

**Define the disconnect consequence** *(requirement on [`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md) — decide it there)*

- Decide and implement, at minimum:
  - Does carried loot drop at their position, or vanish? Dropping is more forgiving and more consistent with the death rules.
  - Do they count as a death for the credit penalty? Counting it punishes the crew for someone else's ISP; not counting it creates an incentive to alt-F4 instead of dying.
  - Does their body appear and need recovering?
- Add a short grace window before applying consequences, so a brief blip is not immediately punished.
- Extend the Crew Roster with a `Disconnected` state distinct from `Dead` and `Extracted`, so the round-end check does not mistake a dropped player for a dead one.

**Implement rejoin** *(requirement on [`25_reconnection.md`](25_reconnection.md) — build it there)*

- On reconnect within the same run, restore the player to the correct state for the current phase: alive in the hub, or spectating if a round is in progress.
- Netcode for Entities provides no rejoin support out of the box — the server must hold the state and reattach it.
- If rejoin is out of scope for now, say so explicitly and make the disconnect consequence match that reality rather than leaving both undefined.
- The join gate and the rejoin path share one entry point — `HandleJoinRequests` — so this component must check "is this a returning player?" *before* applying the new-arrival policy, or a reconnecting intern gets treated as a stranger and spawned fresh.

**Handle the host**

- The host leaving ends the session for everyone, and with Relay the host is a player's machine. Detect it and return clients to the main menu with a clear message rather than a connection error.
- Combined with component 06's host-owns-the-save decision, this means host departure ends the run. Make sure players know that before they invest six in-game days.

## Acceptance Criteria

- [ ] The chosen join policy is implemented in `HandleJoinRequests` and gated on round phase, not connection state.
- [ ] A player connecting mid-round never spawns as a live character inside a location.
- [ ] A mid-round joiner sees an explanation of why they are waiting or spectating.
- [ ] `HandleJoinRequests` distinguishes a returning player from a new arrival before applying the join policy.
- [ ] A new arrival is added to the crew roster and appears on every client's roster.
- [ ] Joining is refused once the crew is at the configured size, with a clear message rather than a silent failure.
- [ ] Crew state is keyed on a stable player identifier, not `NetworkId`, verified by reconnecting and confirming the player is recognized.
- [ ] Disconnecting mid-round applies the defined loot rule, verified by checking the world after the drop.
- [ ] The defined death-penalty rule for disconnects is applied consistently and documented.
- [ ] A brief disconnect inside the grace window does not apply consequences.
- [ ] The Crew Roster distinguishes `Disconnected` from `Dead`, and a round does not end early because a dropped player was counted as dead.
- [ ] Rejoining within the same run restores the correct state for the current phase — or rejoin is explicitly out of scope and documented as such.
- [ ] The host disconnecting returns all clients to the main menu with a clear message, not a raw connection error.
- [ ] Two players disconnecting simultaneously is handled without corrupting the roster or `ClientsMap`.
