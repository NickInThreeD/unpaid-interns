# 88 — Debug & Cheat Tooling

**Source:** [`core_components.md`](../core_components.md) §11 — Technical Foundations
**Status:** ⚠️ `ConfigVar` and Play Mode support exist; no gameplay commands do · **[MVP]**
**Depends on:** nothing — build it alongside the first systems it tests
**Blocks:** every other component being testable in reasonable time

## Summary

The commands that make a ten-minute loop testable in ten seconds.

`core_components.md` gives the justification bluntly: *"without these, testing a 10-minute round loop is prohibitively slow."* That is the entire argument and it is sufficient. A quota system that can only be reached by playing four honest days will be tested twice; one with a force-deadline command will be tested fifty times, and the difference shows up in the shipped game.

There is a second, less obvious reason this is marked MVP. **Nearly every component plan in this project already declares debug commands as an acceptance criterion** — force a spawn, set the seed, grant credits, skip time, force the ending, dump shared state. Those are not twenty separate features; they are one console with twenty commands, and building the console once, early, is what makes each of those criteria a line of code rather than a small project.

The infrastructure is partly there. `ConfigVars.cs`, `PlayModeSettings.cs`, and Multiplayer Play Mode support all exist. What does not exist is a single command surface or any gameplay command behind it.

## How to Build

**Build the surface first, then the commands**

- One in-game console, reachable in a **build** and not only in the Editor. This is the requirement most often skipped and the one that matters most — §12 notes that Editor multiplayer testing does not prove a build works, and the bugs that only appear in builds are exactly the ones needing debug commands to diagnose.
- Back it with the existing `ConfigVar` system rather than a parallel mechanism.
- Commands need to run **on the right world**. Most gameplay commands are server-authoritative — granting credits, forcing a spawn, skipping time — so a client typing one must send a request that the server validates and executes. A command that mutates client-local state on a client will produce a desync that looks like a real bug.
- Gate them: enabled in development builds, and behind an explicit flag in release. Do not ship a command that grants credits to a public build without a switch.

**Cover the commands the plans already require**

Collected from across the component plans, so the console is specified rather than accumulated:

- **Time and phase** — set normalized time, freeze the clock, multiply its speed, force phase transitions, force the deadline ([`03_round_timer_clock.md`](03_round_timer_clock.md), [`64_quota_system.md`](64_quota_system.md)).
- **Economy** — grant and deduct credits, set the quota, add quota progress, force a sale, dump the transaction log ([`63_currency_system.md`](63_currency_system.md), [`64_quota_system.md`](64_quota_system.md), [`65_selling_payout.md`](65_selling_payout.md)).
- **Run flow** — force run success, force run failure, force-advance the day, force any destination including locked ones ([`07_game_over_win_resolution.md`](07_game_over_win_resolution.md), [`01_run_manager.md`](01_run_manager.md), [`27_location_selection_assignment.md`](27_location_selection_assignment.md)).
- **Generation** — set the next round seed, log the current one, regenerate from an arbitrary seed ([`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md), [`28_procedural_interior_generator.md`](28_procedural_interior_generator.md)).
- **Threat** — force-spawn a named monster at a named point, freeze the spawn director, set the power budget, dump the live roster, force a monster to full awareness, explain a specific detection ([`50_spawn_director.md`](50_spawn_director.md), [`53_perception_system.md`](53_perception_system.md)).
- **Items and gear** — spawn an item by id, force a purchase, grant or revoke an upgrade, clear storage ([`67_store_purchasing.md`](67_store_purchasing.md), [`68_upgrades.md`](68_upgrades.md), [`46_storage_hub_inventory.md`](46_storage_hub_inventory.md)).
- **Player** — teleport, god mode, set health, set stamina, set carry weight, kill self, force respawn ([`12_carry_weight.md`](12_carry_weight.md) needs a debug weight value before the inventory exists).
- **World** — force a weather condition, cut and restore power, disable a hazard ([`35_environmental_conditions_weather.md`](35_environmental_conditions_weather.md), [`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md)).
- **State inspection** — dump every shared-state value with the current `NetworkTick` on any machine ([`23_shared_session_state_sync.md`](23_shared_session_state_sync.md)).

**Build the overlays, because several systems are invisible by construction**

Commands answer "make this happen"; overlays answer "why did that happen". The plans require these specifically:

- **Perception** — sight cones, hearing radii, awareness levels, last-known positions ([`53_perception_system.md`](53_perception_system.md)). Without it, "it saw me through a wall, I think" is unfalsifiable.
- **Chase** — current path, search radius, give-up timers ([`55_chase_and_pathfinding.md`](55_chase_and_pathfinding.md)).
- **Targeting** — per-candidate score breakdown ([`56_threat_interest_targeting.md`](56_threat_interest_targeting.md)).
- **Noise** — events as expanding spheres with category and volume, plus a per-player noise log ([`54_noise_emission_system.md`](54_noise_emission_system.md)).
- **Items** — claim state, id, rolled value, banked flag, ghost role on nearby items ([`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md), [`20_networked_interaction_authority.md`](20_networked_interaction_authority.md), which notes it *"costs an hour and saves days"*).
- **Generation** — a top-down view of the assembled layout graph ([`28_procedural_interior_generator.md`](28_procedural_interior_generator.md)).
- **Navigation** — the baked surface and a live agent's path ([`30_runtime_navmesh_baking.md`](30_runtime_navmesh_baking.md)).
- **Bounds** — the location's computed bounds volume ([`34_out_of_bounds_handling.md`](34_out_of_bounds_handling.md)).

Most of these are **server-side data rendered on a client that deliberately does not receive it** ([`49_monster_ghost_and_replication.md`](49_monster_ghost_and_replication.md) forbids replicating targets and perception state). So the overlays need a **debug-only replication channel**, enabled explicitly, carrying what the shipping game withholds. Design that channel once rather than per overlay, and make it impossible to enable in a release build.

**Make the invariant checks part of the tooling**

Several plans specify development-mode assertions, and they belong here as a single reported surface rather than scattered log lines:

- Shared-state hash comparison between server and clients ([`23_shared_session_state_sync.md`](23_shared_session_state_sync.md)).
- Layout hash comparison after generation ([`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md)).
- Credits equal to starting balance plus the transaction log ([`63_currency_system.md`](63_currency_system.md)).
- Summary net equal to the round's actual credit change ([`76_end_of_round_summary.md`](76_end_of_round_summary.md)).
- No ghost id in two inventory slots, no held item without a slot ([`40_inventory_item_bar.md`](40_inventory_item_bar.md)).
- Item ghost holder and player slot array agreeing ([`40_inventory_item_bar.md`](40_inventory_item_bar.md)).

A violation must be **loud and immediate** — an on-screen banner, not a line in a log nobody reads.

**Support the test paths that exist**

- `PlayModeSettings` and Multiplayer Play Mode are `#if UNITY_EDITOR` only. Keep them, and treat them as a fast iteration loop rather than as verification — §12 is explicit that Editor multiplayer testing does not prove a build works.
- The `[ResetOnPlayMode]` pattern in `GameLeaderboard.cs` prevents static state leaking across Editor play sessions. Anything static added by debug tooling needs the same treatment, or the console itself becomes a source of phantom bugs.
- Add a **scenario launcher**: start directly into a location with a given seed, day, quota, and gear loadout. Reaching day four with three upgrades honestly takes an hour, and most bugs live there.

## Acceptance Criteria

- [ ] A single in-game console exists, backed by `ConfigVar`, and works in a standalone build.
- [ ] Server-authoritative commands issued from a client are sent as validated requests, never applied locally.
- [ ] Commands are enabled in development builds and gated behind an explicit flag in release.
- [ ] Every debug command declared as an acceptance criterion in another component plan exists and works in a build.
- [ ] Time can be set, frozen, and accelerated; phases and the deadline can be forced.
- [ ] Credits, quota, and quota progress can be set; the transaction log can be dumped.
- [ ] Run success and failure can both be forced.
- [ ] The next round seed can be set, the current one logged, and a layout regenerated from an arbitrary seed.
- [ ] Monsters can be force-spawned at a named point, the director frozen, and the live roster dumped.
- [ ] Items can be spawned by id, purchases forced, upgrades granted, and storage cleared.
- [ ] Player health, stamina, carry weight, and position can be set, and god mode toggled.
- [ ] Weather, power state, and hazard state can be forced.
- [ ] Shared state can be dumped with the current tick on host and client.
- [ ] Perception, chase, targeting, noise, item claim, generation, navigation, and bounds overlays all exist.
- [ ] A single debug-only replication channel carries server-side data to overlays and cannot be enabled in a release build.
- [ ] All development invariant checks report through one surface and fail loudly and visibly, not only to the log.
- [ ] A scenario launcher can start directly into a location with a given seed, day, quota, and loadout.
- [ ] Debug tooling introduces no static state that leaks across Editor play sessions.
- [ ] No debug command or overlay is reachable in a shipping build without the release flag.
