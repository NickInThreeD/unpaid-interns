# 73 — Interaction Prompts

**Source:** [`core_components.md`](../core_components.md) §9 — UI & Feedback
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Interaction System](41_interaction_system.md), [HUD](71_hud.md), [Item Definition](37_item_definition_data_model.md)
**Blocks:** every verb in the game being discoverable

## Summary

Telling the player what pressing E will do, and why it did not.

This is the smallest component in §9 and it is the one that decides whether the rest of the game is legible. Every verb in `GAME_DESIGN.md`'s core loop passes through it — picking up, opening, climbing, banking, departing — and none of them has any affordance without it. A player looking at an object with no prompt does not know it is interactable; a player whose interaction silently fails does not know whether the game is broken or they are.

`core_components.md` asks for contextual prompts *"with clear affordances, including a hands-full state"*, and that second clause is the real requirement. **The refusals matter more than the offers.** A prompt that says "Pick up" is doing an easy job. A prompt that distinguishes *hands full* from *locked* from *no headroom* from *out of range* is what stops four different failures from feeling like one bug.

[`41_interaction_system.md`](41_interaction_system.md) already requires that "every refusal is explained with a distinct message". This component is where those messages are designed and where the space for them is reserved.

## How to Build

**Drive it from the interaction system's target, not from a second query**

- [`41_interaction_system.md`](41_interaction_system.md) runs exactly one non-allocating raycast per frame and produces a current target with stable hysteresis. The prompt reads that result. It must not raycast on its own — a second query doubles the cost and can disagree with the first, producing a prompt for an object the interact key will not act on.
- Each interactable declares its prompt data — verb, display name, and any cost — as part of the interface component 41 defines. Adding a new interactable should mean implementing that interface, not editing this component.
- Prompts are **per-client presentation** and never replicated.

**Say the verb, the thing, and the cost**

- Three parts: *"Pick up · Brass Bell · 12 lb"*. The verb tells them what happens, the name tells them what it is in the dark, and the cost is what makes the decision before the action rather than after it.
- Weight is required to be visible **before** pickup by [`12_carry_weight.md`](12_carry_weight.md), because value-per-pound decisions happen at the moment of looking, not the moment of carrying. Value follows the scanner's rules — if [`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md) has not replicated a rolled value to this client, the prompt must not invent one.
- For held interactions, show a progress indicator, and cancel it visibly on release, damage, or moving out of range — component 41 requires the behaviour and this is where it is drawn.

**Design the refusals as a set**

Write them once, as a list, so they are consistent and so no failure is left silent:

- **Hands full** — the item bar is full, or a two-handed item is held. [`42_two_handed_item_rule.md`](42_two_handed_item_rule.md) requires each blocked interaction to name the item and the reason: *"Hands full — drop the generator to open this door"*. That specificity is what teaches the rule.
- **Locked** — a door needing a key ([`60_door_system.md`](60_door_system.md)), with the key named if the crew has one.
- **No headroom** — standing blocked while crouched ([`10_crouch.md`](10_crouch.md)).
- **Out of range** — the client's raycast reached it but the server's distance check would not. Rare, and worth its own message because it is the one that looks most like a bug.
- **Not now** — wrong phase: interaction is blocked during settlement ([`02_day_cycle_controller.md`](02_day_cycle_controller.md) holds banking and reconnection during `Settling`), and spectators cannot interact at all ([`22_spectator_mode.md`](22_spectator_mode.md)).
- **Already held by someone else** — a contested item, which [`20_networked_interaction_authority.md`](20_networked_interaction_authority.md) requires to fail *visibly but not punishingly*.

A refusal must appear **at the moment of looking** where the condition is knowable in advance — hands full is knowable, so show it before the player presses anything. A refusal only discoverable on the server appears as a rejection cue after the press.

**Treat the departure control as a special case**

- [`31_entry_point_extraction_zone.md`](31_entry_point_extraction_zone.md) requires the departure control to be hard to trigger by accident, because a fleeing player who reflexively presses interact and ends the round for everyone will not be forgiven.
- Its prompt must state the consequence in full — *"Signal departure — ends the round for the entire crew"* — and it should require a hold rather than a press. This is the one prompt in the game where friction is the feature.

**Fit it into the HUD without a fight**

- The prompt lives in a reserved central region ([`71_hud.md`](71_hud.md)). Interaction prompts, hands-full warnings, and scan results all want that space and will collide there without allocation.
- Only one prompt at a time. Component 41's targeting priority already picks a single target; the prompt must not try to list alternatives.
- Fade in and out rather than popping, and use the same hysteresis as the target so the prompt does not strobe between two overlapping objects.
- No per-frame allocation. A prompt that rebuilds a `Label` every frame while a player sweeps a room full of loot is a measurable cost.

**Make it usable by everyone**

- §9 elevates accessibility to required. Prompts are text, so they need scalable size and adequate contrast, and they must not rely on colour to distinguish an offer from a refusal — a refusal should read as a refusal in monochrome.
- Show the **bound key**, not a hardcoded "E". The Input System supports rebinding and [`78_settings_options_menu.md`](78_settings_options_menu.md) will expose it; a prompt that lies about the binding is worse than no prompt.
- Localisation is deferred (§13) but prompts are the densest string surface in the game — keep them in one table from the start rather than inline, so deferring stays cheap.

## Acceptance Criteria

- [ ] Prompts read the interaction system's current target and perform no raycast of their own.
- [ ] Each interactable supplies its prompt data through the interaction interface; adding one requires no change to this component.
- [ ] Prompts show verb, object name, and relevant cost.
- [ ] Item weight is visible before pickup.
- [ ] Item value appears only where the client legitimately has it, and is never fabricated.
- [ ] Held interactions show progress and cancel visibly on release, damage, or leaving range.
- [ ] Every refusal condition has a distinct, specific message, and the full set is enumerated in this file.
- [ ] Refusals knowable in advance appear at the moment of looking, not only after a press.
- [ ] A hands-full refusal names the held item and the blocked action.
- [ ] A contested pickup failure shows a clear, non-punishing rejection cue.
- [ ] Spectators and wrong-phase interactions produce an explanatory prompt rather than silence.
- [ ] The departure control states that it ends the round for the whole crew and requires a hold.
- [ ] Exactly one prompt is shown at a time, in its reserved HUD region, never overlapping other elements.
- [ ] Prompts fade rather than pop and do not strobe between overlapping targets.
- [ ] No per-frame allocation occurs while sweeping a room full of interactables.
- [ ] Prompt text is scalable, high-contrast, and distinguishes offers from refusals without colour.
- [ ] Prompts display the currently bound key, including after a rebind.
- [ ] All prompt strings live in one table rather than inline in gameplay code.
