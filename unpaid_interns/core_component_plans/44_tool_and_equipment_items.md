# 44 — Tool & Equipment Items

**Source:** [`core_components.md`](../core_components.md) §5 — Items, Loot & Inventory
**Status:** ❌ Not started
**Depends on:** Item Definition, Item Ghost, Inventory, Interaction System, Store
**Blocks:** the store having anything to sell, earning above quota having a purpose

## Summary

The things the crew buys with the money they did not have to spend on quota.

This is the sink that makes the economy a loop rather than a ratchet. Without gear to buy, exceeding quota is a number that goes up and nothing else; §8 says so directly — *"without a spend, earning above quota has no purpose."* Equipment is also the only lever the crew has for **changing how a round plays** rather than just doing the same round better: a flashlight changes the blackout, a radio changes splitting up, a ladder changes the map's topology.

The design constraint that shapes everything here is stated in `core_components.md` §5: weapons are to be **repurposed and de-emphasized** — rare, expensive, mostly defensive, and absent from the starting kit, where they sit today (`ServerGameSystem.SpawnPlayerCharacter` spawns players holding a rifle or shotgun by character index). A full predicted weapon and projectile stack already exists and works. The right move is to make a weapon *one kind of equipment* rather than a parallel system, so the existing stack is reused rather than either preserved as a pillar or thrown away.

## How to Build

**One item type, one activation path**

- Equipment is `ItemData` with a category and an activation behaviour ([`37_item_definition_data_model.md`](37_item_definition_data_model.md)), not a second hierarchy. Everything in §5 — inventory slots, weight, carry, drop, interaction authority, banking rules — applies unchanged, and that uniformity is the whole point.
- **Using a held item maps to the existing `Shoot` action, not to `Interact`.** [`41_interaction_system.md`](41_interaction_system.md) draws this line: interact acts on the world, use acts on what is in your hand. It also means a weapon is exactly a tool whose activation fires a projectile, and `WeaponData`'s existing fields become one activation behaviour among several.
- Activation is subject to the same replay hazard as every other side-effecting verb: prediction replays buffered ticks, so a use must be **tick-stamped and idempotent**, following the `LastShotTick` pattern the weapon code already uses ([`09_sprint.md`](09_sprint.md)).
- Consumables (a key, a medical item) are consumed **on the server**, once, and the item ghost destroyed. A client-predicted consume that the server rejects must restore the item cleanly.

**Ship the set that changes rounds, not the set that adds numbers**

Six pieces, each one owned by a component that already needs it:

- **Flashlight** — the answer to [`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md). Battery-limited, and it makes the holder *more* visible to sight-based monsters. That trade is what stops it from being a strict upgrade, and it must be real.
- **Radio** — long-range comms, the gear half of [`21_proximity_voice_comms.md`](21_proximity_voice_comms.md). Losing the holder loses the channel, and a squawking radio is a noise source.
- **Ladder** — a deployable climb volume ([`17_climbing_and_verticality.md`](17_climbing_and_verticality.md)), spawned as a ghost that survives its owner's death and is cleaned up at round end.
- **Key / lockpick** — the counter to the Door System's locks (§7). Consumable, which makes "which door is worth it" a decision.
- **Medical item** — the only way to heal above the critical-injury threshold mid-round ([`13_health_and_injury.md`](13_health_and_injury.md)). Without it, injury has no counterplay and the plan's "persists as a decision" framing does not hold.
- **Defensive weapon** — rare, expensive, and reusing the existing predicted weapon stack.

Each of those makes an existing mechanic playable. Resist adding gear that only improves a number; a +10% stamina belt is a balance change wearing an item's clothes.

**Get the persistence rules right**

- Equipment is bought in the hub and **delivered at the start of the next round** (§8), not spawned by the loot spawner. A store item appearing in the loot pool is a sell-back exploit ([`39_loot_spawner.md`](39_loot_spawner.md)).
- Equipment **has no meaningful sell value**. Buying a flashlight for 60 credits and banking it must never return 60 credits. This is a data rule on the definition, enforced by [`43_loot_banking_deposit.md`](43_loot_banking_deposit.md)'s distinction between `Banked` scrap and `Retained` gear.
- Gear left in the extraction zone at round end comes home and is available next round. Gear left in the field is lost with the rest of the round's unbanked contents.
- Gear stored in the hub persists across rounds and through a save ([`06_session_persistence.md`](06_session_persistence.md)); the Storage / Hub Inventory component in §5 owns where it lives between deployments.
- Decide what happens to gear on total crew loss. Recommended: **hub storage survives, everything taken into the field is lost** — it keeps the disaster meaningful without erasing a run's accumulated capability in one bad round.
- Read that rule precisely: it covers gear *carried by interns*. Gear left `Retained` in the extraction zone has already come home and survives, exactly as banked scrap already sold does ([`105_departure_and_extraction_resolution.md`](105_departure_and_extraction_resolution.md) holds the full forfeiture table, and [`02_day_cycle_controller.md`](02_day_cycle_controller.md) requires all three end conditions to settle identically). The looser reading — that a wipe voids everything in the zone — would mean a crew's last act of banking counted for nothing, which is not what banking means.

**Make charge the limiter, not cooldowns**

- Battery or charge is a better constraint than a timer because it spans the whole round: a flashlight with 60% charge is a resource the player is managing, while a flashlight on a cooldown is a UI element.
- Charge is per-instance state on the item ghost ([`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md)), server-authoritative, and it must survive being dropped and picked back up — including by a different player.
- Recharging belongs in the hub, which gives the between-rounds state one more thing to do and makes a fully-charged deployment feel like preparation.
- Surface remaining charge on the item bar. A player who cannot see it will not manage it.

**Fix the starting kit while here**

- `ServerGameSystem.SpawnPlayerCharacter` currently equips a weapon on spawn by character index. Interns deploy **empty-handed**. This is a small edit with a large effect on tone and on how the first thirty seconds of a round read.
- The change interacts with `PredictedPlayerGhost.EquippedWeaponID` and the HUD's ammo and reticle display, which `InGameHUD.cs` renders unconditionally today. Those become conditional on holding a weapon rather than always present — §9 already calls for ammo and reticle to become secondary.
- Keep the weapon stack intact behind the item system rather than deleting it. It works, it is predicted correctly, and it is the answer to the "do weapons survive as a pillar" open question in §16 either way: as equipment, both answers remain reachable.

**Serve the systems that read equipment**

- **Monster threat targeting** (§6) reads whether a player is holding a weapon. That is a category check on the held item's definition, and it is why weapon must be a category rather than a flag scattered elsewhere.
- **Noise** — powered tools, radios, and activation sounds all publish noise events (§6), not just audio. A tool that is loud is a real cost and a real decision.
- **Two-handed gear** — a ladder is a plausible two-handed carry and inherits [`42_two_handed_item_rule.md`](42_two_handed_item_rule.md) entirely, including being unable to use itself while carried.
- **Carry weight** — equipment weighs, and competes for slots with loot. That competition is the point: every tool taken is a slot of scrap not carried home.

## Acceptance Criteria

- [ ] Equipment is authored as `ItemData` with a category and an activation behaviour, sharing the inventory, weight, carry, drop, and authority paths with scrap.
- [ ] Using a held item maps to the `Shoot` action and is distinct from world interaction.
- [ ] Activation is tick-stamped and idempotent; a replayed tick never activates an item twice.
- [ ] Consumables are consumed exactly once on the server, and a rejected client prediction restores the item.
- [ ] Players spawn empty-handed; `SpawnPlayerCharacter` equips no weapon.
- [ ] Ammo and reticle HUD elements appear only when a weapon is held.
- [ ] The existing predicted weapon and projectile stack works when a weapon is acquired as an item.
- [ ] Equipment is delivered at round start from store purchases and never appears in the loot pool.
- [ ] Banking equipment pays nothing; buying and banking an item is never a net gain.
- [ ] Equipment left in the extraction zone is retained for the next round; equipment left in the field is lost.
- [ ] Hub-stored gear survives a round, a save and reload, and the documented total-crew-loss rule.
- [ ] Charge is per-instance server-authoritative state that survives being dropped and picked up by another player.
- [ ] Remaining charge is visible on the item bar.
- [ ] Recharging is available in the hub and nowhere else.
- [ ] A flashlight measurably increases its holder's visibility to sight-based monsters.
- [ ] A deployed ladder replicates to all clients, survives its owner's death, and is cleaned up at round end.
- [ ] A key or lockpick opens exactly one locked door and is consumed.
- [ ] A medical item is the only mid-round way to heal above the critical-injury threshold.
- [ ] Monster threat targeting correctly identifies a player holding a weapon by item category.
- [ ] Powered and activated tools raise noise events, not merely audio playback.
- [ ] Every piece of equipment changes how a round is played, not only a number; a purely numeric upgrade is rejected in review.
