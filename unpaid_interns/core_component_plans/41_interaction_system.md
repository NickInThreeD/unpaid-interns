# 41 — Interaction System

**Source:** [`core_components.md`](../core_components.md) §5 — Items, Loot & Inventory
**Status:** ❌ Not started · **[MVP]**
**Depends on:** Sprint (input-verb pattern), Item Ghost
**Blocks:** Inventory, Loot Banking, Door System, Climbing, Breaker Box, Terminal, body recovery — every verb in the game that is not movement

## Summary

Look at something and press E. It is the most-used input in the game and currently there is no such thing.

`PlayerInput.InputFlag` carries only `Jump`, `Shoot` and `Reload`, and `ClientInputReaderSystem.ProcessGameplayInput` wires exactly those three. The **binding already exists** — `Interact` is in the `Player` action map of the generated `InputSystem_Actions`, bound to `<Keyboard>/e` and `<Gamepad>/buttonNorth` — and reaches no gameplay code whatsoever.

This component is small in code and load-bearing in design. Every interactable thing in the game funnels through it: items, doors, ladders, the breaker box, the departure control, the terminal, corpses. If targeting is inconsistent or the prompt lies about what will happen, every one of those feels bad at once. And it is the only component in §5 that runs on every frame regardless of what the player is doing, so its cost matters.

## How to Build

**Add the verb, following the established pattern**

- Add `Interact` to `PlayerInput.InputFlag` at bit `1 << 5`, reserved for it in the allocation table in [`09_sprint.md`](09_sprint.md). Add the accessor alongside the existing three.
- Wire it in `ClientInputReaderSystem.ProcessGameplayInput` with `controls.Player.Interact.triggered` — a press, not a held state, unlike Sprint.
- Hold-to-interact for slow actions (the breaker box, a long deposit, a body pickup) needs a *held* reading as well. Either add a second `InteractHold` bit or read `IsPressed()` into the same bit and let the receiver distinguish press from hold by tick continuity. **Recommended: one bit, held semantics**, with the press edge derived server-side from the first tick the bit is set — one bit is cheaper and the edge is derivable, whereas two bits can disagree.

**Make it idempotent per tick — this is the part that breaks**

- The input stream OR-accumulates and `PlayerPredictionSystem` **replays buffered ticks during reconciliation** ([`09_sprint.md`](09_sprint.md)). An interact handler written as "if the flag is set, do the thing" will pick an item up several times for one keypress.
- Interaction has side effects beyond movement, so it must be keyed on **the tick it happened**, not on the flag being set. Add a `LastInteractTick` to `PredictedPlayerGhost` and compare against a cached tick, exactly as `FirstPersonController.HandleAnimationEvents` already does with `LastShotTick`, `LastJumpTick` and `LastReloadTick`. Reuse that shape; do not invent a new one.
- The server applies each interact request **once per tick per player**, and a duplicate request for a tick already processed is discarded silently.

**Raycast on the client, validate on the server**

- The client raycasts from the camera each frame to find the current target, for the prompt. That is presentation and needs no round trip.
- On press, the client sends the interact flag **plus the target's ghost id** on the input command stream — not as a fire-and-forget RPC. [`20_networked_interaction_authority.md`](20_networked_interaction_authority.md) requires this: the command stream is already tick-stamped and replayed, which is exactly what tick-ordered contention resolution needs.
- The server re-validates everything: does the target exist, is it in range, is there line of sight, is the requester alive and in a phase that permits interaction, does the interaction's own precondition hold. **Distance must be validated server-side** — without it a modified client interacts across the map, and that is the single most important check in the component.
- Do not send world-space hit positions for the server to trust. Send the target id and let the server compute geometry from its own state.

**Get the layer mask right**

- Gameplay physics is built-in PhysX, and in a host process the server and client worlds each instantiate their own copy of every ghost GameObject into the same physics scene. `PlayerGhost` already handles this for players by assigning `LayerIndex.ServerPlayer` or `ClientPlayer` by role, and `PlayerPredictionSystem` masks its client-side hitscan with `~LayerMask.GetMask("ClientPlayer")` and `~LayerMask.GetMask("ClientPlayer", "ServerPlayer")` for exactly this reason.
- The client's interaction raycast must therefore mask to **client-role interactables only**, using the `ServerItem` / `ClientItem` layer split introduced in [`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md), and the server's validation must consider server-role colliders only.
- Get this wrong and the raycast returns whichever duplicate PhysX happened to order first — which works on a dedicated server, fails intermittently on a host, and reproduces for nobody.
- Use `QueryTriggerInteraction` deliberately. Some interactables are trigger volumes (a ladder, the deposit surface) and some are solid; the existing movement and projectile code passes `QueryTriggerInteraction.Ignore` and this system usually must not.

**Define targeting precisely**

- One raycast per frame from the camera, non-allocating (`RaycastNonAlloc`), with a configured maximum range and a small sphere radius so small objects are not pixel-hunts. This is the only per-frame cost in the component and it must stay at one query.
- **Targeting priority must be defined once, here.** [`17_climbing_and_verticality.md`](17_climbing_and_verticality.md) already flags that the interact verb is shared between "climb this" and "pick that up", and the same collision arises with a door in front of an item and a body on the floor. Publish an explicit priority order and let every interactable declare its priority as data.
- Recommended order: held-item context first, then the nearest interactable by hit distance, breaking ties by declared priority. Distance is what the player believes they are pointing at.
- Add hysteresis on target changes so a target does not flicker between two overlapping objects when the crosshair is still. A prompt that strobes is unusable.

**Build the prompt as part of the system, not as UI decoration**

- The prompt is the affordance. It must state the verb, the object, and — where relevant — the cost: *"Pick up · Brass Bell · 12 lb"*, *"Hands full"*, *"Locked"*, *"Hold to restore power"*.
- A refusal must be **explained**, never silent. "Hands full" and "no headroom" and "locked" are three different failures and a player who cannot tell them apart will report the game as broken. This is the Interaction Prompts component in §9 and it should be built with this one rather than after it.
- Show a hold progress indicator for held interactions, and cancel cleanly if the player releases, is damaged, or moves out of range.
- Prompts are per-client presentation and never replicated.

**Cover the interaction types from the start**

- **Pick up** — the common case ([`40_inventory_item_bar.md`](40_inventory_item_bar.md)).
- **Use held item** — a distinct verb from interacting with the world, and it should map to the existing `Shoot` action rather than to `Interact`, so a weapon is simply a tool whose use fires ([`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md)). Deciding this here prevents two parallel activation paths.
- **Open / close / operate** — doors, the breaker box, the departure control. Absolute state, never toggle commands ([`20_networked_interaction_authority.md`](20_networked_interaction_authority.md)).
- **Climb** — attach to a ladder, requiring an explicit interact rather than proximity ([`17_climbing_and_verticality.md`](17_climbing_and_verticality.md)).
- **Carry a body** — a two-handed pickup that inherits the whole path ([`14_death_and_body_system.md`](14_death_and_body_system.md)).
- Define one interface all of them implement, so a new interactable is a component and a prompt string rather than a change to this system.

**Restrict it where it must be restricted**

- Spectators cannot interact at all ([`22_spectator_mode.md`](22_spectator_mode.md)).
- Interaction is blocked while holding a two-handed item, except for dropping it ([`42_two_handed_item_rule.md`](42_two_handed_item_rule.md)).
- Interaction is blocked during `Settling` — a bank arriving mid-settlement corrupts the total, which [`02_day_cycle_controller.md`](02_day_cycle_controller.md) already requires.
- Log every server-side rejection with the reason. "Nothing happened when I pressed E" is otherwise unanswerable.

## Acceptance Criteria

- [ ] `Interact` exists in `PlayerInput.InputFlag` at the reserved bit and is read in `ClientInputReaderSystem.ProcessGameplayInput`.
- [ ] One keypress produces exactly one interaction, even when the tick is replayed during reconciliation.
- [ ] The interaction is tick-stamped on `PredictedPlayerGhost` and the server discards duplicate requests for an already-processed tick.
- [ ] The request travels on the input command stream with a target ghost id, not as a standalone RPC.
- [ ] The server validates existence, range, line of sight, liveness, phase, and the interaction's own precondition on every request.
- [ ] A forged request from a modified client cannot interact beyond the configured range, verified by sending one.
- [ ] The client raycast masks to client-role colliders and the server validates against server-role colliders; behaviour on a host matches a dedicated server exactly.
- [ ] Trigger-volume interactables (ladders, deposit surfaces) are targetable, and solid interactables are not shadowed by them.
- [ ] Exactly one non-allocating raycast runs per frame regardless of scene contents.
- [ ] Targeting priority is defined in one place, declared as data per interactable, and produces a stable target with no flicker.
- [ ] The prompt names the verb and the object, and shows weight or cost where relevant.
- [ ] Every refusal is explained with a distinct message — hands full, locked, no headroom, out of range, wrong phase.
- [ ] Held interactions show progress and cancel cleanly on release, damage, or moving out of range.
- [ ] Using a held item is a separate verb from world interaction, and a weapon uses the same activation path as any other tool.
- [ ] Doors and other shared interactables use absolute state; two players interacting on the same tick converge on one result.
- [ ] Spectators cannot interact.
- [ ] Interaction is blocked during settlement.
- [ ] A new interactable can be added by implementing the interface and supplying a prompt, with no change to this system.
- [ ] Every server-side rejection is logged with a reason.
- [ ] Interaction is responsive under simulated latency, with the uncontended case feeling immediate.
