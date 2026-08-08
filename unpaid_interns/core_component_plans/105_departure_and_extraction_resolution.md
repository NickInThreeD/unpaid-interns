# 105 — Departure & Extraction Resolution

**Source:** [`core_components.md`](../core_components.md) §1 — Game Loop & Session State
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Day Cycle Controller](02_day_cycle_controller.md), [Round Timer](03_round_timer_clock.md), [Entry Point / Extraction Zone](31_entry_point_extraction_zone.md), [Crew Roster](19_crew_roster.md), [Loot Banking](43_loot_banking_deposit.md)
**Blocks:** the core loop's central decision, [Round Teardown](106_round_teardown_and_state_reset.md), end-of-round accounting, monster chase termination

## Summary

Leaving. `GAME_DESIGN.md` names this the central decision of the entire game — *"the central decision every round is how long to stay"* — and step 5 of the core loop is written as a player action: *"players choose when the risk outweighs the reward and can pull out of the location at any time, forfeiting whatever hasn't been returned yet."*

Until now no component owned it. [`02_day_cycle_controller.md`](02_day_cycle_controller.md) owns the phase machine and lists voluntary departure as one of three ways a round can end, but explicitly defers the rule — *"decide and document whether this requires unanimity"*. [`31_entry_point_extraction_zone.md`](31_entry_point_extraction_zone.md) owns the physical control and hands the same question back — *"the unanimity question is component 02's to answer"*. [`19_crew_roster.md`](19_crew_roster.md) declares an `Extracted` state that nothing sets. [`55_chase_and_pathfinding.md`](55_chase_and_pathfinding.md) requires a monster to fall to Search when its target *"extracts"*, an event no component produces. [`22_spectator_mode.md`](22_spectator_mode.md) mentions a vote-to-leave-early as an open hook.

Five plans reference a mechanic none of them implements. **This component is that mechanic**, and it answers four questions that are currently unanswered anywhere:

1. **Who can start the departure, and can it be stopped?**
2. **What is the window between "we are leaving" and "we have left"?**
3. **What happens to an intern who is still inside the building when it closes?**
4. **What exactly is lost — and what survives even a total crew loss?**

**Scope boundary.** The phase enum, the `EndRound(RoundEndReason)` funnel, and settlement arithmetic belong to [`02_day_cycle_controller.md`](02_day_cycle_controller.md). The volume, the spawn transforms, and the physical control object belong to [`31_entry_point_extraction_zone.md`](31_entry_point_extraction_zone.md). What counts as banked belongs to [`43_loot_banking_deposit.md`](43_loot_banking_deposit.md). **This component owns the sequence between the trigger and the settlement, and the per-intern outcome it produces.**

## How to Build

**Model departure as a sequence, not an instant**

The end of a round is four beats, and collapsing any two of them removes something the design needs:

| Beat | State | What is true | Duration |
| --- | --- | --- | --- |
| 1 | `Active` | Nothing has been decided | most of the round |
| 2 | `Departing` | Departure is committed and announced; the crew can still run for it; **banking still works** | the grace window |
| 3 | Point of no return | Outcomes are frozen per intern; banking closes; the location stops accepting input | one tick |
| 4 | `Settling` | Accounting runs on a set that can no longer change | as long as it takes |

- Beats 2 and 3 are the ones that do not exist in any current plan, and beat 2 is where the game's best moments live: the sprint back with a full inventory and something behind you. A departure that resolves instantly deletes that moment entirely.
- **Banking must remain open for the whole of `Departing`.** [`43_loot_banking_deposit.md`](43_loot_banking_deposit.md) closes banking at `Settling`, which is correct — but the two are not adjacent, and reading that rule as "closed once the lever is pulled" would make the last dash pointless. The closing edge is the point of no return, and it is this component's to fire.
- The grace window is a tuned number in the same config asset as the day length ([`03_round_timer_clock.md`](03_round_timer_clock.md)). It must be long enough to cross the building from its furthest room at a loaded walking pace and short enough that it is a sprint rather than a stroll. Measure it against generated interiors rather than guessing; [`28_procedural_interior_generator.md`](28_procedural_interior_generator.md)'s size multiplier changes the answer per destination, so the window is per-location data with a global default.
- The round clock **keeps advancing during `Departing`**. [`03_round_timer_clock.md`](03_round_timer_clock.md) says the clock stops outside an active phase; `Departing` is an active phase for its purposes, and monsters must keep spawning and escalating throughout it. A last dash through a building that has gone quiet is not a last dash.

**Define the trigger rules — this file is the registry for shared crew commit actions**

Three separate components each specify a "one player commits the whole crew" interaction and each says it must stay consistent with the others: destination selection and deploy ([`04_hub_between_rounds_state.md`](04_hub_between_rounds_state.md), [`27_location_selection_assignment.md`](27_location_selection_assignment.md)) and departure ([`31_entry_point_extraction_zone.md`](31_entry_point_extraction_zone.md)). **Define the pattern once, here, and let those files reference it.**

The pattern — *cheap to change, deliberate to commit, visible to everyone*:

| Action | Who may | Confirmation | Reversible | Announced |
| --- | --- | --- | --- | --- |
| Change destination | any intern, in the hub | none | yes, until deploy | yes |
| Deploy | any intern, in the hub | hold-to-confirm | no | yes |
| Start departure | any **living** intern, from the extraction zone | hold-to-confirm | yes, until the point of no return | **loudly** |

- **Recommended: no unanimity requirement, and no vote.** A vote in a game where half the crew is deep inside a building and cannot be reached is a UI that blocks on people who are busy dying. The hold-to-confirm plus the grace window does the same job better: it makes an accidental press impossible and gives everyone else time to object *with their feet*.
- **Departure may be aborted** by a second interaction at the same control, by any living intern, at any point before the point of no return. This is what makes a mistaken or malicious pull recoverable, and it costs one boolean.
- Log every start and abort with the intern responsible, and announce both ([`77_action_feed.md`](77_action_feed.md)). A crew that cannot find out who ended the round will invent an answer.
- **The dead get no vote.** [`22_spectator_mode.md`](22_spectator_mode.md) already recommends this and the reasoning holds — they have no remaining stake and the living carry the risk. Note this is a deliberate divergence from the reference design, which lets spectators vote to leave early ([`Assets/docs/core-loop/time.md`](../../Assets/docs/core-loop/time.md)); that mechanic exists there to rescue a crew waiting on a lost or idle survivor, and the abort rule plus the forced-departure clock covers the same failure here without handing the round's ending to players who are no longer in it.
- The forced-departure trigger comes from the clock reaching its limit and enters the same sequence at beat 2 with the same grace window. The reference gives roughly an in-fiction hour of notice; the important property is that **forced and voluntary departure are the same code path**, so the last dash behaves identically whichever started it.

**Resolve each intern exactly once, at the point of no return**

This is the component's actual output: a per-intern outcome, written to the crew roster ([`19_crew_roster.md`](19_crew_roster.md)) in one pass on one tick.

- **Inside the extraction volume, alive → `Extracted`.** Use the zone's explicit inside test ([`31_entry_point_extraction_zone.md`](31_entry_point_extraction_zone.md)), never a trigger-stay flag — the same reasoning that file gives for loot applies to people.
- **Alive but outside the volume → `LeftBehind`.**
- **Already dead → stays `Dead`.** A body that was carried into the zone is `recovered` per [`14_death_and_body_system.md`](14_death_and_body_system.md); that is a property of the body, not of this pass.
- **Disconnected → stays `Disconnected`**, and is never reclassified as left behind. [`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md) owns their consequence and [`66_bonus_and_penalty_rules.md`](66_bonus_and_penalty_rules.md) forbids double-charging.

**`LeftBehind` does not currently exist in the roster's `CrewState` enum and must be added.** [`70_performance_report.md`](70_performance_report.md) and [`76_end_of_round_summary.md`](76_end_of_round_summary.md) both require the summary to distinguish *left behind* from *deceased* from *disconnected*, which is impossible with the enum as specified. Adding it here, with exactly one writer, is what makes those two plans implementable.

**Decide what "left behind" means — and it has to mean something**

The location unloads. An intern still inside it cannot simply persist, so this is not a question that can be deferred to the art pass.

- **Recommended: a left-behind intern is lost with the location.** A body spawns and is by definition unrecovered, the largest penalty applies ([`66_bonus_and_penalty_rules.md`](66_bonus_and_penalty_rules.md)), and everything they carried is gone with them.
- Report it as **missing rather than deceased** — a distinct line on the summary, and exactly the register the premise wants: the employer does not say someone died, it says the asset was not recovered. The reference design draws the same distinction ([`Assets/docs/core-loop/player-body.md`](../../Assets/docs/core-loop/player-body.md) reports an abandoned employee as *"MISSING"*).
- The alternative — they survive, teleported home, having lost only what they carried — is defensible and much gentler, and it makes the grace window mean far less. **Pick one and record it here.** What is not acceptable is leaving it undecided, because the Day Cycle Controller, the roster, the penalty rules, and the summary all branch on the answer.
- Either way, the player transitions to the same end-of-round state as everyone else. Nobody is left staring at an unloading scene.

**Define what is lost — including on a total crew loss**

`GAME_DESIGN.md` is unambiguous about unbanked loot: it is *"forfeited"*. The cases around it are not, and two plans currently imply different answers.

| Where it was at the point of no return | Outcome |
| --- | --- |
| Banked scrap resting in the extraction zone | **Sold.** This is what banking means |
| Equipment `Retained` in the zone | **Comes home** to hub storage, pays nothing |
| Carried in any intern's inventory — extracted or not | **Lost** |
| Loose anywhere in the location | **Lost** |
| A body inside the zone | Recovered; reduces that intern's penalty |
| A body anywhere else | Unrecovered; larger penalty |

- The row that resolves an existing ambiguity is the first one. [`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md) and [`46_storage_hub_inventory.md`](46_storage_hub_inventory.md) state that on total crew loss *"hub storage survives, everything carried into the field is lost"*, which can be read as forfeiting banked scrap on a wipe. It does not: **anything resting in the extraction zone has already arrived**, and [`02_day_cycle_controller.md`](02_day_cycle_controller.md) requires all three end conditions to converge on one settlement path. A wipe pays out what was banked before it happened, and nothing else. Record the same rule in all three files.
- **Extraction is not a per-intern payout condition.** An intern who banked 400 credits and then died contributed 400 credits. Value follows the item into the zone, never the person out of it — otherwise the correct play is to stop scavenging early and stand on the pad, which inverts the entire risk gradient.
- Destroy the forfeited set explicitly at teardown rather than relying on scene unload ([`106_round_teardown_and_state_reset.md`](106_round_teardown_and_state_reset.md)). A pooled item instance that survives with a stale `Banked` flag credits the next round for this one's scrap ([`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md)).

**Make it loud, because it is the only announcement that must never be missed**

- Replicate the departure state as an absolute `[GhostField]` — `DepartureStartTick` plus the phase — so a client that misses an RPC still converges and a late joiner reads the correct remaining window ([`23_shared_session_state_sync.md`](23_shared_session_state_sync.md)). Add `DepartureStartTick` to that file's shared-state inventory.
- Additionally broadcast a one-shot RPC on start, on abort, and at the point of no return, following the `KillFeedEntryRpc` pattern in `GameLeaderboard.cs`. State for correctness, RPC for timing — the split [`23_shared_session_state_sync.md`](23_shared_session_state_sync.md) requires.
- Every channel fires at once and every one of them must reach a player at the bottom of a dark stairwell: the action feed ([`77_action_feed.md`](77_action_feed.md)), a HUD countdown in its reserved region ([`71_hud.md`](71_hud.md)), an audio stinger and a continuing cue ([`83_ambience_and_time_cues.md`](83_ambience_and_time_cues.md)), and a visual indicator that satisfies the no-audio-only rule ([`79_accessibility.md`](79_accessibility.md)).
- The countdown is the one place this game should show a number. Everywhere else the HUD is encouraged to withhold ([`71_hud.md`](71_hud.md)); here, ambiguity produces an unfair death rather than tension.
- Make the extraction zone findable from anywhere during `Departing` — the scanner already treats it as long-range and through-geometry ([`16_player_scanner_ping_tool.md`](16_player_scanner_ping_tool.md)), and this is the window that requirement exists for.

**Terminate the threat layer correctly**

- Publish an extraction event per intern so [`55_chase_and_pathfinding.md`](55_chase_and_pathfinding.md) can send a pursuing monster to Search rather than instantly retargeting. That plan already requires this behaviour for a target that *"extracts"* and has had no event to hang it on.
- Enforce the zone's monster-entry rule as a chase-termination condition, per the decision recorded in [`31_entry_point_extraction_zone.md`](31_entry_point_extraction_zone.md). Do not write a second rule here.
- Do **not** stop spawning during `Departing`. [`50_spawn_director.md`](50_spawn_director.md) keeps spending its budget until the point of no return, and [`51_difficulty_escalation.md`](51_difficulty_escalation.md)'s curve is at its peak precisely then. Suppressing threat during the escape converts the round's climax into a walk.
- After the point of no return, freeze damage application to extracted interns so nobody is killed during the settlement animation by something that was already mid-swing.

**Make it testable, because reaching it honestly takes ten minutes**

- `ConfigVar` commands to start departure, abort it, set the grace window, jump straight to the point of no return, and force a named intern's outcome. Every downstream system — settlement, penalties, the summary, the report, teardown — is reached through this component, and without these commands each of them is ten minutes from the nearest test.
- Add a pure-logic test over the resolution pass: a matrix of interns who are inside, outside, dead, disconnected, and mid-reconnect at the point of no return, asserting exactly one outcome each and no double-counting against [`66_bonus_and_penalty_rules.md`](66_bonus_and_penalty_rules.md)'s expectations.
- Test the ugly timing cases deliberately: an intern crossing the zone boundary on the exact tick of the point of no return, an item released a tick before it, a death on the same tick as extraction, and a disconnect during the grace window.

## Acceptance Criteria

- [ ] Departure runs as an explicit sequence — trigger, announced grace window, point of no return, settlement — and the phase is identical on host and every client at each beat.
- [ ] Voluntary and forced departure enter the same sequence and behave identically after the trigger.
- [ ] Any living intern can start departure from the extraction zone, with a hold-to-confirm that cannot fire accidentally.
- [ ] Departure can be aborted by any living intern at any point before the point of no return, and never after it.
- [ ] Starting and aborting are announced to the whole crew and logged with the intern responsible.
- [ ] Dead players cannot start, abort, or vote on departure.
- [ ] Banking continues to work throughout the grace window and is refused from the point of no return onward.
- [ ] The round clock, spawn director, and escalation curve all continue to run during `Departing`.
- [ ] The grace window is configurable globally and overridable per location, and is long enough to cross the largest generated interior at a loaded pace.
- [ ] Every intern receives exactly one outcome at the point of no return — `Extracted`, `LeftBehind`, `Dead`, or `Disconnected` — evaluated with the extraction zone's inside test.
- [ ] `LeftBehind` exists in the crew roster's `CrewState` enum and this component is its only writer.
- [ ] A disconnected intern is never reclassified as left behind, and is never charged both penalties.
- [ ] The left-behind rule is implemented as decided and documented in this file, and the Day Cycle Controller, roster, penalty rules, and summary all agree with it.
- [ ] A left-behind intern is reported as missing rather than deceased.
- [ ] Banked scrap pays out under all three end conditions, including a total crew loss.
- [ ] Equipment retained in the zone comes home under all three end conditions.
- [ ] Items carried by an extracted intern are lost, and extraction confers no per-player payout advantage.
- [ ] Forfeited items are explicitly destroyed at teardown, and no `Banked` flag survives onto a pooled instance.
- [ ] Departure state is replicated as absolute state and is correct for a client that misses the RPC or joins during the window.
- [ ] The departure warning reaches a player deep inside the building through the feed, the HUD, audio, and a non-audio visual channel simultaneously.
- [ ] A remaining-time countdown is displayed for the whole grace window in the HUD's reserved region.
- [ ] The extraction zone is findable by scanner from anywhere in the location during `Departing`.
- [ ] An extracting intern sends a pursuing monster to Search rather than to an instant new target.
- [ ] Extracted interns cannot be damaged after the point of no return.
- [ ] Debug commands can start, abort, set the window, skip to the point of no return, and force an intern's outcome, and all work in a build.
- [ ] An automated test covers the resolution matrix — inside, outside, dead, disconnected, reconnecting — with exactly one outcome per intern.
- [ ] An intern crossing the boundary on the exact tick of the point of no return resolves deterministically and identically on host and client.
- [ ] Three consecutive rounds ending by voluntary departure, by the clock, and by total crew loss each settle correctly with no state carried between them.
