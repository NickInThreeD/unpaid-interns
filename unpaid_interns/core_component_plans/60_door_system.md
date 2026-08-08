# 60 — Door System

**Source:** [`core_components.md`](../core_components.md) §7 — Hazards & Environment Interaction
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Interaction System](41_interaction_system.md), [Networked Interaction Authority](20_networked_interaction_authority.md), [Runtime NavMesh Baking](30_runtime_navmesh_baking.md), [Procedural Interior Generator](28_procedural_interior_generator.md)
**Blocks:** chases being survivable, monsters having a counter, the interior having structure

## Summary

The one tool players have for buying time.

`core_components.md` puts it plainly: doors are the primary way a crew survives a chase, and **they must be networked state, not local animation.** That second half is the whole engineering problem. A door that animates locally is a door that is open on your screen and closed on the host's, which in a chase means the monster you thought you shut out is already in the room.

Doors also do quiet structural work. They break line of sight, which makes [`53_perception_system.md`](53_perception_system.md)'s sight checks meaningful. They attenuate sound, which gives [`54_noise_emission_system.md`](54_noise_emission_system.md)'s occlusion something to act on. And they gate navigation, which is what turns a chase from a foot race into a decision.

The reference implementation ([`Assets/docs/hazards/door.md`](../../Assets/docs/hazards/door.md)) supplies the numbers that matter: a **hold to open** (0.3 s, with a progress circle), an **immediate close**, and — the important one — **every creature opens doors at a different speed.** That per-monster open time is what makes closing a door a real tactic rather than a binary win.

## How to Build

**Make the state absolute and server-owned**

- Replicate a `DoorState` enum — `Closed`, `Opening`, `Open`, `Closing`, `Locked` — as a `[GhostField]` on the door ghost. Never replicate a toggle.
- [`20_networked_interaction_authority.md`](20_networked_interaction_authority.md) already states the rule and the reason: two players interacting on the same tick must produce **one open door, not a toggle that lands closed**. Absolute state converges; toggles race.
- Clients animate toward the replicated state. The animation is presentation; the state is truth. A client whose animation is mid-swing when a correcting snapshot arrives blends to the new state rather than snapping.
- Predict the local player's own interaction optimistically for responsiveness — the same pattern as pickup — and accept correction. A door that waits a round trip before moving feels broken at exactly the moment it matters most.

**Get the timings right, because they are the mechanic**

- **Hold to open, instant to close.** This asymmetry is deliberate and correct: opening is a considered act, closing is a panic act. A crew fleeing must be able to slam a door in one press.
- Show a hold progress indicator, which [`41_interaction_system.md`](41_interaction_system.md) already requires of held interactions, and cancel cleanly on release, damage, or moving out of range.
- Per-monster open time comes from monster data ([`48_monster_data_definitions.md`](48_monster_data_definitions.md)). A creature that takes four seconds to work a door is survivable; one that opens it instantly is a different threat entirely, and the crew should be able to learn which is which by sound.
- A monster working at a door should be **audible from the other side**. It is the single best tension beat this component can produce, and it costs one `SoundDef`.

**Gate navigation properly**

- A closed door must block monster pathing, or the whole component is decorative. [`30_runtime_navmesh_baking.md`](30_runtime_navmesh_baking.md) recommends **navigation links** enabled and disabled by door state rather than carving obstacles, because carving re-tessellates the tile on every door movement and a loot-dense interior has many doors.
- The link's traversal permission is per-monster, so "can this creature open doors at all" is data, not a hard-coded branch.
- Doors are ghosts, and [`28_procedural_interior_generator.md`](28_procedural_interior_generator.md) already warns that **the generator's door count sets a bandwidth floor**. Budget it: put doors at meaningful junctions, not in every opening. The reference's approach — reserve door positions during generation, then decide per position whether to place one, leaving a door-sized gap otherwise — is the right shape and keeps the count tunable.

**Make doors do their other three jobs**

- **Sight** — a closed door blocks the perception raycast. This follows automatically if the door has a collider on a layer the sight check tests, but verify it: a door that animates its mesh without moving its collider is a door monsters see straight through.
- **Sound** — a closed door attenuates noise events passing through it. [`54_noise_emission_system.md`](54_noise_emission_system.md) requires occlusion to attenuate rather than block, and requires it to match what the audio system does. One occlusion model, two consumers.
- **Light** — a closed door blocks light spill, which matters once darkness is a tactical state ([`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md)).
- The reference notes that scanning works **through** a closed door. That is a deliberate generosity worth copying — it keeps the scanner a navigation aid rather than something a door defeats ([`16_player_scanner_ping_tool.md`](16_player_scanner_ping_tool.md)).

**Add locks, and give them a key**

- A `Locked` state that neither players nor most monsters can open, with the key or lockpick from [`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md) as the counter. That item is consumable, which makes "is this door worth it" a decision with a cost.
- Locked doors must never make loot unreachable in a way the generator did not intend — [`39_loot_spawner.md`](39_loot_spawner.md)'s harness already reports items behind locked doors, and that number should be small and deliberate.
- The generator must never place a lock that isolates the extraction zone from any room; component 28's reachability guarantee holds regardless of lock state, or it is not a guarantee.
- Consider terminal-controlled doors as a later addition — the reference's secure doors, operated remotely by a hub-bound player, are the natural pairing with [`62_hazard_control_remote_disable.md`](62_hazard_control_remote_disable.md) and give the stay-behind role real power. Build the state machine so an external controller can set state, even if nothing does yet.

**Handle the physical edge cases**

- **A player or monster in the doorway.** Decide: block the close, or push them out. Recommended **block the close** with a clear failure cue — a door that shoves a fleeing teammate back into a corridor is the kind of physics comedy that stops being funny immediately.
- **A two-handed item.** [`42_two_handed_item_rule.md`](42_two_handed_item_rule.md) blocks door operation while carrying one. That is the rule that makes the big payday genuinely dangerous, and this component enforces it by refusing with a legible prompt.
- **Power loss.** If a door is powered, decide its unpowered state. Recommended: powered doors fail **open**, so a blackout cannot seal a crew in — the exception being a door deliberately designed as a hazard, telegraphed as such.
- **Round teardown.** All door state destroyed with the location, no leaked links.

## Acceptance Criteria

- [ ] Door state is an absolute replicated enum; no toggle command exists anywhere in the path.
- [ ] Two players interacting on the same tick leave the door open, never closed.
- [ ] Clients animate toward the replicated state and blend on correction rather than snapping.
- [ ] A player's own door interaction is predicted and feels immediate under simulated latency.
- [ ] Opening requires a hold with a visible progress indicator; closing is a single immediate press.
- [ ] Hold cancels cleanly on release, damage, or moving out of range.
- [ ] Per-monster door open time comes from monster data and measurably differentiates creatures.
- [ ] A monster working at a door is audible from the other side.
- [ ] A closed door blocks monster pathing within one frame of the state changing, via navigation links rather than carving.
- [ ] Which monsters can open doors at all is data, not a hard-coded branch.
- [ ] A closed door blocks the perception sight ray, verified against a moved collider and not just a moved mesh.
- [ ] A closed door attenuates noise events consistently with audio occlusion.
- [ ] A closed door blocks light spill.
- [ ] Scanning works through a closed door.
- [ ] Locked doors exist, are opened only by a consumable key or lockpick, and never isolate the extraction zone from any room.
- [ ] The count of items behind locked doors is small and reported by the loot harness.
- [ ] Door operation is blocked while carrying a two-handed item, with a legible prompt.
- [ ] A body in the doorway blocks the close with a clear cue rather than being pushed.
- [ ] Powered doors fail open on power loss, unless deliberately authored otherwise and telegraphed.
- [ ] Door state can be set by an external controller, ready for terminal control.
- [ ] Total door count per location respects a budget, and a maximum-door layout stays within the snapshot bandwidth budget with four clients.
- [ ] All door state and navigation links are destroyed at round end.
