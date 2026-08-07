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
- Route all three through a single `EndRound(RoundEndReason)` method so settlement logic exists once, not three times.

**Implement settlement**

- On entering `Settling`, enumerate everything registered as banked in the extraction zone, sum its value, and report it to the Run Manager via `RecordQuotaProgress` and `AddCredits`.
- Explicitly destroy or ignore unbanked loot so it cannot survive into the next round.
- Apply per-death and unrecovered-body penalties before finalizing the total.
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
