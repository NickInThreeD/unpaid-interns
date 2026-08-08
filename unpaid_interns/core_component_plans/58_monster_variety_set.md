# 58 — Monster Variety Set

**Source:** [`core_components.md`](../core_components.md) §6 — Monsters & AI
**Status:** ❌ Not started · **[MVP]**
**Depends on:** every other component in §6
**Blocks:** the threat layer being a game rather than a system

## Summary

The actual creatures. Everything else in §6 is machinery; this is the content that machinery exists to run.

`core_components.md` names the target precisely: a starting roster covering **distinct counterplay archetypes** — one that hunts by sound, one by sight, one stationary that blocks a route, one unavoidable that must be fled — and states the principle that matters most, which is that **three or four well-differentiated monsters beat ten similar ones.**

The reason is not budget. It is that a monster is only frightening once the player knows what it does. Fear in this genre is anticipation, and anticipation requires recognition. Ten creatures the player cannot tell apart produce ten instances of the same generic dread; four they can name produce four different plans, four different mistakes, and four different stories. The roster is where the perception system, the noise system, and the targeting weights stop being parameters and become things players talk about afterwards.

This component is therefore mostly **design discipline plus authoring**, and it should be built last in §6 — but designed first, because the archetypes are what tell the earlier components which knobs they actually need.

## How to Build

**Author four, and make each one a different question**

The four archetypes, and what each one is *for*:

- **The sound hunter.** Blind, or effectively so. Uses hearing only ([`53_perception_system.md`](53_perception_system.md) requires "which senses" to be an explicit field, and this monster is why). Counterplay is walking instead of sprinting, not dropping things, and using a thrown item as a decoy ([`47_physics_props_and_throwing.md`](47_physics_props_and_throwing.md)). It makes the noise system legible in a single encounter and it is the reason the crew learns to be quiet.
- **The sight hunter.** Uses sight only, and therefore is beaten by crouching, by stillness, and by darkness ([`10_crouch.md`](10_crouch.md), [`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md)). It is the creature that makes the blackout a trade rather than a punishment, and the one that makes carrying a flashlight a decision.
- **The route blocker.** Stationary or near-stationary, lethal at contact, occupying a corridor. Counterplay is routing around it — which requires the generator's loop connections to exist ([`28_procedural_interior_generator.md`](28_procedural_interior_generator.md)) and gives them a purpose. High power cost despite low stats, because it removes options rather than dealing damage; this is the creature that proves power cost is about the crew's choices, not about health ([`48_monster_data_definitions.md`](48_monster_data_definitions.md)).
- **The unavoidable.** Faster than a sprint, cannot be fought, must be escaped or outlasted. [`55_chase_and_pathfinding.md`](55_chase_and_pathfinding.md) requires anything faster than a sprinting player to be a deliberate archetype rather than a tuning accident — this is that archetype. Its counterplay is doors, distance, and leaving. It is the creature that ends rounds, and it should be the rarest and most expensive thing the spawn director can buy.

Two of these — the sound hunter and the sight hunter — are the ones that teach the game's two core stealth verbs. Build those first; they are also the two that most stress the systems underneath, so building them early surfaces perception bugs while there is still time to fix the perception system.

**Differentiate by rule, not by number**

- Two monsters that differ only in health and speed are one monster. Differentiate on the axes players can perceive: **which senses**, **what counterplay works**, **what it does when it catches you**, and **where and when it appears**.
- Give each one a distinctive **kill behaviour** ([`57_attack_and_damage_application.md`](57_attack_and_damage_application.md)). The one that consumes the body and its items should be exactly one creature, and everyone should know which.
- Give each one a distinctive **targeting rule** if the weights are built ([`56_threat_interest_targeting.md`](56_threat_interest_targeting.md)) — the one that goes for the unarmed, or the loaded, or the isolated. One rule each, strongly expressed.
- Resist the fifth monster until the first four are individually tuned and individually recognisable. A roster grows by addition and gets confusing by accumulation.

**Make each one learnable in three encounters**

- **Encounter one** should be survivable and should teach the sense it uses, through visible behaviour rather than text — the sound hunter turning toward a noise, the sight hunter losing you when you crouch.
- **Encounter two** should punish the wrong instinct. The player who sprinted from the sound hunter learns why not.
- **Encounter three** should reward the right plan. That is when the creature becomes content rather than a hazard.
- This is a design requirement on the audio and animation as much as on the AI: §10 requires distinct, learnable per-monster sounds for idle, alerted, and chasing, and this is the component that consumes that requirement. A player must be able to identify which creature is nearby, and what state it is in, **without line of sight**.
- Accessibility (§9) applies with full force here — identification is primarily an audio skill, so every monster needs a visual and a subtitle equivalent, or an entire archetype becomes unplayable for some players.

**Place them deliberately**

- Assign each to indoor, outdoor, or both ([`48_monster_data_definitions.md`](48_monster_data_definitions.md)), and give the exterior its own creature. [`33_exterior_approach_area.md`](33_exterior_approach_area.md) argues that outdoor threats should be visible at range and avoidable by movement — a fifth archetype that only works in the open is a better use of budget than a fourth indoor one.
- Set the **earliest spawn time** per monster so the late round is qualitatively different, which is [`51_difficulty_escalation.md`](51_difficulty_escalation.md)'s second escalation lever. The unavoidable one should be a late-round creature.
- Vary eligibility by location so destinations feel different for reasons beyond size and loot ([`26_location_catalogue.md`](26_location_catalogue.md)). A destination known for one particular creature is a destination with a reputation.

**Tune against measurements, not impressions**

- Per monster, measure: encounter survival rate, average time-to-escape, and how often the intended counterplay was used. A creature whose counterplay is never used has not been taught, whatever its stats say.
- The spawn director's harness ([`50_spawn_director.md`](50_spawn_director.md)) is where this instrumentation belongs, and the balance telemetry §13 asks for is where it lands in production.
- Watch for the failure where one monster dominates the power budget and the others rarely appear. That is a cost-tuning problem, and it presents as "the game only has one monster".

## Acceptance Criteria

- [ ] Four archetypes exist and are individually recognisable: sound hunter, sight hunter, route blocker, and unavoidable.
- [ ] Each uses a distinct sense configuration, and at least one uses hearing only and one sight only.
- [ ] Each has a counterplay that measurably works and a wrong instinct that measurably fails.
- [ ] No two monsters differ only in numeric stats.
- [ ] Each has a distinct kill behaviour, and exactly one destroys carried items.
- [ ] Each has a distinct targeting rule where targeting weights are implemented.
- [ ] The route blocker's power cost reflects the options it removes, not its health.
- [ ] The unavoidable monster is faster than a sprinting player, is documented as deliberate, and is the most expensive thing the director can buy.
- [ ] Each monster has distinct idle, alerted, and chase audio, identifiable without line of sight.
- [ ] Each monster is identifiable and its state readable through a visual or subtitle equivalent, with audio disabled.
- [ ] At least one archetype is exterior-only and is avoidable by movement in open ground.
- [ ] Earliest spawn times differ across the roster, and the late round presents creatures the early round does not.
- [ ] Location eligibility varies, and at least two destinations have measurably different threat profiles.
- [ ] Encounter survival rate, time-to-escape, and counterplay usage are instrumented per monster.
- [ ] No single monster consumes a disproportionate share of the spawn budget across a measured sample of rounds.
- [ ] A new player, given no text instruction, demonstrates the correct counterplay for the sound hunter and the sight hunter within three encounters each in playtest.
- [ ] Adding a fifth monster requires no code change beyond its own behaviour components.
