# 99 — Performance Budget

**Source:** [`core_components.md`](../core_components.md) §13 — Onboarding, Performance & Long Tail
**Status:** ❌ No budget established · **[MVP]**
**Depends on:** nothing — establish it before the content that will violate it
**Blocks:** finding out too late that the game does not run

## Summary

Deciding what the game is allowed to cost, before building the things that cost it.

`core_components.md` lists what is coming: procedurally generated geometry, multiple active monsters, dynamic lighting, physics props, and pooled audio, all replicated to several clients. It also flags the specific hazard — **procedural interiors silently destroy batching and lightmapping assumptions.**

A budget is worth establishing early for a reason that is easy to under-weight: performance problems in this project will not appear as a single expensive thing. They will appear as **forty small things**, each individually defensible, discovered together at the point where a full location, a full monster budget, and four clients exist simultaneously — which is late. At that point every fix is a renegotiation with a system that already shipped.

The budget's real function is not the number. It is that it makes each system's cost **someone's explicit allocation** rather than whatever it happened to use.

## How to Build

**Fix the target hardware first, because a budget without one is a wish**

- Name a minimum spec and a target frame rate. Everything below is meaningless without it, and "runs well on my machine" is how a project ends up shipping at 24 fps on a laptop.
- Build profiles exist for `Windows Client` and `Android Client`. **Android is the binding constraint** and it is easy to forget while developing on a desktop — a mobile target changes the light budget, the draw-call budget, and the physics budget by an order of magnitude.
- If Android is genuinely a target, say so and budget against it. If it is aspirational, say that instead, so nobody tunes for a platform the game will not ship on.

**Allocate by system, not as a single number**

A frame budget divided into named allocations is what makes a violation attributable:

- **Procedural interior rendering** — draw calls, batches, and real-time lights. [`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md) already requires a per-module light budget and a tighter shadow-caster cap, and warns that forty rooms with four shadow-casting lights each will not run anywhere.
- **Monsters** — server-side AI, perception queries, and pathfinding. [`53_perception_system.md`](53_perception_system.md) caps sight queries per tick and staggers them; [`55_chase_and_pathfinding.md`](55_chase_and_pathfinding.md) caps path requests per tick. Those caps are budget allocations and should be stated as such.
- **Item physics** — awake rigidbody count, capped explicitly by [`47_physics_props_and_throwing.md`](47_physics_props_and_throwing.md), with settled items sleeping.
- **Audio** — concurrent voices, with monster cues taking priority over ambience ([`82_monster_audio_cues.md`](82_monster_audio_cues.md), [`83_ambience_and_time_cues.md`](83_ambience_and_time_cues.md)).
- **UI** — no per-frame allocation, enforced centrally in [`71_hud.md`](71_hud.md).
- **Generation** — the deploy-time budget, which is a *load* budget rather than a frame budget, and belongs to [`28_procedural_interior_generator.md`](28_procedural_interior_generator.md) and [`30_runtime_navmesh_baking.md`](30_runtime_navmesh_baking.md).

**Solve the batching problem before authoring the module set**

- This is the one that cannot be fixed later. A procedurally assembled interior has no static batching and no baked lighting, and the decision about how to render it efficiently — GPU instancing, runtime light probes, module-level combining — **constrains how modules are authored** ([`28_procedural_interior_generator.md`](28_procedural_interior_generator.md) and [`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md) both flag this as a day-one prefab constraint).
- Prototype the rendering approach with a stub module set before the real one is built. Discovering the constraint after authoring forty prefabs means re-authoring forty prefabs.
- The same applies to per-module NavMesh stitching, which [`30_runtime_navmesh_baking.md`](30_runtime_navmesh_baking.md) recommends and which requires modules to align exactly at their seams.

**Profile the worst case, not the average**

The average case will always pass. Define the worst case explicitly and measure against it:

- The **largest location** at its maximum size multiplier, with maximum loot count, maximum spawn budget spent on the cheapest monsters, a full crew, weather active, and the power grid on.
- The **exterior** at the same time — [`33_exterior_approach_area.md`](33_exterior_approach_area.md) notes outdoors is where the frame budget goes, with large view distances, dynamic lighting, and fog, and argues the budget should be established against it.
- **A room full of dropped items**, which [`47_physics_props_and_throwing.md`](47_physics_props_and_throwing.md) calls a normal Tuesday.
- Build a **profiling scenario** using the launcher from [`88_debug_and_cheat_tooling.md`](88_debug_and_cheat_tooling.md) so this configuration is one command, not a manual setup. A worst case that takes twenty minutes to reproduce gets profiled once.

**Measure the host separately — it is doing two jobs**

- With Relay + Lobby the host is a player's machine running the server world *and* a client. It pays for AI, perception, pathfinding, physics, and snapshot construction on top of rendering.
- **The host's budget is the real minimum spec**, and it is the one most likely to be missed because developers usually host.
- Profile as a pure client too, and record both. A game that runs well for clients and poorly for the host is a game where the person who organised the session has the worst experience.
- `com.unity.profiling.core` is available and Unity's profiler works against a build — profile the build, not the Editor ([`96_editor_vs_build_test_paths.md`](96_editor_vs_build_test_paths.md)).

**Enforce it, or it is documentation**

- Fail loudly in development when a budget is exceeded: too many lights in a module, too many awake rigidbodies, too many concurrent voices. Several plans already require exactly this per-system; route them into the single reported surface [`88_debug_and_cheat_tooling.md`](88_debug_and_cheat_tooling.md) establishes.
- Re-profile the worst case at each milestone and record the numbers alongside the build verification pass artefacts ([`97_build_verification_pass.md`](97_build_verification_pass.md)), so regressions are visible as a trend rather than discovered as a complaint.

## Acceptance Criteria

- [ ] A minimum spec and target frame rate are named, including an explicit decision about Android.
- [ ] The frame budget is divided into named per-system allocations covering rendering, AI, physics, audio, and UI.
- [ ] Each system's existing cap — perception queries, path requests, awake rigidbodies, audio voices, lights per module — is stated as a budget allocation.
- [ ] The procedural rendering approach is prototyped with a stub module set before the real modules are authored.
- [ ] Module authoring constraints from rendering and NavMesh stitching are documented before prefab authoring begins.
- [ ] A worst-case profiling scenario is defined: largest location, maximum loot, maximum spawn budget, full crew, weather, power on.
- [ ] The exterior is profiled as its own worst case.
- [ ] A room full of dropped items is profiled explicitly.
- [ ] The worst-case scenario is reproducible with one command via the scenario launcher.
- [ ] The host is profiled separately from a pure client, and both figures are recorded.
- [ ] The host holds the target frame rate in the worst case on minimum spec.
- [ ] Profiling is done against a build, not the Editor.
- [ ] Budget violations fail loudly in development through the single reported surface.
- [ ] The worst case is re-profiled at each milestone and results recorded alongside build verification artefacts.
- [ ] No system exceeds its allocation at the point it is considered complete.
