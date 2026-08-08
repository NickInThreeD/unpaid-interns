# 54 — Noise Emission System

**Source:** [`core_components.md`](../core_components.md) §6 — Monsters & AI
**Status:** ❌ Not started · **[MVP]**
**Depends on:** nothing — build it early
**Blocks:** Perception System, monster hearing, stealth, proximity voice as a risk, thrown-item distraction

## Summary

Every action publishing how loud it was and how far it carried, so something can hear it.

The critical thing to understand before writing a line of it is stated in `core_components.md` and is easy to nod past: **the existing `SoundSystem` is presentation-only.** `Assets/Scripts/Audio/` plays audio — pooled emitters, `SoundDef` assets, mixer routing, a headless no-op path — and models nothing about what entities can perceive. On a dedicated server it is a no-op that plays nothing at all, which is correct for audio and fatal if monsters hear through it.

So there are two systems, and they must **stay in sync without being the same system**. What the player hears and what the monster hears have to match, or the game becomes unlearnable: a player who hears themselves being quiet and gets caught anyway has been lied to.

This is also the component with the widest reach in §6. Footsteps, sprinting, landing, dropping and throwing items, passive noisemakers, tools, voice, injured breathing, doors, and the breaker box all feed it, and every one of those is specified in another plan as "route this into the noise system". It should be built early, before the systems that depend on it accumulate their own ad-hoc answers.

The reference design's catalogue ([`Assets/docs/detection-and-combat/audible-sounds.md`](../../Assets/docs/detection-and-combat/audible-sounds.md)) is the model: every noise is exactly two numbers — **range** and **volume** — and every value in the game is in one table. Walking is range 17 / volume 0.4; sprinting is 22 / 0.6; landing is 7 / 0.5; dropping an item is 8 / 0.5; voice spans 3–36 depending on how loudly you speak. Two numbers is enough, and the flatness of the model is what makes it tunable.

## How to Build

**Define one event, and keep it small**

- A noise event is: **world position, range, volume, source category, and the entity responsible.** Nothing else. Resist adding a frequency, a material, a propagation model — every field added is a field every emitter must supply correctly and every consumer must interpret consistently.
- Source category (`Footstep`, `Item`, `Voice`, `Tool`, `Door`, `Impact`, `Breathing`) exists so a monster can weight categories differently and so the debug view is readable, not so behaviour can branch per category.
- Events are **transient**. They are raised, consumed by whatever is listening this tick, and gone. Nothing accumulates a noise history except a monster choosing to remember a position ([`53_perception_system.md`](53_perception_system.md)).

**Raise every event on the server**

- This is non-negotiable and it is the reason the noise system cannot simply be a hook in the audio system. A client that raises its own noise events can raise none, and the resulting cheat — perfect silence — is undetectable and completely breaks the threat layer.
- The rule appears in several plans already, and this is where it is enforced once: [`21_proximity_voice_comms.md`](21_proximity_voice_comms.md) requires the voice noise event to be raised server-side from replicated speaking state; [`15_fear_and_stress_feedback.md`](15_fear_and_stress_feedback.md) requires the breathing trigger to be computed server-side; [`47_physics_props_and_throwing.md`](47_physics_props_and_throwing.md) requires impact noise to come from the server's simulation.
- Movement noise is derived from the server's authoritative movement state — the player's `MovementType` and speed, which `ServerPlayerMovementSystem` already computes. It is not sent by the client and there is no reason it ever should be.
- The dedicated-server build must produce identical noise events to a host, with `SoundSystemNull` swapped in and no audio playing at all. That is the test that proves the two systems are genuinely separate.

**Put every value in one table**

- A single ScriptableObject config holding range and volume for every noise in the game, following the `WeaponData` pattern. One table is what makes the relationships tunable: sprinting versus walking, a dropped item versus a thrown one, crouched footsteps versus standing.
- Item-specific noise (passive noisemakers, tool activation) lives on the item definition ([`37_item_definition_data_model.md`](37_item_definition_data_model.md)) since it varies per item, but it uses the same two-number shape.
- Publish the table. Players learning that sprinting is much louder than walking is the intended outcome, and §13's orientation is where the two or three rules that generalise get stated.

**Keep audio and noise aligned by construction**

- The strongest guarantee: **one call site raises both.** A helper that takes a `SoundDef` and a noise entry and does the audio playback and the noise event together makes divergence require deliberate effort.
- Where they must differ, be explicit: a non-diegetic stress tone plays audio and raises no noise ([`15_fear_and_stress_feedback.md`](15_fear_and_stress_feedback.md)); a monster's own footsteps may be audible to players but need not be perceivable by other monsters. Each exception should be a named, commented decision.
- The reverse case matters too: a noise with no sound is a bug the player experiences as being caught for nothing.
- Crouched movement must reduce the **noise event**, not merely the mixer volume — [`10_crouch.md`](10_crouch.md) already makes this an acceptance criterion, and it is the canonical example of the two systems drifting.

**Route it through the EventBus, with a bridge**

- §11 requires cross-system communication to go through the shared EventBus package, and noise is the flagship case. It also flags the obstacle: ECS systems cannot hold managed references, so the bus needs a thin bridge at the `GhostMonoBehaviour` boundary.
- Noise is raised from both worlds — from ECS movement systems and from MonoBehaviour item and door code — so the bridge has to work in both directions from the start. Design it here rather than discovering it in §6.
- The package is not present in this project ([`06_session_persistence.md`](06_session_persistence.md) documents the acquisition problem). Until it arrives, define the interface this system needs and implement it directly, so swapping in the bus later is a change of transport rather than a rewrite.
- If the bus proves unworkable for per-tick, high-frequency events, say so explicitly and use a direct server-side queue. A footstep every few hundred milliseconds per player is not a lot, but it is not zero either, and an allocation per event is a real cost.

**Make consumption cheap**

- Consumers are monsters, and there may be many. Do not let every monster iterate every event: spatially partition, or have the emitter query nearby listeners once, using the same broad-phase discipline [`53_perception_system.md`](53_perception_system.md) applies to sight.
- Range is the natural culling key and it is already on the event. Reject on squared distance before doing anything else.
- Budget it alongside perception, and profile with the maximum monster budget and four players sprinting.

**Make it visible, because it is invisible**

- A debug view drawing each noise event as an expanding sphere with its category and volume, plus a log of recent events. Noise is the least observable system in the game and the one players will most often believe is wrong.
- A `ConfigVar` to print every noise a specific player emits, which turns "why did it hear me" into an answerable question.

## Acceptance Criteria

- [ ] A noise event carries position, range, volume, category, and responsible entity, and nothing else.
- [ ] Every noise event is raised on the server; no client can raise, suppress, or alter one.
- [ ] A modified client cannot move, act, or speak silently.
- [ ] Movement noise derives from server-authoritative movement state, not from client input.
- [ ] A dedicated-server build with `SoundSystemNull` produces identical noise events to a host with audio playing.
- [ ] All noise ranges and volumes live in one config asset, tunable without a recompile.
- [ ] Item-specific noise lives on the item definition and uses the same range/volume shape.
- [ ] Sprinting is measurably louder and further-reaching than walking; crouched movement is quieter than both.
- [ ] Crouching reduces the noise event itself, not just audio playback.
- [ ] Every noise event has a corresponding audible sound, and every gameplay-relevant sound raises a noise event, except for documented exceptions.
- [ ] Footsteps, sprinting, landing, item drops and throws, passive item noise, tool use, voice, injured and panicked breathing, doors, and the breaker box all emit events.
- [ ] Voice transmission raises a server-side event that a modified client cannot suppress.
- [ ] Injured and panicked breathing are a single unified noise source with two triggers, not two overlapping ones.
- [ ] The system exposes an interface that can be backed by the shared EventBus once it is available, without rewriting emitters or consumers.
- [ ] The ECS-to-MonoBehaviour bridge carries events in both directions.
- [ ] Consumption is spatially culled; a full monster budget with four sprinting players holds the server frame budget.
- [ ] No per-event managed allocation occurs in the steady state.
- [ ] A debug view renders noise events with category and volume, and a command logs every noise a chosen player emits.
- [ ] Occlusion attenuates noise consistently with the audio system's occlusion, so what a player hears matches what a monster hears.
