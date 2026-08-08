# 38 — Item Ghost / Networked Item State

**Source:** [`core_components.md`](../core_components.md) §5 — Items, Loot & Inventory
**Status:** ❌ Not started · **[MVP]**
**Depends on:** Item Definition / Data Model
**Blocks:** Loot Spawner, Inventory, Interaction System, Networked Interaction Authority, Loot Banking, Scanner, Death & Body System

## Summary

Every object in the world that can be picked up, as replicated state that all four players agree on.

This is the component the rest of §5 is built on, and it is where the project's biggest bandwidth risk lives. §13 states it plainly: ghost snapshot size scales with replicated entity count, Relay bills on bandwidth, and *"every item, monster, and door on the map is a potential ghost."* A loot-dense procedural interior is the exact case that turns that warning into a bill. Getting the item ghost small and mostly-silent is not an optimisation pass to do later — it is the design of the component.

The infrastructure is ready. `GhostGameObject` provides the MonoBehaviour-to-entity bridge, `GhostSpawner.SpawnGhostPrefab(GhostReference, position, rotation, netGuid, uniformScale, postSpawnSpecialisation)` already spawns Addressable prefabs server-side with a network GUID, and `GhostGameObject` carries the pieces an item needs: a serialized `RequireTransformSync` flag, a `Dormant` property, an `UpdateGroup`, and — the useful surprise — `GhostGameObjectGuid.ParentGuid` and `ParentToMovingBase` as `[GhostField]`s, with `UpdateParentReference` resolving the parent by GUID once it links. That is a ready-made mechanism for "this item is in that player's hands".

## How to Build

**Define the replicated state, and keep it small**

- Per item: `ItemId` (uint, resolved against the registry from [`37_item_definition_data_model.md`](37_item_definition_data_model.md)), `RolledValue`, `HeldByNetworkId`, `ClaimTick`, and a `Banked` flag.
- `HeldByNetworkId` and `ClaimTick` are required verbatim by [`20_networked_interaction_authority.md`](20_networked_interaction_authority.md) — that component defines their semantics and this one declares them. Do not invent a second ownership representation.
- **Never replicate the `ItemData` reference, only the id.** Every client resolves the definition locally, which is why registry parity between builds is a hard requirement.
- Nothing else. Weight, two-handedness, noise, and prefab all come from the definition. A field that can be derived on the client must not be on the wire.

**Cut the transform cost**

- `RequireTransformSync` defaults to `false` on `GhostGameObject` and must stay false for an item at rest. **An item lying on the floor does not move**, and paying continuous transform replication for a hundred motionless objects is the single largest avoidable cost in the project.
- Spawn position is set once by the server at spawn and again when the item is dropped. Both are discrete events; treat them as such rather than as an ongoing stream.
- While **held**, parent the item ghost to the holder using `ParentGuid` and `ParentToMovingBase` rather than syncing its transform. The item then rides the player's already-replicated position for free, and `UpdateParentReference` resolves it on each client when the ghost links.
- Enable transform sync **only** while an item is genuinely in free physics motion — thrown, falling, tumbling — and disable it the moment it comes to rest. A short settle timer on the server, then a final authoritative position and sync off.
- Use `Dormant` and ghost relevancy so items far from every player cost nothing. Set a low `GhostImportance` — an item's exact state matters far less per snapshot than a player's or a monster's, and this is the state that should yield first when bandwidth is tight.

**Solve the doubled-collider problem — do this before anything raycasts**

This is the one that will silently break several downstream components if it is missed.

Gameplay physics is built-in PhysX, and in a host process the server world and the client world **each instantiate their own copy of every ghost GameObject into the same physics scene**. The project already handles this for players: `PlayerGhost` assigns `gameObject.layer` to `LayerIndex.ServerPlayer` or `LayerIndex.ClientPlayer` by role (line 150), `Projectile` masks specifically on `ServerPlayer`, and `PlayerPredictionSystem` masks with `~ClientPlayer` and `~(ClientPlayer | ServerPlayer)` for its client-side queries. There is even a commented-out `ServerMovingBase` layer assignment left in `GhostSpawner.cs` around line 164 from the same pattern upstream.

- Add `ServerItem` and `ClientItem` to `LayerIndex.cs` — indices 9 and above are free — and assign the layer by role when the item ghost links, mirroring `PlayerGhost`.
- Every consumer then masks explicitly: the interaction raycast on the client hits `ClientItem` only ([`41_interaction_system.md`](41_interaction_system.md)); the server's validation and the extraction zone's inside test consider `ServerItem` only ([`31_entry_point_extraction_zone.md`](31_entry_point_extraction_zone.md), [`43_loot_banking_deposit.md`](43_loot_banking_deposit.md)).
- Without this, a host banks every item twice, the interaction raycast picks whichever copy the physics query happened to return first, and none of it reproduces on a dedicated server — which is the worst possible bug profile.
- Extend the layer set to bodies, monsters, and any other spawned interactable at the same time, rather than adding a pair per component later. [`49_monster_ghost_and_replication.md`](49_monster_ghost_and_replication.md), [`14_death_and_body_system.md`](14_death_and_body_system.md), [`31_entry_point_extraction_zone.md`](31_entry_point_extraction_zone.md), [`41_interaction_system.md`](41_interaction_system.md), [`57_attack_and_damage_application.md`](57_attack_and_damage_application.md), and [`59_static_map_hazards.md`](59_static_map_hazards.md) all depend on this split — **define the whole set here, once.** `LayerIndex` currently uses indices 0–8, so 9 upward are free.

**Roll the value on the server, and do not give it away**

- The server rolls each item's value at spawn from the loot stream of the round seed ([`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md)) and writes it to `RolledValue`.
- A client that can read every `RolledValue` on the map has a perfect loot radar, which defeats both exploration and the scanner. [`16_player_scanner_ping_tool.md`](16_player_scanner_ping_tool.md) requires that scanned value be server-authoritative and bounded by range — **use ghost relevancy to replicate `RolledValue` only to clients within genuine scan range**, which is the cheaper of the two options that plan offers and aligns with the bandwidth work.
- Value is visible without restriction once the item is held or banked. Those are the two moments the player has earned the information.

**Get the lifecycle right**

- **Spawn** — server-only, through `GhostSpawner.SpawnGhostPrefab`, using the `postSpawnSpecialisation` callback to write the item id and rolled value in the same command buffer. Do not spawn and then patch on a later frame; a client can link the ghost in between and see item id zero.
- **Pick up** — clear the world collider or move it to a non-interactable layer, set `HeldByNetworkId`, parent to the holder. Predicted on the acquiring client, authoritative on the server ([`20_networked_interaction_authority.md`](20_networked_interaction_authority.md)).
- **Drop** — server-validated position, clear the holder and parent, re-enable the collider, enable transform sync until it settles.
- **Bank** — set `Banked`, clear the holder. Exactly-once ([`43_loot_banking_deposit.md`](43_loot_banking_deposit.md)).
- **Round end** — destroy every item ghost, banked and unbanked alike. Verify entity counts return to baseline across five consecutive rounds, alongside the check [`05_location_load_unload_flow.md`](05_location_load_unload_flow.md) already requires. A leaked item ghost is invisible and cumulative.
- **Holder disappears** — on death or disconnect the server clears the claim and drops the item into the world ([`14_death_and_body_system.md`](14_death_and_body_system.md), [`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md)). An item claimed by a `NetworkId` that no longer exists is permanently unpickable and looks identical to a lost item.

**Pool, do not churn**

- A loot-dense location spawns and destroys hundreds of objects per round. `SoundGameObjectPool` establishes the pooling pattern in this project; apply the same approach to item prefab instances so a deploy does not allocate its way through a frame budget.
- Watch for pooled instances retaining state between rounds — a pooled item that keeps last round's `Banked` flag is a credit duplication bug. **Reset on release, not on acquire**: an instance that is cleaned only when it is next handed out is a dirty instance for however long it sits in the pool, and it will be inspected in that state by exactly the leak check that is supposed to catch it ([`106_round_teardown_and_state_reset.md`](106_round_teardown_and_state_reset.md)).

**Make it debuggable**

- The debug overlay required by [`20_networked_interaction_authority.md`](20_networked_interaction_authority.md) — claim state on nearby items — should also show item id, rolled value, banked flag, and ghost role. Item duplication and item loss are both nightmares from a verbal report and trivial with this overlay.
- Log every spawn, claim, drop, bank, and destroy server-side with the round seed. Combined with the seed, this makes a lost item reproducible.

## Acceptance Criteria

- [ ] An item ghost replicates item id, rolled value, holder, claim tick, and banked flag, and nothing else.
- [ ] Item definitions are resolved client-side from the id; no `ItemData` reference crosses the wire.
- [ ] Transform sync is off for items at rest and on only while an item is in free motion, switching off within a second of settling.
- [ ] A held item is parented to its holder via `ParentGuid` and costs no transform bandwidth.
- [ ] Items far from every player are dormant or irrelevant and contribute nothing to snapshot size.
- [ ] `ServerItem` and `ClientItem` layers exist and are assigned by role when an item ghost links.
- [ ] In a host process, a physics query for items returns exactly one collider per item for the querying role.
- [ ] Banking on a host counts each item once, and the behaviour is identical on a dedicated server.
- [ ] Rolled values are generated on the server from the loot seed stream and reproduce for a given seed.
- [ ] A client cannot read the rolled value of an item outside its scan range, verified by inspecting a client's replicated state.
- [ ] Item id and rolled value are set in the same command buffer as the spawn; no client ever observes a partially initialised item.
- [ ] Pickup, drop, bank, and death-drop transitions all leave the collider, layer, parent, and holder fields mutually consistent.
- [ ] An item held by a player who dies or disconnects is released and recoverable, never permanently claimed.
- [ ] All item ghosts are destroyed at round end; five consecutive rounds return entity and memory counts to baseline.
- [ ] Pooled item instances carry no state between rounds; a reused instance never retains a stale banked flag or rolled value.
- [ ] A location with the maximum authored item count stays within the per-snapshot bandwidth budget with four connected clients.
- [ ] A debug overlay shows id, value, holder, claim tick, banked flag, and ghost role for nearby items.
- [ ] Every item lifecycle transition is logged server-side with the round seed.
