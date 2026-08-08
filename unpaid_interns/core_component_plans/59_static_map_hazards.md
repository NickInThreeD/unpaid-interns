# 59 — Static Map Hazards

**Source:** [`core_components.md`](../core_components.md) §7 — Hazards & Environment Interaction
**Status:** ❌ Not started
**Depends on:** [Procedural Interior Generator](28_procedural_interior_generator.md), [Health & Injury](13_health_and_injury.md), [Item Ghost](38_item_ghost_networked_item_state.md)
**Blocks:** the map itself having a personality, careless movement having a cost

## Summary

Things that hurt you and do not chase you.

`core_components.md` puts the case in one line: hazards *"punish careless movement without requiring AI and give the map itself a personality."* Both halves are the point. A mine, a turret, or a crushing trap costs a fraction of what a monster costs — no perception, no pathfinding, no targeting, no replication of a moving transform — and it does something no monster does, which is make a *room* dangerous rather than a moment. A crew that slows down and looks where they are walking is a crew the level design is controlling directly.

They also do specific work the rest of the design needs. They punish sprinting, which gives [`11_stamina.md`](11_stamina.md)'s speed/exhaustion trade a second axis. They punish carrying a two-handed item that blocks your view ([`42_two_handed_item_rule.md`](42_two_handed_item_rule.md)). And they are the natural target of a thrown object, which is what turns [`47_physics_props_and_throwing.md`](47_physics_props_and_throwing.md) from a distraction tool into a way of clearing a corridor from safety.

## How to Build

**Author them, place them procedurally, replicate only the state**

- A hazard is a prefab with a trigger volume, a state machine, and a `SoundDef` set. The generator places them at authored hazard points in room modules, drawn from the interior seed stream — the same pattern as loot points and vents ([`28_procedural_interior_generator.md`](28_procedural_interior_generator.md), [`52_spawn_points_and_vents.md`](52_spawn_points_and_vents.md)).
- Placement density comes from `LocationData` ([`26_location_catalogue.md`](26_location_catalogue.md)), so a destination can be characterised by its hazards rather than only by its monsters. A location known for mined corridors is a location with an identity.
- **Replicate the state, never the geometry.** A hazard's position comes from the seed; only its armed/triggered/disabled state crosses the wire, as a small `[GhostField]` — the same discipline as the power grid's zone flags ([`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md)).
- Hazards are server-authoritative. The trigger test runs on the server against server-role colliders, using the layer discipline established throughout ([`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md)) — a host that evaluates the client copy will trigger twice.

**Make every hazard perceivable before it fires**

This is the rule that separates a hazard from a cheap shot, and it is worth stating as an absolute: **a hazard must be detectable before it hurts you.** Not necessarily easy to notice — a mine that requires looking down is fine — but never invisible.

- Each hazard needs a resting-state cue: a sound, a light, a mark on the floor. The reference design's breaker box hums for exactly this reason.
- Each needs an **arming or wind-up cue** with a window to react, the same requirement placed on monster attacks ([`57_attack_and_damage_application.md`](57_attack_and_damage_application.md)) and vent emergences.
- Both cues need a non-audio equivalent (§9). A hazard whose only warning is a click is an accessibility failure, and hazards are exactly where that failure kills people.
- Scannable at loot range is a reasonable and generous default ([`16_player_scanner_ping_tool.md`](16_player_scanner_ping_tool.md)) — the scanner already highlights hazards as a category in that plan.

**Ship three, not a catalogue**

Three cover the useful space, and each teaches something different:

- **The trap you step on** (mine, pressure plate). Punishes speed and inattention. Should be avoidable once seen and lethal or near-lethal when not — a mine that does 20 damage is scenery.
- **The thing that watches a corridor** (turret). Punishes crossing an open space. Its counterplay is timing, cover, or disabling it, which makes it a small puzzle rather than a toll. Give it a clear arming wind-up and a clear field of fire.
- **The thing that closes** (crusher, shutter, elevator). Punishes standing in the wrong place, and doubles as a route hazard the crew has to time. This is the one that pairs best with carrying a two-handed item.

Each should be **disarmable or avoidable by a stated method**, so a crew can learn a plan. A hazard with no counterplay is a random tax, and a randomly-taxed room is one players avoid entirely — which removes the loot in it from the game.

**Route damage through the one entry point**

- `ApplyDamage(target, amount, source)` with the `Hazard` source classification ([`13_health_and_injury.md`](13_health_and_injury.md)). No hazard writes health directly, and hazard damage is never scaled by the friendly-fire multiplier ([`18_pvp_collision_and_friendly_fire.md`](18_pvp_collision_and_friendly_fire.md)).
- **Hazards must damage monsters too.** A turret that only shoots interns is a rule the player cannot exploit; one that also kills the thing chasing them is the best emergent moment the component can produce, and it costs a layer mask.
- A hazard death is an ordinary death — items drop, a body spawns, the roster updates ([`14_death_and_body_system.md`](14_death_and_body_system.md)). Record the cause; "killed by a landmine while carrying the payday" is exactly the story the end-of-round summary should be able to tell.
- Under latency, prefer generosity: a player who cleared the trigger on their own screen should not be hit. The wind-up window is the mitigation, as with monster attacks.

**Let objects trigger them**

- A thrown item entering a trigger volume should fire the hazard ([`47_physics_props_and_throwing.md`](47_physics_props_and_throwing.md) requires this from the other side). It is the single best use of the throwing verb and it costs almost nothing.
- Decide whether a destroyed item is consumed — recommended **yes**, so clearing a corridor costs a piece of scrap and is therefore a decision. That destruction must clear claims and adjust value correctly ([`43_loot_banking_deposit.md`](43_loot_banking_deposit.md)), and it must be logged, since it is loot leaving the economy.
- A monster stepping on a mine should trigger it, which follows from the same volume test and needs no special case.

**Keep the state per-round and clean**

- A triggered mine stays triggered for the round. Do not re-arm; a re-arming hazard makes a cleared route unclearable and quietly deletes the reward for clearing it.
- Destroy all hazard state at round teardown, with the entity-count-returns-to-baseline check the other per-round systems require.
- Nothing in the hub, and nothing inside the extraction zone. The hub is asserted safe ([`04_hub_between_rounds_state.md`](04_hub_between_rounds_state.md)) and a hazard at the drop-off punishes the one action the game wants players to take.
- Hazards near the main entrance should be rare or absent — the first thirty seconds should not be where a crew loses someone, for the same reason vents are kept away from the entrance.

**Leave room for remote control**

- §7's Hazard Control / Remote Disable component gives a hub-bound player something to do by temporarily disabling hazards for the field team. Build the hazard's state machine with an externally-settable `Disabled` state from the start, even if nothing sets it yet — retrofitting a remote-control path into three hazard types later is three times the work.

## Acceptance Criteria

- [ ] Hazards are authored prefabs placed at authored hazard points, drawn from the interior seed stream, and reproduce exactly for a given seed.
- [ ] Hazard density comes from `LocationData` and measurably differentiates destinations.
- [ ] Only hazard state is replicated; positions derive from the seed and no hazard geometry crosses the wire.
- [ ] Trigger evaluation runs on the server against server-role colliders; a host triggers each hazard once, matching a dedicated server.
- [ ] Every hazard has a resting-state cue and an arming cue, each with a non-audio equivalent.
- [ ] The arming window is long enough to react to and is tunable from data.
- [ ] Hazards are scannable at loot range.
- [ ] Three hazard types exist, each with a distinct counterplay, and each is avoidable or disarmable by a stated method.
- [ ] A triggered trap is lethal or near-lethal, not a minor tax.
- [ ] All hazard damage flows through the single damage entry point with the `Hazard` classification.
- [ ] Hazard damage is never scaled by the friendly-fire multiplier.
- [ ] Hazards damage monsters as well as players.
- [ ] A hazard death drops items, spawns a body, and records the cause, which appears in the end-of-round summary.
- [ ] A thrown item can trigger a hazard from a distance.
- [ ] An item destroyed by a hazard has its claim cleared, its value removed correctly, and the loss logged.
- [ ] A triggered hazard does not re-arm during the round.
- [ ] No hazards exist in the hub or inside the extraction zone, and none spawn within the configured distance of the main entrance.
- [ ] All hazard state is destroyed at round end, returning entity counts to baseline across five rounds.
- [ ] Every hazard supports an externally-settable disabled state, ready for remote control.
- [ ] Under simulated latency, a player who visibly cleared a trigger on their own screen is not hit.
