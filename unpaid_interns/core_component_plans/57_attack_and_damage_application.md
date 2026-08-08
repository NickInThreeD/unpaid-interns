# 57 — Attack & Damage Application

**Source:** [`core_components.md`](../core_components.md) §6 — Monsters & AI
**Status:** ⚠️ A player-damage path exists; monster→player and player→monster do not · **[MVP]**
**Depends on:** [Health & Injury](13_health_and_injury.md) (the single damage entry point), [Monster Ghost](49_monster_ghost_and_replication.md), [Chase & Pathfinding](55_chase_and_pathfinding.md)
**Blocks:** monsters being lethal, weapons being useful, the death system having a cause

## Summary

The moment contact is made.

Half of this exists. `PredictedPlayerGhost` carries `CurrentHealth`, `MaxHealth`, `LastDamageAmount`, and `LastHitTick`; `Projectile.cs` applies damage server-side; `DamageVisualsController` shows a first-person vignette; `FirstPersonController.HandleAnimationEvents` compares `LastHitTick` against a cached tick to fire a hit reaction exactly once. That is a working, correctly-shaped, server-authoritative damage path — for weapons hitting players.

What is missing is **monster→player** and **player→monster**, plus the hitboxes, telegraphs, and per-monster kill behaviour that make an attack readable rather than an instantaneous loss of health.

The prerequisite is not in this component. [`13_health_and_injury.md`](13_health_and_injury.md) requires consolidating the existing writes into a single `ApplyDamage(target, amount, source)` **before** adding sources, because `Projectile.cs` currently writes `CurrentHealth`, `ControllerState.IsHit`, `LastDamageAmount`, and `LastHitTick` directly in two separate branches with duplicated kill bookkeeping. Adding monster damage as a third such site is how the injury rules, the friendly-fire multiplier, and the death penalties end up applied inconsistently. **Do that consolidation first; this component is its second customer, not its cause.**

## How to Build

**Telegraph before you hit**

- Every monster attack has a wind-up: an animation, a sound, and a configured duration during which the player can still act. `core_components.md` names telegraphs explicitly, and the reason is the same as for spawn wind-ups ([`52_spawn_points_and_vents.md`](52_spawn_points_and_vents.md)) — an attack that cannot be anticipated is a random loss of health rather than a failure the player can learn from.
- Wind-up duration is per-monster data ([`48_monster_data_definitions.md`](48_monster_data_definitions.md)) and is one of the two numbers that decide whether a creature feels fair. The other is chase speed.
- The telegraph must be **perceivable without audio**, per §9's accessibility requirement — an attack cue is exactly the class of critical audio-only warning that plan forbids.
- Commit or cancel explicitly. A monster that begins a wind-up and then re-aims mid-swing removes the counterplay the wind-up exists to create; recommended is **commit to the position at wind-up start**, so dodging works.

**Resolve on the server, from server geometry**

- Attacks resolve on the server against **server-role colliders only**, using the layer discipline established across the project — `ServerPlayer` for players, `ServerMonster` for monsters ([`49_monster_ghost_and_replication.md`](49_monster_ghost_and_replication.md), [`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md)). In a host process both roles' colliders share one PhysX scene, and a hit test that catches the client copy will double-apply damage or miss entirely, reproducing on no dedicated server.
- Use built-in PhysX overlaps and sweeps, as the rest of the project does — `Physics.OverlapSphereNonAlloc` and `SphereCast`, non-allocating, with an explicit mask. This is the same API `Projectile.cs` and `ServerGameSystem` already use.
- Never trust a client-reported hit. The player's *weapon* attack is predicted for responsiveness and reconciled by `ProjectileReconciliationSystem`, which is the existing correct pattern; the monster's attack has no client to predict it and should not acquire one.

**Give monsters hitboxes worth aiming at**

- Player→monster damage needs the monster to have hit geometry beyond its navigation capsule. A single capsule makes every weapon a binary; distinguished hitboxes make aiming a skill.
- Keep it modest — a body and a weak point is enough. The reference's hitbox model ([`Assets/docs/detection-and-combat/hitbox.md`](../../Assets/docs/detection-and-combat/hitbox.md)) is worth reading before deciding how much fidelity to buy.
- Weak points are a strong lever for [`58_monster_variety_set.md`](58_monster_variety_set.md): a creature that must be hit somewhere specific is a puzzle, not a health bar.
- Hitboxes live on the **server instance** of the monster prefab. The client instance needs none.

**Route everything through the one entry point**

- `ApplyDamage(target, amount, source)` with the source enumeration [`13_health_and_injury.md`](13_health_and_injury.md) defines — `Monster` joins `Projectile`, `Fall`, `Drowning`, `Hazard`, and `OutOfBounds`.
- Monster damage must bypass the friendly-fire multiplier entirely; that multiplier applies to teammate-sourced damage only ([`18_pvp_collision_and_friendly_fire.md`](18_pvp_collision_and_friendly_fire.md)).
- Damage to monsters needs its own path with the same shape — one server-side `ApplyDamageToMonster` rather than direct health writes scattered through weapon and hazard code. The lesson from `Projectile.cs` applies identically in the other direction.
- A killing blow records the cause of death, which [`14_death_and_body_system.md`](14_death_and_body_system.md) wants to carry on the body ghost, and which the end-of-round summary and balance telemetry both read.

**Make the attack's consequence per-monster, not global**

- `core_components.md` asks for **per-monster kill behaviour**, and this is where the roster gets its personality. Options worth having: ordinary damage; instant death; grabbing and carrying the player; and the one [`14_death_and_body_system.md`](14_death_and_body_system.md) already anticipates — **consuming the body and the carried items entirely**, so there is nothing to recover.
- That last one has real economic weight. A creature that permanently deletes a haul is remembered and feared in a way a damage number never is. It must be authored as a deliberate rule, not emerge from a bug, and the crew must be able to tell which creature does it.
- Any behaviour that removes items must clear their claims and remove their value cleanly ([`20_networked_interaction_authority.md`](20_networked_interaction_authority.md), [`43_loot_banking_deposit.md`](43_loot_banking_deposit.md)); silently destroying ghost items is how the running banked total drifts.
- A grab that immobilises a player needs an escape or a rescue condition, or it is a death with extra waiting.

**Reuse the feedback that exists**

- `LastHitTick` and `LastDamageAmount` already drive the hit reaction and the damage vignette exactly once per hit. Monster damage should flow through them unchanged — no new one-shot mechanism, no RPC per swing.
- Add a distinct hit cue for monster damage versus weapon damage. [`15_fear_and_stress_feedback.md`](15_fear_and_stress_feedback.md) makes the same argument about keeping fear and damage visually distinct, and for the same reason: the player must know what is happening to them without looking.
- Damage to monsters needs its own feedback — a hit flash, a sound, a stagger. Weapons that do not visibly connect feel broken even when they are working, and this is doubly true when ammunition is scarce ([`45_weapons_as_tools.md`](45_weapons_as_tools.md)).

**Verify it under the conditions it will fail in**

- Latency: a player who dodged on their screen and was hit on the server is the classic complaint. The wind-up duration is the mitigation — a long enough telegraph makes the disagreement window irrelevant, which is a better fix than lag compensation for monster attacks.
- Simultaneity: two monsters hitting one player on the same tick, and one attack landing on the same tick a player dies. Both must resolve once, cleanly.
- Death mid-attack: a monster killed during its own wind-up must not land the hit.

## Acceptance Criteria

- [ ] All damage flows through the single server-side entry point; no path writes `CurrentHealth` or monster health directly.
- [ ] Damage carries a source classification, and monster damage is never scaled by the friendly-fire multiplier.
- [ ] Player→monster damage has its own single entry point with the same shape.
- [ ] Every monster attack has a configured wind-up with an animation, a sound, and a non-audio cue.
- [ ] Wind-up duration is per-monster data and tunable without a recompile.
- [ ] An attack commits to its target position at wind-up start; dodging during the wind-up works.
- [ ] Attacks resolve on the server against server-role colliders only; behaviour on a host matches a dedicated server.
- [ ] No client-reported hit is trusted for monster attacks.
- [ ] Monsters have distinguishable hit geometry beyond a navigation capsule, on the server instance only.
- [ ] At least one monster has a weak point that meaningfully changes how it is fought.
- [ ] Hit reactions and the damage vignette fire exactly once per hit, using the existing tick-stamp mechanism.
- [ ] Monster damage is visually and audibly distinguishable from weapon damage.
- [ ] Damage dealt to a monster produces immediate, legible feedback.
- [ ] A killing blow records a cause of death that reaches the body ghost and the end-of-round summary.
- [ ] Per-monster kill behaviours are implemented as authored data, including at least one that destroys carried items.
- [ ] Any behaviour that destroys items clears their claims and adjusts the banked total correctly.
- [ ] A grab or immobilise attack has a documented escape or rescue condition.
- [ ] Two monsters hitting one player on the same tick each apply once.
- [ ] An attack landing on the tick a player dies does not double-apply or corrupt the death sequence.
- [ ] A monster killed during its wind-up does not land the attack.
- [ ] Under simulated latency, a player who visibly cleared the telegraph is not hit.
