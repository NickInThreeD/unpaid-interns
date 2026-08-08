# 87 — Data-Driven Configuration

**Source:** [`core_components.md`](../core_components.md) §11 — Technical Foundations
**Status:** ⚠️ The pattern exists and carries a defect that must not be copied · **[MVP]**
**Depends on:** nothing — this is a convention, established early
**Blocks:** items, monsters, locations, weather, upgrades, quota curves — every tunable in the game

## Summary

The convention that lets designers change the game without a programmer.

`core_components.md` gives the reason plainly — it is *"essential given how much of this game is balance work"* — and that is not a general truth about games, it is specific to this one. The quota curve, loot density, monster power costs, sell rates, stamina drain, and fall damage bands are all judgement calls that will be wrong on the first attempt and will be retuned dozens of times. A tunable behind a recompile gets tuned once.

The pattern already exists: `WeaponData` ScriptableObjects in `Assets/Data/Weapons/`, a `WeaponRegistry` resolving a numeric id, and `LightingProfile` doing the same for per-scene lighting. Ghost fields carry ids rather than object references, which is the correct shape for a networked game and the reason the pattern is worth standardising on rather than replacing.

**It also carries one defect that every plan in this project has had to work around individually.** `WeaponRegistry.GetWeaponData(uint weaponID)` returns `Weapons[(int)weaponID]` — the id **is** the list position. Reordering the list silently reassigns every id. That is survivable with two weapons and catastrophic once a save file records purchased gear, a ghost field carries an item id across a version boundary, or a location's loot table references items by id. [`26_location_catalogue.md`](26_location_catalogue.md), [`37_item_definition_data_model.md`](37_item_definition_data_model.md), [`48_monster_data_definitions.md`](48_monster_data_definitions.md), [`35_environmental_conditions_weather.md`](35_environmental_conditions_weather.md), and [`68_upgrades.md`](68_upgrades.md) each independently mandate explicit ids as a correction. This component is where that correction becomes the rule rather than five separate footnotes.

## How to Build

**Fix the id convention, once, for everything**

- **Every registry entry carries an explicit serialized `Id`.** Never list position, never array index, never enum ordinal.
- The registry builds a **dictionary at load** and asserts loudly on duplicates. A duplicate id is a silent data-corruption bug that presents as the wrong item spawning.
- An id, once shipped, is **permanent**. Retired content keeps its id reserved rather than recycled. Put that in the asset's tooltip, because the person who reuses a retired id will not have read this file.
- Retrofit `WeaponRegistry` when weapons become items ([`45_weapons_as_tools.md`](45_weapons_as_tools.md) folds them into the item registry). Until then, treat its ids as already-shipped and do not reorder the list.
- Write **one generic registry base** — `ScriptableObjectRegistry<T>` with the id lookup, the duplicate assertion, and the validation hook — rather than five hand-written registries that will each acquire their own subtle differences.

**Make client/server parity a first-class concern**

- Only ids cross the wire. That means the **registry must be identical on both sides**, and a mismatch resolves to null on one side only — presenting as an invisible item, an unpickable object, or a monster that exists but has no prefab.
- Version-stamp the registries and check the stamp at connect, alongside the build-version rejection §12 requires. Several plans list this as an acceptance criterion; implement it once, here, covering all registries.
- The check must produce a **clear message** ("content mismatch: item registry v14 vs v13"), not a generic connection failure.

**Separate the two kinds of data**

There are two distinct things under this heading and conflating them produces a mess:

- **Content registries** — items, monsters, locations, weather, upgrades. Many instances, id-addressed, replicated by id.
- **Tuning configs** — the quota curve, the spawn budget curve, stamina rates, fall damage bands, noise range/volume tables, penalty percentages. **One instance each**, never replicated, read directly.

Tuning configs need no ids and no registry. They need to be **findable** — a single `GameTuning` asset holding references to each config, rather than a dozen assets discovered by string path — and they need to be readable from ECS, which means the values ECS consumes must reach a blittable singleton rather than being read from a managed asset in a job.

**Consolidate the scattered tuning surfaces**

Plans across the project each specify "put this in a config asset". Left uncoordinated that becomes twenty assets nobody can find:

- Round clock: day length, phase thresholds ([`03_round_timer_clock.md`](03_round_timer_clock.md))
- Movement: stamina rates, exhaustion thresholds, carry weight curve ([`11_stamina.md`](11_stamina.md), [`12_carry_weight.md`](12_carry_weight.md))
- Damage: fall bands, friendly-fire multipliers, injury thresholds ([`61_fall_and_environmental_damage.md`](61_fall_and_environmental_damage.md), [`18_pvp_collision_and_friendly_fire.md`](18_pvp_collision_and_friendly_fire.md), [`13_health_and_injury.md`](13_health_and_injury.md))
- Economy: quota curve, sell rates, penalties, bonuses, starting balance ([`64_quota_system.md`](64_quota_system.md), [`65_selling_payout.md`](65_selling_payout.md), [`66_bonus_and_penalty_rules.md`](66_bonus_and_penalty_rules.md), [`63_currency_system.md`](63_currency_system.md))
- Threat: spawn budget curve, escalation ramps ([`50_spawn_director.md`](50_spawn_director.md), [`51_difficulty_escalation.md`](51_difficulty_escalation.md))
- Noise: the range/volume table for every sound ([`54_noise_emission_system.md`](54_noise_emission_system.md))

Group them by domain — a handful of assets, not one giant one and not twenty small ones — and reference them all from one root so a designer opens one thing.

**Validate at author time and fail the build**

- An editor pass per registry: no missing prefabs, no zero or duplicate ids, no inverted ranges, no field whose consumer does not exist. [`37_item_definition_data_model.md`](37_item_definition_data_model.md) and [`48_monster_data_definitions.md`](48_monster_data_definitions.md) both require this and both say to fail the build.
- Validate **cross-registry** references too, which no single registry can do alone: a location's loot table referencing a live item id, its eligible monsters existing and fitting its layout set's agent sizes ([`30_runtime_navmesh_baking.md`](30_runtime_navmesh_baking.md)), its weather set being eligible. That cross-check belongs here because it is the only component that sees every registry.
- **Every field must have a named consumer or be explicitly marked as flavour.** [`26_location_catalogue.md`](26_location_catalogue.md) establishes this rule for a specific reason: a field with no effect will be tuned by someone expecting one.

**Get Addressables grouping right alongside it**

- §12 notes only a "Default Local Group" exists, and content must be built **before** the player build or Addressable references resolve to null in a shipped game while working in the Editor.
- Group by location so a destination's room modules and props load and unload together with the round ([`26_location_catalogue.md`](26_location_catalogue.md)), with shared items and monsters in common groups.
- This is content configuration and it belongs to the same discipline, even though it is not a ScriptableObject.

## Acceptance Criteria

- [ ] A generic registry base provides id lookup, duplicate assertion, and a validation hook; no registry hand-rolls these.
- [ ] Every registry entry carries an explicit serialized id; no registry uses list position.
- [ ] Reordering any registry list changes no id.
- [ ] Duplicate ids fail loudly at load and at author time.
- [ ] Shipped ids are never recycled, and the rule is visible in the authoring UI.
- [ ] Registries are version-stamped, and a client/server mismatch is reported at connect with a specific message.
- [ ] Content registries and tuning configs are clearly separated, with tuning configs holding one instance each and never replicated.
- [ ] Tuning values consumed by ECS reach a blittable singleton rather than being read from a managed asset in a job.
- [ ] All tuning configs are reachable from one root asset.
- [ ] Every tunable named across the component plans lives in a config asset and is changeable without a recompile.
- [ ] An editor validation pass covers every registry and fails the build on a violation.
- [ ] Cross-registry references — loot tables, eligible monsters, agent sizes, weather sets — are validated centrally.
- [ ] Every field has a named consumer or is explicitly marked as flavour.
- [ ] Addressables groups are organised per location plus shared groups, and content builds before the player build.
- [ ] A designer can add an item, a monster, a location, and a weather condition, and retune the quota curve, with no code change and no recompile.
- [ ] A shipped standalone build resolves every Addressable reference with no nulls.
