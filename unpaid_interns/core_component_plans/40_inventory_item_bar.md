# 40 — Inventory / Item Bar

**Source:** [`core_components.md`](../core_components.md) §5 — Items, Loot & Inventory
**Status:** ❌ Not started · **[MVP]**
**Depends on:** Item Definition, Item Ghost, Interaction System
**Blocks:** Carry Weight, Two-Handed Item Rule, Loot Banking, Death & Body System, Tool & Equipment Items

## Summary

A hard, small number of things you can carry at once.

The limit is the mechanic. Everything the design calls tension — repeated trips, leaving loot behind, choosing between the heavy valuable thing and two light ones — comes from the fact that a player cannot simply take everything. Four slots is the genre default and a good starting point; the exact number matters far less than the fact that it is small and never negotiable.

The inventory also has to be **client-predicted**, and that is where the difficulty lives. Picking up is the most frequent action in the game. A pickup that waits a round trip feels broken, and a pickup that mispredicts corrects the player's *carry weight*, which corrects their *movement speed*, which produces a visible position snap. [`12_carry_weight.md`](12_carry_weight.md) already flags this as the case that will be missed; this component is where the tick alignment has to actually hold.

## How to Build

**Put slot state on the predicted ghost**

- Slots live on `PredictedPlayerGhost` in `Assets/Scripts/GhostBridge/Player/PredictionComponents.cs`, beside `CurrentHealth` and `EquippedWeaponID`, marked `[GhostField]`. That struct already holds gameplay state and is the established home — prefer it over `ControllerState`, which carries the explicit *"adding more members might break network serialisation"* warning at lines 59 and 148.
- Per slot: the item's **ghost id** (so the world object can be found) and its **item id** (so the definition resolves without a lookup through the ghost). Rolled value is fine to replicate for items this player holds — the restriction in [`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md) is about items the player has *not* earned the information for.
- Use a **fixed-size** representation — four explicit fields or a `FixedList` — not a dynamic buffer. Predicted ghost buffers are heavier to replicate and to roll back, and the slot count is a constant by design.
- Also on the ghost: the **selected slot index**, and the total carried weight that [`12_carry_weight.md`](12_carry_weight.md) specifies. Both are read by prediction, so both must be predicted state.

**Make selection idempotent — this is the trap**

- The input stream OR-accumulates and prediction **replays buffered ticks during reconciliation**, as documented in [`09_sprint.md`](09_sprint.md). A "scroll to next slot" delta applied on a replayed tick advances the selection twice, and the player ends up holding something other than what they picked.
- **Send the absolute desired slot index in the command, not a delta.** `ClientCommandInput` carries a `PlayerInput` struct that already holds `MoveInput` and `LookYawPitchDegrees` as values; a `SelectedSlot` byte belongs there beside them. Replaying a tick that says "slot 2" produces slot 2 every time.
- The client computes the absolute index locally from the scroll or number-key input and sends the result. The server validates the range and applies it.
- Bindings exist but are incomplete: the `Player` action map already has `Previous` and `Next` actions, bound to `<Keyboard>/1` and `<Gamepad>/dpad/left` and their counterparts — but **no mouse-wheel binding**. Scroll-select is the expected control for this genre and must be added to `InputSystem_Actions`, along with direct number-key selection per slot.

**Define the operations**

- **Pick up** — first free slot, or refuse. Refusal must be legible: a "hands full" prompt on the interaction UI ([`41_interaction_system.md`](41_interaction_system.md)), never a silent no-op.
- **Drop** — drops the selected slot at a server-validated position in front of the player. `Drop` has bit `1 << 6` reserved in the allocation table in [`09_sprint.md`](09_sprint.md).
- **Drop** must be **idempotent per tick** for the same reason selection is: stamp it with the tick it happened on and compare against a cached tick, the pattern `FirstPersonController.HandleAnimationEvents` already uses with `LastShotTick` and `LastReloadTick`. A drop that replays three times during reconciliation drops three items.
- **Swap** — picking up while full is a design choice. Recommended: **refuse rather than swap.** An accidental swap that drops a 400-credit item to pick up a 20-credit one is an unrecoverable mistake made in a fraction of a second, and it will happen during a chase.
- **Drop all** — on death ([`14_death_and_body_system.md`](14_death_and_body_system.md)) and on the disconnect rule ([`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md)).

**Keep the two representations consistent**

- An item exists in two places: as a slot entry on the player and as a ghost in the world with `HeldByNetworkId` set ([`20_networked_interaction_authority.md`](20_networked_interaction_authority.md)). These must never disagree.
- **The item ghost's holder field is the authority.** The slot array is a convenience index into it, rebuildable from the world state. Where they conflict, the ghost wins — and a development-build assertion should fire when they do.
- The single worst failure this component can produce is **duplication**: an item in a slot and also lying in the world is money created from nothing. Make every transition write both sides in one server operation, and add a periodic development-only audit that no ghost id appears in two slots and no held item lacks a slot.

**Honour the two-handed rule without duplicating it**

- A two-handed item takes the whole bar, not a slot ([`42_two_handed_item_rule.md`](42_two_handed_item_rule.md)). Represent it as a distinct field rather than as four occupied slots, so "what am I carrying" has one answer.
- The `HandsFull` state is **derived** from that field, never a separate replicated bool. Two representations of one fact drift.

**Predict it correctly**

- On sending a pickup request the client immediately fills the slot and updates carry weight, then accepts the server's correction. The uncontended case is overwhelmingly the majority ([`20_networked_interaction_authority.md`](20_networked_interaction_authority.md)).
- The weight change must land on **the same tick** on client and server, or every pickup produces a movement correction. This is the acceptance criterion that matters most in this file.
- A lost contest must roll the slot and the weight back as cleanly as they were applied, and show a clear rejection cue — the item snapping back to the floor with a sound.

**Surface it**

- Add the item bar to `Assets/UI Toolkit/GameUI/PlayerHUD.uxml` and drive it from `InGameHUD.cs`, which already queries `PredictedPlayerGhost` through an `EntityQuery` on `GhostOwnerIsLocal` — the same pattern extends directly and should not allocate per frame.
- Show per-slot icon, weight, and — once known — value; show total carried weight, which [`12_carry_weight.md`](12_carry_weight.md) requires; show the selected slot unambiguously.
- Show the held item in first person, and on the third-person rig so teammates can see what someone is carrying. A crew that can see who has the heavy item can make decisions about each other.
- Accessibility (§9): slot state must be readable without relying on colour alone.

## Acceptance Criteria

- [ ] Slot state, selected slot, and carried weight live on `PredictedPlayerGhost` as fixed-size replicated fields.
- [ ] The slot count is a single configured constant and is enforced on the server.
- [ ] Slot selection is sent as an absolute index; replaying a tick during reconciliation never changes the selection.
- [ ] Mouse-wheel and number-key slot selection are bound in `InputSystem_Actions`.
- [ ] Dropping is tick-stamped and idempotent; a replayed tick never drops more than one item.
- [ ] Picking up with a full inventory is refused with a legible prompt and drops nothing.
- [ ] The item ghost's holder field and the player's slot array never disagree; a development assertion fires if they do.
- [ ] No sequence of pickup, drop, death, disconnect, or contested grab produces a duplicated item.
- [ ] A periodic development audit confirms no ghost id occupies two slots and no held item lacks a slot.
- [ ] A two-handed item occupies the whole bar as a distinct field, and `HandsFull` is derived rather than replicated separately.
- [ ] Pickup feels immediate on the acquiring client, with no round-trip delay in the uncontended case.
- [ ] Carry weight changes on the same tick on client and server; no pickup produces a position correction under simulated latency.
- [ ] A lost contest rolls back the slot and the weight cleanly and plays a clear rejection cue.
- [ ] Dying drops every carried item, including a two-handed item, at the death position.
- [ ] The item bar renders in `PlayerHUD.uxml` with icon, weight, value where known, total weight, and an unambiguous selection indicator, allocating nothing per frame.
- [ ] The held item is visible in first person and on the third-person rig to other players.
- [ ] Slot state is readable without relying on colour alone.
- [ ] Inventory is empty at the start of every round and no slot state survives a round transition.
