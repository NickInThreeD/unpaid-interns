# 84 — Player Audio

**Source:** [`core_components.md`](../core_components.md) §10 — Audio
**Status:** ⚠️ Footsteps exist, are local-only and context-blind · **[MVP]**
**Depends on:** [Noise Emission System](54_noise_emission_system.md), [Crouch](10_crouch.md), [Sprint](09_sprint.md)
**Blocks:** what players hear matching what monsters hear

## Summary

The sounds interns make, and the fact that other things can hear them.

`core_components.md` is precise about the current state: footsteps exist via `HandleFirstPersonFootstepSFX` but fire **only for the locally owned client** and are unaware of surface, speed, or crouch. Two of those three are structural problems rather than polish.

**Local-only is the serious one.** In a co-op game, hearing a teammate's footsteps is how you know where they are, whether they are coming back, and — in a game with monsters that imitate players — whether what is approaching moves like a person. A game where nobody can hear anybody else is a game where the crew is four separate single-player sessions sharing a quota.

The second requirement is the one `core_components.md` states as a rule: player audio *"must also feed the noise-emission system, so what players hear matches what monsters hear."* That alignment is the whole point. A player who hears themselves being quiet and gets caught anyway has been lied to, and once they suspect the audio is decorative they stop using stealth at all.

## How to Build

**Make remote players audible**

- Footsteps, landings, and every other movement sound must play for **all clients**, spatialised at the emitting player's replicated position, not only for the owner.
- The current implementation is a first-person effect. What is needed is a **third-person emitter** on every player ghost, with the local player's first-person layer as an additional, quieter, non-spatialised presentation on top — which is how the two views usually differ, and it keeps the owner's own footsteps from sounding like they come from a body five metres away.
- Drive it from replicated movement state so a remote player's footsteps match what their character is visibly doing. `PlayerGhost` and the 3P animator rig already exist; this hangs off the same state that drives the animation.
- Verify it against the mimic archetype ([`80_teammate_identification.md`](80_teammate_identification.md), [`58_monster_variety_set.md`](58_monster_variety_set.md)) — if a monster imitates a player, whether it imitates their footsteps is a design decision that should be made deliberately rather than falling out of which emitter it happens to have.

**Make it aware of speed, stance, and surface**

- **Speed and stance** are the gameplay-relevant axes. Sprinting is loud, walking is moderate, crouching is quiet — and those differences must be audible before they are meaningful. [`09_sprint.md`](09_sprint.md) and [`10_crouch.md`](10_crouch.md) both establish states this reads from; the movement type and speed are already on `ControllerState`.
- **Surface** is the flavour axis and the cheaper one to defer. Metal grating, concrete, water, and carpet are worth having eventually, and a `SoundDef` per surface tagged on the material is the standard approach.
- Note the existing timing implementation is unusual: `HandleFirstPersonFootstepSFX` uses `Time.time` bookkeeping *"called multiple times per update, so I can't use Time.delta"*, because it runs inside the prediction loop. Any rework must respect that — footstep timing sits in replayed code, so a naive accumulator will fire multiple times per tick during reconciliation. Drive step events from **distance travelled** rather than elapsed time, which is replay-safe and also automatically correct across speed changes.

**Raise the noise event from the same place — this is the rule**

- [`54_noise_emission_system.md`](54_noise_emission_system.md) recommends **one call site raising both** the audio and the noise event, so divergence requires deliberate effort. Player movement is the highest-frequency case and the one where drift would be most damaging.
- The noise event must be raised **on the server**, from server-authoritative movement state, so a modified client cannot move silently. That plan makes this non-negotiable and this component is the main emitter it was written for.
- [`10_crouch.md`](10_crouch.md) already carries the acceptance criterion that crouching must reduce the **noise event**, not merely the mixer volume. This is where that is implemented, and it is the canonical example of the two systems drifting apart if built separately.
- The reference's values give a starting calibration: sprinting at range 22 / volume 0.6, walking at 17 / 0.4, landing at 7 / 0.5 ([`Assets/docs/detection-and-combat/audible-sounds.md`](../../Assets/docs/detection-and-combat/audible-sounds.md)). The ratios matter more than the absolute numbers.

**Cover the rest of the player's sound surface**

- **Landing** — scaled by fall height, which [`61_fall_and_environmental_damage.md`](61_fall_and_environmental_damage.md) requires to raise a proportional noise event. A hard landing after a shortcut drop should cost attention as well as health.
- **Breathing** — injured and panicked breathing must be **one unified source with two triggers**, not two overlapping loops. [`13_health_and_injury.md`](13_health_and_injury.md) and [`15_fear_and_stress_feedback.md`](15_fear_and_stress_feedback.md) both specify it and both point at the same requirement; the breathing trigger is computed server-side so a client cannot silence itself.
- **Exhaustion** — [`11_stamina.md`](11_stamina.md) suggests laboured breathing as an exhaustion cue that doubles as a noise source, making exhaustion a compounding risk rather than a flat speed penalty. Same unified breathing source, third trigger.
- **Item handling** — pickup, drop, and throw impacts ([`47_physics_props_and_throwing.md`](47_physics_props_and_throwing.md)), plus passive noisemaker items ([`37_item_definition_data_model.md`](37_item_definition_data_model.md)).
- **Voice** — routed into the noise system by [`21_proximity_voice_comms.md`](21_proximity_voice_comms.md), raised server-side from replicated speaking state.

**Keep the owner's mix honest**

- The local player's own sounds should be quieter in their own mix than a teammate's at the same distance, or a sprinting player drowns out the monster behind them — which inverts the risk the sprint was supposed to carry.
- Do not suppress them entirely. A player needs to hear their own footsteps to learn that sprinting is loud; the lesson is taught by the sound, not by a tooltip.
- Mind the `AudioListener` constraint ([`22_spectator_mode.md`](22_spectator_mode.md)): exactly one active listener, positioned at the current view. Spectators hear from the spectated position, not from their corpse.

**Budget and verify**

- Four players plus monsters plus ambience competes for voices. Cap concurrent player-audio voices and prioritise by proximity, yielding to monster cues ([`82_monster_audio_cues.md`](82_monster_audio_cues.md)).
- The dedicated-server build runs `SoundSystemNull` and plays nothing — but **must still raise identical noise events**. That is the test that proves the two systems are genuinely separate, and it is the single most valuable check in this component.

## Acceptance Criteria

- [ ] Footsteps and all movement sounds play for every client, spatialised at the emitting player's position.
- [ ] The local player hears their own sounds through a distinct first-person layer, quieter than a teammate's at equivalent distance.
- [ ] Remote footsteps match the remote player's visible movement.
- [ ] Whether a mimic reproduces player footsteps is a documented decision.
- [ ] Sprinting, walking, and crouching produce audibly different footsteps.
- [ ] Surface type varies footstep sound, or its deferral is documented.
- [ ] Step events are driven by distance travelled and fire exactly once per step during prediction replay.
- [ ] Audio and the corresponding noise event are raised from one call site.
- [ ] Noise events are raised on the server from authoritative movement state; a modified client cannot move silently.
- [ ] Crouching reduces the noise event itself, not only playback volume.
- [ ] Sprint, walk, crouch, and landing noise values are calibrated with correct relative ratios.
- [ ] Landing noise scales with fall height.
- [ ] Injured, panicked, and exhausted breathing are one unified source with three triggers, raised server-side.
- [ ] Item pickup, drop, throw impact, and passive item noise all produce audio and noise events.
- [ ] Exactly one `AudioListener` is active, positioned at the current view including while spectating.
- [ ] Concurrent player-audio voices are capped and yield priority to monster cues.
- [ ] A dedicated-server build plays no audio and raises identical noise events to a host.
- [ ] Four players moving in one room hold the audio and frame budgets on the lowest-spec target.
