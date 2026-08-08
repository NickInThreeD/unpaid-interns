# 56 — Threat / Interest Targeting

**Source:** [`core_components.md`](../core_components.md) §6 — Monsters & AI
**Status:** ❌ Not started
**Depends on:** [Perception System](53_perception_system.md), [Chase & Pathfinding](55_chase_and_pathfinding.md), [Inventory](40_inventory_item_bar.md), [Health & Injury](13_health_and_injury.md)
**Blocks:** aggro being manipulable, monsters reading as intelligent rather than nearest-target-seeking

## Summary

Choosing between two valid targets.

`core_components.md` puts the value of this component in one line: it is *"the difference between a chase system and an AI system."* A monster that always picks the closest player is a proximity function. A monster that picks the **armed one**, or the **injured one**, or the **one carrying the most loot**, is something the crew can reason about — and, more importantly, something they can *manipulate*.

That last part is where the design payoff lives. Once targeting has inputs the player controls, dropping your haul to become uninteresting, or holding a weapon to become the one it comes for, are real tactical choices. The reference design builds this from three numeric properties — visibility, **threat level**, and **interest level** ([`Assets/docs/detection-and-combat/entity-targeting.md`](../../Assets/docs/detection-and-combat/entity-targeting.md)) — and is explicit that only certain creatures use each. That selective use is the important structural idea: it means the roster stays varied without every monster needing bespoke code.

This is the one component in §6 that is genuinely optional for MVP. A nearest-visible-target rule ships a working game. Build the interface early so it can be slotted in, and build the scoring when the roster is large enough for the choice to matter.

## How to Build

**Score targets, do not sort them by distance**

- Perception produces a set of *perceivable* targets ([`53_perception_system.md`](53_perception_system.md)); this component picks one from that set. Keep the two strictly separate — a monster that cannot perceive a target must never select it, however attractive it scores.
- Each candidate gets a score from weighted terms, and each monster carries its own weights in data ([`48_monster_data_definitions.md`](48_monster_data_definitions.md)). A monster that ignores threat entirely has a zero weight, not a special case.
- Terms worth having, each with a player-facing behaviour that teaches it:
  - **Distance** — the baseline, and still the dominant term for most monsters.
  - **Threat** — is this target dangerous? Holding a weapon is the main input ([`45_weapons_as_tools.md`](45_weapons_as_tools.md) makes this an item-category check). A positive weight produces a monster that attacks the armed player; a *negative* weight produces one that avoids them and goes for the defenceless — which is a far more interesting creature and costs the same to build.
  - **Interest** — is this target appealing? Carried loot value is the natural input, and it creates the game's best voluntary sacrifice: drop the haul and stop being the most attractive thing in the room.
  - **Vulnerability** — injured, exhausted, or heavily loaded targets ([`13_health_and_injury.md`](13_health_and_injury.md), [`11_stamina.md`](11_stamina.md), [`12_carry_weight.md`](12_carry_weight.md)). A monster that finishes the wounded is thematically correct and mechanically cruel in the right way.
  - **Isolation** — a lone target versus one with teammates nearby. This one directly rewards the design's central social tension: splitting up is efficient and dangerous.

**Keep it sticky**

- Re-score on an interval, not per frame, and require a **meaningful margin** before switching targets. [`55_chase_and_pathfinding.md`](55_chase_and_pathfinding.md) already requires stickiness for a specific reason — a monster oscillating between two players catches neither and reads as broken rather than as indecisive.
- Add hysteresis on the switch threshold, the same shape as the stamina exhaustion thresholds in [`11_stamina.md`](11_stamina.md): the bar to acquire a new target is higher than the bar to keep the current one.
- A target that becomes unperceivable does not trigger an immediate reselect; it triggers Search at the last known position. Instant retargeting is the single clearest tell that a monster is omniscient.

**Never replicate the choice**

- [`49_monster_ghost_and_replication.md`](49_monster_ghost_and_replication.md) forbids sending the current target, and this is the component that would be tempted to. A client that knows which player a monster is hunting knows more than the game intends to tell anyone, and a modified client will draw it on screen.
- The targeted player may be told, and only them — the fear system's "being hunted" term already needs it ([`15_fear_and_stress_feedback.md`](15_fear_and_stress_feedback.md)). Send it as a per-player value, not as monster state.
- Everything else — scores, weights, timers — is server-only.

**Make the inputs discoverable**

- Manipulable aggro is worthless if nobody knows it is manipulable. The behaviour has to be **observable**: a monster that visibly turns from the empty-handed player toward the one with a full inventory teaches the rule in one encounter.
- Keep the roster's rules few and strong. Two or three monsters with one distinctive targeting rule each is legible; six monsters with five weighted terms each is noise that players will experience as randomness.
- The in-fiction orientation §13 asks for is the right place to hint that what you carry and what you hold changes who they come for — without stating the weights.
- Never show a numeric aggro value, for the same reason [`53_perception_system.md`](53_perception_system.md) refuses a detection meter.

**Cover the non-player targets**

- Corpses, dropped loot, and other monsters may all be valid targets for some creatures. A monster that investigates dropped scrap instead of the player who dropped it is exactly the distraction mechanic [`47_physics_props_and_throwing.md`](47_physics_props_and_throwing.md) is trying to enable — and building targeting to accept non-player candidates from the start is what makes that possible without a second system.
- Spectators are never targets, and neither are players in the extraction zone if the zone-entry rule in [`31_entry_point_extraction_zone.md`](31_entry_point_extraction_zone.md) forbids it.
- A dead player's body should be a target only if a monster archetype specifically consumes or moves bodies — which is a memorable threat and should be a deliberate authored behaviour, not an emergent one.

**Test it as a matrix, not by playing**

- A harness that places one monster and several scripted targets with controlled properties — armed, injured, loaded, isolated — and asserts the expected selection per monster archetype. Targeting bugs are otherwise found only by a player insisting the monster "went for the wrong person", which is unfalsifiable.
- Extend the debug overlay to show each candidate's score breakdown for the selected monster. This is the only way the weights ever get tuned.

## Acceptance Criteria

- [ ] Targeting selects from the perceivable set only; an imperceptible target is never chosen regardless of score.
- [ ] Scoring is a weighted sum with per-monster weights in data, and a zero weight cleanly disables a term.
- [ ] Distance, threat, interest, vulnerability, and isolation terms are all implemented and individually tunable.
- [ ] Holding a weapon measurably changes which target a threat-weighted monster selects.
- [ ] Dropping carried loot measurably reduces a player's attractiveness to an interest-weighted monster.
- [ ] At least one authored monster uses a negative threat weight and preferentially targets the unarmed.
- [ ] Injury, exhaustion, and heavy load each measurably increase selection likelihood for a vulnerability-weighted monster.
- [ ] An isolated player is measurably more likely to be selected than one beside teammates.
- [ ] Re-scoring runs on an interval, and switching requires a margin with hysteresis.
- [ ] A monster mid-chase does not oscillate between two nearby players.
- [ ] A target becoming unperceivable sends the monster to Search rather than triggering an immediate reselect.
- [ ] The current target is never replicated to non-targeted clients.
- [ ] The targeted player receives a per-player "being hunted" signal usable by the fear system.
- [ ] No numeric aggro or threat value is shown to players.
- [ ] Targeting behaviour is observable enough that a player can infer the rule from one encounter.
- [ ] Non-player candidates — dropped items, corpses, other monsters — are supported by the same scoring path.
- [ ] A thrown item can draw an appropriately-weighted monster away from a player.
- [ ] Spectators are never targeted, and the extraction-zone rule is honoured.
- [ ] A harness asserts expected selection per archetype across a matrix of target properties.
- [ ] The debug overlay shows per-candidate score breakdowns for a selected monster.
- [ ] Falling back to nearest-visible-target requires only zeroing the weights, so the system can ship incrementally.
