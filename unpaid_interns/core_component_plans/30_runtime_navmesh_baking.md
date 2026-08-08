# 30 — Runtime NavMesh Baking

**Source:** [`core_components.md`](../core_components.md) §4 — Location & World Generation
**Status:** ❌ Not started · **[MVP]**
**Depends on:** Procedural Interior Generator, Location Load / Unload Flow
**Blocks:** Chase & Pathfinding, Spawn Director, every monster that moves

## Summary

Giving monsters something to walk on. A procedurally assembled building has no navigation data until something builds it, and nothing in this project has ever built one.

`com.unity.ai.navigation` 2.0.11 is in `Packages/manifest.json` and a repo-wide grep for `NavMesh` across `Assets/Scripts` returns **nothing**. There is no surface, no agent, no link, no baking call. This is genuinely greenfield, and `core_components.md` §4 flags it as *do not defer* for a specific reason: **the baking strategy constrains how the generator is allowed to assemble rooms**, and that constraint has to exist in the room prefabs from the day they are authored. Retrofitting it means re-authoring the module set.

There is one large piece of good news, and it is worth stating early because it is easy to assume the opposite in an ECS project. **Gameplay collision in this project runs on built-in PhysX, not DOTS physics.** `FirstPersonController` is `CharacterController`-based and queries with `Physics.SphereCastNonAlloc`; `Projectile.cs` uses `UnityEngine.Physics.SphereCast` and `OverlapSphere`; `ServerGameSystem` uses `UnityEngine.Physics.OverlapSphereNonAlloc` and imports `using Collider = UnityEngine.Collider;` specifically to disambiguate from `Unity.Physics`. `com.unity.physics` is present but is not what the player collides against. That matters here because `NavMeshSurface` bakes from built-in colliders and renderers — so the geometry the baker reads is exactly the geometry the player walks on, with no bridging layer to build.

## How to Build

**Bake on the server only — decide this first**

- Monsters are server-simulated and replicate to clients as interpolated ghosts (§6). Clients never path anything. **Only the server world needs a NavMesh**, and saying so out loud removes an entire class of problem.
- The consequence is that **navigation data does not have to be deterministic across machines.** Everything else downstream of the round seed does ([`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md)); this does not, because no client ever compares its NavMesh to anyone else's. Do not spend effort making the bake reproducible.
- It also halves the cost: the client's deploy time covers geometry assembly only, and the host absorbs the bake.
- The exception is debug tooling. A client-side bake behind a `ConfigVar` is worth having for visualising paths, but it must never be a gameplay dependency.
- In a host process the server and client worlds both exist in one PhysX scene, and both instantiate their own copy of every ghost GameObject. The baker must be pointed at the server's geometry only, or it will bake duplicate interpenetrating floors. This is the same role-separation problem `PlayerGhost` already solves by assigning `LayerIndex.ServerPlayer` or `ClientPlayer` by role at line 150 — use a layer-based include mask on the surface, not a scene sweep.

**Choose the bake strategy before the module set is authored**

Two viable approaches, and the choice is a hard constraint on prefab authoring:

- **Bake the whole assembled interior at once.** `NavMeshSurface.BuildNavMeshAsync` over the collected geometry. Simplest to implement, no authoring constraints, and slow — seconds on a large interior, on the machine that is also running the server. The load barrier means every player waits for it.
- **Pre-bake per module and stitch at runtime.** Each room prefab ships a baked `NavMeshData` asset; assembly calls `NavMesh.AddNavMeshData(data, position, rotation)` per placed module. Near-instant, and it requires **modules to align exactly at their connection seams** so adjacent tiles join. That alignment rule has to be in the prefabs from the start.
- **Recommended: pre-baked per module, stitched.** The deploy budget is shared by every player and generation already spends most of it. Accept the authoring constraint; it is the same grid discipline the generator's footprint-overlap test wants anyway.
- Whichever is chosen, keep `BuildNavMeshAsync` available as the fallback path for hand-built scenes — the hub and the exterior approach area ([`33_exterior_approach_area.md`](33_exterior_approach_area.md)) are static and can bake offline or once at load.

**Fix the agent types before anything else is authored**

- Agent radius is what decides whether a monster can fit through a doorway, and it is baked into the surface. Getting it wrong means re-baking every module.
- Define the agent types the game will ever have — small, human-sized, large — as Navigation Agent Types in project settings, and bake one surface per type per module. Two or three types is plenty; each one multiplies bake time and memory.
- Publish the radii to the generator as a hard constraint: **no corridor may be narrower than the largest agent that can spawn there.** [`18_pvp_collision_and_friendly_fire.md`](18_pvp_collision_and_friendly_fire.md) already places a minimum-width requirement on doorways from the collision side; these two numbers must be reconciled in one place, and the larger wins.
- Record the chosen radii in [`26_location_catalogue.md`](26_location_catalogue.md)'s eligible-monster data so a location cannot list a monster its layout set cannot accommodate.

**Sequence it inside the load barrier**

- Order is: seed replicated → geometry assembled → **NavMesh built** → barrier opens → phase moves out of `Deploying`. A monster that spawns before the bake completes will either stand still or fall through the world.
- The bake is a server-side step and the barrier in [`05_location_load_unload_flow.md`](05_location_load_unload_flow.md) already waits on every world. Extend the server's ready condition to include "navigation built" rather than adding a second gate.
- Use the async build and yield — a synchronous multi-second bake on the host stalls the whole session, including the netcode send loop, and looks to every client like a disconnect.
- Report progress into `LoadingData.LoadingSteps` so a slow bake reads as slow rather than hung.

**Handle the things that move**

- **Doors** — a closed door must block pathing or monsters will walk through it, which destroys the one tool players have for buying time (§7). Two options: a `NavMeshObstacle` with carving, or an off-mesh link that the door's open state enables and disables. Carving is simpler and re-tessellates the tile every time a door moves, which is expensive with many doors. **Recommended: links, with obstacles reserved for genuinely dynamic props.**
- **Ladders and drops** — [`17_climbing_and_verticality.md`](17_climbing_and_verticality.md) adds vertical connections the walkable surface cannot express. Author off-mesh links per module at the same time as the climb volumes, and decide per monster which links it may traverse. A monster that follows you up a ladder and one that cannot are two different threats, and that should be data.
- **Deployed ladders** — a runtime-spawned ladder item must add its link when placed and remove it when the round ends. Do not leak links across rounds.
- **Physics props** — dropped items must **not** carve the NavMesh. A floor covered in loot would re-tessellate constantly and cost more than the monsters do. Items are not obstacles; monsters walk over them.

**Tear it down**

- Remove every added `NavMeshData` instance on round unload. `NavMesh.RemoveNavMeshData` per handle, tracked in a list — leaked navigation data is invisible in the scene view and shows up as monsters pathing through walls that no longer exist.
- Verify across five consecutive deploys that navigation memory returns to baseline, alongside the entity-count check already required by [`05_location_load_unload_flow.md`](05_location_load_unload_flow.md).

**Budget and verify it**

- Set a hard bake-time budget as part of the deploy budget, and fail loudly in development when a location exceeds it. A generator change that doubles bake time will otherwise be discovered by a playtester.
- Add the navigation assertions to the generation harness in [`28_procedural_interior_generator.md`](28_procedural_interior_generator.md): after building, sample a path from each vent/emergence point to the extraction zone and assert it completes. **Connectivity of geometry is not connectivity of navigation** — a flood fill can succeed on a layout the baker turns into two disconnected islands, and that bug presents as "the monsters never came", which nobody reports.
- Add a debug overlay that draws the baked surface and a live agent's current path. Diagnosing pathfinding from first person is close to impossible.

## Acceptance Criteria

- [ ] A NavMesh is built for every generated interior before the round leaves `Deploying`, and no monster spawns before it exists.
- [ ] Navigation is built on the server world only; a pure client allocates no navigation data.
- [ ] The bake reads only server-role geometry and produces no duplicate surfaces in a host process.
- [ ] The chosen strategy — whole-interior bake or per-module stitching — is implemented and documented in this file.
- [ ] Agent types and their radii are fixed in project settings, and the generator enforces a minimum corridor width no smaller than the largest agent.
- [ ] The minimum-width rule is reconciled with the collision-mode width requirement in [`18_pvp_collision_and_friendly_fire.md`](18_pvp_collision_and_friendly_fire.md), with one number in one place.
- [ ] The bake is asynchronous and never stalls the server's network loop.
- [ ] Bake progress is visible in the loading UI.
- [ ] A closed door blocks monster pathing; opening it restores the route within one frame of the door state changing.
- [ ] Ladders and drops are traversable only by the monsters authored to use them.
- [ ] A deployed ladder item adds a usable link and removes it at round end.
- [ ] Dropped items never carve or re-tessellate the NavMesh.
- [ ] All navigation data is removed on unload; five consecutive deploys return navigation memory to baseline.
- [ ] The generation harness asserts a valid path from every emergence point to the extraction zone across at least 1,000 seeds.
- [ ] No generated layout produces unreachable navigation islands.
- [ ] Bake time stays within the deploy budget on the lowest-spec host, and exceeding it fails loudly in development builds.
- [ ] A debug overlay renders the baked surface and a live agent path, toggleable from a `ConfigVar` in a build.
