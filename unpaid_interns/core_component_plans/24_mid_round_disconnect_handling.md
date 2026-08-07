# 24 — Mid-Round Disconnect Handling

**Source:** [`core_components.md`](../core_components.md) §3 — Multiplayer & Team
**Status:** ⚠️ Cleanup exists, gameplay semantics do not · **[MVP]**
**Depends on:** Crew Roster, Death & Body System, Networked Interaction Authority
**Blocks:** any playtest with real internet connections

## Summary

What happens to a player's loot, body, penalty, and crew slot when their connection drops mid-round.

The plumbing is correct and the meaning is absent. `ServerGameSystem.RefreshClientsMap` handles `ConnectionState.State.Disconnected` by finding and destroying the player entity via its `GhostOwner`, destroying the input entity via its `PlayerCommandTarget`, calling `RemovePlayerFromLeaderboard`, and clearing the `ClientsMap` slot. Every one of those is a reasonable cleanup step. Not one of them decides anything about the game.

The stakes are specific to this design. The quota is collective and death penalties are shared, so **one person's router reboot can cost four people a run.** Whatever rule is chosen will feel unfair to somebody; an *unstated* rule feels unfair to everybody and is also non-deterministic, since the outcome falls out of whatever the code happens to do that day.

**Scope boundary:** this component decides the *consequence* of a drop. Who may connect and when is [`08_late_join_rejoin_policy.md`](08_late_join_rejoin_policy.md); restoring a player who comes back is [`25_reconnection.md`](25_reconnection.md); the state field itself lives on [`19_crew_roster.md`](19_crew_roster.md).

## How to Build

**Add a grace window before anything happens**

- Do not apply consequences on the disconnect event. Start a timer instead, and hold the player's crew slot as `Disconnected` for a configurable window — 30–60 seconds is a reasonable starting point.
- Inside the window: their character is removed from the world (the existing cleanup is fine), but no penalty is applied, their loot is not resolved, and their roster entry is untouched.
- If they return inside the window, [`25_reconnection.md`](25_reconnection.md) restores them and nothing was lost. This single mechanism turns the majority of real-world drops — brief blips — into non-events, and it is the highest-value thing in this component.
- Distinguish a **deliberate quit** from a drop. Netcode surfaces both as `Disconnected`, so send an explicit "leaving" RPC on a clean exit and apply consequences immediately for it. A player who alt-F4s to dodge a penalty must not be rewarded with a grace window.

**Decide the loot rule**

- Recommended: **carried items drop at the player's last valid position**, exactly as they would on death. It is consistent with [`14_death_and_body_system.md`](14_death_and_body_system.md), it is recoverable by the crew, and it means a disconnect costs a trip rather than a payday.
- Vanishing the items is simpler and strictly worse — it makes a teammate's ISP delete real value with no counterplay.
- Keeping the items "with" the disconnected player and restoring them on rejoin sounds generous but creates an exploit: disconnect while holding the haul, reconnect after the danger passes. If rejoin restores inventory, it must only do so inside the grace window and only if the round has not settled.
- The drop happens when the grace window expires, not at disconnect — otherwise a two-second blip scatters someone's inventory across the floor for no reason.
- Clearing the item claims is mandatory regardless: [`20_networked_interaction_authority.md`](20_networked_interaction_authority.md) requires that no item stays claimed by a `NetworkId` that no longer exists.

**Decide the death rule**

- Does a disconnect count as a death for the credit penalty? Both answers are defensible and both have an exploit:
  - **Counts as a death** — punishes people for their internet, and a crew will resent it.
  - **Does not count** — creates a clean incentive to disconnect instead of dying, which is strictly better than dying and will absolutely be discovered.
- Recommended: **does not count as a death, but the drop is not free.** Apply the unbanked-loot loss (their items are on the floor, not in the extraction zone) and nothing else. That removes the alt-F4 exploit's upside — quitting mid-chase loses exactly what dying would have lost, minus the body penalty — while never charging a player for a blackout.
- Do not spawn a body for a disconnect. A body implies death, recovery, and a penalty the rule just declined to apply; it will confuse the crew about what happened.
- Whichever is chosen, **write it in this file** and make the Day Cycle Controller and the performance report agree with it.

**Keep the round coherent**

- Update the roster to `Disconnected` — a state distinct from `Dead` — so the total-crew-loss check in [`02_day_cycle_controller.md`](02_day_cycle_controller.md) does not end the round early because it counted a dropped player as a corpse.
- Define the edge case where **everyone disconnects**. The server has a round in progress and nobody in it. It should settle the round and hold in the hub, not spin forever; a dedicated-server build in particular needs this to avoid a permanently occupied session.
- Announce the drop through the repurposed `ActionFeed` (§9). A crew that does not know someone left will wait for them.

**Handle the host specially**

- With the chosen Relay + Lobby strategy the host is a player's machine, so a host drop ends the session for everyone. That is not a disconnect rule, it is a run-ending event.
- Detect it distinctly and return clients to the main menu with a clear message rather than a raw transport error. Combined with the host-owns-the-save decision in [`06_session_persistence.md`](06_session_persistence.md), the run itself is at stake, and players deserve to be told that before they commit to a six-day contract.

## Acceptance Criteria

- [ ] A disconnect starts a configurable grace window; no penalty, loot drop, or roster removal occurs inside it.
- [ ] A clean quit is distinguished from a drop and applies consequences immediately.
- [ ] The chosen loot rule is implemented and documented in this file, and items drop at the last valid position when the window expires.
- [ ] All item claims held by the disconnected player are released immediately, at disconnect, not at window expiry.
- [ ] The chosen death-penalty rule is implemented, documented, and matches the Day Cycle Controller and the performance report.
- [ ] Disconnecting mid-chase is not mechanically better than dying.
- [ ] The roster marks the player `Disconnected`, distinct from `Dead`, and the round does not end early as a result.
- [ ] No body ghost is spawned for a disconnect (or the contrary rule is documented).
- [ ] Every player disconnecting settles the round and returns the session to the hub without hanging.
- [ ] The drop is announced to the remaining crew.
- [ ] A host disconnect returns all clients to the main menu with a clear message, not a transport error.
- [ ] Two players dropping on the same tick corrupts neither `ClientsMap` nor the roster.
- [ ] A drop during settlement does not change the settled total or double-apply a penalty.
- [ ] A dedicated-server build handles all of the above with no client attached at the end.
