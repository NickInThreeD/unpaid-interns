# 51 — Difficulty Escalation

**Source:** [`core_components.md`](../core_components.md) §6 — Monsters & AI
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Spawn Director](50_spawn_director.md), [Round Timer](03_round_timer_clock.md), [Run Manager](01_run_manager.md)
**Blocks:** the round having an ending, risk-free farming being impossible

## Summary

Making staying longer worse, and making a run that is going well get harder.

Two escalations, on two timescales, and they are easy to conflate. **Within a round**, threat rises with the clock so that the decision to leave is forced by danger rather than by a countdown — `GAME_DESIGN.md` is explicit that *"there's no forced timer inside a location — the danger itself is the pressure."* **Across a run**, threat rises with the crew's success so that a competent team cannot settle into a safe, repeatable, profitable pattern.

The first is the design's core tension and is non-negotiable. The second is the thing that stops the game from being solved: once a crew finds a destination and a route that reliably clears quota with no risk, the game is over even though it is still running.

The distinction from the spawn director matters and should be held firmly: **the director spends a budget; this component decides how large the budget is allowed to get.** Putting both in one class produces a system nobody can tune, because two curves are being changed by one edit.

## How to Build

**Escalate within the round, primarily through the budget**

- The in-round curve is the spawn budget curve owned by [`50_spawn_director.md`](50_spawn_director.md), keyed on normalized time. This component supplies the shape and the reasoning; that one spends it.
- Add a second lever beyond quantity, because more of the same thing stops being scary: **eligibility by time**. Monsters carry an earliest normalized time ([`48_monster_data_definitions.md`](48_monster_data_definitions.md)), so the late round is not merely busier, it is qualitatively different. The thing that appears at 80% should be a thing the crew has not seen yet that day.
- A third lever, cheap and effective: **behaviour tightening**. Longer give-up times and longer last-known-position searches late in the round make the same monster harder to escape without changing its stats. Scale those from normalized time in [`55_chase_and_pathfinding.md`](55_chase_and_pathfinding.md).
- Resist scaling monster damage or health with time. Numeric inflation is invisible to the player and reads as inconsistency — "that one killed me in two hits and the last one took four" is not a lesson anyone can learn from.

**Escalate across the run, keyed on the quota — not on the crew's mood**

- The clean signal is already replicated and already central: **quota cycles completed** ([`01_run_manager.md`](01_run_manager.md)). Each time the crew clears a quota and the next one rises, threat rises with it. It is legible, it is fair, and it is the same number the player is already watching.
- This also solves risk-free farming without a hidden system: a crew that keeps clearing quota keeps facing more, so the safe route stops being safe on its own schedule.
- **Do not react to the crew's failure by making things easier.** [`50_spawn_director.md`](50_spawn_director.md) already refuses this and the reasoning is the same here — an invisible rescue system removes the quota's threat and, once suspected, makes every decision feel unearned.
- Reacting to *exceptional* success is defensible and should be gentle: a crew banking far above quota may face a modest additional escalation. Keep it small, keep it derived from a replicated number, and be prepared to cut it if playtesters describe the game as punishing them for playing well.

**Make it visible enough to be a decision**

- Escalation the player cannot perceive is not tension, it is variance. The crew must be able to tell the late round from the early round **without a number on screen**: more frequent audio cues, ambience shifts at the clock's phase boundaries ([`03_round_timer_clock.md`](03_round_timer_clock.md)), lighting changes, and the arrival of a monster type they know means the day is late.
- Across the run, the escalation should be **announced in fiction** — the employer's briefing noting that the contract has been upgraded. §13's tutorial section observes that this genre is unusually opaque; a stated escalation is one of the cheapest places to be transparent.
- Never show a raw difficulty number. It invites optimisation against a value that is a judgement, and it strips the horror of its ambiguity.

**Keep both curves in data**

- One config asset holding the in-round budget curve, the per-quota-cycle multiplier, the time-eligibility thresholds, and the behaviour-tightening ramps. All of it will be retuned repeatedly and none of it should require a recompile.
- Both curves must be replicated inputs, not locally computed: normalized time and quotas completed are both already on the shared-state inventory ([`23_shared_session_state_sync.md`](23_shared_session_state_sync.md)), so every client's presentation of escalation agrees with the server's simulation of it.

**Cap it**

- A run can in principle continue indefinitely, and an uncapped escalation eventually produces a round nobody can survive — which is a fine ending if it is *deliberate*, and a bug if it is arithmetic.
- Decide which: either cap escalation at a survivable ceiling and let the quota curve carry the ending, or let it grow without bound and accept that the run has a natural death. Recommended: **cap the spawn budget, keep escalating the quota.** Failure then comes from the money, which is what the design says kills the crew, rather than from a monster count no crew could handle.
- Whichever is chosen, record it here, and make [`07_game_over_win_resolution.md`](07_game_over_win_resolution.md) agree with it.

**Measure it rather than feeling it**

- Extend the spawn director's harness to report, per quota cycle: total power spent, monster count over time, and simulated encounter frequency. Escalation that looks right in a config curve and is imperceptible in play is the normal outcome of the first attempt.
- Instrument real rounds too — round duration, deaths, and value banked per cycle are exactly the balance telemetry §13 asks for, and escalation is the system that most needs it.

## Acceptance Criteria

- [ ] In-round escalation is expressed as the spawn budget curve, defined in a config asset, and keyed on normalized time.
- [ ] Threat measurably increases across a round in monster count, in monster variety, and in chase persistence.
- [ ] Monsters gated by earliest spawn time appear only in the late round, so late rounds differ qualitatively and not only in quantity.
- [ ] Monster damage and health do not scale with time of day.
- [ ] Across-run escalation is keyed on quotas completed and rises with each cycle.
- [ ] Difficulty never decreases in response to the crew performing badly.
- [ ] Any response to exceptional success is small, derived from replicated state, and documented here.
- [ ] A player can tell a late round from an early round without reading a number.
- [ ] Across-run escalation is announced in fiction before the round it applies to.
- [ ] No raw difficulty value is ever shown to players.
- [ ] All curves and thresholds live in one config asset and are tunable without a recompile.
- [ ] Escalation inputs are replicated shared state; host and clients agree on the current level.
- [ ] The escalation cap decision is implemented, documented here, and consistent with the run's failure condition.
- [ ] The headless harness reports power spent, monster count over time, and encounter frequency per quota cycle.
- [ ] Round duration, deaths, and banked value per quota cycle are instrumented for balance telemetry.
- [ ] A crew repeating the same destination and route across three quota cycles faces measurably increasing danger.
