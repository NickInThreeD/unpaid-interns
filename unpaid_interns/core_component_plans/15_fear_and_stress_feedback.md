# 15 — Fear / Stress Feedback

**Source:** [`core_components.md`](../core_components.md) §2 — Player Character
**Status:** ⚠️ Damage vignette exists and can be extended
**Depends on:** [Health & Injury](13_health_and_injury.md), [Death & Body System](14_death_and_body_system.md), [Threat / Interest Targeting](56_threat_interest_targeting.md) (for "being hunted"), [Lighting & Power Grid](36_lighting_and_power_grid.md) (for the darkness term)
**Blocks:** nothing mechanically — but it is what makes the horror land

## Summary

The layer that tells a player they are in danger before anything has hit them. Proximity to a monster, darkness, being actively hunted, low health, and seeing a teammate's corpse all push a stress value up; it decays when the player is safe.

`GAME_DESIGN.md` describes the tone as "dark workplace-comedy horror" and locates the tension in the moment-to-moment decision of how long to stay. That decision is emotional before it is arithmetic. A player who cannot *feel* the room getting worse will leave on a spreadsheet, and the game will read as a chore rather than a horror game.

**This should be feedback, not a stat.** The strong recommendation is that fear applies **no mechanical penalty** — no reduced speed, no aim sway, no stamina cost. A horror overlay that also makes you worse at surviving punishes the player twice for the same event and pushes them toward avoiding content. The Lethal Company reference in [`Assets/docs/core-loop/fear.md`](../../Assets/docs/core-loop/fear.md) is explicit that its fear state has no direct penalty and ends up functioning as a **free proximity alarm** — being noticed is what triggers it, so the effect is information. That is the better design and the one to copy.

The one deliberate exception: fear should be allowed to *feed* the noise system through panicked breathing, so being terrified is audible. That is a consequence of the fiction rather than a stat penalty, and it is optional.

## How to Build

**Compute a stress value client-side**

- Fear is presentation, so compute it on the **owning client** from state that is already replicated. It does not need to be a `[GhostField]`, and adding one would spend bandwidth on something no other system reads.
- Inputs, each contributing a weighted term: distance to the nearest known monster, whether a monster is currently targeting this player, ambient light level at the player's position (the queryable value from [`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md)), current health relative to the critical threshold from [`13_health_and_injury.md`](13_health_and_injury.md), being alone versus near a teammate, and line of sight to a player body.
- The "being targeted" term needs care. [`49_monster_ghost_and_replication.md`](49_monster_ghost_and_replication.md) forbids replicating a monster's current target, precisely because a client that knows it can draw a wallhack. So this term cannot be derived from monster state — it must arrive as a **per-player flag sent only to the targeted player** by [`56_threat_interest_targeting.md`](56_threat_interest_targeting.md). Reading it from the monster's replicated behaviour state instead would tell every client that *someone* is being hunted, which is both a leak and wrong for this player's overlay.
- Attack fast, decay slowly. Fear that vanishes the instant the monster turns away removes the aftermath, which is half the effect.
- Clamp to 0–1 and expose it as a single readable property so every presentation system reads one number rather than re-deriving its own.

**Extend the existing vignette**

- `Assets/Scripts/Gameplay/VisualEffects/DamageVisualsController.cs` already owns a full-screen pass: it builds a runtime material from `screenDamageMaterial`, wraps it in `FullScreenPassWrapper`, and injects it via `RenderPipelineManager.beginCameraRendering`, filtered to `_playerGhost.GetPlayerCamera()` so it only affects the owning player's view. That injection plumbing is exactly what fear needs.
- Add a **second pass and second material** rather than reusing the damage vignette. The two must be visually distinct: damage is a red flash that fades in about half a second (`fadeSpeed = 2f`), fear is a slower, colder distortion that lingers. If a player cannot tell "I am hurt" from "something is near me", the feedback is worse than none.
- Note the current `Update` only drives the material while `_currentIntensity > 0` and the pass early-outs at zero intensity — keep that structure so a calm player pays no rendering cost.
- Respect the accessibility requirement in §9: intensity must be scalable to zero from the options menu, and the distortion must never obscure the HUD elements a player needs to act (health, stamina, item bar). The reference implementation's habit of blurring the whole UI is a bug to avoid, not a feature to copy.

**Add the audio layer**

- Route fear audio through the existing `SoundSystem` (`Assets/Scripts/Audio/`) using `SoundDef` assets, so mixer routing and the headless no-op path come for free.
- Two channels: a **non-diegetic** stress tone heard only by the affected player, and a **diegetic** breathing layer that other players — and the noise system — can hear.
- Keep the non-diegetic tone out of the noise system entirely. Only the breathing is a world sound.
- Interaction with injury: [`13_health_and_injury.md`](13_health_and_injury.md) already specifies injured breathing as a noise source. Fear breathing and injury breathing must be one system with two triggers, not two overlapping loops.

**Decide what a corpse does**

- Seeing a teammate's body should spike fear. This is the cheapest, most reliable horror beat in the genre and it costs almost nothing once [`14_death_and_body_system.md`](14_death_and_body_system.md) spawns body ghosts.
- It also usefully punishes the "carry the corpse home" plan with an atmosphere cost rather than a mechanical one.

**Keep it honest about the network**

- Fear reads replicated state; it never *drives* replicated state. A client that spoofs its own fear value gains nothing — this is the reason to keep it client-side in the first place.
- The one exception is the breathing noise event, which must be raised on the server from the same inputs, or a cheating client could simply be silent. Compute the breathing trigger server-side from health and monster proximity; let the client compute only the visuals.

## Acceptance Criteria

- [ ] A stress value rises with monster proximity, being targeted, darkness, low health, isolation, and line of sight to a body, and decays when safe.
- [ ] Fear applies no movement, stamina, aim, or health penalty; if that decision is reversed it is documented explicitly here first.
- [ ] The fear overlay is visually distinct from the damage vignette, and both can be active at once without stacking into an unreadable screen.
- [ ] The overlay never obscures health, stamina, or the item bar.
- [ ] Fear intensity is scalable from 0–100% in the options menu, and 0% fully disables the overlay and the non-diegetic tone.
- [ ] The non-diegetic stress tone is audible only to the affected player.
- [ ] Panicked/injured breathing is audible to nearby players and registers in the noise system as a single unified breathing source.
- [ ] The breathing noise event is raised on the server, so a modified client cannot silence itself.
- [ ] Seeing a teammate's body triggers fear, and the trigger does not re-fire every frame the body is visible.
- [ ] Fear decays to zero in the hub, and no fear state persists across a round boundary.
- [ ] The full-screen passes are unregistered on destroy, with no leaked runtime materials after repeated deaths and round transitions.
- [ ] With fear at zero intensity there is no measurable per-frame rendering or allocation cost.
