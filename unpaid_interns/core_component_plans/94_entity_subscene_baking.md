# 94 — Entity Subscene Baking

**Source:** [`core_components.md`](../core_components.md) §12 — Build & Release Readiness
**Status:** ⚠️ Correct today for two subscenes; the process does not scale
**Depends on:** [Location Load / Unload Flow](05_location_load_unload_flow.md)
**Blocks:** any new subscene working in a shipped build

## Summary

The registration step that makes baked entity data exist at runtime.

The current state is correct and fragile in the same breath. `GameResourcesSubScene` and `SpawnPointsSubScene` are baked entity scenes and both are properly listed in `EditorBuildSettings`. The fragility is in the process, not the data: `core_components.md` states that **any new subscene must be added to build settings and to every relevant build profile**, or the entity world comes up empty at runtime.

That failure has the same shape as the Addressables one and is worse in one specific way — it produces **no error**. A subscene absent from a build profile does not throw; the entities simply are not there, and every system that queries for them finds nothing and does nothing. `ServerGameSystem.FindSpawnPoint` querying an empty `SpawnPoint` archetype means players spawn at the origin, or not at all, with a clean log.

The reason this matters now rather than later: [`05_location_load_unload_flow.md`](05_location_load_unload_flow.md) turns one-time scene loading into **per-round loading of a different location every time**, and [`26_location_catalogue.md`](26_location_catalogue.md) adds destinations as content. The number of subscenes is about to grow from two to one per location plus per-location content, and a manual registration step that works for two will not survive twelve.

## How to Build

**Automate the registration check**

- A build-time validation that every subscene under the project's scene directories is present in `EditorBuildSettings` **and** in each build profile's scene list, failing the build on a mismatch.
- This is the entire component's value. A checklist item will be missed; a failing build will not.
- Include `FPS2 Windows Server` explicitly. The server bakes the authoritative copy of the world, so a subscene missing from the server profile is worse than one missing from a client — the client will show geometry the server does not simulate.
- Report the specific missing subscene and profile, not just "validation failed".

**Understand what the barrier already does, and what it does not**

- `ScenesLoader.WaitForAllSubScenesToLoadAsync` polls `SceneSystem.IsSceneLoaded` per `SceneReference` entity, **per world**, which [`05_location_load_unload_flow.md`](05_location_load_unload_flow.md) correctly identifies as the right check and the right foundation.
- But it waits for the subscenes that **are** referenced. A subscene missing from the build has no `SceneReference` entity, so the barrier passes immediately and reports success. **The barrier cannot detect a missing subscene** — it can only detect a slow one.
- That is why the validation must happen at build time. Add a runtime assertion too: after loading, assert that the expected subscene set for this location is present, so a packaging error surfaces at the loading screen rather than as an empty world.

**Make per-round baking work**

- Subscene load and unload repeated every round is new behaviour for this project — nothing repeats today. [`05_location_load_unload_flow.md`](05_location_load_unload_flow.md) already flags ECS subscene reload as *"the most likely source of a slow memory climb over a long run"*.
- Verify entity counts and memory return to baseline across five consecutive load/unload cycles, which that plan requires. Subscene sections that stay resident after unload are the specific thing to watch.
- Confirm unloading a subscene while another is loading behaves — a crew returning to the hub and immediately deploying again is normal play, not an edge case.

**Keep baked and generated content separate**

- A location's **authored** parts — the exterior scene, the extraction zone, hand-placed props ([`33_exterior_approach_area.md`](33_exterior_approach_area.md)) — are baked subscenes.
- A location's **generated** interior is not baked; it is assembled at runtime from a seed ([`28_procedural_interior_generator.md`](28_procedural_interior_generator.md)) and deliberately never replicated.
- Room module prefabs are Addressable content ([`93_addressables_content_build.md`](93_addressables_content_build.md)), not subscenes. Keeping that boundary clear matters, because the two have different registration requirements and different failure modes, and conflating them produces a build where one half works.
- Document which is which per location, so adding a destination has an unambiguous checklist.

**Watch baking cost as content grows**

- Baking happens at build time and in the Editor on change. Twelve locations of authored content is meaningfully more than two subscenes, and a slow bake becomes a tax on every iteration.
- Measure it once there are more than three locations, and split subscenes by concern if it becomes painful — a designer editing props should not rebake collision.

## Acceptance Criteria

- [ ] A build-time validation asserts every subscene is present in `EditorBuildSettings` and in every build profile, and fails the build on a mismatch.
- [ ] The validation names the specific missing subscene and profile.
- [ ] The `FPS2 Windows Server` profile is covered by the validation.
- [ ] A runtime assertion confirms the expected subscene set for a location is present after loading, surfacing a packaging error at the loading screen.
- [ ] The existing per-world `SceneSystem.IsSceneLoaded` barrier is retained and not replaced by a plain scene-load await.
- [ ] Subscenes load and unload repeatedly across at least five deploy cycles with entity counts and memory returning to baseline.
- [ ] Unloading one subscene while another loads behaves correctly.
- [ ] Authored subscene content and runtime-generated content are documented separately per location.
- [ ] Room module prefabs ship as Addressable content, not as subscenes.
- [ ] Adding a new location has an unambiguous registration checklist that the build validation enforces.
- [ ] Bake time is measured once more than three locations exist, and subscenes are split by concern if iteration becomes slow.
- [ ] A standalone build and a dedicated-server build both come up with fully populated entity worlds.
