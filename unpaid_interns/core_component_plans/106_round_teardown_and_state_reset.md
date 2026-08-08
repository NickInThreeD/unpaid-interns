# 106 — Round Teardown & State Reset

**Source:** [`core_components.md`](../core_components.md) §1 — Game Loop & Session State
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Day Cycle Controller](02_day_cycle_controller.md), [Departure & Extraction Resolution](105_departure_and_extraction_resolution.md), [Location Load / Unload Flow](05_location_load_unload_flow.md), [Hub State](04_hub_between_rounds_state.md)
**Blocks:** the loop repeating — every component with a "two consecutive rounds" acceptance criterion

## Summary

Making round two identical to round one.

Step 7 of `GAME_DESIGN.md`'s core loop is *"repeat"*, and it is the only step in the loop that no component owns. The project's entire scene flow assumes one continuous session: `ScenesLoader.LoadGameplayAsync` loads exactly one hardcoded scene once, and `UnloadGameplayScenesAsync` is only ever called while tearing down the whole session on return to the main menu. **Nothing in this codebase has ever run a second round**, and a quota game is six to twelve of them back to back.

The evidence that this needs an owner is already written across the plan set. Nineteen plans carry a teardown instruction and a "nothing leaks into the next round" acceptance criterion, each phrased slightly differently, each assuming somebody else sequences it:

> *"Clear every outstanding item claim at teardown"* — [`02_day_cycle_controller.md`](02_day_cycle_controller.md) · *"Entity count and memory return to baseline after unload"* — [`05_location_load_unload_flow.md`](05_location_load_unload_flow.md) · *"no leaked entities, timers, or monsters"* — [`04_hub_between_rounds_state.md`](04_hub_between_rounds_state.md) · *"No spectator camera, input mapping, or UI state leaks into the following round"* — [`22_spectator_mode.md`](22_spectator_mode.md) · *"a leaked banked flag on a pooled item instance would credit the next round for last round's scrap"* — [`43_loot_banking_deposit.md`](43_loot_banking_deposit.md) · *"a stale zone reference in the banking system after a round transition would credit the next round's items into the last round's total"* — [`31_entry_point_extraction_zone.md`](31_entry_point_extraction_zone.md)

Nineteen independent cleanup implementations, each verified by its own author against its own system, is how a project gets a bug that only appears on round three and only when someone died on round two. **This component is the ordering, the ledger, and the proof.**

It is deliberately small in logic and strict in discipline. It writes almost no gameplay code; it defines a sequence, a registration mechanism, and a verification harness that fails loudly when a system forgets.

**Scope boundary:** [`05_location_load_unload_flow.md`](05_location_load_unload_flow.md) owns *scene and subscene* loading and unloading, and its load barrier is the mirror image of this sequence. [`02_day_cycle_controller.md`](02_day_cycle_controller.md) owns settlement arithmetic. [`105_departure_and_extraction_resolution.md`](105_departure_and_extraction_resolution.md) owns per-intern outcomes. **This component owns everything that must be destroyed, cleared, or reset between the end of settlement and the crew standing in the hub — and the order it happens in.**

## How to Build

**Fix the order, because the order is the component**

Teardown is not "delete everything"; several steps read state that an earlier step would have destroyed. Run it server-first, with a client counterpart, in exactly this sequence:

| Order | Step | Where | Owner of the work |
| --- | --- | --- | --- |
| 1 | Freeze the round — no banking, no damage, no interaction | server | [`105_departure_and_extraction_resolution.md`](105_departure_and_extraction_resolution.md) |
| 2 | Settle: enumerate banked items, sell, apply bonuses and penalties | server | [`02_day_cycle_controller.md`](02_day_cycle_controller.md), [`65_selling_payout.md`](65_selling_payout.md), [`66_bonus_and_penalty_rules.md`](66_bonus_and_penalty_rules.md) |
| 3 | Snapshot everything the summary and report will need | server | [`76_end_of_round_summary.md`](76_end_of_round_summary.md), [`70_performance_report.md`](70_performance_report.md) |
| 4 | Release all claims and holds on every item and body | server | [`20_networked_interaction_authority.md`](20_networked_interaction_authority.md) |
| 5 | Despawn round ghosts: items, bodies, monsters, doors, hazards, deployed gear, the extraction zone | server | [`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md), [`49_monster_ghost_and_replication.md`](49_monster_ghost_and_replication.md), [`60_door_system.md`](60_door_system.md) |
| 6 | Destroy navigation data and runtime links | server | [`30_runtime_navmesh_baking.md`](30_runtime_navmesh_baking.md), [`17_climbing_and_verticality.md`](17_climbing_and_verticality.md) |
| 7 | Stop round systems: clock, spawn director, escalation, weather, power zones | server | [`03_round_timer_clock.md`](03_round_timer_clock.md), [`50_spawn_director.md`](50_spawn_director.md), [`35_environmental_conditions_weather.md`](35_environmental_conditions_weather.md), [`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md) |
| 8 | Unload the location scene and its subscenes on **every** world | server + clients | [`05_location_load_unload_flow.md`](05_location_load_unload_flow.md) |
| 9 | Reset per-round crew state; dead and left-behind interns become playable | server | [`19_crew_roster.md`](19_crew_roster.md), [`14_death_and_body_system.md`](14_death_and_body_system.md) |
| 10 | Reset per-round client state: spectator camera, fear, HUD, scan highlights, prompts | clients | [`22_spectator_mode.md`](22_spectator_mode.md), [`15_fear_and_stress_feedback.md`](15_fear_and_stress_feedback.md), [`71_hud.md`](71_hud.md) |
| 11 | Verify baseline, then enter the hub | server | this component, [`04_hub_between_rounds_state.md`](04_hub_between_rounds_state.md) |

- **Step 3 before step 5 is the constraint that is easiest to get wrong.** Per-item value, per-player attribution, and the map's total spawned value ([`70_performance_report.md`](70_performance_report.md) needs it for the grade) all live on objects that step 5 destroys, and none can be reconstructed afterwards. Snapshot into plain values, then destroy.
- **Step 4 before step 5**, so no claim outlives the thing it claimed. [`20_networked_interaction_authority.md`](20_networked_interaction_authority.md) already requires claims to be cleared at teardown; this is where "at teardown" acquires a precise meaning.
- **Step 9 after step 8.** Restoring a dead intern to playable while their location is still loaded gives them a live character in a building that is about to be deleted.
- [`04_hub_between_rounds_state.md`](04_hub_between_rounds_state.md) already asks for per-round resets to happen *"in one place, on the hub transition, rather than letting each system pick its own moment."* This is that one place, and steps 9 and 10 are what that file is describing.

**Make systems register rather than be remembered**

- Expose a single teardown registration — a `RoundScoped` interface, or an ordered callback list on the Day Cycle Controller ghost — that any per-round system implements. The sequence above is then a set of ordered phases, not a hardcoded list of nineteen calls that a twentieth system will be forgotten from.
- **The registration is the point.** A hardcoded teardown routine is correct exactly until the next component lands. A registry means adding a per-round system means implementing an interface, which is a compiler-enforced reminder rather than a documentation one.
- Registration must be idempotent and safe to call on a system that never started — a round aborted during loading tears down through the same path as a round played to completion ([`05_location_load_unload_flow.md`](05_location_load_unload_flow.md) requires a failure path that does not hang, and that path lands here).
- Server systems and client presentation systems register separately. They are different sequences on different machines and conflating them produces the class of bug where the host is clean and everyone else is not.

**Write down what survives, because that list is shorter and more important**

Everything not on this list is destroyed:

- **Run state** — day, credits, quota, progress, quotas completed, run seed, unlock state ([`01_run_manager.md`](01_run_manager.md)).
- **Per-run crew state** — stable player ids, names, deaths this run. Per-*round* fields on the same roster entries reset ([`19_crew_roster.md`](19_crew_roster.md)).
- **Hub storage** — retained equipment, stored loot, and anything delivered by the store ([`46_storage_hub_inventory.md`](46_storage_hub_inventory.md)).
- **Upgrades** ([`68_upgrades.md`](68_upgrades.md)) and **local player settings** ([`78_settings_options_menu.md`](78_settings_options_menu.md)).
- **Connections themselves.** A round boundary is not a reconnect; nobody is disconnected by teardown.

The corollary is the rule that catches most bugs: **a value that is neither on this list nor explicitly torn down is a leak.** The verification harness below exists to find those.

**Know the specific failure modes, because they are already documented**

Each of these is a real hazard some plan has already flagged, and each has a cheap check:

- **Static `Instance` fields and static queues.** `LeaderboardManager` needs `[ResetOnPlayMode(resetMethod: "ResetStaticState")]` for exactly this reason, and every manager ghost added since inherits the hazard. A stale static across a round transition behaves identically to one across a play-mode entry.
- **Pooled instances carrying state.** `SoundGameObjectPool` establishes the pooling pattern and [`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md) extends it to items. A pooled item returned with `Banked` still set is money printed from nothing ([`43_loot_banking_deposit.md`](43_loot_banking_deposit.md)). Reset on release, not on acquire.
- **Cross-scene references held by hub systems.** The banking system holding a destroyed zone, the HUD holding a destroyed player ghost, a monster's cached target. Null them explicitly rather than relying on Unity's fake-null.
- **EventBus subscriptions.** [`85_eventbus_integration.md`](85_eventbus_integration.md) and [`54_noise_emission_system.md`](54_noise_emission_system.md) both publish heavily from per-round systems. An unsubscribed handler on a destroyed object is a leak that grows linearly with rounds played and fails on the round where it finally fires.
- **Seed streams.** [`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md) derives per-system streams from the round seed. They are re-derived from the new round seed, never carried forward, or round two is not reproducible from its own seed.
- **ECS subscene reload.** [`05_location_load_unload_flow.md`](05_location_load_unload_flow.md) names this as the most likely source of a slow memory climb. It is also the one that will not show up in a single-round test at all.
- **Timers and coroutines** started during a round on objects that outlive it — the hub, the managers, the UI documents.

**Verify it mechanically, not by playing**

This is the component's real deliverable, and it is the reason it is worth being a component at all.

- Capture a **baseline snapshot** in the hub before the first deploy: entity count per world, live ghost count, GameObject count, registered EventBus handler count, and managed heap size.
- Capture the same snapshot at step 11 of every teardown. A count above baseline by more than a defined tolerance **fails loudly in development** with the category that grew. A number that grows by the same amount every round is a leak; a number that grows once is usually a legitimate hub addition and should be re-baselined deliberately.
- Add a soak test to the debug tooling: deploy, generate, spawn loot and monsters, kill a player, disconnect a player, depart, and repeat — driven entirely by the `ConfigVar` commands the other plans already require ([`88_debug_and_cheat_tooling.md`](88_debug_and_cheat_tooling.md)). Ten cycles unattended is enough to expose almost everything in the list above.
- Run the soak on a **dedicated server build** as well as a host. The host runs both worlds in one process and hides an entire class of ownership bug, which is the same reason [`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md) insists on role-separated layers.
- Log each teardown with the round number, each step's duration, and the resulting counts. A teardown that gets slower each round is the same signal as one that leaks.

**Keep the player informed while it happens**

- Teardown is not instant — despawning several hundred ghosts and unloading subscenes takes real time. Reuse the staged loading UI (`LoadingData.LoadingSteps`, `LoadingScreen.cs`) rather than freezing on the summary screen ([`05_location_load_unload_flow.md`](05_location_load_unload_flow.md) extends the same step enum for per-round loading).
- The summary and report are shown *during* steps 4–8, not after them. The crew reads the ledger while the building is being deleted behind it, which costs nothing and removes the entire perceived load time ([`76_end_of_round_summary.md`](76_end_of_round_summary.md) already requires that screen not block on input, which makes this safe).
- If teardown fails partway — an unload that never completes, a client that never confirms — fall back to a full session reset to the hub rather than continuing into a half-built round. Silent partial teardown is the worst outcome available and is what the verification step exists to make impossible.

## Acceptance Criteria

- [ ] Teardown runs as one ordered sequence with a single owner, not as per-system cleanup invoked ad hoc.
- [ ] The order matches the table in this file, and each step's prerequisites are respected.
- [ ] Summary and report data are snapshotted before any round object is destroyed, and the report's map-total value survives teardown.
- [ ] All item and body claims are released before the objects carrying them are despawned.
- [ ] Every round ghost — items, bodies, monsters, doors, hazards, deployed gear, the extraction zone — is despawned, with none surviving into the next round.
- [ ] Navigation data and every runtime navigation link are destroyed.
- [ ] The clock, spawn director, escalation, weather, and power systems are all stopped and reset.
- [ ] The location scene and every subscene are unloaded on the server world and on every client world.
- [ ] Dead and left-behind interns are playable again in the hub, and per-run crew state survives while per-round state does not.
- [ ] Spectator camera, fear state, HUD state, scan highlights, and interaction prompts are all cleared on every client.
- [ ] Per-round systems register for teardown through one mechanism; adding a new one without registering fails a check rather than silently leaking.
- [ ] Teardown is idempotent and safe for a round that failed during loading and never started.
- [ ] Run state, per-run crew state, hub storage, upgrades, and local settings all survive teardown intact.
- [ ] No connection is dropped by a round transition.
- [ ] A baseline snapshot is captured in the hub and compared at every teardown, failing loudly on growth beyond tolerance.
- [ ] Entity count, ghost count, GameObject count, EventBus handler count, and managed heap all return to baseline across ten consecutive rounds.
- [ ] A pooled item instance never carries `Banked`, `Retained`, or a claim from a previous round.
- [ ] Static manager state is reset between rounds as well as between play-mode entries.
- [ ] Seed streams are re-derived from the new round seed and never carried forward.
- [ ] An unattended ten-cycle soak — including a death, a disconnect, and all three round-end conditions — completes with no leak, no error, and no growing teardown duration.
- [ ] The soak passes on a dedicated server build as well as on a host.
- [ ] Each teardown is logged with round number, per-step duration, and resulting counts.
- [ ] The loading UI covers the transition; no player watches a frozen screen.
- [ ] The summary and report are readable while teardown runs, and do not block it.
- [ ] A teardown that fails partway resets the session to the hub rather than continuing into a partially built round.
- [ ] Round two, round three, and round ten are behaviourally identical to round one, verified by running the same seed in each slot and comparing the generated layout, loot totals, and spawn schedule.
