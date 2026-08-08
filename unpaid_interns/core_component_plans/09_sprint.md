# 09 — Sprint

**Source:** [`core_components.md`](../core_components.md) §2 — Player Character
**Status:** ⚠️ Constants exist, never applied · **[MVP]**
**Depends on:** nothing
**Blocks:** Stamina, Carry Weight, noise emission, monster perception, every later input verb

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

**Reserve the bits — this file is the registry**

Several later components add verbs, and a silently duplicated bit produces two verbs that fire together with no compile error. Claim bits here and keep this table current:

| Bit | Flag | Semantics | Component |
| --- | --- | --- | --- |
| `1 << 0` | `Jump` | edge | exists |
| `1 << 1` | `Shoot` | held | exists — also carries "use held item" ([`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md)) |
| `1 << 2` | `Sprint` | held | this component |
| `1 << 3` | `Reload` | edge | exists |
| `1 << 4` | `Crouch` | edge or held | [`10_crouch.md`](10_crouch.md) |
| `1 << 5` | `Interact` | held, press edge derived server-side | [`41_interaction_system.md`](41_interaction_system.md) |
| `1 << 6` | `Drop` | edge, tick-stamped | [`40_inventory_item_bar.md`](40_inventory_item_bar.md) |
| `1 << 7` | `Scan` | edge, cooldown-gated | [`16_player_scanner_ping_tool.md`](16_player_scanner_ping_tool.md) |

**Not everything belongs in this table.** A verb whose input is a *value* rather than an on/off state must be a field on `PlayerInput`, not a bit — because a bit can only accumulate, and a value can be replayed idempotently. The known case is inventory slot selection, which [`40_inventory_item_bar.md`](40_inventory_item_bar.md) sends as an **absolute `SelectedSlot` index** precisely so that a replayed tick reselects the same slot instead of scrolling again. Adding a "next slot" bit would be the natural-looking mistake and would break under reconciliation. Any future verb that means "change by an amount" has the same problem and the same fix.

**Understand how the input stream accumulates — every later verb depends on this**

The pipeline is **OR-accumulating and biased toward "on"**, and nothing in the code says so. `PlayerInput.UpdateFrom` is `InputFlags |= input.InputFlags` — it sets bits and never clears them. `ClientInputSenderSystem` uses that in two places: on a new tick it does `SetFrom(current)` then `UpdateFrom(inProgressCommandInput)` to fold in flags raised during intervening client frames, and within an already-sent tick it does `existingCommandData.UpdateFrom(current)` to refresh the tick's data.

Three consequences that shape every verb built on this:

- **A held flag releases up to one tick late.** Within a tick, a bit can turn on but never off. Sprint stopping one tick after the key is released is acceptable; a verb where a single extra tick matters is not, and must be designed around it.
- **A flag raised in any client frame belonging to a tick applies to the whole tick.** A key tapped and released between two ticks still registers. This is what makes `triggered` work for Jump and Reload, and it is deliberate.
- **Predicted input is replayed.** `PlayerPredictionSystem` re-simulates buffered ticks during reconciliation, so a tick's flags are processed *more than once* on the client. Anything with a side effect beyond movement — interact, drop, scan, a crouch toggle — must therefore be **idempotent per tick**, keyed on the tick it happened, not "do the thing when the flag is set". The existing code already solves this shape with server-authoritative tick stamps (`LastShotTick`, `LastJumpTick`, `LastReloadTick`) compared against a locally cached tick in `HandleAnimationEvents`; reuse that approach rather than inventing a new one.

Sprint itself is immune to all three — it is a continuous state read fresh each tick with no side effect. That is exactly why it is the right verb to establish the pattern with, and why the components that follow must not assume their verb is as forgiving.

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
- [ ] Releasing sprint stops the speed increase within one tick and never leaves the flag latched across ticks.
- [ ] The bit-allocation table in this file matches `PlayerInput.InputFlag` and contains no duplicate bits.
