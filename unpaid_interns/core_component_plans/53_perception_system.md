# 53 — Perception System

**Source:** [`core_components.md`](../core_components.md) §6 — Monsters & AI
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Monster Data Definitions](48_monster_data_definitions.md), [Noise Emission System](54_noise_emission_system.md), [Crouch](10_crouch.md), [Lighting & Power Grid](36_lighting_and_power_grid.md)
**Blocks:** Chase & Pathfinding, Threat Targeting, stealth being a skill rather than a hope

## Summary

How a monster decides that a player is there.

This is the component the entire stealth layer rests on, and `core_components.md` states its hardest requirement plainly: it **must be inspectable and consistent, because counterplay depends on players learning the rules.** That is unusual for a game system. Most systems are allowed to be approximately right; this one has to be *learnable*, which means it has to be simple enough to hold in a player's head and stable enough that what worked yesterday works today.

The design points everything at it. [`10_crouch.md`](10_crouch.md) exists to produce a visibility value nothing currently consumes. [`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md) requires darkness to cost monsters something, so a blackout is a trade rather than a punishment. [`35_environmental_conditions_weather.md`](35_environmental_conditions_weather.md) requires fog to cap monster sight by the same replicated number that caps the player's. All three of those are promises made to this component, and this is where they are kept.

The reference design's targeting page ([`Assets/docs/detection-and-combat/entity-targeting.md`](../../Assets/docs/detection-and-combat/entity-targeting.md)) is worth reading in full before building: it models perception as a small set of **numeric properties on the target** — visibility, threat, interest — that entities evaluate through their own thresholds. Crouching subtracts 0.25 from visibility; standing still for half a second subtracts another 0.16; different creatures ignore targets below different thresholds at different ranges. That shape is exactly right, and it is worth copying because it puts the *player-facing* rules on the player and the *creature-facing* rules on the creature.

## How to Build

**Model it as target properties plus per-monster thresholds**

- Every perceivable thing publishes a small set of numbers. For a player: **visibility** (how easy to see) and, separately, the noise events they emit ([`54_noise_emission_system.md`](54_noise_emission_system.md)).
- Every monster carries its own sense configuration — which senses it uses at all, ranges, view angle, and the thresholds it ignores things below ([`48_monster_data_definitions.md`](48_monster_data_definitions.md)).
- This split is what makes the roster legible. "Crouching makes you harder to see" is one rule the player learns once; "this one cannot see at all" is one fact per monster. The alternative — bespoke detection logic per creature — produces a game where nothing generalises and every monster must be learned from scratch by dying.

**Compute visibility from state that already exists**

- [`10_crouch.md`](10_crouch.md) specifies a normalized visibility value derived from movement type and speed, on server-readable state, deliberately avoiding a new replicated field. Consume that; do not invent a second one.
- The terms, following the reference: base 1.0; reduced when crouched; reduced further when stationary for a short dwell; zero when dead. A floor below which it cannot fall keeps a stationary crouched player from being literally invisible, which is important — perfect stealth is not a mechanic, it is an exit from the game.
- Two further terms this project needs and the reference handles elsewhere: **ambient light level** at the player's position ([`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md) provides the queryable value) and **carrying an active light source**, which must *raise* visibility so the flashlight is a real trade ([`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md)).
- Compute on the **server**. A client-authored visibility value is trivially cheatable and this is the single most valuable thing to cheat in the game.

**Make hearing a consumer, not a duplicate**

- Hearing does not poll for players; it **receives noise events** from [`54_noise_emission_system.md`](54_noise_emission_system.md), each carrying a position, a range, and a volume. A monster hears an event if it is within range and its hearing threshold is below the volume.
- That inversion is what keeps the two systems honest. A hearing implementation that samples player speed independently will drift from what the noise system says, and the player will experience two different sets of rules.
- Hearing gives a **position, not a target.** The reference is clear that some creatures use sound for targeting and others only for attention — turning to look. Both must be expressible, and "investigate the noise" is what feeds the last-known-position search in [`55_chase_and_pathfinding.md`](55_chase_and_pathfinding.md).
- Noise heard through a wall is still heard. Occlusion should attenuate it, not block it — §10 already flags occlusion as a gameplay system, and this is the system that consumes it.

**Keep line of sight cheap and honest**

- Sight requires: within range, within view angle, visibility above this monster's threshold for that range, and an unobstructed ray. Test in that order — the cheap rejections first, the raycast last.
- Raycast against **world geometry only**, on server-role layers, using the layer discipline established for role separation ([`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md), [`49_monster_ghost_and_replication.md`](49_monster_ghost_and_replication.md)). A sight check that hits the client copy of a player in a host process is a bug that reproduces nowhere else.
- Do not raycast every monster against every player every frame. Budget it: stagger checks across frames, reject on distance and angle first, and cap total sight queries per tick. A dozen monsters against four players at 60 Hz is the kind of cost that appears only in a full playtest.

**Give perception a memory, and make forgetting a rule**

- Detection is not binary. A monster accumulates **awareness** while a target is perceivable and decays it when not, crossing thresholds into `Alerted` and then `Chasing`. That ramp is what gives the player the "it noticed me" second that makes stealth playable.
- The reference makes this concrete: an alert timer that fills faster in proportion to the target's visibility. Copy that — it means crouching does not only reduce detection range, it buys time.
- Awareness, last-known position, and search timers are **server-only state** and must not be replicated ([`49_monster_ghost_and_replication.md`](49_monster_ghost_and_replication.md) forbids sending the target). Only the resulting behaviour state crosses the wire, which is enough for animation, audio, and the fear system's "being hunted" term ([`15_fear_and_stress_feedback.md`](15_fear_and_stress_feedback.md)).

**Publish the rules to players — deliberately**

- Because counterplay depends on learning, the rules must be discoverable. Not a stats screen: **feedback**. A monster that visibly turns toward a noise, pauses, and searches has taught the player that noise attracts attention without a single line of text.
- The in-fiction orientation §13 asks for is the right place to state the two or three rules that generalise — crouching helps, sprinting is loud, some things cannot see.
- Never surface a numeric detection meter. It converts a horror mechanic into an optimisation problem, and it makes the ambiguity — *did it hear me?* — go away, which is the feeling the game is selling.

**Build the debug view first, not last**

- A server-side overlay drawing each monster's sight cone, hearing radius, current awareness level, and last-known position. This is not a nicety: perception is invisible by construction, and without the overlay every tuning session is guesswork and every bug report is "it saw me through a wall, I think".
- Add a `ConfigVar` to freeze perception, to force a monster to full awareness, and to print why a specific detection did or did not occur. The last one turns the most common bug class in the game into a two-line answer.

## Acceptance Criteria

- [ ] Perception runs entirely on the server; no perception state is replicated beyond the resulting behaviour state.
- [ ] Target visibility is computed from crouch, movement, dwell time, ambient light, and carried light sources, using the value [`10_crouch.md`](10_crouch.md) already specifies.
- [ ] Visibility has a documented range, a floor above zero, and its meaning is recorded in one place.
- [ ] Carrying an active light source measurably increases visibility to sight-based monsters.
- [ ] Darkness measurably reduces sight range, using the same light-level value the renderer uses.
- [ ] A visibility-reducing weather condition reduces monster sight by the same replicated number that reduces the player's.
- [ ] Hearing consumes noise events rather than sampling player state, and never disagrees with the noise system.
- [ ] A monster that uses hearing only can find a player who is making noise and cannot find a silent one, regardless of line of sight.
- [ ] A monster that uses sight only cannot find a player in total darkness.
- [ ] Occlusion attenuates heard noise rather than blocking it.
- [ ] Sight requires range, angle, threshold, and an unobstructed ray, evaluated cheapest-first.
- [ ] Sight raycasts hit server-role colliders only; behaviour on a host matches a dedicated server.
- [ ] Total perception queries per tick are capped and staggered, and a full monster budget against four players holds the server frame budget.
- [ ] Awareness ramps and decays rather than toggling, and fills faster against a more visible target.
- [ ] Crouching measurably increases the time before detection, not only the distance.
- [ ] A monster that loses its target searches its last known position rather than instantly forgetting.
- [ ] The same action produces the same detection outcome across sessions; perception is stable enough to learn.
- [ ] No numeric detection meter is shown to players.
- [ ] A monster reacting to a noise is visibly legible as such.
- [ ] A debug overlay draws sight cones, hearing radii, awareness levels, and last-known positions.
- [ ] A debug command explains why a specific detection did or did not occur.
