# 49 — Monster Ghost & Replication

**Source:** [`core_components.md`](../core_components.md) §6 — Monsters & AI
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Monster Data Definitions](48_monster_data_definitions.md), [Runtime NavMesh Baking](30_runtime_navmesh_baking.md)
**Blocks:** every monster behaving identically for all four players

## Summary

Monsters simulate on the server and appear on clients. That sentence is the whole architecture, and getting it stated plainly up front prevents the expensive mistake.

`core_components.md` §6 says it directly: monsters should **not** be client-predicted, unlike players. The reason is worth internalising rather than accepting on authority. Prediction is for the thing whose input you have locally — your own character. A monster's next move depends on server-side perception, pathfinding, and targeting state that the client does not have and should not have; predicting it would mean either replicating the AI's whole world model (expensive and a cheat vector, since a client that knows a monster's target knows where it is looking) or predicting from incomplete state and mispredicting constantly, in the most visually obvious way available — a monster that jitters and teleports.

Interpolated ghosts are also simply correct here. A monster that arrives 100 ms late looks fine. A monster in the wrong place looks like a broken game.

The GhostBridge already provides everything needed. `GhostSpawner.SpawnGhostPrefab` handles Addressable prefab spawning with a network GUID; `GhostGameObject` bridges to a MonoBehaviour; and the transform pipeline — `ServerGhostTransformRetrieveSystem` writing into a `NativeArray<LocalTransform>` and `ClientGhostTransformApplySystem` applying through a batched `TransformAccessArray` job — is built for exactly this traffic, including error blending via `GhostGameObjectTransformSync.ErrorOffset` and `ErrorBlendTime`.

## How to Build

**Spawn and own on the server**

- Server-only spawn through `GhostSpawner.SpawnGhostPrefab`, with the monster id and initial state written in the `postSpawnSpecialisation` callback so no client ever observes a partially initialised monster. This is the same discipline [`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md) requires of items, and it matters more here — a monster that links with monster id zero for one frame is a visible wrong creature.
- Set `RequireTransformSync = true`. Unlike items, a monster's defining characteristic is that it moves, and it is the traffic this component exists to budget.
- The `NavMeshAgent` and all AI components live on the **server instance only**. The client instance is a puppet: mesh, animator, audio, and nothing that thinks. Guard on `Role == MultiplayerRole.Server`, following `GhostMonoBehaviour`'s `IUpdateServer` / `IUpdateClient` split as `GameLeaderboard.cs` demonstrates.
- Apply the role-separated layer rule from [`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md): `ServerMonster` and `ClientMonster` layers assigned on link, mirroring `PlayerGhost`'s line-150 pattern. In a host process both instances share one PhysX scene, and without the split a player's melee swing hits whichever copy the query returned first, and monster hit detection double-counts.

**Replicate the minimum**

What crosses the wire:

- **Monster id** — resolved locally against the registry. Never the definition.
- **Transform** — position and rotation, through the existing pipeline.
- **A small behaviour state enum** — `Idle`, `Alerted`, `Searching`, `Chasing`, `Attacking`, `Dead`. This drives animation, audio selection, and the fear system's "am I being hunted" term ([`15_fear_and_stress_feedback.md`](15_fear_and_stress_feedback.md)), and it is far cheaper than replicating the inputs that produced it.
- **Health**, if the monster is killable and its state should be legible.
- **A tick stamp per one-shot event** — `LastAttackTick`, `LastSpawnTick` — following the `LastShotTick` / `LastHitTick` pattern already used on `PredictedPlayerGhost` and compared against a cached tick in `HandleAnimationEvents`. This is how a client plays an attack animation exactly once without a reliable RPC per swing.

What must **not** cross the wire:

- The current target. A client that knows which player a monster is hunting has been handed the single most valuable piece of information in the game, and a modified client will display it. [`56_threat_interest_targeting.md`](56_threat_interest_targeting.md) keeps targeting server-side; if a UI needs "this monster is after *me*", send that only to the targeted player.
- Path, perception memory, last-known-position, or any internal timer. All server-side.
- Anything a client can derive from the behaviour state and the transform.

**Budget the bandwidth honestly**

- §13 puts monsters and players in the same tier: they move constantly and matter constantly. Set a **high `GhostImportance`** relative to items and session state, which yield first.
- Use **distance-based relevancy**. A monster on the far side of a large interior is not worth a snapshot slot — but be careful with the threshold, because a monster that becomes relevant only at close range will pop into existence in front of a player. Set the radius comfortably beyond audible range and beyond the fear system's proximity term, or the horror arrives before the creature does.
- Quantize position and rotation. A monster's exact sub-centimetre position is not information anyone needs.
- Measure with the maximum power budget spent on the cheapest monsters — the worst case is many small creatures, not one large one.

**Make the client puppet look right**

- Drive animation from the replicated behaviour state and the ghost's own velocity. `GhostGameObject` computes a `MovementContext` carrying `Velocity`, `VelocitySqrd`, and `MinDistSqrdFromAPlayer` when `RequireMovementContextCalculation` is set — that is the ready-made input for locomotion blending and for distance-based LOD, and it means the client does not need its own velocity estimate.
- Use the error-blend fields for corrections rather than snapping. A monster teleporting a metre is more alarming than a monster arriving smoothly a frame late, and not in the way the game wants.
- Route audio through the existing `SoundSystem` and `SoundDef` assets, selected by behaviour state ([`48_monster_data_definitions.md`](48_monster_data_definitions.md)). The headless no-op path means a dedicated server pays nothing.
- `MinDistSqrdFromAPlayer` is also the natural gate for expensive client-side presentation — full animation and audio near the player, reduced at distance.

**Die and clean up properly**

- Death is server-authoritative, sets the state to `Dead`, and returns the monster's power cost to the spawn director's budget ([`50_spawn_director.md`](50_spawn_director.md)).
- Decide whether a corpse persists. Recommended: a short-lived body that despawns, because a permanent corpse is a permanent ghost and a loot-dense map cannot afford many.
- Destroy every monster ghost at round teardown, and verify entity and memory counts return to baseline across five consecutive rounds — the same check [`05_location_load_unload_flow.md`](05_location_load_unload_flow.md) and [`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md) require. A leaked monster is invisible, cumulative, and still consuming a budget slot.
- A monster whose navigation is destroyed under it — round ending, a deployed ladder removed — must not throw or freeze. Despawn cleanly.

**Make it debuggable**

- A server-side debug overlay showing each monster's behaviour state, current target, path, and perception radii. Diagnosing AI from a client that deliberately does not receive any of that is impossible, and this overlay is the only way anyone will ever tune the roster.
- Log spawn, state transitions, target changes, and death with the round seed, so a reported encounter can be reconstructed.

## Acceptance Criteria

- [ ] Monsters spawn server-side through `GhostSpawner.SpawnGhostPrefab` with id and initial state set in the same command buffer.
- [ ] Monsters replicate as interpolated ghosts and are never client-predicted.
- [ ] AI components and the `NavMeshAgent` exist only on the server instance; the client instance runs no decision logic.
- [ ] `ServerMonster` and `ClientMonster` layers are assigned by role on link, and a physics query in a host process returns one collider per monster per role.
- [ ] Replicated state is limited to monster id, transform, behaviour state, health where applicable, and one-shot tick stamps.
- [ ] A monster's current target is never replicated to non-targeted clients, verified by inspecting a client's replicated state.
- [ ] Attack and other one-shot animations fire exactly once per event on every client under latency.
- [ ] Monster ghosts use a high importance and distance relevancy, with the relevancy radius exceeding audible and fear-proximity range so nothing pops in nearby.
- [ ] A full monster power budget of the cheapest monsters stays within the per-snapshot bandwidth budget with four clients.
- [ ] Client animation is driven from behaviour state and the ghost's computed movement context, with no client-side velocity estimation.
- [ ] Position corrections blend rather than snapping.
- [ ] Per-monster idle, alerted, and chase audio is distinct and selected from the replicated behaviour state.
- [ ] Expensive client presentation is reduced at distance using the ghost's distance-to-player value.
- [ ] Death is server-authoritative and returns power to the spawn budget.
- [ ] The corpse persistence rule is implemented and documented here.
- [ ] All monster ghosts are destroyed at round end; five consecutive rounds return entity and memory counts to baseline.
- [ ] A monster whose navigation disappears despawns cleanly without errors.
- [ ] A server-side debug overlay shows behaviour state, target, path, and perception radii.
- [ ] Two clients under simulated latency see the same monster in the same place, within interpolation tolerance, throughout a chase.
