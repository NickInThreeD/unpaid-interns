# 26 — Location Catalogue

**Source:** [`core_components.md`](../core_components.md) §4 — Location & World Generation
**Status:** ❌ Not started · **[MVP]**
**Depends on:** Data-Driven Configuration
**Blocks:** Location Selection, Procedural Interior Generator, Loot Spawner, Spawn Director, Weather, Alternate Exits, Exterior Approach Area, Store

## Summary

The set of destinations the employer can send a crew to, and the numbers that make each one feel different.

`GAME_DESIGN.md` opens with "random, unfamiliar locations" and asks, in its open questions, how locations are generated or selected. This component is the answer's data half: every destination is a ScriptableObject carrying a difficulty tier, a size multiplier, a loot count range, a monster power budget, and a travel cost. The generator, the loot spawner, and the spawn director all read from it, which means **a designer can create a new destination without writing code** — the property that decides whether this game gets balanced or merely shipped.

It is also the cheapest replayability in the project. Three well-differentiated destinations produce more variety than one destination with three times the content, because the crew's *decision* about where to go is itself content.

The pattern is already established: `WeaponData` ScriptableObjects with a `WeaponRegistry` that resolves a numeric id (`GetWeaponData(uint weaponID)`), so ghost fields carry ids rather than object references. Copy it exactly. The Lethal Company reference in [`Assets/docs/world/moons.md`](../../Assets/docs/world/moons.md) documents a working version of these same fields — size multiplier, min/max scrap, separate indoor/outdoor/daytime power caps, route cost — and is worth reading before choosing which properties to include.

## How to Build

**Author the data**

- Add `Assets/Scripts/Gameplay/Locations/LocationData.cs` as a ScriptableObject with a `[CreateAssetMenu]` attribute, and `LocationRegistry.cs` alongside it with a `GetLocationData(uint locationId)` lookup, mirroring `WeaponRegistry` line for line.
- Store the assets under `Assets/Data/Locations/`, beside the existing `Assets/Data/Weapons/`.
- **The id must be stable and explicit.** `WeaponRegistry` uses list position as the id, which silently reassigns every id if someone reorders the list. That is survivable with two weapons and a disaster with a save file that stores which locations are unlocked. Give `LocationData` an explicit serialized `Id` field and have the registry build a dictionary at load, asserting on duplicates.

**Choose the properties**

- **Identity** — id, display name, a short in-fiction description for the terminal, difficulty tier.
- **Generation** — size multiplier (drives interior extent), interior layout set (which room-module collection to assemble from), guaranteed features (fire exit count, whether a breaker box exists), and any per-location day-length override, which [`03_round_timer_clock.md`](03_round_timer_clock.md) already anticipates.
- **Economy** — minimum and maximum item count, a loot table with per-item rarity weights, and travel cost in credits.
- **Threat** — a monster power budget, split into indoor and outdoor pools so an open exterior and a cramped interior can be tuned independently, plus which monsters are eligible here at all. Validate eligibility against navigation: a monster whose agent radius exceeds what this location's layout set can accommodate must be rejected at authoring time, not discovered as a monster stuck in a doorway ([`30_runtime_navmesh_baking.md`](30_runtime_navmesh_baking.md)).
- **World** — the exterior scene reference ([`33_exterior_approach_area.md`](33_exterior_approach_area.md)), the alternate-exit count ([`32_alternate_exits.md`](32_alternate_exits.md)), whether the location has a power grid ([`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md)), and the location's maximum bounds extent ([`34_out_of_bounds_handling.md`](34_out_of_bounds_handling.md)).
- **Presentation** — a `LightingProfile` reference (the type already exists at `Assets/Scripts/Gameplay/VisualEffects/LightingProfile.cs` with an applier, and is the ready-made hook for per-location mood — but read the limits documented in [`35_environmental_conditions_weather.md`](35_environmental_conditions_weather.md) before relying on the applier as written), an ambience `SoundDef` set, and the eligible weather set.
- Resist adding a raw "difficulty number" that does nothing. The reference design's risk rating is explicitly cosmetic; if a field has no mechanical consumer, mark it as flavour so nobody tunes against it expecting an effect.

**Keep loot and threat honest against each other**

- Value and danger must be correlated by *data*, not by hope. The catalogue is where the risk/reward curve actually lives: a destination with a high loot ceiling and a low monster budget is a free-money exploit, and it will be found within one session.
- Separate "how much is here" from "how valuable each piece is". A location with many low-value items rewards carry capacity and repeated trips; one with few high-value items rewards a single careful run. Those are genuinely different rounds and the data model should be able to express both.
- Weather must not change loot value — only difficulty. Otherwise the crew is picking weather rather than destinations, and the destination choice stops mattering.

**Make it network-safe**

- Only the **id** crosses the wire. The selected destination is a `[GhostField]` on the Run Manager, as required by [`23_shared_session_state_sync.md`](23_shared_session_state_sync.md); every client resolves it against its own copy of the registry.
- That means the registry must be **identical on server and client**, which makes it a build-parity concern: a location present in one build and not the other resolves to null on one side. Version-stamp the registry alongside the build version check in §12.
- Load location content through Addressables, which is already configured and used for ghost prefabs. Group per location so a destination's room modules and props load and unload together with the round — the group organization §12 flags as still outstanding.

**Ship three, not twelve**

- Author a small starting set — an easy short one, a medium one, and a hard large one — and tune those to genuinely different feels before adding more. A catalogue of near-identical destinations is worse than one destination, because it costs the player a decision that has no consequence.
- Add a debug/test location with a fixed tiny layout and known loot for automated tests and for tuning everything downstream.

## Acceptance Criteria

- [ ] `LocationData` and `LocationRegistry` exist, follow the `WeaponData` / `WeaponRegistry` pattern, and live under `Assets/Data/Locations/`.
- [ ] Location ids are explicit serialized values, not list positions; reordering the registry changes no id.
- [ ] The registry asserts loudly on duplicate ids at load.
- [ ] Every property has a named consumer, or is explicitly marked as flavour.
- [ ] Size multiplier, item count range, monster power budget, and travel cost all measurably change a round.
- [ ] Indoor and outdoor monster budgets are tunable independently.
- [ ] A monster whose agent size a location's layout set cannot accommodate is rejected at authoring time.
- [ ] Every location declares its exterior scene, alternate-exit count, power-grid presence, and maximum bounds extent.
- [ ] A per-location day-length override is honoured by the round clock.
- [ ] A `LightingProfile` and ambience set apply per location.
- [ ] Weather affects difficulty only, never loot count or value.
- [ ] Only the location id is replicated; every client resolves the same destination from its own registry.
- [ ] A registry mismatch between client and server is detected at connect, not discovered as a null at generation time.
- [ ] Location content loads and unloads through Addressables in per-location groups, with no residue after a round.
- [ ] At least three destinations exist and play measurably differently in loot density, size, and threat.
- [ ] A fixed debug location exists with a known layout and known loot for testing.
- [ ] A designer can add a new destination with no code changes and no recompile.
