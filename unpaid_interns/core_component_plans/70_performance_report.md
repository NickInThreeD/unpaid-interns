# 70 — Performance Report

**Source:** [`core_components.md`](../core_components.md) §8 — Economy & Progression
**Status:** ⚠️ Working replicated-scoreboard plumbing exists with the wrong semantics
**Depends on:** [Crew Roster](19_crew_roster.md), [Selling / Payout](65_selling_payout.md), [Bonus & Penalty Rules](66_bonus_and_penalty_rules.md), [Loot Spawner](39_loot_spawner.md)
**Blocks:** Rank / Progression, the round's decisions being judged

## Summary

The screen where the day gets graded.

`core_components.md` describes this as a **repurposing**, and the repurposing is unusually clean. `LeaderboardManager` + `LeaderboardUi` already implement everything structurally required: a ghost dynamic buffer of per-player entries (`PlayerScoreEntry` with `[GhostField]` on `NetworkId`, `PlayerName`, `Kills`, `Deaths`), the deferred-write queues that handle calls arriving before the ghost links, `BroadcastRPC`/`ConsumeRPC` for transient events, and a `ListView`-driven UI bound from a `VisualTreeAsset` template. **The plumbing is right. The semantics are wrong** — kills and deaths are deathmatch scoring, and this game has a shared objective.

The report is also where the game's tone does its heaviest lifting. `GAME_DESIGN.md` describes an employer that *"treats interns as expendable labor"*, and a corporate performance review delivered to a crew that just lost someone is the single best comedic vehicle the premise offers. The reference's version — grading F to S, assigning individual employees joke notes like *"the laziest employee"* ([`Assets/docs/core-loop/performance-report.md`](../../Assets/docs/core-loop/performance-report.md)) — is precisely the register, and it costs almost nothing on top of numbers the game already has.

**Scope boundary:** this component is the *between-rounds judgement and grade*. The immediate post-round breakdown of what was banked and earned is [`76_end_of_round_summary.md`](76_end_of_round_summary.md); in practice they are one screen with two sections, and this file owns the grading half.

## How to Build

**Convert the buffer, do not rebuild it**

- Replace `PlayerScoreEntry`'s `Kills` and `Deaths` with the fields this game grades on: value banked, items banked, deaths this round, bodies recovered, and survival status. The `[GhostField]` layout, the `FixedString64Bytes` name, and the buffer mechanics all stay.
- **Key it on the stable player id**, not `NetworkId` — [`19_crew_roster.md`](19_crew_roster.md) establishes that `NetworkId` is reassigned on reconnect and is a routing key rather than an identity. `PlayerScoreEntry` currently uses it as *"the unique key for a player"*, and that comment is the bug.
- Better still: **do not maintain a second buffer at all.** The crew roster already holds per-round stats — items banked, value banked, deaths this run — and [`19_crew_roster.md`](19_crew_roster.md) recommends putting it on the Run Manager ghost. The report should read the roster rather than duplicating it, and `LeaderboardManager` should be retired rather than converted once the roster lands.
- Whichever path is taken, `RemovePlayer` must go. It deletes a player's row on disconnect, which [`19_crew_roster.md`](19_crew_roster.md) forbids: the row is what a reconnect matches against and what the penalty applies to, and a disconnected intern must still appear in the report.

**Grade the crew, not the individuals**

- One grade for the whole crew. The quota is collective and the failure is collective; a per-player grade reintroduces the competitive framing this repurposing exists to remove.
- The reference's inputs are the right ones: **value recovered as a fraction of the value that was actually on the map**, combined with deaths. The fraction matters — it makes a small location and a large one comparable, and it means a crew that stripped a poor map cleanly is graded well.
- [`39_loot_spawner.md`](39_loot_spawner.md) knows the map's total value; that number must be recorded at spawn time and carried to settlement, because it cannot be reconstructed afterwards once items are destroyed.
- Deduct for deaths and for unrecovered bodies, using the same events [`66_bonus_and_penalty_rules.md`](66_bonus_and_penalty_rules.md) priced. The grade and the penalty should never disagree about what happened.
- Letter grades over percentages. `D` is a judgement; `43%` is a number players will try to optimise, and optimising a grade is not what the game is about.

**Give individuals notes, not scores**

- Per-player notes are where the comedy lives and where individual recognition belongs. Derive them from data the game already collects: most valuable single item, most damage taken and survived, furthest from the extraction zone, first to die, banked nothing at all.
- Notes must be **observations, not rankings**. "Sustained the most injuries" is funny; "3rd place" is a scoreboard, and a scoreboard in a co-op game creates exactly the blame dynamic [`66_bonus_and_penalty_rules.md`](66_bonus_and_penalty_rules.md) is trying to avoid.
- Report survival status per intern — extracted, deceased, left behind, or disconnected — which the roster distinguishes once `LeftBehind` is added ([`19_crew_roster.md`](19_crew_roster.md), written by [`105_departure_and_extraction_resolution.md`](105_departure_and_extraction_resolution.md)). A player who disconnected reads as disconnected, not as dead.
- The map's total spawned value ([`39_loot_spawner.md`](39_loot_spawner.md)) that the grade divides by must be captured during settlement, before teardown destroys the items — step 3 of [`106_round_teardown_and_state_reset.md`](106_round_teardown_and_state_reset.md) exists to guarantee that ordering, and without it this grade cannot be computed at all.

**Reuse the UI, adapt the semantics**

- `LeaderboardUi` binds a `ListView` from `ScoreItem.uxml` with `name`, `kills`, and `deaths` labels. Rebind those to the new fields and the screen is most of the way there; the column headers are the actual work.
- Show the crew grade prominently, the itemised payout from [`65_selling_payout.md`](65_selling_payout.md) and [`66_bonus_and_penalty_rules.md`](66_bonus_and_penalty_rules.md) beneath it, and the per-intern rows with status and note last. Grade first, arithmetic second, comedy third.
- Every figure must reconcile with the credited amount. A crew that cannot square the report with their balance will assume the economy is broken, and in a game where the payout decides survival that assumption ends the session.
- Follow the accessibility requirement in §9 — the report is dense text and the place a colour-only status indicator would be least excusable.

**Show it at the right moment, to everyone**

- After settlement completes and before the hub is playable, so the round is closed before the next decision starts. [`02_day_cycle_controller.md`](02_day_cycle_controller.md) settles then advances the day; the report sits between.
- Every client sees the **same report**. The grade and totals come from replicated state, and a client whose snapshot arrives late must converge rather than render zeros ([`23_shared_session_state_sync.md`](23_shared_session_state_sync.md) requires a loading state rather than zeros in exactly this window).
- Dead players see it too. They have the largest stake in how the round was judged.
- Do not block on input from all players. One person reading slowly should not hold four others in a results screen; a dismissable screen with a timeout is kinder than a ready-check.

**Feed rank from here and nowhere else**

- [`69_rank_and_progression.md`](69_rank_and_progression.md) derives XP solely from this grade, awarded equally to every crew member. That is the only consumer, and keeping it single-source is what stops a second scoring system from appearing.

## Acceptance Criteria

- [ ] Kill and death scoring is removed entirely; no component records a kill to any scoring system.
- [ ] Per-player report data is keyed on the stable player id, not `NetworkId`.
- [ ] A disconnected player still appears in the report; no row is deleted on disconnect.
- [ ] The report reads per-player stats from the crew roster rather than maintaining a duplicate buffer, or the duplication is documented as temporary with a removal plan.
- [ ] One grade is produced for the whole crew; no per-player grade or ranking exists.
- [ ] The grade uses value recovered as a fraction of the map's actual total value, combined with deaths.
- [ ] The map's total spawned value is recorded at spawn and available at settlement.
- [ ] The grade and the penalties agree about deaths and unrecovered bodies.
- [ ] Grades are letters, not percentages.
- [ ] Per-intern notes are observations derived from collected data, never rankings.
- [ ] Survival status distinguishes alive, deceased, left behind, and disconnected.
- [ ] The existing `ListView` and `ScoreItem.uxml` are reused with rebound fields.
- [ ] Every figure on the report reconciles exactly with the credited amount.
- [ ] Status and grade are readable without relying on colour alone.
- [ ] The report appears after settlement and before the hub becomes playable.
- [ ] Every client sees an identical report, and a late-joining or lagging client shows a loading state rather than zeros.
- [ ] Dead and spectating players see the report.
- [ ] The report does not block on input from all players.
- [ ] Rank XP is derived from this grade and from no other source.
- [ ] Two consecutive rounds produce independent reports with no data carried over.
