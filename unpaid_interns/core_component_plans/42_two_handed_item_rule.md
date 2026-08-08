# 42 — Two-Handed Item Rule

**Source:** [`core_components.md`](../core_components.md) §5 — Items, Loot & Inventory
**Status:** ❌ Not started
**Depends on:** Item Definition, Inventory, Interaction System
**Blocks:** body recovery as a real decision, ladder tension, the biggest payday being a real risk

## Summary

Some things take both hands, and while you are holding one you can barely do anything else.

This is the smallest component in §5 with the largest effect on how a round feels. A player carrying the most valuable object in the building who cannot climb, cannot open a door, cannot pick anything else up, and cannot defend themselves is the design working exactly as intended: **the payday is the vulnerability.** Everything else in the game asks the player to trade value against risk in the abstract; this one makes them feel it in their hands for two minutes.

It is also the rule that gives several other components their teeth. [`17_climbing_and_verticality.md`](17_climbing_and_verticality.md) says outright that its ladders only mean something because two-handed items block them. [`14_death_and_body_system.md`](14_death_and_body_system.md) makes a corpse a two-handed carry, which is what turns body recovery from a formality into a decision. Neither works without this.

The reference implementation ([`Assets/docs/items/item-bar.md`](../../Assets/docs/items/item-bar.md)) replaces the whole item bar with a "HANDS FULL" prompt and disables a specific enumerated list of interactions. The enumerated list is the important part — the rule has to be a published list, not an emergent side effect.

## How to Build

**Represent it once**

- The two-handed flag is a property of the item definition ([`37_item_definition_data_model.md`](37_item_definition_data_model.md)). It never varies per instance.
- The carried state is a **distinct field** on the predicted ghost, not four occupied inventory slots ([`40_inventory_item_bar.md`](40_inventory_item_bar.md)). "What am I carrying" must have one answer, and four fake slot entries is three extra places to get it wrong.
- **`HandsFull` is derived from that field.** Never a separate replicated bool. Two representations of one fact will drift, and the drift will present as a player who cannot pick anything up while apparently holding nothing.
- Weight is handled by [`12_carry_weight.md`](12_carry_weight.md) through the normal weight sum. Do not add a separate two-handed speed penalty; a heavy item is already slow, and two mechanisms tuned against the same feeling will fight.

**Publish the blocked list**

Write it down here, in this file, as data the interaction system reads — not as scattered checks:

- **Picking up anything else** — blocked. This is the core of the rule.
- **Climbing ladders** — blocked, with a prompt explaining why ([`17_climbing_and_verticality.md`](17_climbing_and_verticality.md)).
- **Operating doors, the breaker box, the terminal, and the departure control** — blocked. A player must put the prize down to open the door, and that pause is the whole mechanic.
- **Using a held tool or weapon** — blocked, since both hands are occupied. This is what makes carrying the big item genuinely defenceless.
- **Sprinting** — decide explicitly. Recommended: **allowed**, because the weight penalty already makes it slow and expensive, and removing the ability to run while holding the valuable thing during a chase is punishing past the point of interesting.
- **The scanner** — recommended allowed. It is navigation, not manipulation, and a player who cannot find the exit while carrying the payload is being punished twice.
- **Dropping it** — always allowed, always instant, always the first thing the prompt offers. A player being chased must be able to abandon the item without a menu.

Whatever is decided, each entry needs a *reason a player can infer*. A blocked list that reads as arbitrary is experienced as a bug.

**Make refusal legible**

- Replace the item bar with an unmistakable held-item display and a "hands full" state, following the reference's approach — the UI change is what teaches the rule without a tutorial.
- Every blocked interaction produces its own prompt: *"Hands full — drop the generator to open this door"*, not a silent no-op. [`41_interaction_system.md`](41_interaction_system.md) already requires distinct refusal messages; this is the component that supplies most of them.
- Show the drop prompt permanently while a two-handed item is held.

**Enforce it server-side**

- Every restriction is validated on the server, at the same entry point that validates range and liveness. A client that ignores the rule locally must simply have its requests refused.
- The pickup precondition — **is the item two-handed and are both hands free** — is already listed as a server-side validation in [`20_networked_interaction_authority.md`](20_networked_interaction_authority.md). Implement it there rather than duplicating a check here.
- A player who acquires a two-handed item while holding one-handed items is a real case: the reference allows keeping what was already in hand but blocks acquiring more. **Recommended: keep existing slot contents, block all further pickups.** It is more forgiving, it avoids surprise loot loss, and it still produces the "I cannot take that" moment.

**Handle the transitions**

- **Death** — the two-handed item drops with everything else, at the death position ([`14_death_and_body_system.md`](14_death_and_body_system.md)). This is called out as a specific test case in that plan for a reason.
- **Disconnect** — drops per the rule in [`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md), with its claim released immediately.
- **Damage** — decide whether a hit forces a drop. Recommended: **no.** Losing the payload to an unavoidable graze is the kind of loss players correctly resent, and the item is already a liability without it.
- **Banking** — depositing a two-handed item clears the state and restores the item bar. Verify the transition explicitly; it is the moment the player is most relieved and least forgiving of a bug.
- **Round end** — no two-handed state may survive into the next round.

**Carrying a body is the same code path**

- A corpse is a two-handed item ([`14_death_and_body_system.md`](14_death_and_body_system.md)). Building it as a special case means every rule above needs a second implementation, and the two will diverge.
- Verify the contested case specifically: two players grabbing the same corpse must resolve to one holder, as [`20_networked_interaction_authority.md`](20_networked_interaction_authority.md) requires.
- The body's visual carry pose should differ from an object's, but the rules must not.

**Show it to everyone**

- A teammate carrying a two-handed item must be visible as such on the third-person rig. Half the value of the rule is social: knowing that one crew member is helpless changes how the others move, and that only happens if it is visible.
- Consider a distinct movement or breathing cue, consistent with the encumbrance cue [`12_carry_weight.md`](12_carry_weight.md) already suggests, rather than a second overlapping signal.

## Acceptance Criteria

- [ ] The two-handed flag lives on the item definition and never varies per instance.
- [ ] The carried two-handed item is a distinct field on the predicted ghost, not four occupied slots.
- [ ] `HandsFull` is derived from that field and is never separately replicated.
- [ ] The blocked-interaction list is written in this file and read as data by the interaction system.
- [ ] Every blocked interaction produces a distinct prompt naming the item and the reason.
- [ ] Dropping a two-handed item is always available, instant, and prominently prompted.
- [ ] All restrictions are enforced server-side; a modified client gains nothing by ignoring them.
- [ ] Acquiring a two-handed item retains existing slot contents and blocks all further pickups.
- [ ] The item bar is replaced by a clear hands-full display while a two-handed item is held.
- [ ] Sprint and scanner permissions match the decisions recorded in this file.
- [ ] Dying drops the two-handed item at the death position with its claim released.
- [ ] Disconnecting drops it per the disconnect rule with the claim released immediately.
- [ ] The forced-drop-on-damage decision is implemented and documented here.
- [ ] Banking a two-handed item clears the state and fully restores the item bar.
- [ ] Body recovery uses this exact code path with no special-casing beyond the carry pose.
- [ ] Two players grabbing the same corpse resolve to exactly one holder with no duplication.
- [ ] A teammate carrying a two-handed item is visibly identifiable at a distance.
- [ ] No two-handed state survives a round transition.
- [ ] Carrying a two-handed item through an alternate exit or any scene transition preserves the item and its claim.
