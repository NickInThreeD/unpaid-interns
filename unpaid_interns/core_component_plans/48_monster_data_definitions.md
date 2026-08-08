# 48 — Monster Data Definitions

**Source:** [`core_components.md`](../core_components.md) §6 — Monsters & AI
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Data-Driven Configuration](37_item_definition_data_model.md) (same pattern)
**Blocks:** Spawn Director, Difficulty Escalation, Perception, Monster Variety Set, Location Catalogue validation

## Summary

What a monster *is*, as numbers a designer can change.

§6 opens by noting that nothing in it exists in any form, which makes this the first brick. It is also the component that decides whether the threat layer is ever balanced: monsters are the single most-tuned thing in a horror game, and a roster whose stats live in code gets tuned exactly as often as someone is willing to recompile.

The most important field is the one that sounds like bookkeeping. Every monster carries a **power cost**, and the spawn director spends a per-round budget against it ([`50_spawn_director.md`](50_spawn_director.md)). That single number is what turns "how dangerous is this round" from an emergent accident into a knob. Without it, difficulty is whatever the random spawn rolls produced, and the location catalogue's monster budgets have nothing to spend.

The pattern is `WeaponData` + `WeaponRegistry`, with the same correction already applied to items and locations: **explicit serialized ids, never list position** ([`37_item_definition_data_model.md`](37_item_definition_data_model.md), [`26_location_catalogue.md`](26_location_catalogue.md)).

## How to Build

**Author the type and registry**

- `Assets/Scripts/Gameplay/Monsters/MonsterData.cs` as a ScriptableObject with `[CreateAssetMenu]`, and `MonsterRegistry.cs` with `GetMonsterData(uint monsterId)` backed by a dictionary built at load, asserting on duplicate ids.
- Assets under `Assets/Data/Monsters/`, beside `Assets/Data/Weapons/` and `Assets/Data/Items/`.
- The prefab is a `GhostSpawner.GhostReference`, the same Addressable-plus-`Hash128` wrapper `WeaponData` uses for projectiles — the spawn path is then [`49_monster_ghost_and_replication.md`](49_monster_ghost_and_replication.md)'s and needs no bespoke loader.
- Registry parity between client and server builds is a hard requirement, as with items and locations: only the id crosses the wire and a missing definition on one side is an invisible monster rather than an error.

**Choose the fields, each with a named consumer**

- **Identity** — id, display name, and a category: `Indoor`, `Outdoor`, or `Both`. The category is what [`26_location_catalogue.md`](26_location_catalogue.md)'s split indoor/outdoor budgets spend against.
- **Combat** — health, damage per hit, attack cadence, attack range, and whether the monster can be killed at all. An unkillable monster is a legitimate and valuable archetype ([`58_monster_variety_set.md`](58_monster_variety_set.md)) and must be expressible, not hacked in with a large health number.
- **Movement** — patrol speed, chase speed, acceleration, and the **navigation agent type** it uses. That last one is not cosmetic: [`30_runtime_navmesh_baking.md`](30_runtime_navmesh_baking.md) bakes one surface per agent type and the agent radius decides which corridors this monster can use. A monster whose agent type a location's layout set does not support must be rejected at authoring time.
- **Senses** — which senses it uses at all (`Sight`, `Hearing`, `Both`, `Neither`), plus per-sense ranges and angles. [`53_perception_system.md`](53_perception_system.md) consumes these, and making "which senses" explicit is what lets the roster be *legibly* varied rather than accidentally samey.
- **Behaviour** — give-up distance, give-up time, last-known-position search duration, and whether it can open or traverse doors and navigation links ([`55_chase_and_pathfinding.md`](55_chase_and_pathfinding.md)).
- **Spawning** — power cost, spawn weight, maximum simultaneous count, earliest normalized time it may appear, and which emergence points it may use ([`52_spawn_points_and_vents.md`](52_spawn_points_and_vents.md)).
- **Presentation** — idle, alerted, and chase `SoundDef` sets, which §10 requires to be distinct and learnable, plus the audio wind-up used to telegraph a spawn.

**Make power cost mean something**

- Power cost must be **proportional to how much a monster shrinks the crew's options**, not to its health. A stationary route-blocker with 5 HP that closes the only stairwell is far more expensive than a wandering thing with 30 HP the crew can walk around.
- Tune it by measurement: run rounds with a fixed budget and vary the roster, and watch round duration and death rate. A cost that was guessed will produce rounds that are trivially safe or unsurvivable, and the spawn director will get blamed.
- Keep the budget scale small and integral — a 0–20 range with monsters costing 1 to 5. Fine-grained costs create the illusion of precision in a number that is fundamentally a judgement.

**Do not put behaviour in the data**

- The temptation is a `BehaviourType` enum that switches between hard-coded AI routines, and it collapses the moment two monsters want to share half a routine.
- Prefer: each monster is a prefab with behaviour components, and `MonsterData` supplies the numbers those components read. New monster = new prefab + new asset, no change to this type.
- A field with no consumer is worse than a missing field, because someone will tune against it. Mark flavour explicitly, as [`26_location_catalogue.md`](26_location_catalogue.md) requires of location data.

**Validate at author time**

- An editor pass that rejects: missing prefab, zero or duplicate id, an agent type no layout set supports, a sense configuration with no ranges, a monster eligible on a location whose budget is smaller than its power cost, and a maximum count of zero.
- That last one matters — a monster that can never spawn because its cost exceeds every location's budget is invisible in testing and looks exactly like a bug in the spawn director.
- Fail the build on a violation. The failure mode otherwise is a null prefab producing a monster that exists in the simulation, hunts the player, and cannot be seen.

## Acceptance Criteria

- [ ] `MonsterData` and `MonsterRegistry` exist under `Assets/Scripts/Gameplay/Monsters/`, with assets in `Assets/Data/Monsters/`.
- [ ] Monster ids are explicit serialized values; reordering the registry changes no id.
- [ ] The registry builds a dictionary at load and asserts loudly on duplicates.
- [ ] A registry mismatch between client and server is detected at connect, not as a null at spawn.
- [ ] Prefabs are referenced through `GhostSpawner.GhostReference` and load through Addressables.
- [ ] Every monster declares category, health or unkillability, damage, speeds, agent type, senses used, per-sense ranges, give-up rules, power cost, spawn weight, max count, earliest spawn time, and audio sets.
- [ ] "Which senses" is an explicit field, and at least one authored monster uses only hearing and one only sight.
- [ ] An unkillable monster is expressible without abusing the health field.
- [ ] Power cost is tuned against measured round duration and death rate, not guessed.
- [ ] Behaviour lives in prefab components; adding a monster requires no change to `MonsterData`.
- [ ] Every field has a named consumer or is explicitly marked as flavour.
- [ ] An editor validation pass rejects invalid definitions and fails the build.
- [ ] A monster whose agent type a location's layout set cannot support is rejected at authoring time.
- [ ] A monster whose power cost exceeds every location's budget is reported at author time, not discovered as an absence.
- [ ] A designer can add a monster and see it spawn in a round with no code change and no recompile.
