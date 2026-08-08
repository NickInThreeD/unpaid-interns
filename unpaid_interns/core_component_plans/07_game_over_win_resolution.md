# 07 — Game Over / Win Resolution

**Source:** [`core_components.md`](../core_components.md) §1 — Game Loop & Session State
**Status:** ❌ Not started · **[MVP]**
**Depends on:** Run Manager, Day Cycle Controller, Session Persistence
**Blocks:** nothing — this is the terminal node of the game loop

## Summary

The moment the contract is judged. At the deadline the crew has either sold enough to meet the quota or they have not, and the consequence is total: continue to a harder quota, or the run ends and everyone dies or is fired.

Mechanically this is a small component — one comparison and a branch. Its importance is disproportionate to its size because it is the only thing that makes every prior decision matter. A quota that never actually kills anyone is just a score display.

The design specifies the failure state as collective: *"they all die or are all fired."* That is one outcome with two flavors of presentation, not two different mechanics. Worth deciding which the game commits to, because it sets the tone of the whole ending.

## How to Build

**Define the evaluation point**

- Evaluate exactly once, on the Run Manager's `EvaluateDeadline()`, after the Day Cycle Controller has fully settled the final round. Settlement must complete first or the evaluation reads a stale total — this ordering is the single most likely bug in this component.
- Compare accumulated quota progress against the current quota. Meeting it exactly must count as success, not failure; use `>=`.
- On success: increment `QuotasCompleted`, calculate and apply the next quota, reset cycle progress, and return the crew to the hub.
- On failure: set `RunState` to `Failed` and begin the ending sequence.

**Decide the mid-run total-crew-death rule**

- The design says quota failure ends the run. It does **not** say what happens when every intern dies inside a location with days still remaining.
- Two defensible answers: the run continues (they lost the day's unbanked loot and a day of the deadline, which is punishment enough), or the run ends immediately.
- The first preserves the quota as the single failure condition and makes bad rounds recoverable; the second makes every round lethal. **Pick one and record it** — leaving this implicit will produce inconsistent behavior between the Day Cycle Controller and this component.
- Whichever is chosen, it must be keyed on the Crew Roster's *dead* count, not on "nobody is in the field" ([`19_crew_roster.md`](19_crew_roster.md)). A round that ends because everyone disconnected is not a crew wipe, and treating it as one would let a network outage end a run — the exact failure [`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md) exists to prevent.
- **Decide whether `LeftBehind` counts toward the wipe.** [`105_departure_and_extraction_resolution.md`](105_departure_and_extraction_resolution.md) makes being left behind a fourth outcome, distinct from dead and from disconnected, and recommends it be lethal. If it is lethal, it counts; if it is not, a round can end with every intern left behind and nobody dead, and this component must not read that as a wipe. Both files must state the same answer.
- Note that a wipe does not zero the round: loot banked before it happened still pays out ([`02_day_cycle_controller.md`](02_day_cycle_controller.md)). A crew can lose everyone and still clear quota, which is a legitimately grim outcome the premise is well suited to.

**Build the ending**

- Replicate the outcome as a `[GhostField]` on the Run Manager so every client resolves identically, and additionally broadcast a one-shot RPC for the ending trigger, following the `KillFeedEntryRpc` pattern in `GameLeaderboard.cs`. Ghost state alone can be missed by a client whose snapshot arrives late.
- Present a run summary: days survived, total earned, quotas completed, deaths, and the final shortfall. This is where the dark-comedy tone pays off — a performance review delivered to people who are already dead.
- Freeze gameplay input during the ending so no one wanders off mid-verdict.

**Tear the run down**

- Delete or invalidate the run save on failure. A failed run that can be reloaded is not a failure. Coordinate with component 06 so this is a deliberate delete, not an orphaned file.
- Clear Run Manager state, hub storage, and purchased gear, then return to the main menu or offer a fresh contract.
- Ensure a new run started immediately after a failure begins from true defaults with no residue — this is the most common place for leaked state to appear.

**Make it testable**

- Add `ConfigVar` commands to force success and force failure at any point. Nobody should have to play a full contract to test the ending, and without this the component will be under-tested precisely because reaching it honestly is slow.

## Acceptance Criteria

- [ ] Reaching the deadline with progress exactly equal to the quota counts as success.
- [ ] Reaching the deadline short of quota sets `RunState` to `Failed` on the host and every client.
- [ ] Evaluation happens after final settlement, verified by log ordering — never on a stale total.
- [ ] Meeting quota raises the next quota, resets cycle progress, and returns the crew to the hub with credits intact.
- [ ] The chosen total-crew-death rule is implemented, documented, and consistent between the Day Cycle Controller and this component.
- [ ] A round ending because every player disconnected never triggers the crew-wipe rule.
- [ ] Every client sees the same outcome and the same ending screen, including a client under simulated latency.
- [ ] The run summary reports accurate days survived, total earned, quotas completed, and deaths.
- [ ] Player input is disabled during the ending sequence.
- [ ] A failed run's save is deleted and cannot be reloaded.
- [ ] Starting a new run immediately after a failure begins from defaults with no leaked credits, gear, or storage.
- [ ] Debug commands to force success and force failure both work in a build.
