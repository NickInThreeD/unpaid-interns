# 35 — Environmental Conditions / Weather

**Source:** [`core_components.md`](../core_components.md) §4 — Location & World Generation
**Status:** ❌ Not started
**Depends on:** Location Catalogue, Deterministic Generation Seed, Location Selection
**Blocks:** nothing — but it is the cheapest replayability in the project

## Summary

Per-location modifiers that change how a round plays without changing what it pays: fog, rain, storm, flooding, blackout.

The value proposition is unusually good. A handful of conditions multiplies every destination in the catalogue by the number of conditions, and each one is mostly presentation plus one or two rules. Three destinations and four weathers is twelve distinct rounds for a fraction of the cost of a fourth destination.

The rule that makes it work is stated in [`26_location_catalogue.md`](26_location_catalogue.md) and is worth repeating because everything else follows from it: **weather changes difficulty, never loot count or value.** The reference design is explicit about this ([`Assets/docs/world/weather.md`](../../Assets/docs/world/weather.md)) and the reason is structural — if weather changed payout, the crew would be choosing weather rather than destinations, and the destination decision built in [`27_location_selection_assignment.md`](27_location_selection_assignment.md) would stop mattering.

The second rule is fairness, and it is the one most likely to be got wrong: **a condition that reduces player visibility must reduce monster perception too.** Fog that blinds the crew while monsters see through it is not difficulty, it is a punishment, and players will correctly read it as broken.

## How to Build

**Roll it with the seed and replicate the id**

- Roll the condition on the server from the **weather stream** of the round seed ([`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md)) — its own derived stream, so adding a draw to interior generation does not silently reshuffle the forecast.
- Replicate it as a `WeatherId` `[GhostField]` on the Run Manager beside `SelectedLocationId` and `RoundSeed`, and add it to the shared-state inventory in [`23_shared_session_state_sync.md`](23_shared_session_state_sync.md). Only the id crosses the wire; every client resolves it against its own registry.
- Eligible conditions per destination are `LocationData` data, not a global table — a location with no exterior has no weather, and a flooded interior only makes sense somewhere with drainage to flood.
- Anything with gameplay consequence (flood water level, a lightning strike, a quicksand patch's position) is **server-authoritative state**, not a client-side visual. Clients render the condition; the server decides what it does.

**Show the forecast before the crew commits**

- The forecast for each destination must be visible in the hub, on the same terminal screen as difficulty and travel cost ([`27_location_selection_assignment.md`](27_location_selection_assignment.md)). A weather system the crew discovers on arrival is a random punishment; one they can see is a decision.
- That means the condition is rolled **at destination-offer time, not at deploy** — or, more simply, rolled per destination per day from the run seed and day number, so the whole board is deterministic and forecastable. The reference design re-rolls every moon's weather each day and shows the whole board; copy it.
- A crew declining a stormy destination and taking a lesser payout elsewhere is the system working.

**Build the condition as data, not as five classes**

- Define a `WeatherData` ScriptableObject with a registry, following the same explicit-`Id`-plus-dictionary pattern mandated in [`26_location_catalogue.md`](26_location_catalogue.md) — not `WeaponRegistry`'s list-position ids.
- Per condition: a `LightingProfile` override, a fog/visibility range, a perception-range multiplier applied to monsters, a movement modifier, an ambience `SoundDef` set, an optional hazard spawner, and a difficulty weight used by the offer generator.
- A condition should be **one legible rule plus atmosphere**. "You cannot see far" is a good condition. "Visibility down 30%, movement down 10%, stamina drain up 15%" is four invisible rules the player will never learn.

**Know what the existing lighting hook actually does**

`LightingProfile` and `LightingProfileApplier` (`Assets/Scripts/Gameplay/VisualEffects/`) are the natural hook and are cited across the plans as such — but read them before relying on them, because they are narrower than they look:

- `LightingProfileApplier.OnEnable` writes global `RenderSettings` — skybox, ambient, fog colour/mode/density/distances — and calls `DynamicGI.UpdateEnvironment()`. It is a **one-shot apply with no blending**, so a condition that changes during a round (flooding rising, fog thickening) has no path through it as written.
- `RenderSettings` is **global process state**, not per-scene. Applying a location's profile and then returning to the hub leaves the hub lit like the location unless something explicitly restores it. This is a real bug waiting in the round-transition path.
- `LightingProfile.sun` is a `Light` **component reference serialized in a ScriptableObject**, which cannot point at a scene object. Resolve the sun at runtime from the loaded scene instead of trusting the asset field.

What to build on top: a runtime applier that takes a base profile plus a condition override, blends between them over time, and **restores the previous settings on unload**. Keep the `LightingProfile` asset type — it is the right data shape — and replace the applier.

**Make the effects reach the systems that matter**

- **Perception** — the visibility range in the condition data is consumed by the monster perception system (§6) as a hard cap on sight range, on the server. This is the fairness rule; it must be one number read by both the renderer and the AI, not two numbers that drift.
- **Noise** — rain and storm raise the ambient noise floor, which should *mask* player noise. A condition that makes you harder to hear is a genuinely interesting inversion and costs one multiplier in the noise system (§6).
- **Movement** — flooding and mud belong in the movement layer, through the same `combinedMoveSpeedModifier` hook in `FirstPersonController.AccumulateMovement` that [`12_carry_weight.md`](12_carry_weight.md) uses. Both must compose rather than overwrite. Note that anything affecting movement speed is **client-predicted**, so the condition's modifier must be identical on client and server at the same tick or every step in water produces a correction.
- **Water** — `LayerIndex.Water = 4` exists and is unused. If flooding is implemented, that layer is the ready-made mechanism for volume detection, and the drowning damage source is already required by [`13_health_and_injury.md`](13_health_and_injury.md).
- **Audio** — ambience beds route through the existing `SoundSystem` and `SoundDef` assets. The headless no-op path means a dedicated server pays nothing for this.

**Keep it out of the interior, mostly**

- Weather is an **exterior** condition ([`33_exterior_approach_area.md`](33_exterior_approach_area.md)). The interior's atmosphere is the power grid's job ([`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md)).
- The interesting exceptions are the ones that reach inside: flooding that inundates the lower floor, a storm that knocks the power out. Each of those is a *deliberate crossover* with a specific rule, and each is worth more than a fifth outdoor-only condition.
- Do not let weather change interior loot placement. The loot spawner draws from the location's data and the loot stream; weather is not an input to it.

## Acceptance Criteria

- [ ] `WeatherData` and a registry exist with explicit serialized ids and a dictionary built at load, asserting on duplicates.
- [ ] The condition is rolled from its own derived stream of the round seed and reproduces exactly for a given seed and day.
- [ ] The condition id is replicated on the Run Manager, appears in the shared-state inventory, and is identical on every client before the load begins.
- [ ] Every destination's forecast is visible in the hub before the crew commits, and matches what they arrive to.
- [ ] Weather never changes loot count, loot value, or loot placement — verified by generating the same seed under every eligible condition and comparing the loot manifest.
- [ ] Eligible conditions are per-location data; an ineligible condition never rolls for a location.
- [ ] Any condition that reduces player visibility reduces monster sight range by the same replicated value, read by both the renderer and the AI.
- [ ] Ambient noise conditions measurably mask player noise in the noise system.
- [ ] Movement modifiers compose with carry weight rather than overwriting it, and produce no prediction correction under simulated latency.
- [ ] Lighting transitions blend rather than snapping, and can change during a round.
- [ ] Returning to the hub fully restores the hub's lighting, fog, and ambient settings, verified across three consecutive rounds in different conditions.
- [ ] The sun light is resolved from the loaded scene at runtime, not from a serialized asset reference.
- [ ] Gameplay-consequential weather state — water level, hazard positions, strikes — is server-authoritative and identical on every client.
- [ ] Each condition is describable to a player in one sentence, and that sentence is what the terminal shows.
- [ ] A debug command forces any condition on any destination.
- [ ] A dedicated-server build runs every condition with no rendering or audio cost.
