# 82 — Monster Audio Cues

**Source:** [`core_components.md`](../core_components.md) §10 — Audio
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Monster Ghost & Replication](49_monster_ghost_and_replication.md), [Monster Variety Set](58_monster_variety_set.md), [Accessibility](79_accessibility.md)
**Blocks:** identifying a threat without seeing it — the game's primary survival skill

## Summary

Knowing what is nearby, and what it is doing, without looking at it.

`core_components.md` sets the bar as *"distinct, learnable per-monster sounds for idle, alerted, and chasing"*, with the goal that **players identify a threat and its state without line of sight**. That is not atmosphere — it is the game's core information channel. [`53_perception_system.md`](53_perception_system.md) builds monsters that hunt by sound and sight; the crew's counter-instrument is their own hearing, and this component is the instrument.

Two words in that requirement do the work. **Distinct** means a player can tell one creature from another, which is what makes the roster's archetypes ([`58_monster_variety_set.md`](58_monster_variety_set.md)) into knowledge rather than four kinds of dread. **Learnable** means the mapping is stable and simple enough to acquire by playing — a sound that means one thing on Tuesday and another on Thursday teaches nothing.

The infrastructure exists and is good. `Assets/Scripts/Audio/` provides pooled emitters, `SoundDef` assets, `SoundMixer` routing, and a headless `SoundSystemNull` path. What does not exist is any monster to play through it, or the discipline that keeps the cues legible as the roster grows.

## How to Build

**Drive cues from the replicated behaviour state, not from local guesses**

- [`49_monster_ghost_and_replication.md`](49_monster_ghost_and_replication.md) replicates a small behaviour enum — `Idle`, `Alerted`, `Searching`, `Chasing`, `Attacking`, `Dead` — precisely so clients can select animation and audio without receiving the AI's internals.
- Each monster's `SoundDef` sets are per-state fields on `MonsterData` ([`48_monster_data_definitions.md`](48_monster_data_definitions.md)). State changes select the set; the client does not infer state from velocity or proximity.
- One-shot cues — an attack, a spawn wind-up, a death — use the replicated tick stamps rather than a reliable RPC per event, following the `LastShotTick` pattern already used on `PredictedPlayerGhost` and compared against a cached tick in `HandleAnimationEvents`. That guarantees exactly one play per event under latency.
- **Transitions are the most valuable cue in the set.** The moment a creature goes from idle to alerted is the moment the player still has options; a distinct, unmissable transition sting is worth more than a perfect idle loop.

**Make the roster legible, not rich**

- Design the cues as a **set that must be told apart**, not one creature at a time. Four monsters authored independently by different ears will converge on similar low growls and the player will hear one undifferentiated threat.
- Separate them on axes a player can perceive without training: pitch register, rhythm (steady versus irregular), and texture (mechanical, wet, vocal). Two monsters may share a register if their rhythm differs sharply.
- Test the set the honest way: play the cues **without visuals** to someone who has played a few rounds and ask which creature it is. If they cannot, the set has failed regardless of how good each sound is alone.
- Keep idle loops sparse. A monster that vocalises constantly stops carrying information and becomes ambience the player filters out — which is exactly the wrong reflex to train.
- Reserve the most alarming sound in the game for the archetype that cannot be fought ([`58_monster_variety_set.md`](58_monster_variety_set.md)'s unavoidable). It should be recognisable in one syllable and it should end conversations.

**Make distance and occlusion honest**

- §10 flags occlusion as *a gameplay system, not polish*, and this is the component where that matters most. A monster behind a wall must sound like a monster behind a wall, or the player's distance estimate — the thing they are betting their life on — is wrong.
- Verify the existing `SoundSystem` supports occlusion, and if it does not, that is a prerequisite rather than a nice-to-have. [`60_door_system.md`](60_door_system.md) requires a closed door to attenuate sound consistently with what the noise system does, and there must be **one occlusion model** feeding both.
- Spatialisation must be accurate enough to act on. "Behind me" and "ahead of me" are the two most important distinctions the game makes, and a listener misconfiguration breaks both — note [`22_spectator_mode.md`](22_spectator_mode.md)'s warning about a second `AudioListener`, which produces exactly this failure.
- Attenuate by distance in a way that maps to actual danger. A creature audible at 40 metres and lethal at 2 gives the player a usable gradient; one that is inaudible until it is close gives them nothing.

**Keep the two systems aligned**

- What monsters emit as audio and what they emit as **noise events** are separate systems ([`54_noise_emission_system.md`](54_noise_emission_system.md)) and mostly serve opposite directions — monster audio informs players, noise events inform monsters. But a monster's own sounds should be perceivable by other monsters where that is interesting, and the plan already flags that as a named exception to be decided rather than left implicit.
- The dedicated-server build runs `SoundSystemNull` and plays nothing. Confirm no gameplay logic hangs off an audio callback, or a headless server will behave differently from a host — the same separation test [`54_noise_emission_system.md`](54_noise_emission_system.md) requires.

**Ship the accessibility counterpart at the same time**

- [`79_accessibility.md`](79_accessibility.md) exists because this component makes survival information audio-only. Every cue authored here needs its visual counterpart, and the cheapest way to guarantee that is structural: **the cue and its noise event are raised together**, and the visual-indicator system consumes the noise event.
- That means a monster's audible cues should raise noise events even when no monster is listening — the event is what the indicator subscribes to. Design them that way from the first creature rather than retrofitting.
- Subtitle non-speech monster sounds in closed-caption style, per component 79.

**Budget it**

- Monsters are pooled emitters and there may be several active. Cap concurrent monster voices, prioritise by proximity, and use the ghost's `MinDistSqrdFromAPlayer` — already computed by `GhostGameObject` when movement context is enabled — to cheaply drive both LOD and voice priority.
- Do not let a distant monster's idle loop occupy a voice that a nearby chase cue needs.

## Acceptance Criteria

- [ ] Each monster has distinct idle, alerted, searching, chasing, attacking, and death audio, selected from replicated behaviour state.
- [ ] Clients never infer monster state from velocity or proximity for audio selection.
- [ ] One-shot cues fire exactly once per event under latency, using replicated tick stamps.
- [ ] State transitions produce a distinct, unmissable cue.
- [ ] A player who has played a few rounds can identify each monster and its state from audio alone, with no visuals, in a blind test.
- [ ] Monsters are separated on register, rhythm, and texture, not only on pitch.
- [ ] Idle loops are sparse enough that players do not filter them out.
- [ ] The unavoidable archetype has the most recognisable cue in the game.
- [ ] Occlusion is implemented and a monster behind a wall sounds meaningfully different from one in the open.
- [ ] One occlusion model serves both audio and the noise system's attenuation.
- [ ] Exactly one `AudioListener` is active, and directional cues are accurate enough to distinguish ahead from behind.
- [ ] Distance attenuation gives a usable danger gradient rather than a sudden onset.
- [ ] Every audible monster cue raises a corresponding noise event, so the accessibility indicator system covers it automatically.
- [ ] Non-speech monster sounds are subtitled in closed-caption style.
- [ ] A dedicated-server build with `SoundSystemNull` behaves identically; no gameplay logic depends on an audio callback.
- [ ] Concurrent monster voices are capped and prioritised by proximity.
- [ ] A full monster power budget of active creatures holds the audio and frame budgets on the lowest-spec target.
- [ ] Adding a monster to the roster requires authoring `SoundDef` sets and no code change.
