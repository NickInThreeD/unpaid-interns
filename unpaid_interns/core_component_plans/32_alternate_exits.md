# 32 — Alternate Exits

**Source:** [`core_components.md`](../core_components.md) §4 — Location & World Generation
**Status:** ❌ Not started
**Depends on:** Procedural Interior Generator, Exterior / Approach Area, Entry Point / Extraction Zone
**Blocks:** nothing — but it is what makes a large interior survivable

## Summary

A second way out of the building, dropping into a different part of the exterior.

Its value is entirely about the **return trip**. Without it, every haul retraces the route in, and the deepest room in the building is priced at twice its distance — which pushes crews toward shallow, safe, boring play. A fire exit turns a deep room into a viable one-way run: go in the front, come out the side, walk the long way home outside where the threats are different and visibility is better.

It is also the best answer to being cornered. [`28_procedural_interior_generator.md`](28_procedural_interior_generator.md) already requires loop connections so that a dead end with a monster in it is not the whole game; an alternate exit is that principle applied at the building's scale.

The reference implementation is worth reading before deciding placement: [`Assets/docs/hazards/fire-exit.md`](../../Assets/docs/hazards/fire-exit.md) documents exits whose **interior** placement is random per round but whose **exterior** placement is fixed per location — one of them deliberately unreachable from one side, and one location with three. That asymmetry is the design: the exit is a known landmark from outside and a surprise from inside.

## How to Build

**Get the placement rule right — this is the whole component**

- The generator owns placement; this component owns the rule it places against. Place the interior end **far from the main entrance by path distance, not straight-line distance.** A fire exit twenty metres of corridor from the entrance that happens to be across the building in world space is worthless, and straight-line distance produces exactly that.
- Placement is drawn from the interior stream of the round seed ([`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md)) so every machine agrees, and so a reported layout reproduces.
- The **exterior** end is fixed per location, authored in the approach area ([`33_exterior_approach_area.md`](33_exterior_approach_area.md)). Fixed exteriors are what let a crew learn a destination — "the side door comes out behind the ridge" is knowledge worth having, and it is knowledge the procedural interior cannot give them.
- Count is per-location data on `LocationData` ([`26_location_catalogue.md`](26_location_catalogue.md)), not a constant. One is the default; a large destination with three is a genuinely different place, and a destination with zero is a legitimate difficulty lever.

**Decide the direction rule**

- Two-way (enter and exit freely) or one-way (exit only). Both are defensible and they are different games:
  - **Two-way** makes the exit a second entrance, halving the walk into deep rooms and substantially reducing the interior's difficulty.
  - **One-way** preserves the main entrance as the commitment point and makes the fire exit purely a relief valve.
- **Recommended: two-way, but only findable from outside once discovered.** The exterior door is visible in the world; the interior side is wherever the generator put it. A crew that has scouted the exterior can use it as an entrance; a crew that has not will only ever find it on the way out. That gets the strategic depth without removing the surprise.
- Whichever is chosen, record it here, and make sure the generator's reachability guarantee still holds: the extraction zone must be reachable from every room *including* through the alternate exit's route, which for a one-way door means the outdoor path back has to exist and be walkable.

**It is an exit, not an extraction point**

- This is the rule that keeps the component honest. Stepping out of a fire exit does **not** bank anything. The haul still has to reach the extraction zone ([`31_entry_point_extraction_zone.md`](31_entry_point_extraction_zone.md)), and the outdoor walk from the fire exit back to the drop-off is the price of the shortcut.
- Make that legible in the world. A player who assumes the fire exit is safety and drops their loot there has been misled by the level, not by their own mistake.
- The exterior placement is therefore a tuning knob with real consequences: an exit that emerges next to the drop-off makes deep rooms free, and one that emerges across a ravine makes the shortcut a trap. Author them deliberately per location and tune them against measured round times.

**Build it as networked state**

- A door is shared state, not local animation. Reuse the Door System (§7) rather than writing a bespoke fire-exit door: an alternate exit is a door with an authored destination and a scannable marker.
- State is absolute (`Open` / `Closed`), never a toggle command, so two players opening it on the same tick converge — the rule established in [`20_networked_interaction_authority.md`](20_networked_interaction_authority.md).
- Traversal is a **transition between two authored volumes**, not a physical walk through a doorway, if the interior and exterior are separate scenes. Decide this early: if they are one scene, a fire exit is just geometry and this is nearly free; if they are separate, every traversal is a teleport with a load, and that changes the feel enormously. **Recommended: one scene** — the exterior is small, the interior is the expensive part, and a loading screen mid-chase is unacceptable.

**Make it findable**

- The exterior door is scannable at long range like the extraction zone; the interior side is scannable at normal loot range only ([`16_player_scanner_ping_tool.md`](16_player_scanner_ping_tool.md)). That asymmetry encodes the whole design in the scanner's data.
- Give it a distinct audio and visual signature from every other door in the building. A player fleeing at low health needs to recognise it at a glance.
- Mark it on any map or monitoring view (§9) once the crew has used it, not before.

**Cover the interactions**

- **Monsters** — decide whether they may use it. A monster that follows you out into the exterior is a genuinely alarming moment; a monster that leaks out of the building every round and camps the drop-off ruins the exterior as a decompression space. Recommended: interior monsters may reach the exit but not pass through it, enforced by the navigation link's agent permissions ([`30_runtime_navmesh_baking.md`](30_runtime_navmesh_baking.md)).
- **Two-handed items** — carrying a corpse or a large item through the exit must work. If the traversal is a teleport, verify the carried item and its claim survive it ([`42_two_handed_item_rule.md`](42_two_handed_item_rule.md)).
- **Power** — an exit that stops working during a blackout is a cruel and excellent hazard, but it must be telegraphed before the power goes out, not discovered afterwards ([`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md)).

## Acceptance Criteria

- [ ] Every generated interior with a non-zero exit count places its alternate exits at the configured path distance from the main entrance, measured by path and not straight line.
- [ ] Exit count is read from `LocationData` and a location can be authored with zero, one, or several.
- [ ] Exterior placements are fixed per location and identical every visit.
- [ ] Interior placements vary per round and reproduce exactly from the same seed.
- [ ] The direction rule — one-way or two-way — is implemented and documented in this file.
- [ ] Passing through an alternate exit banks nothing; loot still has to reach the extraction zone.
- [ ] The outdoor route from every alternate exit back to the extraction zone is walkable and does not require the main entrance.
- [ ] The exit is a networked door with absolute state; two players opening it on the same tick leaves it open.
- [ ] Traversal preserves carried items, including two-handed items and bodies, with their claims intact.
- [ ] The exterior side is scannable at long range; the interior side only at loot range.
- [ ] The exit is visually and audibly distinct from ordinary doors.
- [ ] The monster-traversal rule is implemented via navigation link permissions and documented here.
- [ ] Using an alternate exit measurably shortens the return trip from a deep room compared with backtracking.
- [ ] Alternate exits are torn down with the location, with no leaked links or door ghosts.
