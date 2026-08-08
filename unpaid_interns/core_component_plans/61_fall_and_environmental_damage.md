# 61 — Fall & Environmental Damage

**Source:** [`core_components.md`](../core_components.md) §7 — Hazards & Environment Interaction
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Health & Injury](13_health_and_injury.md) (single damage entry point)
**Blocks:** verticality having a cost, water being dangerous, the map being able to kill you

## Summary

The map hurting you without anything deciding to.

This is the cheapest gameplay in the project. `core_components.md` says fall damage is *"close to free to add"* and it is right for a specific, verified reason: **`ControllerState.FallHeight` is already tracked and replicated.** `FirstPersonController.ShouldUpdateFallHeight` accumulates it during a fall, it resets on state change, and `CachedFallHeight` exposes it — and a repo-wide grep finds **no reference to it outside `FirstPersonController.cs`.** The measurement exists, is correct, is predicted, and is consumed by nothing.

The value beyond the cost is real. Fall damage is what makes vertical space a decision rather than a shortcut: dropping down a stairwell to escape a chase should be a trade, not a free action. It is also what gives [`17_climbing_and_verticality.md`](17_climbing_and_verticality.md) its stakes and what makes a deployable ladder worth buying.

The one prerequisite is not in this component. [`13_health_and_injury.md`](13_health_and_injury.md) requires the single `ApplyDamage(target, amount, source)` entry point to exist first — this is one of the sources it was designed to accept, and adding it as a direct health write would be the fourth such site in a file that already documents why that is a problem.

## How to Build

**Use the fall height that already exists**

- Apply banded damage on landing, in `GroundedCheck`, where the fall-to-standing transition is already detected. The state machine has the event; it simply does nothing with it.
- Bands, not a continuous curve: a safe height with no damage at all, a hurt band, a badly-hurt band, and a fatal height. Bands are learnable — a player can find out that "one floor is fine, two floors hurts" and act on it. A continuous function teaches nothing and feels arbitrary.
- The safe band must comfortably cover a jump and a normal step-down, or ordinary movement will chip health and players will stop jumping.
- Put every threshold in the same config asset as the other tunables; these will be retuned once the interior module set exists and its floor heights are known.

**Get the prediction right**

- Fall height is predicted state and landing is a predicted event, but **damage is server-authoritative** — [`13_health_and_injury.md`](13_health_and_injury.md) is explicit that clients predict movement and never health.
- So the client may predict the *feedback* (a landing grunt, a camera dip) and must not predict the health change. A client that predicts damage and is corrected will show a health bar that jumps, which is worse than one that arrives 80 ms late.
- Verify the server's `FallHeight` matches the client's at the landing tick. If reconciliation has been correcting position during the fall, the two can disagree — and a disagreement at a band boundary is the difference between fine and dead. If they drift, the server's value wins and the bands should have enough separation that a small difference does not cross one.

**Suppress it where it would be wrong**

- **Climbing.** [`17_climbing_and_verticality.md`](17_climbing_and_verticality.md) already requires `FallHeight` accumulation to be suppressed while climbing, or dismounting a tall ladder applies damage the player never earned. `ShouldUpdateFallHeight` is the function to extend.
- **Water.** Landing in deep water should not kill. This is the classic omission and it will be found immediately by anyone who jumps off the dam.
- **Out-of-bounds recovery.** A player returned to a valid position by [`34_out_of_bounds_handling.md`](34_out_of_bounds_handling.md) must not take fall damage from the teleport.
- **Deliberate design geometry.** If the generator produces a one-way drop as a shortcut ([`28_procedural_interior_generator.md`](28_procedural_interior_generator.md) allows them but requires the extraction zone to remain reachable across them), its height must be inside the safe band or the shortcut is a trap.

**Add the other environmental sources**

- **Drowning.** Time underwater without air, then damage over time. `LayerIndex.Water = 4` exists and is unused, which makes the volume detection nearly free. The reference notes players cannot swim ([`Assets/docs/world/water.md`](../../Assets/docs/world/water.md)) — no vertical movement underwater — which turns any deep water into a genuine hazard rather than an inconvenience, and pairs with flooded weather ([`35_environmental_conditions_weather.md`](35_environmental_conditions_weather.md)).
- **Instant-death volumes.** Pits and the kill plane, owned by [`34_out_of_bounds_handling.md`](34_out_of_bounds_handling.md) but applying damage through this component's path.
- Each source needs its **own classification** in the damage enum, because the friendly-fire multiplier must never touch them and the end-of-round summary should be able to say what killed someone. "Drowned while carrying the payday" is a better story than "died".

**Make the danger readable before it happens**

- A player must be able to judge a drop. Give a visual cue at the edge of a fatal height — an audible wind, a visible depth, a railing that stops being decorative. A fall that kills without warning is the same failure as an untelegraphed hazard ([`59_static_map_hazards.md`](59_static_map_hazards.md)).
- Landing feedback must scale with the band: a soft landing, a hard landing, and a bone-breaking one should sound and feel different, so the player learns the thresholds without a number.
- A hard landing should be **loud** — route it into the noise system, which the reference costs at range 7 / volume 0.5. Dropping down a shaft to escape should trade fall damage *and* attention, which is a much better decision than trading health alone.
- Injury from a fall enters the same critical-injury state as any other damage ([`13_health_and_injury.md`](13_health_and_injury.md)), so a bad landing during a chase compounds properly.

**Cover the interactions**

- Falling while holding a two-handed item, or a body, must not lose the item ([`42_two_handed_item_rule.md`](42_two_handed_item_rule.md) declines forced drops on damage; a fall is damage).
- Falling to death drops items and spawns a body at the landing position, not the departure position ([`14_death_and_body_system.md`](14_death_and_body_system.md)).
- Monsters should be subject to fall damage only if it is a deliberate archetype behaviour; most will be navigating on a NavMesh and never fall at all.
- Thrown items take no fall damage and are not destroyed by drops — that is [`47_physics_props_and_throwing.md`](47_physics_props_and_throwing.md)'s domain, and dropping loot down a stairwell should be a valid way to move it.

## Acceptance Criteria

- [ ] Fall damage is applied in bands from the existing `ControllerState.FallHeight`, on landing, through the single server-side damage entry point.
- [ ] The safe band comfortably covers a jump and a normal step-down; ordinary movement never causes damage.
- [ ] A fatal height exists and is documented.
- [ ] All thresholds live in a config asset and are tunable without a recompile.
- [ ] Damage is server-authoritative; the client predicts landing feedback only and never a health change.
- [ ] Server and client fall heights agree at the landing tick, and band separation absorbs any small drift.
- [ ] Fall height does not accumulate while climbing; dismounting a tall ladder causes no damage.
- [ ] Landing in deep water causes no fall damage.
- [ ] A player returned to a valid position by out-of-bounds handling takes no fall damage.
- [ ] Any generator-produced one-way drop is inside the safe band.
- [ ] Drowning applies damage after a configured time underwater, and the player cannot swim upward out of deep water.
- [ ] Instant-death volumes apply damage through the same entry point.
- [ ] Fall, drowning, and pit damage each carry a distinct source classification and are never scaled by the friendly-fire multiplier.
- [ ] Cause of death from each source reaches the body ghost and the end-of-round summary.
- [ ] A fatal drop is visually or audibly identifiable before committing to it.
- [ ] Landing feedback differs audibly and visually between bands.
- [ ] A hard landing raises a noise event proportional to the impact.
- [ ] Fall damage can push a player into the critical-injury state and compounds correctly with existing injury.
- [ ] Falling while carrying a two-handed item or a body does not drop it.
- [ ] A fatal fall drops items and spawns the body at the landing position.
- [ ] Dropped and thrown items are never destroyed or damaged by falling.
