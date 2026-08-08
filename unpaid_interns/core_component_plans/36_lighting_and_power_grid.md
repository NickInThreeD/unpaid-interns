# 36 — Lighting & Power Grid

**Source:** [`core_components.md`](../core_components.md) §4 — Location & World Generation
**Status:** ⚠️ Static lighting configured; nothing dynamic or networked · **[MVP-adjacent]**
**Depends on:** Procedural Interior Generator, Interaction System, Item Ghost
**Blocks:** darkness as a tactical state, flashlight gear, blackout hazards, monster/player asymmetry

## Summary

Darkness that can be turned off and on, and a switch somewhere inconvenient that does it.

Two separate problems live under this heading and it is worth keeping them apart.

The **rendering problem** is that the project's lighting is authored for one static scene. URP is configured with baked probe volumes for `GameScene`, and a procedurally assembled interior cannot use any of it — there is nothing to bake at build time, and §13 flags directly that procedural geometry destroys batching and lightmapping assumptions. Solving this is a prerequisite for the interior looking like anything at all.

The **gameplay problem** is the interesting one: a facility whose power can be cut turns darkness from a constant into a **state**, and a state is something players can cause, avoid, and exploit. A breaker box in an awkward corner makes restoring power a decision with a cost. The reference implementation ([`Assets/docs/hazards/breaker-box.md`](../../Assets/docs/hazards/breaker-box.md)) uses a five-switch combination purely to make the fix take time in a place you do not want to stand.

The rule that keeps it fair is the same one weather has: **darkness must cost monsters something too.** A blackout that blinds only the crew is a punishment. A blackout that blinds sight-based monsters while helping sound-based ones is a tactical trade the crew can actually play against — and it is what makes crouching, the flashlight, and the scanner all matter at once.

## How to Build

**Solve the runtime lighting problem first**

- Decide the approach before the module set is authored, exactly as with navigation ([`30_runtime_navmesh_baking.md`](30_runtime_navmesh_baking.md)) — this is a constraint on prefabs, not a post-process.
- Realtime lights only, with a **hard per-module light budget** and a much tighter cap on shadow-casting lights. An interior of forty rooms each with four shadow-casting point lights will not run anywhere, and it will be discovered after the modules are authored.
- Use runtime light probes or an adaptive probe volume placed after assembly, plus GPU instancing for repeated module geometry. Both need to be planned in from the start.
- Budget this against the exterior's frame cost ([`33_exterior_approach_area.md`](33_exterior_approach_area.md)), which is the worst case, and profile on the lowest-spec target before the module set grows.
- Lighting is **presentation and is not replicated**. Every client renders the interior from the same seed and the same power state; light objects themselves never become ghosts. Only the *power state* crosses the wire.

**Model power as zones, not as one switch**

- Author each module with a power zone id. The interior's power state is a small set of zone flags — a handful of bits — rather than per-light state.
- Replicate the zone flags as a `[GhostField]` on a per-round manager or on the door/breaker ghost. This is cheap, it is the only part of lighting that is networked, and it is what guarantees two players in the same corridor see the same darkness.
- Clients apply zone state to their local lights on change, not per frame. A zone flipping should be one event and one batched light update.
- Emergency lighting is a separate, always-on tier that survives a cut. **Total darkness must not be the unpowered state.** A room with genuinely zero light is unplayable without a flashlight, which makes the flashlight mandatory rather than valuable, and makes losing it a run-ender. Dim red emergency light preserves navigability and looks better anyway.

**Build the breaker box**

- One per location that has a power grid, its presence and eligible rooms declared in `LocationData` ([`26_location_catalogue.md`](26_location_catalogue.md)); placement drawn from the interior seed stream by the generator ([`28_procedural_interior_generator.md`](28_procedural_interior_generator.md)) into a room that is deliberately inconvenient.
- It is a networked interactable: absolute state, server-authoritative, resolved through [`20_networked_interaction_authority.md`](20_networked_interaction_authority.md) so two players operating it on the same tick converge on one result rather than toggling past each other.
- Give it a **cost in time and position**, not in puzzle. The five-switch combination in the reference is a way of making you stand still in a bad place for twenty seconds; anything that achieves that is equivalent. A multi-second hold interaction is simpler, reads better, and does not need a UI.
- It hums audibly, which is how it is found in the dark. Route that through the existing `SoundSystem` and make sure it is loud enough to be a landmark and quiet enough to be missed.

**Decide what cuts the power**

- **The crew, deliberately** — cutting power is a tactic if darkness hurts monsters more than players. Allow it.
- **The location, on a timer or an event** — a scheduled brownout as the round progresses pairs with difficulty escalation and makes the late round visibly worse, which is exactly the pressure [`03_round_timer_clock.md`](03_round_timer_clock.md) exists to create.
- **Weather** — a storm knocking the power out is the crossover that makes weather reach inside the building ([`35_environmental_conditions_weather.md`](35_environmental_conditions_weather.md)) and is worth more than another outdoor-only condition.
- **A hazard or monster** — an entity that kills lights ahead of itself is a strong, cheap threat design.
- Whatever the source, restoring power must always be *possible*. A permanent unrecoverable blackout is a round the crew cannot play.

**Make darkness two-sided**

- Publish the light level at a world position as a queryable value, computed the same way on the server for AI and on the client for presentation. This is the number [`15_fear_and_stress_feedback.md`](15_fear_and_stress_feedback.md) already expects for its darkness term, and the one the perception system (§6) needs for sight range.
- Sight-based monsters lose range in the dark. Sound-based monsters do not — and if the crew is running because they cannot see, they are also louder, which is the whole trade.
- A player carrying a light source is **more visible**, not less. A flashlight should be a genuine decision, not a strict upgrade ([`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md)).
- Verify the scanner still works with the power cut ([`16_player_scanner_ping_tool.md`](16_player_scanner_ping_tool.md)) — that requirement is already written there and this is the component that has to honour it.

**Respect accessibility**

- §9 elevates accessibility to required. A minimum-brightness or gamma setting must exist and must be honoured by the blackout state, because "the game is unplayably dark on my monitor" is the single most common accessibility complaint in this genre.
- Never make an unlit interior the only way to receive critical information. If something must be seen, it must be findable by scan or sound as well.

**Tear it down**

- Power zone state is per-round. Reset it on unload, and verify no lighting override, fog change, or `RenderSettings` mutation survives into the hub — the same restoration requirement [`35_environmental_conditions_weather.md`](35_environmental_conditions_weather.md) places on weather, and the same one-shot global-state bug in `LightingProfileApplier` is the likely cause if it does.

## Acceptance Criteria

- [ ] A procedurally assembled interior renders with correct lighting, with no reliance on build-time baked data.
- [ ] Per-module light and shadow-caster budgets are enforced, and exceeding them fails loudly in development.
- [ ] Interior lighting holds the frame budget on the lowest-spec target with a full crew and a fully assembled large location.
- [ ] Power is modelled as zone flags, replicated as a small `[GhostField]`, and identical on every client.
- [ ] No light object is ever replicated as a ghost.
- [ ] A zone changing state updates client lighting in one batched operation, not per frame.
- [ ] Emergency lighting keeps every unpowered area navigable without a flashlight.
- [ ] A breaker box spawns per the location's data, in a deliberately inconvenient room, and hums audibly enough to be found in the dark.
- [ ] Operating the breaker is server-authoritative and absolute; two players operating it on the same tick produce one outcome.
- [ ] Restoring power always costs time spent standing in a fixed place.
- [ ] Power can be cut by the crew, by a timed event, and by weather, and can always be restored.
- [ ] A queryable light level at a world position exists, is computed identically on server and client, and is consumed by monster perception and by fear feedback.
- [ ] Sight-based monsters measurably lose range in the dark; sound-based monsters do not.
- [ ] A player carrying an active light source is more visible to sight-based monsters, not less.
- [ ] The scanner remains usable and legible during a full blackout.
- [ ] A brightness or gamma accessibility setting exists and applies to the blackout state.
- [ ] No critical information is available only through vision.
- [ ] Power state resets on unload, and no lighting or `RenderSettings` change leaks into the hub across three consecutive rounds.
