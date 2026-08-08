# 10 — Crouch

**Source:** [`core_components.md`](../core_components.md) §2 — Player Character
**Status:** ❌ Not started · **[MVP]**
**Depends on:** Sprint (establishes the input-verb pattern)
**Blocks:** monster perception / visibility, stealth counterplay

## Summary

Move slower, become harder to see, fit through low gaps. Crouch is the primary stealth verb and the main way a player exercises agency against sight-based monsters — the difference between hiding as a mechanic and hiding as a hope.

Unlike Sprint, nothing exists for this. `MovementType` has only `Standing`, `Jumping`, and `Falling`. There is no crouched state, no collider adjustment, and no visibility concept for anything to read.

That last point is the one that matters most. Crouch is only worth building if something *consumes* it. The visibility value this component produces is the contract the Perception System will read later — get its shape right now, or every monster will need retrofitting.

## How to Build

**Add the input**

- Follow the input-flag pattern documented in [`09_sprint.md`](09_sprint.md): add `Crouch` to `PlayerInput.InputFlag` at the bit reserved for it in that file's allocation table, add the accessor, and read `controls.Player.Crouch` in `ClientInputReaderSystem.ProcessGameplayInput`. The `Crouch` action already exists in `InputSystem_Actions`.
- Decide hold-to-crouch versus toggle. Toggle is kinder during long stealth sequences and is the better default for a game where crouching may be sustained for minutes; offer both in settings if possible.
- **If toggle is chosen, do not implement it as "flip a bool when the flag is set".** As documented in [`09_sprint.md`](09_sprint.md), the prediction system replays buffered ticks during reconciliation, so a naive toggle flips several times for one keypress and the player ends up crouched or standing at random. Store the crouch *state* on the predicted ghost and derive it from the tick the press occurred on, so replaying the same tick produces the same result. Hold-to-crouch has none of this problem, which is a legitimate argument for it.
- Whichever is chosen, the release-lags-one-tick behaviour of the input stream applies. Standing up one tick late is harmless; make sure nothing (headroom check, visibility value) assumes the transition is instantaneous.

**Add the movement state**

- Add `Crouching` to `FirstPersonController.MovementType`. Check every `switch` over `MovementType` — `AccumulateJumpAndGravity`, `AccumulateMovement`, and `GetStateConsts` all have `default` branches that log errors on unhandled states, so an incomplete addition will spam the console rather than fail silently.
- Add a `Crouch` block to `ControllerConsts.StateConsts` beside `Walk` and `Sprint`, and select it in `GetStateConsts`.
- Make crouch and sprint mutually exclusive, resolving the conflict in one place rather than letting both apply.

**Adjust the collider**

- Reduce `CharacterController.height` and re-centre it when crouched, restoring on stand.
- **Block standing when there is no headroom** — sweep or overlap-test above the character before restoring height, or players will clip through geometry by standing under a low ceiling.
- Note that `UpdateGround` uses `m_Controller.radius` and `GetGroundRaycastOrigin` derives from `controller.bounds.center`; both must remain correct at reduced height. Ground detection breaking while crouched is the likely first bug.

**Expose visibility — the important part**

- Produce a normalized visibility value that perception systems will consume: full when standing and moving, reduced when crouched, reduced further when crouched and stationary.
- Put this on the predicted ghost state so the server — which runs monster AI — can read it authoritatively. A client-only visibility value is trivially cheatable.
- Mind the `ControllerState` serialization warning at lines 59 and 148. Prefer deriving visibility from existing replicated state (movement type plus speed) over adding a new replicated field.
- Define and document the value's range and meaning now, before any monster reads it. [`53_perception_system.md`](53_perception_system.md) is the consumer and specifies the shape it expects: a base value reduced by crouching, reduced further by a short stationary dwell, with a floor above zero — plus two terms this component does not own, ambient light level and whether the player is carrying an active light source. Produce the crouch and stillness terms here and let perception compose the rest; do not build a second visibility value there.

**Presentation**

- Lower the camera smoothly rather than snapping — the transition is felt constantly and a hard cut is jarring.
- Add a crouched third-person animation state so remote players can see a crouching teammate.
- Reduce or silence footstep noise while crouched, and make sure that reduction reaches the noise-emission system rather than only the audio mixer.

## Acceptance Criteria

- [ ] Crouching reduces movement speed to the configured values and restores on standing.
- [ ] `Crouching` is handled in every `MovementType` switch, with no `default`-branch error logs during normal play.
- [ ] The character controller's height and centre adjust correctly, and ground detection continues to work while crouched.
- [ ] Standing is blocked when there is insufficient headroom; the character never clips through a ceiling.
- [ ] Crouch and sprint cannot both be active; the conflict resolves consistently.
- [ ] A normalized visibility value is exposed on server-readable state, with its range and meaning documented.
- [ ] Visibility is lower crouched than standing, and lower still when crouched and stationary.
- [ ] Remote clients see a crouching teammate in the correct pose.
- [ ] Camera height transitions smoothly in both directions.
- [ ] Crouched movement produces reduced noise in the noise system, not merely quieter audio playback.
- [ ] Crouch state predicts correctly with no position correction under simulated latency.
- [ ] If toggle is used, one keypress produces exactly one state change even when the tick is replayed during reconciliation.
- [ ] The visibility value consumed by monster perception is derived from the same crouch state the server holds, not from a client-local flag.
