# 93 — Addressables Content Build

**Source:** [`core_components.md`](../core_components.md) §12 — Build & Release Readiness
**Status:** ⚠️ Works in the Editor; one missing step breaks the shipped build
**Depends on:** [Data-Driven Configuration](87_data_driven_configuration.md) (group organisation)
**Blocks:** anything loading at runtime in a standalone build

## Summary

The build step that, when forgotten, produces a game that works perfectly in the Editor and is empty when shipped.

`core_components.md` states the failure exactly: Addressable content **must be built before the player build**, or ghost prefabs, projectiles, and player prefabs resolve to null at runtime while working fine in the Editor. That asymmetry is what makes it dangerous — the Editor's asset database resolves Addressable references directly, so nothing in day-to-day development ever exercises the packed content path.

Addressables is genuinely load-bearing here, not incidental. `GhostSpawner.GhostReference` wraps an `AssetReferenceGameObject` with a serialized `Hash128` GUID and is how every ghost prefab is resolved — `WeaponData` uses it for projectile, muzzle flash, and hit VFX prefabs, and the plans extend it to items ([`37_item_definition_data_model.md`](37_item_definition_data_model.md)), monsters ([`48_monster_data_definitions.md`](48_monster_data_definitions.md)), and room modules ([`28_procedural_interior_generator.md`](28_procedural_interior_generator.md)). If content resolution fails, essentially nothing spawns.

The second half is organisational. Only a **"Default Local Group"** exists today, which is fine for a handful of prefabs and wrong for a game that will load a destination's content per round.

## How to Build

**Make the content build unskippable**

- The failure is a **process** failure, not a code one, so the fix is process: hook the Addressables content build into the player build rather than trusting anyone to remember it.
- Unity supports building content as part of the player build pipeline; use it. A build script that builds content then the player, invoked by CI or by a single menu item, removes the whole class of failure.
- If a manual step remains, it must be **impossible to miss** — a build-time check that fails the build when the content catalogue is older than the newest Addressable asset is better than documentation.
- Every build profile needs this: `Windows Client`, `Android Client`, and `FPS2 Windows Server`. The dedicated server loads ghost prefabs too — it is the authoritative spawner — so a server built without content spawns nothing while reporting no error.

**Organise the groups before there are hundreds of assets**

- Group **per location**, so a destination's room modules and props load and unload with the round ([`26_location_catalogue.md`](26_location_catalogue.md) requires per-location groups with no residue after a round).
- Shared groups for items, monsters, and common VFX, which are needed everywhere and should not be duplicated into each location's bundle.
- A group's contents are its load granularity. A single giant bundle means every deploy loads everything; hundreds of tiny ones means hundreds of requests. Aim for one bundle per location plus a few shared ones.
- Do this **before** the asset count grows. Re-grouping later is mechanical but invalidates every cached bundle and complicates any patching story.

**Unload what you load**

- [`05_location_load_unload_flow.md`](05_location_load_unload_flow.md) requires entity count and memory to return to baseline across five consecutive load/unload cycles, and Addressables is one of the two places that silently fails — a released `AsyncOperationHandle` that nobody released keeps its whole bundle resident.
- Track every handle acquired during a round and release it at teardown. `GhostSpawner`'s prefab resolution is the main acquirer; audit it for handle lifetime alongside the pooling work in [`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md).
- Profile bundle memory across repeated deploys specifically, not just entity counts. A slow climb over ten rounds is the signature.

**Handle the load failure honestly**

- An Addressable that fails to resolve currently produces a null and a spawn that silently does not happen. In a game where a missing prefab could be the extraction zone, that must be **loud**.
- Fail the load barrier ([`05_location_load_unload_flow.md`](05_location_load_unload_flow.md)) on a content load failure rather than proceeding into a half-populated location. That plan already requires a defined failure path rather than silent partial failure; content resolution is one of its causes.
- Log the failing asset's GUID. `GhostSpawner.GhostReference` carries a serialized `Hash128`, which is exactly what makes an otherwise anonymous failure traceable.

**Verify against a real build**

- The only meaningful test is a **standalone build with packed content**, launched and played. §12 notes Editor testing does not prove a build works, and this component is the clearest case.
- Switch the Addressables play mode to "Use Existing Build" during development periodically, so the packed path gets exercised without a full build every time.
- Include a content check in the build verification pass ([`97_build_verification_pass.md`](97_build_verification_pass.md)): spawn one of every registered ghost prefab in a smoke test and assert none resolved to null.
- Content is also a **parity** surface. A client and server built from different content produces the missing-prefab failure on one side only — [`95_client_server_build_parity.md`](95_client_server_build_parity.md) and [`87_data_driven_configuration.md`](87_data_driven_configuration.md) both require a version stamp, and the content catalogue version belongs in it.

## Acceptance Criteria

- [ ] Addressable content is built automatically as part of the player build, or an unskippable check fails the build when content is stale.
- [ ] Content is built for every build profile, including the dedicated server.
- [ ] Groups are organised per location plus shared groups for items, monsters, and common VFX.
- [ ] Bundle granularity is one per location plus a small number of shared bundles.
- [ ] A location's content loads on deploy and unloads on return to the hub.
- [ ] Every `AsyncOperationHandle` acquired during a round is released at teardown.
- [ ] Bundle memory returns to baseline across five consecutive deploys, profiled explicitly.
- [ ] A failed content load fails the load barrier loudly rather than producing a half-populated location.
- [ ] A failed load logs the asset's GUID.
- [ ] A standalone build with packed content spawns every ghost prefab correctly.
- [ ] A smoke test spawns one of every registered ghost prefab and asserts none resolve to null.
- [ ] The content catalogue version is part of the client/server version stamp.
- [ ] "Use Existing Build" play mode is exercised periodically during development.
