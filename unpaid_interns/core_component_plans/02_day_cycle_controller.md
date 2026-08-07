# 02 — Day Cycle Controller

**Source:** [`core_components.md`](../core_components.md) §1 — Game Loop & Session State
**Status:** ❌ Not started · **[MVP]**
**Depends on:** Run Manager, Round Timer
**Blocks:** Spawn Director, extraction, end-of-round summary, difficulty escalation

## Summary

The Day Cycle Controller drives one round from deployment to settlement. Where the Run Manager tracks the whole contract, this owns a single day: it moves the round through its phases, decides when the round ends, and hands the results back.

The phases are roughly **Deploying → Active → Departing → Settling → Complete**. Most of the round sits in `Active`; `Departing` is the warning window before forced extraction; `Settling` is where banked loot is counted and sold.

Its most important job is answering *how does this round end*. Three ways: the crew leaves voluntarily, the clock forces departure, or every intern is dead or left behind. All three must converge on the same settlement path so loot accounting can't be bypassed.

This is server-authoritative. The phase must be identical for everyone — an intern who thinks they have five minutes left when the host thinks the ship is leaving is a broken game.

## How to Build

**Create the phase state machine**

- Add `Assets/Scripts/Gameplay/Run/DayCycleController.cs` as a `GhostMonoBehaviour` implementing `IGhostManager` and `IUpdateServer`, registered in `ManagerGhostsSpawner.ManagersToSpawn` like the Run Manager.
- Define a `RoundPhase` enum and replicate it as a single `[GhostField]`, alongside `RoundStartTick` and the count of interns still in the field.
- Drive transitions in `UpdateServer` only. Clients read the phase and react; they never set it.
- Make every transition explicit and one-way within a round — no phase should be re-enterable, so downstream systems can safely treat each transition as a one-shot event.

**Implement the three end conditions**

- **Voluntary departure** — an interaction at the extraction point that begins the departure sequence. Decide and document whether this requires unanimity, a majority, or any single intern; it is a design decision with real social consequences.
- **Forced departure** — the Round Timer reaching its limit moves the phase to `Departing`, then to `Settling` after a grace window.
- **Total crew loss** — when every intern is dead or left behind, end the round immediately. Per the design's loss condition, unbanked loot is forfeit.
- Read the total-crew-loss condition from the Crew Roster's `AnyAliveInField()` ([`19_crew_roster.md`](19_crew_roster.md)) rather than counting player entities. Counting entities is what makes a disconnect look like a death: `ServerGameSystem.RefreshClientsMap` destroys the character entity on drop, so an entity count reaches zero for a crew that is merely offline, and the round would end under everyone. The roster distinguishes `Dead` from `Disconnected`; use it.
- Decide what "every intern disconnected" means — the roster can be entirely non-alive without anyone dying. [`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md) requires this to settle and return to the hub rather than hang, which is a fourth path into `EndRound` even if it shares the total-loss reason code.
- Route all of them through a single `EndRound(RoundEndReason)` method so settlement logic exists once, not four times.

**Implement settlement**

- On entering `Settling`, enumerate everything registered as banked in the extraction zone, sum its value, and report it to the Run Manager via `RecordQuotaProgress` and `AddCredits`.
- Explicitly destroy or ignore unbanked loot so it cannot survive into the next round.
- Clear every outstanding item claim at teardown, per [`20_networked_interaction_authority.md`](20_networked_interaction_authority.md) — a claim that survives into the next round makes an item permanently unpickable.
- Apply per-death and unrecovered-body penalties before finalizing the total. Read deaths from the Crew Roster, and apply the disconnect rule from [`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md) exactly as that file documents it — a player who dropped must not be double-charged as both a death and a disconnect.
- Reject any state change arriving during `Settling`. A player reconnecting or an item being banked mid-settlement will corrupt the total; hold both until the hub.
- Then call the Run Manager's `AdvanceDay()` — settlement must complete before the day advances, or the deadline evaluation will read stale figures.

**Broadcast phase changes**

- Publish each transition through the shared EventBus so HUD, audio, and the Spawn Director react without direct references.
- Use `GhostGameObject.BroadcastRPC` for one-shot client notifications that must not be missed, following the `KillFeedEntryRpc` pattern in `GameLeaderboard.cs` — ghost fields alone can drop a transient state if a client's snapshot arrives late.

## Acceptance Criteria

- [ ] A round progresses through every phase in order, with the phase identical on host and all clients at each step.
- [ ] Voluntary departure ends the round for the entire crew, including interns still inside the location.
- [ ] The timer expiring ends the round even with no player input.
- [ ] All interns dying ends the round immediately without waiting for the timer.
- [ ] All three end conditions produce the same settlement behavior — verified by banking identical loot and comparing payouts.
- [ ] Loot inside the extraction zone is counted; loot elsewhere is not, and does not persist into the next round.
- [ ] Settlement completes before the Run Manager advances the day, verified by log ordering.
- [ ] Death penalties are applied to the settled total, not to the pre-settlement balance.
- [ ] Phase transitions fire exactly once per round — no duplicate or repeated events under lag.
- [ ] A client joining mid-round sees the correct current phase, not `Deploying`.
- [ ] Two consecutive rounds run cleanly with no state bleeding between them.
- [ ] A disconnect does not end the round early; the total-loss check reads the roster, not live entity count.
- [ ] Every crew member disconnecting settles the round and returns the session to the hub without hanging.
- [ ] A player who disconnects is not charged both a death penalty and a disconnect penalty.
- [ ] Banking and reconnection attempts during `Settling` are held and do not alter the settled total.
- [ ] All item claims are cleared at round teardown.
