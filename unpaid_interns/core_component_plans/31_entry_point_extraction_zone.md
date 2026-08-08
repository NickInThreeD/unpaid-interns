# 31 — Entry Point / Extraction Zone

**Source:** [`core_components.md`](../core_components.md) §4 — Location & World Generation
**Status:** ⚠️ Static spawn points exist; the zone does not · **[MVP]**
**Depends on:** Location Load / Unload Flow, Procedural Interior Generator
**Blocks:** Loot Banking, Death & Body System (recovery), Day Cycle Controller (voluntary departure), Player Scanner

## Summary

The one place in the location that is safe, and the reason everywhere else is dangerous.

The extraction zone is where the crew arrives, where loot has to be carried back to, where bodies are recovered, and where the decision to leave is made. `GAME_DESIGN.md` puts steps 2, 4 and 5 of the core loop through it — *return items to the start point*, *decide when to leave* — which makes its **position** the single most important number in a generated location. Everything in the building is priced in round-trips from this spot.

What exists today is a spawn point and nothing more. `SpawnPointAuthoring` bakes an empty `SpawnPoint : IComponentData` plus a `LocalToWorld` from objects placed in `SpawnPointsSubScene`, and `ServerGameSystem.FindSpawnPoint` picks the least-crowded one using `UnityEngine.Physics.OverlapSphereNonAlloc` at a 2-metre radius against the `ServerPlayer` layer, into a fixed `Collider[16]` buffer. That is a deathmatch spawn selector. It has no volume, no concept of a location, no notion of loot, and it is baked into a subscene that only exists for the one hardcoded `GameScene`.

**Scope boundary:** this component owns *the place* — its geometry, its authority, its spawn behaviour, and the departure control attached to it. What counts as banked and how value converts to credits is [`43_loot_banking_deposit.md`](43_loot_banking_deposit.md). Keep the accounting out of this file.

## How to Build

**Make it a volume, not a point**

- Author an `ExtractionZone` prefab carrying a trigger volume, a set of player spawn transforms, a deposit surface, and the departure control. One prefab, used by the hub, by hand-built locations, and by the generator.
- The zone must expose an explicit **inside test** that the server can run against an arbitrary world position, rather than relying only on trigger callbacks. Banking is exactly-once accounting ([`20_networked_interaction_authority.md`](20_networked_interaction_authority.md)) and trigger enter/exit events are the wrong foundation for it — an item spawned already inside the volume fires no enter event at all, and a physics body resting on the boundary will fire enter and exit repeatedly.
- Keep the volume generous and its edge legible. A player who thinks they are inside and is not has lost their haul to a rendering decision. Mark the floor, light it differently, or fence it — whatever is chosen, the boundary must be visible from inside the building's approach.

**Watch the doubled-collider trap**

- Gameplay physics is built-in PhysX and, in a host process, the server and client worlds each instantiate their own copy of every ghost GameObject **into the same physics scene**. This is why `PlayerGhost` assigns `LayerIndex.ServerPlayer` or `ClientPlayer` by role (line 150) and why `Projectile` masks specifically on `ServerPlayer`.
- The extraction zone inherits the problem: a naive trigger will see both the server's and the client's copy of every item and every player, and count each one twice.
- Follow the established pattern — role-separated layers for item colliders ([`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md)) and an include mask on the zone's server-side query. The server's authoritative inside test must only ever consider server-role colliders.

**Place it, per location**

- **Hand-built locations and the hub** keep using authored placement. Extend `SpawnPointAuthoring` — or replace it — so a zone is a first-class baked entity rather than a bare marker, and so `FindSpawnPoint` selects from *this zone's* spawn transforms rather than from every `SpawnPoint` in every loaded subscene. With repeated per-round loads ([`05_location_load_unload_flow.md`](05_location_load_unload_flow.md)), a global query over all spawn points will eventually return one from the wrong location.
- **Generated interiors** place the zone at the main entrance, in the dedicated starting room the generator guarantees ([`28_procedural_interior_generator.md`](28_procedural_interior_generator.md)). The generator owns placement; this component owns what gets placed.
- **The exterior approach area** ([`33_exterior_approach_area.md`](33_exterior_approach_area.md)) is where the zone physically sits in the shipped design — the crew lands outside and walks in. Decide whether the deposit surface is at the drop-off outdoors or just inside the main entrance, because that distance *is* the risk gradient's baseline. Recommended: outdoors at the drop-off, so the walk between the building and safety is itself exposed.
- The generator must guarantee the zone is reachable from every room, which is a stronger condition than connectivity and is already an acceptance criterion on component 28.

**Fix the spawn selection**

- Spawn every intern here at round start, using the zone's own transforms. Keep the least-crowded selection — it is sound — but scope it to the zone and size the overlap buffer to the real crew size rather than the current `Collider[16]` sized against `GameManager.MaxPlayer = 32`.
- Spawning must happen after the load barrier clears, not during load. A player placed before geometry finishes baking falls through the floor.
- Returning players and late joiners arrive here too, per [`25_reconnection.md`](25_reconnection.md)'s recommendation that a mid-round reconnect appears at the extraction point rather than at their drop position.

**Own the departure control**

- The voluntary-departure end condition in [`02_day_cycle_controller.md`](02_day_cycle_controller.md) is an interaction on an object in this zone. It is the most consequential button in the game and needs to be treated like one.
- Server-authoritative: the client sends an interaction request, the server validates phase and position and starts the departure sequence. Never a client-side trigger.
- The unanimity question is component 02's to answer, but the *feedback* is this component's: whoever is inside the building must be told loudly and immediately that departure has begun, with time to run. A ship that leaves silently is a bug report every session.
- Make the control unmistakable and hard to trigger by accident. A player fleeing a monster who reflexively presses interact and ends the round for everyone will not be forgiven.

**Serve the systems that read it**

- **Scanner** — the zone is scannable at a much longer range than loot and through geometry ([`16_player_scanner_ping_tool.md`](16_player_scanner_ping_tool.md)). Losing the exit is the failure this exists to prevent.
- **Bodies** — depositing a corpse inside the volume registers recovery ([`14_death_and_body_system.md`](14_death_and_body_system.md)). It uses the same inside test as loot; do not write a second one.
- **Alternate exits** — a fire exit is *not* an extraction zone ([`32_alternate_exits.md`](32_alternate_exits.md)). It is a shortcut out of the interior, and the haul still has to reach this volume. Making that distinction visible in the world is what keeps alternate exits interesting rather than free.
- **Safety** — decide explicitly whether monsters may enter the zone. Recommended: they may approach but not enter the deposit volume, so a chase can end at the threshold. An unassailable safe room removes the tension of the last twenty metres; a zone monsters camp makes the game unplayable.

**Clean up**

- The zone is per-round world state. It is destroyed with the location on unload and rebuilt on the next deploy. Nothing about it may persist — a stale zone reference in the banking system after a round transition would credit the next round's items into the last round's total.

## Acceptance Criteria

- [ ] An `ExtractionZone` prefab exists with a trigger volume, spawn transforms, a deposit surface, and a departure control.
- [ ] The zone exposes a server-side inside test usable against an arbitrary position, not only trigger enter/exit events.
- [ ] The inside test considers server-role colliders only, and nothing is counted twice in a host process.
- [ ] An item spawned already inside the volume is detected without needing to cross the boundary.
- [ ] The zone boundary is visually unambiguous from both inside and outside.
- [ ] Every intern spawns inside the zone at round start, after the load barrier clears, with no falling through geometry.
- [ ] Spawn selection is scoped to the current location's zone and never returns a point from another loaded scene.
- [ ] The spawn overlap buffer is sized to the configured crew size, not to 32.
- [ ] A generated interior always places exactly one extraction zone, reachable from every room.
- [ ] The departure control is server-validated, cannot be triggered from outside the zone, and cannot be triggered by accident.
- [ ] Beginning departure is announced immediately and clearly to every player, including those deep inside the building.
- [ ] The zone is scannable at long range and through geometry.
- [ ] Depositing a body registers recovery using the same inside test as loot.
- [ ] The monster-entry rule is implemented and documented in this file.
- [ ] The zone is fully destroyed on unload, and no banking state survives into the next round.
- [ ] Two consecutive rounds in different locations each place their own zone with no cross-contamination.
