# 04 — Hub / Between-Rounds State

**Source:** [`core_components.md`](../core_components.md) §1 — Game Loop & Session State
**Status:** ❌ Not started · **[MVP]**
**Depends on:** Run Manager, Location Load / Unload Flow
**Blocks:** Store, location selection, storage, upgrades, end-of-round summary

## Summary

The safe state between rounds. No monsters, no clock, no danger — the crew reviews what they earned, sees how far from quota they are, spends money, picks where to go next, and chooses when to deploy.

Mechanically it is downtime. Dramatically it is where the pressure lands: a hub with no threat in it is where players actually feel the quota, because it is the only moment they can think. Rounds are for panicking, the hub is for regretting.

The blocking problem is that **the project has no concept of this state at all.** `GlobalGameState` (`GameSettings.cs:9`) has exactly three values — `MainMenu`, `InGame`, `Loading` — and the flow assumes one continuous gameplay session from connect to disconnect. Adding a hub means changing the shape of the session, not adding a screen.

## How to Build

**Extend the global state model**

- Add a `Hub` value to `GlobalGameState` in `Assets/Scripts/Gameplay/GameManager/GameSettings.cs`.
- Audit every existing consumer before adding it — `InGameHUD.LateUpdate`, `SessionInfo`, `PauseMenu`, and the UI Toolkit style bindings all branch on `GameState` and currently assume `InGame` means "in a dangerous place". Several will need to distinguish hub from field.
- Add a `[CreateProperty]` display-style binding for hub UI, following the existing `InGameUI` and `MainMenuStyle` pattern, so hub screens show and hide declaratively.

**Make the hub a real place**

- Build the hub as its own scene rather than a menu overlay — a physical space players walk around supports the tone far better, and gives the store, storage, and departure control natural physical locations.
- Spawn players into it on session start, before any location is loaded, so the first thing a crew does is stand in their own base and read the quota.
- Keep it loaded or reloadable between rounds via the Location Load / Unload Flow (component 05).

**Make it authoritative and shared**

- Hub transitions are server-driven: the server decides when the crew returns to the hub and when they deploy. Clients follow.
- The deploy control must be a single shared action, not per-player — one intern pulling the lever commits everyone. It follows the shared-commit pattern registered in [`105_departure_and_extraction_resolution.md`](105_departure_and_extraction_resolution.md): **any intern may act, cheap actions need no confirmation, committing actions need a hold-to-confirm, and every one of them is announced.** Deploy is a committing action and is not reversible; destination selection is cheap and reversible, per [`27_location_selection_assignment.md`](27_location_selection_assignment.md). Do not invent a third convention here — three near-identical interactions with three different rules is how a crew learns to distrust all of them.
- Replicate the selected destination so everyone sees where they are about to go before it happens — as a location id on the Run Manager, per [`26_location_catalogue.md`](26_location_catalogue.md).
- Deploy is also where the round seed is rolled ([`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md)); it must be replicated before the load begins, not during it.

**Guarantee safety**

- Assert that no monster spawning, no round clock, and no damage sources are active in the hub. Bugs here are especially damaging because players correctly treat the hub as safe and stop paying attention. Friendly fire is disabled here too, per [`18_pvp_collision_and_friendly_fire.md`](18_pvp_collision_and_friendly_fire.md).
- Ensure round state from the previous location is fully torn down on entering the hub — leftover monsters or timers are the most likely failure mode. **That sequence has an owner:** [`106_round_teardown_and_state_reset.md`](106_round_teardown_and_state_reset.md) defines the order, the registration mechanism, and the leak check, and entering the hub is its final step. This component's obligation is to refuse to become playable until that verification has passed.
- Returning to the hub is where per-round crew state resets: dead and left-behind players become playable again ([`14_death_and_body_system.md`](14_death_and_body_system.md)), spectators return to normal characters ([`22_spectator_mode.md`](22_spectator_mode.md)), and the roster's per-round fields clear while per-run fields persist ([`19_crew_roster.md`](19_crew_roster.md)). This file asked for that to happen "in one place, on the hub transition" — steps 9 and 10 of the teardown sequence are that one place.

## Acceptance Criteria

- [ ] `GlobalGameState.Hub` exists and every existing `GameState` consumer has been reviewed and updated where needed.
- [ ] Players spawn into the hub at session start, not directly into a location.
- [ ] The hub shows current credits, quota, quota progress, and days remaining, all matching the Run Manager on every client.
- [ ] The round clock does not advance while in the hub.
- [ ] No monsters spawn and no damage can be taken in the hub, verified with the spawn director active.
- [ ] Deploying transitions the whole crew together; no player is left behind in the hub.
- [ ] Returning from a round lands the crew back in the hub with the previous location fully unloaded.
- [ ] Two full round-to-hub-to-round cycles run with no leaked entities, timers, or monsters.
- [ ] A client joining while the crew is in the hub arrives in the hub with correct state.
- [ ] Hub UI is hidden during a round, and field HUD is hidden in the hub.
- [ ] The selected destination and round seed are replicated and identical on every client before deploy begins.
- [ ] Entering the hub resets dead players, spectators, and per-round roster state in one place, and a crew that lost members last round is whole again.
- [ ] No damage of any kind, including friendly fire, can be dealt in the hub.
