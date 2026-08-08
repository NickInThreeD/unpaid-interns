# 64 — Quota System

**Source:** [`core_components.md`](../core_components.md) §8 — Economy & Progression
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Run Manager](01_run_manager.md), [Currency System](63_currency_system.md)
**Blocks:** Game Over resolution, difficulty escalation across a run, every decision the crew makes

## Summary

The number that kills everyone.

`GAME_DESIGN.md` puts it at the centre: *"a cumulative money quota that must be hit,"* and *"falling short of quota when time runs out is a fail state for the whole team."* Every other system in the game is instrumentation for one question — are we going to make it — and this component is what makes that question have an answer.

Its importance is out of proportion to its size. Mechanically it is a target, a deadline, and a growth curve. But the curve is what decides whether the game is tense or hopeless, and it is the single hardest thing in the project to tune, because it has to work at day one for a crew that is bad at the game and at day twenty for a crew that has mastered it.

§16 leaves *"is the quota per-cycle-escalating or a single fixed target?"* open. **It should escalate**, and the answer is not really in doubt: a fixed target is a game with an ending the crew walks toward at a constant difficulty, and the design explicitly says the cycle *"repeats"* under pressure. Recording the decision here closes the open question.

## How to Build

**Model the cycle, not just the number**

- A quota cycle is: a **target**, a number of **days** to reach it, and a **deadline** on which it is evaluated. The Run Manager already declares `CurrentQuota`, `QuotaProgress`, `QuotasCompleted`, and `DaysUntilDeadline` ([`01_run_manager.md`](01_run_manager.md)); this component defines what they mean and how they move.
- The reference uses a **fixed cycle length** — four days, counting 3 → 2 → 1 → 0, with the last day as the deadline ([`Assets/docs/core-loop/profit-quota.md`](../../Assets/docs/core-loop/profit-quota.md)). Fixed is right: a variable cycle length is one more thing the crew has to track and it adds no tension the target itself does not already provide.
- Evaluate **once**, at the deadline, after the final round has fully settled. [`07_game_over_win_resolution.md`](07_game_over_win_resolution.md) already flags the ordering as the most likely bug in that component; the same ordering constraint is this component's responsibility to honour.
- Meeting the target exactly counts as success. Use `>=`.

**Choose a growth curve, and choose it as data**

- The curve is the game's difficulty. It must live in a config asset — a starting quota, a growth function, and any per-cycle parameters — and it will be retuned more than anything else in the project.
- The reference escalates the **increase** quadratically with the number of quotas fulfilled, which makes the quota itself grow cubically and total money required grow quartically. That is aggressive, and it is deliberate: the run is meant to end.
- **Recommended: start gentler than the reference and tune upward.** A curve that is too steep produces runs that die at cycle three regardless of skill, which reads as the game being broken rather than hard. It is much easier to notice "nobody ever fails" than "everybody fails for reasons they cannot affect".
- Whether to add randomness to the increase is a real decision. Some variance stops the curve from being memorised; too much makes a run's difficulty a dice roll the crew cannot plan around. If randomness is used, draw it from the **run seed** so a run is reproducible ([`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md)) and cap the variance tightly.
- Do **not** scale the quota by crew size. A four-person crew clears more than a two-person crew, and scaling the target to compensate erases the advantage of having friends, which is the game's whole premise. If small crews prove unviable, fix it in loot density per location, not here.

**Keep progress and credits separate**

- `QuotaProgress` measures **value sold this cycle**; `TeamCredits` measures **money the crew currently has**. They are different numbers and conflating them is the most likely design bug in §8.
- The distinction matters because spending must not endanger the quota. A crew that sells 500 toward a 600 quota and then buys a 200-credit ladder has 300 credits and 500 progress — and if the store deducted from progress, buying equipment would be a trap that kills runs. Nobody would ever buy anything.
- Progress resets at the start of each cycle; credits do not.
- Both are on the shared-state inventory ([`23_shared_session_state_sync.md`](23_shared_session_state_sync.md)) and both must be identical on every client, because both feed the decision the crew makes every single round.

**Make the pressure visible and constant**

- The crew must always be able to answer *"how far behind are we, and how long do we have?"* without opening a screen. [`72_quota_and_deadline_display.md`](72_quota_and_deadline_display.md) owns the presentation; this component owns supplying a shortfall and a days-remaining value that are cheap to read and never stale.
- Surface the **shortfall**, not the raw progress. "410 short, one day left" is actionable; "190 / 600" requires arithmetic under stress, and players will do it wrong.
- The deadline approaching should escalate presentation — colour, audio, the employer's tone. This is where the workplace-comedy-horror premise does its best work: an increasingly passive-aggressive employer as the deadline nears costs nothing and lands every time.

**Handle the transitions cleanly**

- **Quota met** — increment `QuotasCompleted`, compute the next target from the curve, reset progress, reset the day counter, and announce it. [`51_difficulty_escalation.md`](51_difficulty_escalation.md) keys across-run threat escalation on `QuotasCompleted`, so this increment is also a difficulty event.
- **Quota missed** — hand off to [`07_game_over_win_resolution.md`](07_game_over_win_resolution.md), which owns the ending. This component does not implement game over; it produces the verdict.
- **Mid-cycle** — the quota never changes mid-cycle. A target that moves while the crew is working toward it is the fastest way to lose their trust.
- Persist the quota, progress, cycle, and quotas-completed with the run ([`06_session_persistence.md`](06_session_persistence.md)).

**Make it testable, because reaching cycle five honestly takes hours**

- `ConfigVar` commands to set the quota, add progress, force the deadline, and jump to an arbitrary cycle number. [`07_game_over_win_resolution.md`](07_game_over_win_resolution.md) already requires force-success and force-failure; this is the other half.
- Add a pure-logic test over the curve: quotas are monotonically increasing, no cycle produces a target the best-case total map value across all unlocked locations cannot cover, and the curve does not overflow at high cycle counts. Quota math is exactly the cheap, high-value test §11 says the project should start with.
- Model it in a spreadsheet before implementing. Expected value per round times days per cycle against the curve tells you where the run dies, and that number should be a decision rather than a discovery.

## Acceptance Criteria

- [ ] The quota escalates per cycle, and the decision to escalate is recorded here, closing the §16 open question.
- [ ] Cycle length is fixed, configured in data, and counts down visibly to a deadline day.
- [ ] Evaluation happens exactly once per cycle, after the final round has fully settled.
- [ ] Meeting the target exactly counts as success.
- [ ] The starting quota and growth curve live in a config asset and are tunable without a recompile.
- [ ] Any randomness in the increase is drawn from the run seed and is reproducible.
- [ ] The quota does not scale with crew size.
- [ ] `QuotaProgress` and `TeamCredits` are distinct; spending credits never reduces quota progress.
- [ ] Progress resets at the start of each cycle; credits persist.
- [ ] Both values are replicated, identical on every client, and never computed client-side.
- [ ] The shortfall and days remaining are always available and never stale.
- [ ] The quota never changes mid-cycle.
- [ ] Meeting quota increments `QuotasCompleted`, sets the next target, resets progress and the day counter, and is announced to the crew.
- [ ] Missing quota produces a verdict handed to the game-over component rather than resolving here.
- [ ] Quota, progress, cycle, and quotas-completed persist with the run and are wiped on failure.
- [ ] Debug commands can set the quota, add progress, force the deadline, and jump to a cycle, and all work in a build.
- [ ] An automated test asserts the curve is monotonic, non-overflowing, and never sets a target no location can supply.
- [ ] The curve has been modelled against expected round income before implementation, and the intended run length is documented.
