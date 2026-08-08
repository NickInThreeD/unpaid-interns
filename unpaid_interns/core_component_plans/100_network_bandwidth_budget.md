# 100 — Network Bandwidth Budget

**Source:** [`core_components.md`](../core_components.md) §13 — Onboarding, Performance & Long Tail
**Status:** ❌ No budget, no measurement, and no relevancy rules
**Depends on:** [Item Ghost](38_item_ghost_networked_item_state.md), [Monster Ghost](49_monster_ghost_and_replication.md), [Relay & Lobby Service Enablement](90_relay_and_lobby_service_enablement.md)
**Blocks:** a loot-dense map being affordable and playable

## Summary

Snapshot size, and the fact that it costs money.

`core_components.md` states the problem in one sentence that contains both halves: **Relay bills on bandwidth, and ghost snapshot size scales with replicated entity count** — *"every item, monster, and door on the map is a potential ghost."* Most games treat bandwidth as a latency concern. Here it is also an operating cost, and it is the one that grows fastest with exactly the content the design calls for.

The numbers make it concrete. A loot-dense procedural interior might hold a hundred items, several dozen doors, and a full monster budget, replicated to four clients at tick rate. Handled naively — every ghost relevant to every client, transform-synced continuously — that is enormously more traffic than the deathmatch this codebase came from ever produced, and the deathmatch is what the current settings were tuned for.

The good news is that the mitigations are already specified per-component. What is missing is a **number to measure against** and the discipline of measuring.

## How to Build

**Set a per-client budget and measure against it constantly**

- Pick a target: bytes per snapshot and average bytes per second per client, on the worst-case map. Without a number, "it seems fine" is the only available assessment.
- Derive part of it from cost. [`90_relay_and_lobby_service_enablement.md`](90_relay_and_lobby_service_enablement.md) requires the free-tier limits and overage pricing to be recorded and linked here — a budget that ignores the bill is only half a budget.
- Measure with **thin clients** ([`96_editor_vs_build_test_paths.md`](96_editor_vs_build_test_paths.md) says this is what they are actually good at) so player count can be varied without needing four machines.
- Instrument it continuously rather than testing it once. Netcode for Entities exposes snapshot statistics; surface them in the debug overlay ([`88_debug_and_cheat_tooling.md`](88_debug_and_cheat_tooling.md)) so a change that doubles snapshot size is noticed the day it lands.

**Apply the three levers deliberately, per ghost type**

The plans already specify these; this component is where they are set as a coherent policy rather than three independent guesses.

- **Importance.** A priority ordering when the snapshot cannot carry everything. Players and monsters are high ([`49_monster_ghost_and_replication.md`](49_monster_ghost_and_replication.md)); items are low ([`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md)); shared session state is lowest and changes rarely ([`23_shared_session_state_sync.md`](23_shared_session_state_sync.md)). Set them relative to each other in one place, or they will be tuned individually and end up meaningless.
- **Relevancy.** Distance-based culling, and the biggest single win available. Most items on a large map are irrelevant to most clients most of the time.
  - The trap is **pop-in**: [`49_monster_ghost_and_replication.md`](49_monster_ghost_and_replication.md) requires the monster relevancy radius to exceed audible and fear-proximity range, or the horror arrives before the creature does.
  - [`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md) uses relevancy for a second purpose — replicating an item's rolled value only within scan range, so a client cannot enumerate the map's loot. [`16_player_scanner_ping_tool.md`](16_player_scanner_ping_tool.md) requires the radius to comfortably exceed scan range so values are present when a pulse fires.
  - [`75_monitoring_camera_system.md`](75_monitoring_camera_system.md) needs relevancy to follow the operator's **camera**, not their player position, or the feed shows an empty corridor.
  These four requirements interact, and setting relevancy radii is a single decision that has to satisfy all of them.
- **Quantization and change-only replication.** Position and rotation to the precision that is actually needed ([`49_monster_ghost_and_replication.md`](49_monster_ghost_and_replication.md) notes sub-centimetre monster positions are information nobody needs), and — the largest item win — **transform sync off for items at rest** ([`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md), [`47_physics_props_and_throwing.md`](47_physics_props_and_throwing.md)).

**Do not replicate what can be derived**

The cheapest bandwidth is the kind never spent, and several plans already take this route:

- **Geometry is never replicated.** The interior is generated from a seed on every machine ([`28_procedural_interior_generator.md`](28_procedural_interior_generator.md)), so a thousand-piece building costs the four bytes of the seed.
- **Derive rather than send.** [`03_round_timer_clock.md`](03_round_timer_clock.md) replicates only `RoundStartTick` and computes everything else; [`23_shared_session_state_sync.md`](23_shared_session_state_sync.md) calls this the pattern to copy rather than the exception.
- **Only ids cross the wire** for items, monsters, locations, and weather ([`87_data_driven_configuration.md`](87_data_driven_configuration.md)).
- **Never replicate AI internals** — targets, paths, perception memory ([`49_monster_ghost_and_replication.md`](49_monster_ghost_and_replication.md)). That is a security requirement first and a bandwidth saving second.

**Budget the door count as a design decision**

- [`28_procedural_interior_generator.md`](28_procedural_interior_generator.md) makes the point sharply: doors are ghosts, so **the generator's door count directly sets a bandwidth floor** — a cost paid on every snapshot for the whole round, whether or not anyone is near them.
- [`60_door_system.md`](60_door_system.md) accordingly requires doors at meaningful junctions rather than in every opening, with a per-location budget.
- This is the clearest case of a bandwidth constraint being a **level design constraint**, and it needs to be stated to whoever authors the module set rather than discovered in profiling.

**Profile the worst case, and profile it as the host**

- Maximum location size, maximum loot count, full spawn budget spent on the cheapest monsters (many small creatures, not one large one), maximum doors, four clients. Reuse the profiling scenario from [`99_performance_budget.md`](99_performance_budget.md).
- The host constructs snapshots for every client on top of playing, so its outbound cost scales with crew size. Measure the host's send rate specifically.
- Re-measure at each milestone and record it alongside the build verification artefacts ([`97_build_verification_pass.md`](97_build_verification_pass.md)) so growth is visible as a trend.

## Acceptance Criteria

- [ ] A per-client bandwidth budget is set in bytes per snapshot and bytes per second, against the worst-case map.
- [ ] Relay free-tier limits and overage pricing are recorded and factored into the budget.
- [ ] Snapshot statistics are surfaced in the debug overlay and visible during normal development.
- [ ] Ghost importance is set relative across players, monsters, items, doors, and session state in one place.
- [ ] Distance relevancy is applied to items, monsters, and doors.
- [ ] Monster relevancy radius exceeds audible and fear-proximity range; no monster pops in nearby.
- [ ] Item rolled values are replicated only within a radius comfortably exceeding scan range.
- [ ] Relevancy follows the monitoring operator's active camera, or that limitation is documented.
- [ ] Position and rotation are quantized to the precision actually needed.
- [ ] Items at rest have transform sync disabled, and awake-item traffic returns to zero when nothing moves.
- [ ] No geometry, AI internals, or derivable values are replicated.
- [ ] Door count per location respects a stated budget, communicated to module authors as a design constraint.
- [ ] Worst-case bandwidth is measured with thin clients across varying player counts.
- [ ] The host's outbound send rate is measured separately and stays within budget at full crew size.
- [ ] A change that materially increases snapshot size is detected in development rather than in a playtest.
- [ ] Bandwidth is re-measured at each milestone and recorded as a trend.
