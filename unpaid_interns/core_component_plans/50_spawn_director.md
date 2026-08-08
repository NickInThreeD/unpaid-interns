# 50 — Spawn Director

**Source:** [`core_components.md`](../core_components.md) §6 — Monsters & AI
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Monster Data Definitions](48_monster_data_definitions.md), [Monster Ghost](49_monster_ghost_and_replication.md), [Round Timer](03_round_timer_clock.md), [Location Catalogue](26_location_catalogue.md), [Spawn Points / Vents](52_spawn_points_and_vents.md)
**Blocks:** Difficulty Escalation, round pacing, monster tuning being possible at all

## Summary

Deciding what shows up, when, and how much of it. `core_components.md` calls this **the single most important pacing knob**, which is not an exaggeration — it is the component that turns a location full of loot into a round with a shape.

The design's whole tension is stated in `GAME_DESIGN.md` as *"locations get more dangerous the longer players linger"*, and it explicitly says **the danger itself is the pressure, not a countdown.** That is a direct instruction to this component: the crew's sense of "we should go" must come from the room getting worse, not from a clock they are watching. If the spawn director produces a flat threat level, the round has no arc and the "when do we leave" decision degenerates into "when are our slots full".

The mechanism is a **power budget**, spent on periodic spawn cycles, weighted by time of day and location. Every monster carries a power cost ([`48_monster_data_definitions.md`](48_monster_data_definitions.md)) and every location carries indoor and outdoor budgets ([`26_location_catalogue.md`](26_location_catalogue.md)); the director is what spends one against the other.

## How to Build

**Run one cycle, on the server, on the tick**

- Server-only, driven from the round clock's normalized time ([`03_round_timer_clock.md`](03_round_timer_clock.md)) rather than `Time.deltaTime`. A spawn schedule derived from local time drifts, and a director that drifts produces a different round for the host than for everyone else.
- Fire a spawn cycle on a fixed interval — the reference genre uses a periodic attempt rather than continuous trickle, and periodic is easier to reason about and to tune. Each cycle: compute the currently allowed budget, subtract what is already spent on live monsters, and decide whether to spend the remainder.
- Draw every random decision from the **monster stream** of the round seed ([`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md)), so a reported round reproduces. This is the third consumer of the seed and the one where a shared stream would be most damaging — a change to loot placement silently reshuffling monster spawns is exactly the coupling that plan forbids.
- Keep indoor and outdoor pools **separately budgeted**, per the location data. An open exterior and a cramped interior tolerate completely different threat densities, and one shared budget means tuning one breaks the other.

**Make the budget curve the round's shape**

- The allowed budget is a function of normalized time, defined as a **curve in a config asset**, not as code. This curve is the round's difficulty arc and it will be retuned constantly.
- Start it low enough that the first minute is quiet. A crew that meets something in the first thirty seconds never establishes the false confidence the round needs to take away from them.
- Ramp it so that lingering is always worse than leaving — that is the design's stated pressure, and it means the curve must keep rising, never plateau at a level the crew can settle into.
- Allow a per-location multiplier so a hard destination is hard from the start rather than only at the end.
- **Keep spending through the departure window.** [`105_departure_and_extraction_resolution.md`](105_departure_and_extraction_resolution.md) puts an announced grace window between the decision to leave and the point of no return, and the curve is at its peak there. Stopping the director when the lever is pulled turns the crew's most dangerous run of the round into a walk through an empty building. The director stops at the point of no return, with the rest of the round systems ([`106_round_teardown_and_state_reset.md`](106_round_teardown_and_state_reset.md), step 7).

**Spawn where it is fair**

- Spawns come from authored emergence points ([`52_spawn_points_and_vents.md`](52_spawn_points_and_vents.md)), never from arbitrary positions. That component owns the telegraphing and the minimum-distance-from-entrance rule.
- **Never spawn within sight or minimum distance of a player.** A monster materialising in front of someone is not tension, it is a bug report, and it is the fastest way to lose trust in the threat layer.
- Prefer emergence points far from the crew's current position, but not so far that nothing ever finds them — a director that only spawns in empty wings produces a round where the crew never meets anything.
- If no valid point exists this cycle, **spend nothing and try again next cycle.** Do not relax the safety rules to hit a budget; an unspent budget is a quiet minute, which is a legitimate round.

**Respect the caps**

- Per-monster maximum simultaneous count, from the definition. A budget large enough to buy six of the same creature will buy six unless stopped, and six of anything is a different game.
- A global cap on live monsters, independent of budget, as a hard performance and bandwidth ceiling. §13's snapshot budget is the real constraint and it must not be reachable through data alone.
- Return power to the budget when a monster dies or despawns ([`49_monster_ghost_and_replication.md`](49_monster_ghost_and_replication.md)). Whether killing something should *immediately* free the budget for a replacement is a design decision — recommended **yes, with a delay**, so clearing a room buys real respite without permanently reducing the round's threat.

**Do not hide a difficulty director inside it**

- The temptation is to spawn less when the crew is doing badly. Resist it, for the same reason [`27_location_selection_assignment.md`](27_location_selection_assignment.md) rejects hidden destination weighting: an invisible system that rescues a struggling crew removes the quota's threat, and players who suspect it stop trusting any of their decisions.
- Reacting to *success* is different and defensible — [`51_difficulty_escalation.md`](51_difficulty_escalation.md) owns that, and it exists to prevent risk-free farming. Keep the two clearly separated: this component spends a budget, that one decides how large the budget is allowed to get.

**Make it testable, because it is the knob**

- `ConfigVar` commands to force a spawn of a named monster at a named point, to freeze the director, to set the budget directly, and to dump the current budget and live roster. Tuning a ten-minute round without these is prohibitively slow, and §11 already flags debug tooling as MVP for exactly this reason.
- A headless harness that runs N seeds per location and reports: total power spent over the round, monster count over time, time-to-first-encounter, and how often a spawn was skipped for lack of a valid point. That last number is the one that reveals a generator producing bad emergence layouts.
- Log every spawn decision — cycle, budget available, budget spent, monster chosen, point chosen, or the reason nothing spawned — with the round seed.

## Acceptance Criteria

- [ ] The director runs server-side only, driven from the round clock's normalized time.
- [ ] Every random decision draws from the monster seed stream, and the same seed reproduces the same spawn schedule.
- [ ] Changing the loot or interior streams does not alter monster spawns for the same seed.
- [ ] The budget curve lives in a config asset and can be retuned without recompiling.
- [ ] The first configured quiet period passes with no spawns.
- [ ] Threat measurably increases across the round; lingering is always more dangerous than leaving.
- [ ] Indoor and outdoor budgets are spent independently and cannot borrow from each other.
- [ ] A per-location multiplier changes the whole curve.
- [ ] Spawns occur only at authored emergence points.
- [ ] No monster ever spawns within the minimum distance of, or in line of sight of, a player.
- [ ] A cycle with no valid emergence point spends nothing rather than relaxing the rules.
- [ ] Per-monster maximum counts and a global live cap are both enforced, and the global cap is not reachable through data alone.
- [ ] A monster's power returns to the budget on death or despawn, after the configured delay.
- [ ] The director never reduces threat in response to the crew doing badly.
- [ ] Debug commands can force spawns, freeze the director, set the budget, and dump the live roster, and all work in a build.
- [ ] A headless harness reports power spent, monster count over time, time to first encounter, and skipped-spawn count across at least 200 seeds per location.
- [ ] Skipped spawns due to missing valid points are rare, and a location that skips often fails the harness.
- [ ] Every spawn decision is logged with the seed and enough detail to reconstruct the round's pacing.
- [ ] A full round at maximum budget holds the server frame and bandwidth budgets with four clients.
