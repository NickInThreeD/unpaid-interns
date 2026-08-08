# 34 — Out-of-Bounds Handling

**Source:** [`core_components.md`](../core_components.md) §4 — Location & World Generation
**Status:** ❌ Not started
**Depends on:** Exterior / Approach Area, Health & Injury (single damage entry point)
**Blocks:** shipping a location without a hole in it

## Summary

Keeping players inside the play space without breaking the fiction, and deciding what happens to anything that gets out anyway.

Two failures are being prevented and they are not the same. The first is a player **walking** out — over a ridge, past the fence, into terrain that was never meant to be reached. The reference design's own wiki page on this ([`Assets/docs/world/out-of-bounds-areas.md`](../../Assets/docs/world/out-of-bounds-areas.md)) is a tour of leftover terrain with no content in it, which is what happens when the boundary is left to the art pass. The second is a player or an **item falling out of the world** through a seam in procedural geometry, which will happen, because a generator that has never produced a hole has not been run enough times.

The second failure is the expensive one, and specifically for items: **an item that falls out of the world is quota that has silently vanished.** In an economy where items convert directly to the crew's survival, losing one to a geometry seam is a bug that costs a run, and nobody will be able to report it.

## How to Build

**Prefer terrain over volumes**

- The best out-of-bounds handling is a boundary players never test. Ridges, water, fences, sheer drops, and buildings do more than any volume, and they cost nothing at runtime.
- The exterior is hand-authored ([`33_exterior_approach_area.md`](33_exterior_approach_area.md)), so this is achievable there. Author the boundary deliberately as part of the scene rather than as a pass afterwards.
- Where geometry cannot close the space, use a soft boundary: a warning zone with a visible and audible cue, a countdown, and a return. Turning the player around beats killing them, and killing them beats letting them wander into nothing.

**Layer the response**

Three tiers, in order of preference:

- **Blocked** — invisible collision at the edge of authored terrain. Correct where the fiction supports a wall. Wrong on open ground, where an invisible wall is worse for immersion than the void behind it.
- **Warned and returned** — the player enters a boundary zone, gets an unmistakable warning, and is walked or teleported back to the nearest valid position after a few seconds. Recommended default for the exterior.
- **Killed** — a kill volume below the world and above the ceiling, as the last resort for anything that has genuinely escaped containment. This must exist regardless of the other two, because falling through geometry is not a boundary problem, it is a physics accident.

**Run it on the server, and forgive prediction**

- The check is server-authoritative — a client cannot be trusted to report itself out of bounds, and a client that thinks it is out of bounds when the server disagrees must not act on it.
- **Do not kill on a single frame's reading.** Movement is client-predicted with reconciliation, and a correction can briefly place a character somewhere it never actually was. Require the condition to hold for a short, configurable duration before responding. This is the difference between a safety net and a component that randomly kills people during lag spikes.
- Route lethal out-of-bounds damage through the single server-side damage entry point required by [`13_health_and_injury.md`](13_health_and_injury.md), with its own source classification. It must not be attributed to a teammate, and the friendly-fire multiplier must not apply to it.
- A death out of bounds is a normal death: items drop, a body spawns, the roster updates ([`14_death_and_body_system.md`](14_death_and_body_system.md)). Placing the body at the death position is wrong here, though — put it at the nearest valid position, or the crew has been given a recovery objective inside a wall.

**Handle items separately — this is the part that gets missed**

- Items are physics bodies that get thrown, dropped down stairwells, and knocked through seams. They need their own containment, and destroying them silently is not acceptable.
- **Recommended: teleport an out-of-bounds item back to the nearest valid position on the navigable surface**, rather than destroying it. The value stays in the economy, the crew can still recover it, and a geometry bug costs a walk rather than a payday.
- If an item genuinely cannot be relocated, destroy it **and log it loudly with the round seed, item id, and rolled value**. That log is the only way this class of bug is ever found, and the seed makes it reproducible ([`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md)).
- The same applies to bodies. A corpse that falls out of the world is a permanent penalty the crew cannot avoid.

**Cover the other actors**

- **Monsters** — a monster that escapes the navigable space is stuck forever and silently reduces the round's threat. Detect it, despawn it, and let the spawn director reclaim its power budget (§6).
- **Spectators** — free-cam must be constrained to the location bounds, which [`22_spectator_mode.md`](22_spectator_mode.md) already requires. This component supplies the bounds definition; do not let it define a second one.
- **The hub** — the hub is a location too, and a player who escapes it is in a safe state with no way back. Apply the same boundary there, with the "warned and returned" tier rather than anything lethal.

**Define the bounds as data**

- Each location carries an explicit bounds volume, authored with the exterior and extended to enclose the generated interior's actual extent after assembly. A fixed volume authored before generation will either clip a large layout or be uselessly large for a small one.
- The interior's extent is known once the generator finishes, inside the load barrier — compute the final bounds there and share the one value with the kill plane, the spectator constraint, and the item containment check.
- Add a debug command to visualise the bounds and to teleport to the nearest valid position, and add a generation-harness assertion that the assembled interior fits inside the location's declared maximum extent.

## Acceptance Criteria

- [ ] Every location has an explicit bounds volume that encloses both the authored exterior and the assembled interior's real extent.
- [ ] Bounds are computed once, inside the load barrier, and shared by the kill plane, the spectator constraint, and item containment.
- [ ] The exterior boundary is primarily terrain; volumes are used only where geometry cannot close the space.
- [ ] A player approaching the boundary receives an unmistakable warning before anything happens to them.
- [ ] The warned-and-returned tier returns the player to a valid position without killing them.
- [ ] A kill plane exists below the world and above the ceiling in every location, including the hub and the generated interior.
- [ ] The out-of-bounds condition must persist for the configured duration before a response fires; a single lag spike never kills anyone.
- [ ] Out-of-bounds damage flows through the single server-side damage entry point with its own source classification, and is never attributed to a teammate.
- [ ] A body created by an out-of-bounds death spawns at the nearest valid position and is recoverable.
- [ ] An out-of-bounds item is returned to a valid position rather than destroyed.
- [ ] An item that cannot be relocated is destroyed only after logging the seed, item id, and rolled value.
- [ ] No item, body, or credit value is lost silently under any out-of-bounds condition.
- [ ] A monster that escapes the navigable space is despawned and its power budget reclaimed.
- [ ] Spectator free-cam is constrained to the same bounds volume, with no second definition.
- [ ] The hub has boundary handling and it is non-lethal.
- [ ] A debug command visualises the bounds and teleports to the nearest valid position.
- [ ] The generation harness asserts that assembled interiors fit inside the declared maximum extent across at least 1,000 seeds.
