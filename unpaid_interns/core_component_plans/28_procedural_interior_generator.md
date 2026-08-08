# 28 — Procedural Interior Generator

**Source:** [`core_components.md`](../core_components.md) §4 — Location & World Generation
**Status:** ❌ Not started · **[MVP]**
**Depends on:** Location Catalogue, Deterministic Generation Seed, Location Load / Unload Flow
**Blocks:** [Runtime NavMesh Baking](30_runtime_navmesh_baking.md), [Entry Point / Extraction Zone](31_entry_point_extraction_zone.md), [Alternate Exits](32_alternate_exits.md), [Lighting & Power Grid](36_lighting_and_power_grid.md), [Loot Spawner](39_loot_spawner.md), Spawn Points / Vents, Door System

## Summary

Assembling an interior from modular room prefabs, sized by the destination, guaranteeing that everything is reachable and that there is a way out.

**This is the largest single piece of new work in the project**, and it is the component most likely to go wrong in ways that are expensive to fix later. It is also the component that makes the game replayable: `GAME_DESIGN.md` puts the tension in unfamiliarity — "random, unfamiliar locations" — and a hand-built map is unfamiliar exactly once per player.

The critical thing to internalize before writing any code: **the generator has four downstream customers, and each imposes a constraint that is cheap to honour during generation and brutally expensive to retrofit.**

1. **Runtime NavMesh baking** needs walkable, connected, watertight geometry with no interpenetrating floors. Monster pathfinding is built on it, and a generator that produces geometry the baker chokes on has to be partly rewritten.
2. **The loot spawner** needs tagged placement points with categories and densities, not arbitrary floor space.
3. **The spawn director** needs vent/emergence points that are away from players and readable.
4. **Networking** needs the layout to be identical on every machine and to cost nothing to replicate — which is why it is generated from a seed rather than streamed.

Read [`Assets/docs/world/interior.md`](../../Assets/docs/world/interior.md) before designing the module set. It documents a shipped version of this system — main entrance with a dedicated starting room, fire exits that can replace nearly any room's doorway, vents as the sole indoor spawn points, a per-interior power grid — and those four features are the structural skeleton, not decoration.

## How to Build

**Generate on the server, replicate the seed, build identically everywhere**

- The server rolls one seed per round and replicates it (see [`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md)). Every client — and the server — runs the same generator and gets the same building.
- **Never replicate the geometry.** A loot-dense procedural map has thousands of pieces; replicating them would exhaust the bandwidth budget in §13 before a single monster spawned.
- Generation must be **fully deterministic**: no `UnityEngine.Random`, no `Time`, no `DateTime`, no iteration over an unordered collection whose order can differ between machines, no floating-point accumulation that depends on frame timing. Every random draw comes from the seeded stream, in a fixed order.
- Run it inside the load barrier in [`05_location_load_unload_flow.md`](05_location_load_unload_flow.md), so no player enters a half-built building.

**Design the module set as a graph problem**

- A room module is a prefab with typed connection points (doorway, corridor mouth, stair top/bottom), a footprint volume, and metadata: category, loot point count, whether it may host the main entrance, whether it may host a fire exit.
- Assemble by connecting compatible points, then reject or backtrack on footprint overlap. Overlap testing is the whole algorithm's cost; keep footprints as simple boxes and use a spatial hash rather than physics queries.
- Size the target room count from the location's size multiplier (§26). Do not let it be unbounded — a runaway multiplier is how a generation step takes forty seconds and every player times out.
- **Guarantee connectivity by construction, then verify it.** Build as a tree (every room attaches to an existing room, so connectivity is automatic), then add loop connections between nearby rooms — loops are what make a chase survivable, because a dead end with a monster in the doorway is not a decision. Verify with a flood fill from the entrance afterwards anyway; construction invariants break.
- Guarantee the extraction point is reachable from every room. This is not the same as connectivity — a one-way drop can break it.

**Honour the four customers explicitly**

- **NavMesh** — decide the baking strategy *before* the module set is authored. Baking the whole assembled interior at once is simplest and slowest; pre-baking per-module NavMesh and stitching at connection points is far faster and constrains modules to align exactly at their seams. That constraint has to exist in the prefabs from day one. [`30_runtime_navmesh_baking.md`](30_runtime_navmesh_baking.md) recommends the stitched approach and fixes the agent radii; **no corridor may be narrower than the largest agent that can spawn in the location**, reconciled with the doorway-width requirement the collision mode places on the generator in [`18_pvp_collision_and_friendly_fire.md`](18_pvp_collision_and_friendly_fire.md).
- **Loot points** — author tagged spawn transforms in each module with a category (floor, shelf, hidden, high-value) and let the loot spawner draw against them ([`39_loot_spawner.md`](39_loot_spawner.md)). Placing loot by raycasting the finished geometry produces items in walls. Compute each point's **path distance from the extraction zone** during assembly and publish it — that number is what the spawner uses to build the risk gradient, and it is far cheaper to record here than to recover afterwards.
- **Power zones** — tag each module with a power zone id so lighting can be cut by area rather than per light ([`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md)), and reserve eligible rooms for the breaker box.
- **Vents / emergence points** — likewise authored per module, with a rule that none may be within a minimum distance of the entrance.
- **Doors** — every connection between modules is a candidate door. Doors are networked state (§7) and each one is a ghost, so the generator's door count directly sets a bandwidth floor. Budget it deliberately rather than putting a door in every opening.

**Place the fixed features**

- Exactly one **main entrance**, attaching to the exterior scene's entrance socket ([`33_exterior_approach_area.md`](33_exterior_approach_area.md)), with a dedicated starting room that has at least one exit. The extraction zone sits at or just outside it ([`31_entry_point_extraction_zone.md`](31_entry_point_extraction_zone.md)), and its position is what makes the far corner of the building expensive.
- Alternate exits per the location's declared count, dropping into a random part of the map. [`32_alternate_exits.md`](32_alternate_exits.md) owns the semantics and the generator owns the placement rule: far from the main entrance, measured by path distance rather than straight-line distance, with the exterior end fixed in the location's authored exterior.
- A **breaker box** if the location has a power grid, placed somewhere inconvenient enough that restoring power is a decision.
- Cap vertical extent to what the Climbing & Verticality component ([`17_climbing_and_verticality.md`](17_climbing_and_verticality.md)) can actually traverse. A generator that builds a shaft with no ladder has built a trap.

**Budget the performance**

- Generation runs during a loading screen, so a second or two is acceptable — but it runs on every client, including the weakest, and the load barrier means the slowest machine sets the deploy time for everyone.
- Procedural interiors destroy static batching and lightmapping assumptions, which §13 flags directly. Plan for GPU instancing and runtime light probes from the start; discovering this after the module set is authored means re-authoring it.
- Pool and reuse module instances between rounds rather than instantiating and destroying thousands of objects each deploy — the object-pooling infrastructure exists for audio (`SoundGameObjectPool`) and the same approach applies.
- Set a hard cap on rooms, doors, and props per location, and fail loudly in development if a location's data exceeds it.

**Make it testable, because it will be wrong**

- Add a headless generation harness that runs N seeds and asserts: connectivity, extraction reachable from every room, no overlapping footprints, room count within tolerance, at least one fire exit, loot point count within the location's range. `com.unity.test-framework` is installed with no tests written, and this is the highest-value place in the project to start — it is pure logic and cheap to cover.
- Add a debug command to regenerate with an arbitrary seed, and a top-down debug view of the assembled graph. Diagnosing a bad layout from inside first person is nearly impossible.
- Keep the seed of every generated layout in the log. A player reporting "the exit was walled off" is useless without it and actionable with it.

## Acceptance Criteria

- [ ] The same seed produces byte-identical layouts on server and every client, verified by comparing a layout hash across machines.
- [ ] Generation uses only the seeded random stream — no `UnityEngine.Random`, wall-clock time, or order-dependent iteration.
- [ ] No geometry is replicated; snapshot size is unaffected by interior size.
- [ ] Every room is reachable from the main entrance, verified by flood fill on every generated layout.
- [ ] The extraction point is reachable from every room, including across any one-way drops.
- [ ] Loop connections exist; no generated layout is a pure tree of dead ends.
- [ ] No two module footprints overlap.
- [ ] Exactly one main entrance exists, with a dedicated starting room with at least one exit.
- [ ] At least one fire exit exists, placed far from the main entrance by path distance.
- [ ] Loot points, vent points, and door candidates are authored per module and are present and correctly categorized in the assembled layout.
- [ ] Every loot point carries its path distance from the extraction zone, computed during assembly.
- [ ] Every module carries a power zone id, and the breaker box is placed in an eligible room.
- [ ] No corridor or doorway is narrower than the largest eligible monster agent, and the width also satisfies the collision-mode requirement.
- [ ] The interior attaches cleanly to the exterior's entrance socket with no gap or interpenetration.
- [ ] No vent point is within the minimum distance of the entrance.
- [ ] The layout bakes a valid NavMesh with no unreachable islands (verified once Runtime NavMesh Baking lands).
- [ ] Vertical connections are always traversable by the player's available movement verbs.
- [ ] Room, door, and prop counts respect the location's size multiplier and a hard cap.
- [ ] Generation completes within the loading budget on the lowest-spec target device.
- [ ] Module instances are pooled; five consecutive deploys return entity and memory counts to baseline.
- [ ] An automated test runs at least 1,000 seeds and passes every structural assertion.
- [ ] A debug command regenerates from an arbitrary seed, and a top-down debug view of the layout graph exists.
- [ ] The seed of every generated layout is logged.
