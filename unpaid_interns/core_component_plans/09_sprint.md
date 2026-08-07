# 09 — Sprint

**Source:** [`core_components.md`](../core_components.md) §2 — Player Character
**Status:** ⚠️ Constants exist, never applied · **[MVP]**
**Depends on:** nothing
**Blocks:** Stamina, Carry Weight, noise emission, monster perception

## Summary

Move faster at a cost. Sprint is the verb that makes every other movement decision meaningful — without it, walking is free and there is no reason to ever stop.

The work here is smaller than it looks, because the data is already there and simply unused. `FirstPersonController.ControllerConsts` defines a full `Sprint` block of `StateConsts` (speed, change rate, rotation smoothing, landing multiplier, animation scale) sitting right beside `Walk`. But `GetStateConsts` assigns `consts.Walk` in **every** branch of its switch — `Standing`, `Jumping`, and `Falling` all get walk values. The sprint constants have never been read.

This plan is the first to add a new input verb, so it also establishes the input-pipeline pattern that Crouch and Interact will follow. That pipeline is documented in full here and referenced by the later plans rather than repeated.

## How to Build

**Add the input flag (the pattern for all new verbs)**

- Add `Sprint` to `PlayerInput.InputFlag` in `Assets/Scripts/Gameplay/Input/PlayerCommandInput.cs`. Note `1 << 2` is currently unused — the enum jumps from `Shoot = 1 << 1` to `Reload = 1 << 3` — so take that bit rather than extending the range.
- Add the matching `bool Sprint => (InputFlags & (uint)InputFlag.Sprint) != 0;` accessor next to the existing three.
- Wire it in `ClientInputReaderSystem.ProcessGameplayInput`: `playerInput.SetFlag(PlayerInput.InputFlag.Sprint, controls.Player.Sprint.IsPressed());`. The `Sprint` action **already exists** in the generated `InputSystem_Actions` — it has simply never been read.
- Use `IsPressed()`, not `triggered` — sprint is a held state, unlike Jump and Reload.
- `InputFlags` is part of the replicated command stream, so no serialization change is needed for the flag itself.

**Apply the constants**

- In `FirstPersonController.GetStateConsts`, select `consts.Sprint` instead of `consts.Walk` when the sprint input is held and the character is in a state that permits it. The method already receives `in PlayerInput input`, so the flag is available with no signature change.
- Decide whether sprinting is permitted while airborne. Allowing it mid-jump makes bunny-hopping the fastest way to travel, which will undermine stamina as a constraint.
- Feed the resulting speed into `state.AnimatorTargetSpeed` as the existing code does, so animation follows without special-casing.

**Respect the serialization warning**

- `ControllerState` carries an explicit comment at lines 59 and 148: *"Adding more members to this struct might break network serialisation."* Sprint needs **no new member** — it is derived from the input flag each tick. Keep it that way. Resist adding an `IsSprinting` bool; it is redundant state that must then be kept in sync.
- If a sprint-derived value must be visible to other systems, expose it as a computed property rather than stored state.

**Verify prediction holds**

- Sprint changes movement speed, which is client-predicted. Confirm that `PlayerPredictionSystem` and `ServerPlayerMovementSystem` produce the same result for the same input, or players will see position corrections every time they start sprinting.
- Test under simulated latency using the network simulator already available via `EntityDriverConstructor`.

## Acceptance Criteria

- [ ] Holding the sprint input increases movement speed to the configured `Sprint` values; releasing returns to `Walk`.
- [ ] `GetStateConsts` no longer returns `Walk` unconditionally, and the `Sprint` constants are demonstrably read.
- [ ] The sprint decision is derived from the input flag with no new member added to `ControllerState`.
- [ ] Speed changes are predicted correctly — no visible rubber-banding or position snap when starting or stopping a sprint under simulated latency.
- [ ] The airborne-sprint rule is implemented as decided and documented.
- [ ] Third-person animation on remote clients reflects sprinting, not just local first-person speed.
- [ ] Sprint speed is tunable from the controller's serialized constants without a code change.
- [ ] Two clients sprinting simultaneously show correct speed for each other.
