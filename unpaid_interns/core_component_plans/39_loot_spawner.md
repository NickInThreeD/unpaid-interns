# 39 — Loot Spawner

**Source:** [`core_components.md`](../core_components.md) §5 — Items, Loot & Inventory
**Status:** ❌ Not started · **[MVP]**
**Depends on:** Item Definition, Item Ghost, Location Catalogue, Procedural Interior Generator, Deterministic Generation Seed
**Blocks:** the economy having anything in it, quota tuning, scanner value reporting

## Summary

Filling a generated location with things worth taking.

It sounds like a loop over spawn points and it is the component that decides whether the game is balanced. The loot spawner sets **how much money is on the map**, which is the numerator of every decision the crew makes: whether the quota is achievable, whether a second trip is worth it, whether a destination is worth its travel cost. Every number in §8 is tuned against this component's output, so its output has to be predictable, measurable, and reproducible.

It also owns the thing the design is actually about. `GAME_DESIGN.md` locates the tension in *how long to stay* — and that tension only exists if the good stuff is further in. A map with uniformly distributed loot has no risk gradient, and no amount of monster tuning will create one.

## How to Build

**Spawn on the server, from a dedicated seed stream**

- Server-authoritative, inside the load barrier, after the interior is assembled and before the barrier opens ([`05_location_load_unload_flow.md`](05_location_load_unload_flow.md)). A client must never generate loot; items are ghosts and the server owns them.
- Draw from the **loot stream** of the round seed, not from a shared generator ([`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md)). This is the exact case that plan warns about: if the interior generator and the loot spawner share a stream, adding one draw to room placement reshuffles the entire loot table and a reported seed stops reproducing after an unrelated commit.
- Fix the draw order within the stream — placement, then item selection, then value roll, in a stable iteration over a sorted list. Iterating a `Dictionary` or `HashSet` here is a determinism bug that will only appear on someone else's machine.

**Place against authored points, never against geometry**

- [`28_procedural_interior_generator.md`](28_procedural_interior_generator.md) requires each room module to carry tagged loot transforms with a category — floor, shelf, hidden, high-value. Draw against those.
- Raycasting the assembled geometry to find surfaces produces items in walls, items floating, and items inside each other. It is the obvious approach and it is wrong.
- Respect the category. A high-value point in a module is a designer saying "this spot is worth walking to"; ignoring it wastes the authoring.
- Assert that the assembled layout has at least as many eligible points as the location's maximum item count, and fail loudly during generation if it does not — a location that silently spawns half its loot because the generator produced small rooms is a balance bug nobody will diagnose.

**Build the risk gradient explicitly**

- Compute each loot point's **path distance from the extraction zone** during generation and weight high-value placement toward the far end.
- This is a deliberate design decision and should be stated as one: value correlates with distance from safety. Without it, the optimal play is to sweep the nearest rooms and leave, and the "one more trip" tension the whole game rests on never appears.
- Keep it a bias, not a rule. A small chance of something excellent near the door, and of something worthless in the deepest room, is what stops the map from becoming a solved gradient the crew reads off a minimap.
- Path distance, not straight-line — the same distinction [`32_alternate_exits.md`](32_alternate_exits.md) makes, and for the same reason.

**Roll count, selection, and value in that order**

- **Count** comes from the location's min/max item range ([`26_location_catalogue.md`](26_location_catalogue.md)), scaled by nothing else. Resist scaling it by crew size or by quota pressure — a hidden difficulty director that quietly enriches a struggling crew removes the quota's threat, the same objection [`27_location_selection_assignment.md`](27_location_selection_assignment.md) raises about hidden destination weighting.
- **Selection** draws from the location's weighted loot table. A weight is meaningful only relative to the pool's total, so the same item is legitimately common in one destination and rare in another ([`Assets/docs/items/scrap.md`](../../Assets/docs/items/scrap.md)).
- **Value** rolls per instance from the item's min/max range and is written to the item ghost's `RolledValue` ([`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md)).
- Respect eligibility hints — an indoor-only item never spawns in the exterior, and vice versa.

**Watch the total, not the items**

- The number that matters is **total value on the map**, and it is a distribution, not a constant. Measure its mean and variance per location across many seeds and tune the location's ranges against the quota curve in §8.
- A destination whose worst seed cannot cover its own travel cost is a trap; one whose best seed clears a whole quota in one trip is an exploit. Both are found by running seeds, not by playing.
- Add a headless harness — the same one [`28_procedural_interior_generator.md`](28_procedural_interior_generator.md) establishes — that generates N seeds per location and reports total value, item count, value distribution by distance band, and the count of items behind a locked door or otherwise unreachable. This is pure logic, cheap to test, and the highest-leverage tooling in §8.

**Do not defer spawning to save bandwidth**

- The tempting optimisation — spawn items only when a player gets close — breaks two things. The scanner's aggregate value report ([`16_player_scanner_ping_tool.md`](16_player_scanner_ping_tool.md)) needs the map's contents to exist, and a deferred spawn is a second source of nondeterminism keyed on where players walked.
- Spawn everything at load and control the cost with **ghost relevancy and importance**, which is the mechanism [`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md) already specifies. Existence is cheap; replication is what costs.

**Cover the special cases**

- **Guaranteed items** — a location may require specific items to exist (a key, a quest object). Place those first, before the weighted pool fills the remaining points, and exclude them from the count range so they cannot crowd out the payload.
- **Equipment purchased in the store** is delivered, not spawned here ([`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md)). Keep the two paths separate; a store item appearing in the loot pool is a sell-back exploit.
- **A single-item round** — the reference design's low-probability day where only one item type spawns is genuinely memorable and costs one branch. Worth having, worth keeping rare.
- **Nothing spawns inside the extraction zone.** Free credits at the drop-off undercuts the entire haul loop, and it will happen by accident if the zone overlaps a module with loot points.

## Acceptance Criteria

- [ ] Loot spawns server-side inside the load barrier, after generation and before the round starts.
- [ ] The spawner draws only from its own derived seed stream; a change to interior generation does not alter loot for the same seed.
- [ ] The same seed produces an identical loot manifest — positions, items, and values — on every run.
- [ ] Items are placed only at authored loot points, never by raycasting geometry; no item spawns in a wall, floating, or inside another item.
- [ ] Generation fails loudly if a layout offers fewer eligible loot points than the location's maximum count.
- [ ] High-value items are measurably biased toward greater path distance from the extraction zone, with meaningful exceptions in both directions.
- [ ] Item count falls within the location's configured range and is not scaled by crew size or quota progress.
- [ ] Selection honours the location's weighted table, and the same item can be common on one destination and rare on another.
- [ ] Each item's value is rolled per instance within the definition's range and written to its ghost at spawn.
- [ ] Indoor/outdoor eligibility hints are respected.
- [ ] No item spawns inside the extraction zone.
- [ ] Guaranteed items are placed before the weighted pool and do not consume the count range.
- [ ] Store-purchased equipment never appears in the loot pool.
- [ ] The headless harness reports total value, count, and distribution by distance band across at least 1,000 seeds per location.
- [ ] No location's worst-case seed fails to cover its travel cost, and no location's best-case seed clears a full quota in one trip.
- [ ] Every generated item is reachable; the harness reports zero unreachable items.
- [ ] All items exist at round start; nothing is spawned lazily on player approach.
- [ ] A debug command reports the current round's total map value and its distribution.
