# 52 — Spawn Points / Vents

**Source:** [`core_components.md`](../core_components.md) §6 — Monsters & AI
**Status:** ❌ Not started
**Depends on:** [Procedural Interior Generator](28_procedural_interior_generator.md), [Spawn Director](50_spawn_director.md), [Runtime NavMesh Baking](30_runtime_navmesh_baking.md)
**Blocks:** spawns being readable rather than arbitrary, monster counterplay existing at all

## Summary

Fixed places monsters come out of, and a noise they make before they do.

This is a small component doing something disproportionately important: it makes threat **legible**. A monster that materialises from nothing is a random event the crew cannot learn from, plan around, or feel clever about avoiding. A monster that comes out of a vent — a vent the crew walked past, in a room they chose to enter, after a sound they had a second to react to — is an encounter they participated in.

`core_components.md` states the requirement precisely: emergence locations with a **telegraphed audio wind-up**, so spawns are readable and avoidable rather than arbitrary, and **never on top of a player**. Both halves matter. The telegraph without the fixed location gives the crew a warning they cannot act on; the location without the telegraph gives them a landmark that ambushes them anyway.

The reference design makes vents the *sole* indoor emergence mechanism ([`Assets/docs/world/interior.md`](../../Assets/docs/world/interior.md)), which is worth copying: one rule the player can learn beats several they cannot.

## How to Build

**Author emergence points per module, never at runtime**

- Each room module carries authored vent transforms with a category and an orientation, the same way it carries loot points ([`28_procedural_interior_generator.md`](28_procedural_interior_generator.md)). Placement by raycasting assembled geometry produces vents in walls facing nowhere, exactly as it produces loot in walls.
- Record each point's **path distance from the extraction zone** during assembly, alongside the loot points' distances. The director wants to prefer far points, and computing that at spawn time means a pathfinding query on every cycle.
- Enforce a **minimum path distance from the main entrance**, as component 28's acceptance criteria already require. A vent in the starting room means the crew is ambushed before they have made a single decision.
- The exterior's emergence points are authored by hand in the fixed exterior scene ([`33_exterior_approach_area.md`](33_exterior_approach_area.md)) — outdoor threats should arrive from the horizon or from cover, not from vents.

**Validate against navigation, not against geometry**

- A vent that is not on the NavMesh spawns a monster that cannot move. Assert during generation that every emergence point produces a valid path to the extraction zone for every eligible agent type ([`30_runtime_navmesh_baking.md`](30_runtime_navmesh_baking.md) already requires exactly this assertion in the generation harness — this component is what it validates).
- Agent radius matters here too: a vent in a corridor a large monster cannot leave is a trap for the spawn director, not for the player. Tag each point with the largest agent it can emit.
- A point that fails validation must be **excluded from the director's candidate set**, not fixed up at runtime. Silent runtime repair hides a generator bug.

**Telegraph it, and make the telegraph honest**

- The wind-up is a sound played at the vent, through the existing `SoundSystem` and `SoundDef` assets, for a configured duration before the monster appears. It is the player's entire window to act, so its length is a real tuning number — long enough to leave the room, short enough to be frightening.
- **The telegraph must be truthful.** If a wind-up plays, something must emerge; if something emerges, a wind-up must have played. A false positive teaches players to ignore it and a false negative teaches them it is unreliable, and either one destroys the mechanic permanently.
- Vary the wind-up by monster where it is useful — a distinct sound per archetype lets an experienced crew know what is coming and decide differently ([`58_monster_variety_set.md`](58_monster_variety_set.md) and §10's requirement that monster audio be learnable).
- Accessibility (§9) applies directly: this is a **critical audio-only warning**, which is exactly the category that plan says must have a visual equivalent. A directional indicator or a visible vent animation is mandatory, not optional, or a deaf player cannot use the mechanic at all.

**Enforce the safety rules at spawn time**

- Never emerge within the configured minimum distance of any player, and never within their view. The director owns the skip-if-no-valid-point rule ([`50_spawn_director.md`](50_spawn_director.md)); this component owns the test.
- Check at **wind-up start and again at emergence**, because a player can walk into the room during the telegraph. Recommended on a late violation: let it proceed but suppress nothing — the player heard the warning and entered anyway, which is a decision, and cancelling silently would make the telegraph a lie in the other direction.
- Never emerge inside the extraction zone, and never on the far side of a locked door from the crew's reachable space.

**Make vents part of the world, not invisible markers**

- A vent should be **visible and identifiable** before anything comes out of it. That is what turns it into information a crew can route around, and it is what makes the wind-up meaningful rather than a jump scare with extra steps.
- Consider making them scannable at loot range ([`16_player_scanner_ping_tool.md`](16_player_scanner_ping_tool.md)) — this is not the same as making monsters scannable, which that plan forbids, and it gives the scanner a genuinely tactical second use.
- Decide whether players can interact with them — blocking a vent, or hearing through one. Both are attractive; both are scope. Recommended: **not for MVP**, but author the vent as an interactable prefab so the option stays open.

**Do not reuse the player spawn point type**

- `SpawnPoint` (`SpawnPointAuthoring`) is an empty `IComponentData` marker used by `ServerGameSystem.FindSpawnPoint` for player spawning, and [`31_entry_point_extraction_zone.md`](31_entry_point_extraction_zone.md) is already narrowing its scope to the extraction zone. Monster emergence points are a separate type with separate data.
- Sharing the type would mean a query that returns both, and the first symptom would be an intern spawning in a vent.

## Acceptance Criteria

- [ ] Emergence points are authored per room module with category and orientation, never placed by raycasting assembled geometry.
- [ ] Each point's path distance from the extraction zone is computed during assembly and available to the director without a runtime query.
- [ ] No emergence point is within the configured minimum path distance of the main entrance.
- [ ] Exterior emergence points are hand-authored in the fixed exterior scene.
- [ ] Every emergence point is validated to produce a path to the extraction zone for every eligible agent type, asserted in the generation harness.
- [ ] Each point records the largest agent it can emit, and the director never selects a point for a monster that cannot leave it.
- [ ] A point failing validation is excluded from the candidate set rather than repaired at runtime.
- [ ] A configured audio wind-up plays at the vent before every emergence.
- [ ] Every wind-up results in an emergence, and every emergence is preceded by a wind-up.
- [ ] Wind-up duration is tunable from data and is long enough to leave the room.
- [ ] Wind-ups are distinguishable between monster archetypes.
- [ ] A visual equivalent of the wind-up exists and is sufficient to act on without audio.
- [ ] No monster emerges within the minimum distance of, or in view of, a player, checked at wind-up start and again at emergence.
- [ ] Nothing ever emerges inside the extraction zone.
- [ ] Vents are visible and identifiable in the world before use.
- [ ] Monster emergence points use a separate type from player spawn points; no query can confuse the two.
- [ ] Across 1,000 generated seeds, every layout has at least the minimum required emergence points and none violate the entrance distance rule.
