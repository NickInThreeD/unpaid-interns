# 33 — Exterior / Approach Area

**Source:** [`core_components.md`](../core_components.md) §4 — Location & World Generation
**Status:** ❌ Not started
**Depends on:** Location Catalogue, Location Load / Unload Flow
**Blocks:** Alternate Exits (exterior ends), Environmental Conditions, Out-of-Bounds Handling, outdoor threat set

## Summary

The outdoor space between where the crew lands and where the building starts.

It looks like connective tissue and it does three jobs no other component does. It is the **decompression zone** — the walk out, and more importantly the walk back, which is where a full haul is at its most exposed and where the round's tension resolves. It is the **anchor** that makes a procedural interior locatable, because a fixed exterior is the only stable landmark a crew ever gets. And it is a **habitat for a different threat set**, which is what stops every encounter in the game from being a corridor encounter.

The strong recommendation, and the one the reference design uses ([`Assets/docs/world/outdoor.md`](../../Assets/docs/world/outdoor.md)): **the exterior is hand-authored and fixed per location; only the interior is procedural.** Fixed exteriors are cheap, they are where per-location character actually lives, they give the alternate exits somewhere consistent to emerge ([`32_alternate_exits.md`](32_alternate_exits.md)), and they let a crew learn a destination without ever making the interior predictable. Procedurally generating both would double the hardest work in the project for almost no gain.

## How to Build

**Author one exterior per location**

- Each destination gets a hand-built scene containing the drop-off, the extraction zone, the main entrance socket, the exterior ends of every alternate exit, the terrain, and the boundary.
- The **main entrance socket** is the contract with the generator: a transform plus an orientation where the assembled interior attaches. Keep the socket's geometry identical across every location so any layout set can attach to any exterior.
- Load the exterior as part of the location load ([`05_location_load_unload_flow.md`](05_location_load_unload_flow.md)), before generation runs, so the generator has a socket to build from.
- Keep interior and exterior in **one scene and one physics space**. The alternative — separate scenes with a transition — turns every door into a load and makes a chase through the front door impossible. This decision is shared with [`32_alternate_exits.md`](32_alternate_exits.md) and must match.

**Size it against the round, not against the art**

- The walk from the drop-off to the entrance is a fixed tax on every trip, paid several times a round. Thirty seconds each way sounds atmospheric and is four minutes of a ten-minute day.
- Tune it against the round clock ([`03_round_timer_clock.md`](03_round_timer_clock.md)): the approach should cost enough that a crew notices, and little enough that a second trip is still worth considering. Measure it, do not guess it.
- Locations may legitimately differ here — a destination whose entrance is a long exposed walk is a harder destination, and that belongs in `LocationData` as a documented property rather than as an accident of the art pass ([`26_location_catalogue.md`](26_location_catalogue.md)).

**Make it a different kind of danger**

- Outdoors should be **open, visible, and fast**, where the interior is cramped, dark and slow. That contrast is the entire reason the space exists; an exterior that plays like a large room is wasted.
- Give it its own eligible-monster pool, budgeted separately from the interior pool — [`26_location_catalogue.md`](26_location_catalogue.md) already splits indoor and outdoor monster budgets for exactly this. Outdoor threats should be things you can see coming and have to outrun or avoid, not things that ambush.
- The exterior is where weather is felt ([`35_environmental_conditions_weather.md`](35_environmental_conditions_weather.md)). Fog that removes the open sightlines, or a storm that makes crossing open ground lethal, changes the approach's character without changing its geometry — the cheapest replayability available to this component.
- Resist filling it with loot. If the exterior pays, crews will farm it and never enter the building, which inverts the whole risk gradient. A small number of authored, high-visibility outdoor items is fine as flavour; a scrap field is not.

**Anchor navigation and orientation**

- The exterior is static geometry, so its NavMesh bakes offline and ships with the scene — no runtime bake needed ([`30_runtime_navmesh_baking.md`](30_runtime_navmesh_baking.md)). Only the interior needs the runtime path. Stitch the two at the entrance socket with an explicit link.
- Provide a landmark visible from most of the exterior — the drop-off itself, lit, is usually enough. A player emerging from a fire exit in fog with a full haul needs something to walk toward, and the scanner should not be the only answer.
- Verify orientation works at night and during a blackout. The exterior is the one place where "which way is home" must always be answerable.

**Bound it deliberately**

- The exterior is where players will try to leave the play space, because it is the only place that looks like it continues. [`34_out_of_bounds_handling.md`](34_out_of_bounds_handling.md) owns the mechanism; this component owns making the boundary read as terrain rather than as a wall — ridges, water, fences, drops.
- Author the boundary as part of the scene, not as an afterthought volume. A soft boundary the player never tests is worth more than a kill volume that works perfectly.

**Budget the performance**

- Outdoors is where the frame budget goes: large view distances, dynamic lighting, weather effects, and fog all cost more here than in a corridor. §13's performance budget should be established **against the exterior**, since it is the worst case.
- The existing `LightingProfile` / `LightingProfileApplier` pair applies global `RenderSettings` and is the hook for per-location mood, but note its limits before relying on it — see [`35_environmental_conditions_weather.md`](35_environmental_conditions_weather.md), which documents them.
- Keep exterior geometry static and batched. It is the one part of a location that can be, and giving that up is how a procedural game ends up with a static scene that runs worse than the procedural one.

## Acceptance Criteria

- [ ] Every location in the catalogue has a hand-authored exterior scene containing the drop-off, extraction zone, entrance socket, and alternate-exit exteriors.
- [ ] The entrance socket has an identical contract across locations, and any layout set attaches to any exterior.
- [ ] The exterior loads before generation runs, and the interior attaches to the socket with no gap or interpenetration.
- [ ] Interior and exterior share one scene and one physics space; passing through the main entrance requires no load.
- [ ] The approach time from drop-off to main entrance is measured, recorded per location, and within the tuned budget.
- [ ] The exterior has its own monster pool and power budget, independent of the interior's.
- [ ] Outdoor threats are visible at range and are avoidable by movement, not by ambush.
- [ ] Weather visibly changes the exterior's difficulty without changing its loot.
- [ ] Exterior loot, if present, is authored and sparse; the exterior is never more profitable per minute than the interior.
- [ ] The exterior NavMesh is baked offline and links correctly to the runtime-baked interior at the socket.
- [ ] A landmark makes the drop-off findable from anywhere in the exterior, including at night, in fog, and during a blackout.
- [ ] The boundary reads as terrain and is reached only by a player deliberately testing it.
- [ ] The exterior holds the frame budget on the lowest-spec target with weather active and a full crew present.
- [ ] The exterior unloads completely with the location, leaving no residual lighting or fog state applied to the hub.
