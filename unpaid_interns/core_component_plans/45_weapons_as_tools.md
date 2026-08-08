# 45 — Weapons as Tools

**Source:** [`core_components.md`](../core_components.md) §5 — Items, Loot & Inventory
**Status:** ⚠️ A complete predicted weapon stack exists and is wired the wrong way round
**Depends on:** [Item Definition](37_item_definition_data_model.md), [Inventory](40_inventory_item_bar.md), [Tool & Equipment Items](44_tool_and_equipment_items.md)
**Blocks:** the starting kit reading as horror rather than shooter, monster threat targeting, store weapon pricing

## Summary

Taking the shooter apart without throwing it away.

The project inherited a working, predicted, server-reconciled weapon stack — `WeaponData` ScriptableObjects, a `WeaponRegistry`, ammo and reload and cooldown state on `PredictedPlayerGhost`, projectiles with `ProjectileReconciliationSystem`, muzzle flash and hit VFX, firing and reload SFX, and 1P/3P animation. That is a substantial amount of correct netcode that would be expensive to rebuild and is worth keeping.

What is wrong is not the stack, it is the **position weapons occupy**. `core_components.md` §5 says weapons should become *rare, expensive, mostly-defensive, and absent from the starting kit*, and §16 leaves "do weapons survive as a pillar?" as an open question. Making a weapon **one category of item** rather than a parallel system is the move that keeps both answers reachable: the stack survives, the shooter framing does not, and the decision can be revisited by changing store prices and loot weights rather than by rewriting code.

There is a specific obstacle, and it is larger than the component summary suggests.

## The Real Problem: the weapon is baked into the player prefab

`ServerGameSystem.SpawnPlayerCharacter` does this:

```
var playerEntityPrefab = characterIndex == 0
    ? playerEntityPrefabs.PlayerRifleEntityPrefab
    : playerEntityPrefabs.PlayerShotgunEntityPrefab;
var weaponId = characterIndex == 0 ? (uint)0 : 1;
```

The weapon is not equipment the player is holding — it is **which player prefab they are**. `PredictedPlayerGhost.EquippedWeaponID` and `CurrentAmmo` are then initialised from the registry to match. There is one entity prefab per weapon, and the character index selected in the main menu picks both.

That means "pick up a weapon" is currently impossible without decoupling the two, and the decoupling is the bulk of this component's work. Everything else here is deletion and data.

## How to Build

**Separate the weapon from the character**

- Collapse `PlayerRifleEntityPrefab` and `PlayerShotgunEntityPrefab` into **one** player entity prefab with no weapon attached. `PlayerEntityPrefabsAuthoring` and `SpawnPlayerCharacter` both change; the character index stops selecting a weapon and becomes what it should have been, a cosmetic appearance choice.
- The held weapon's visual is attached at runtime to the 1P and 3P rigs from the item definition's prefab, the same way any other held item is shown ([`40_inventory_item_bar.md`](40_inventory_item_bar.md)). One attach point, one code path, whether the held thing is a shotgun or a brass bell.
- Keep `EquippedWeaponID` on `PredictedPlayerGhost`, but **derive it from the selected inventory slot** rather than setting it at spawn. When the selected slot holds a weapon-category item, the weapon id is that item's; otherwise it is invalid and the weapon systems idle.
- Deriving rather than storing is what prevents the two from disagreeing — the failure mode otherwise is a player holding a lamp who can still fire.

**Move ammo onto the item, not the player**

- `CurrentAmmo` on `PredictedPlayerGhost` is per-player, which is correct for a game where the player *is* the weapon and wrong once weapons are objects. A shotgun dropped with two shells left must still have two shells when someone else picks it up.
- Ammo is per-instance state on the item ghost, exactly like tool charge in [`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md) — same mechanism, same persistence rule, same acceptance criteria.
- Keep a **predicted mirror** of the held weapon's ammo on `PredictedPlayerGhost` so firing stays client-predicted and the HUD stays responsive; the item ghost is authoritative and reconciles it. This is the same shape as carry weight: a value that lives on an item but must be predicted while held.
- `ReloadTimer`, `WeaponCooldown`, `LastShotTick` and `LastReloadTick` stay on the player. They describe the *act* of firing, not the object.

**Reuse the activation path rather than adding one**

- [`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md) routes "use held item" through the existing `Shoot` action. A weapon is then a tool whose activation behaviour fires — no branch in the input layer, no second verb, and `WeaponData` becomes one activation behaviour among several.
- The existing tick-stamped idempotency (`LastShotTick` compared against a cached tick in `HandleAnimationEvents`) is already the correct pattern for a replayed input stream and needs no change.
- Reload keeps its own bit; it is weapon-specific and there is no general "reload a lamp".

**Retire the deathmatch semantics that ride along**

- `Projectile.cs` calls `LeaderboardManager.AddKill` on a lethal hit. [`13_health_and_injury.md`](13_health_and_injury.md) already requires removing it while consolidating the damage path, and [`18_pvp_collision_and_friendly_fire.md`](18_pvp_collision_and_friendly_fire.md) requires the friendly-fire multiplier to replace the current "damage anyone who is not the shooter" behaviour. Both land here in practice, because this is when weapons stop being the default.
- `WeaponRegistry` keeps working but should be **subsumed by the item registry** rather than maintained in parallel — a weapon is an item, its `WeaponData` is the activation payload, and two registries with two id spaces is exactly the drift [`37_item_definition_data_model.md`](37_item_definition_data_model.md) is trying to prevent. If they are kept separate for expedience, the weapon id must be resolved *through* the item id and never used as a primary key.
- The HUD's ammo bar and reticle become conditional on holding a weapon, as §9 requires.

**Make the design intent real in the data**

- Weapons are **rare in loot tables and expensive in the store**. That is the whole de-emphasis — it is two numbers, and it is reversible if playtests say the game is better armed.
- Give them weight that competes with loot. A shotgun occupying a slot and slowing you down is the cost that makes carrying one a decision rather than a default.
- Ammunition should be scarce and separately acquired, so a weapon is a limited resource rather than a permanent capability. Infinite ammo makes any weapon a pillar regardless of its price.
- Melee is the better fit for "defensive tool" than firearms and reuses less of the projectile stack — worth authoring one melee weapon early precisely to prove the item system does not assume projectiles.

**Feed monster targeting**

- Threat targeting (§6, [`56_threat_interest_targeting.md`](56_threat_interest_targeting.md)) reads whether a player is holding a weapon, and the reference design makes this explicit — holding a weapon changes how entities evaluate you. That check must be *the item's category*, which is why weapon is a category in [`37_item_definition_data_model.md`](37_item_definition_data_model.md) rather than a flag hidden in `WeaponData`.
- This creates the good decision the de-emphasis is aiming for: carrying a weapon makes you safer against some things and a more attractive target to others.

## Acceptance Criteria

- [ ] There is exactly one player entity prefab, with no weapon baked into it.
- [ ] Character index selects appearance only and never a weapon or an ammo count.
- [ ] Players spawn holding nothing; `SpawnPlayerCharacter` sets no `EquippedWeaponID`.
- [ ] `EquippedWeaponID` is derived from the selected inventory slot and is invalid when the held item is not a weapon.
- [ ] A player holding a non-weapon item cannot fire, reload, or display a reticle.
- [ ] A weapon can be picked up from the world, held, dropped, and picked up by another player.
- [ ] Ammo is per-instance item state; a dropped weapon retains its remaining ammo across owners and across the round.
- [ ] Firing remains client-predicted with no added latency, and the predicted ammo mirror reconciles against the item ghost.
- [ ] Weapon activation runs through the same held-item use path as every other tool.
- [ ] One shot per press survives prediction replay, as it does today.
- [ ] No kill is recorded to any scoring system from the damage path.
- [ ] Weapon damage flows through the single server-side damage entry point and respects the friendly-fire multiplier.
- [ ] Weapon ids resolve through item ids; there is no second primary key space.
- [ ] Ammo and reticle HUD elements appear only while a weapon is held.
- [ ] Weapons carry weight that competes with loot for slots.
- [ ] Ammunition is a separately acquired, finite resource.
- [ ] At least one melee weapon exists and works without the projectile stack.
- [ ] Monster threat targeting identifies an armed player by item category.
- [ ] The existing projectile reconciliation still passes its own tests after the decoupling, verified under simulated latency.
