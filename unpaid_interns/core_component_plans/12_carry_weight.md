# 12 — Carry Weight

**Source:** [`core_components.md`](../core_components.md) §2 — Player Character
**Status:** ❌ Not started · **[MVP]**
**Depends on:** Stamina, Inventory, Item Definition
**Blocks:** meaningful loot prioritization, monster interest targeting

## Summary

Total held weight slows you down and drains stamina faster. This is what turns loot from a number into a decision.

Without it, the optimal play is always to fill every slot with the highest-value items and walk out. With it, a heavy low-value item is actively bad — it costs speed and stamina you may need to escape — and *value per pound* becomes the real currency of the game. That single derived statistic is what makes one player's haul smarter than another's.

It is a small component that depends on two larger ones. It cannot be finished before items have weights and the inventory can report what is held, but the movement and stamina hooks can be built and tested against a stubbed weight value first.

## How to Build

**Compute the total**

- Sum the weight of everything held: inventory slots plus any two-handed item.
- Recompute on inventory change, not per frame. Weight changes only when something is picked up or dropped.
- Store the total on `PredictedPlayerGhost` as a `[GhostField]`, alongside stamina. The server needs it for authoritative movement, and monster interest targeting will read it later.
- Until the inventory exists, drive it from a debug value so movement and stamina tuning can begin immediately.

**Apply the effects**

- **Stamina drain multiplier** — the primary effect. A simple normalized factor derived from total weight, multiplied into the sprint drain rate from [`11_stamina.md`](11_stamina.md).
- **Movement speed penalty** — apply in `FirstPersonController.GetStateConsts` or as a scale on `state.MovementSpeed` in `AccumulateMovement`. The existing `combinedMoveSpeedModifier` local in `AccumulateMovement` is currently hardcoded to `1f` and is exactly the hook this needs.
- Keep both effects continuous rather than stepped. Threshold-based penalties encourage players to sit precisely under a breakpoint, which is fiddly rather than interesting.
- Consider a maximum carry weight beyond which movement is severely penalized, so there is a natural ceiling without a hard block.

**Keep it predicted**

- Weight affects movement speed, which is predicted. The value must be identical on client and server at the same tick, or every pickup will cause a correction.
- Because weight changes only on inventory events, and those are server-authoritative, ensure the client's predicted pickup updates weight at the same tick the server does. [`20_networked_interaction_authority.md`](20_networked_interaction_authority.md) is where that tick alignment is actually enforced — a pickup predicted on one tick and granted on another produces a movement correction on every single pickup, which is the most frequent action in the game.
- A *contested* pickup that the client loses must roll the weight change back as cleanly as it applied it. Test this specifically; it is the case that will be missed.

**Surface it**

- Show total carried weight, or at least a coarse encumbrance indicator, in the HUD. Players cannot make value-per-pound decisions against a hidden number.
- Show item weight when looking at an item, so the decision happens **before** picking it up rather than after.
- Consider a distinct movement or breathing cue when heavily loaded — it communicates the cost without a number.

## Acceptance Criteria

- [ ] Total carried weight is computed from all held items and updates on pickup and drop, not per frame.
- [ ] Weight is replicated and identical on client and server at the same tick.
- [ ] Higher weight measurably increases stamina drain while sprinting.
- [ ] Higher weight measurably reduces movement speed.
- [ ] Both effects scale continuously, with no threshold cliff players can sit beneath.
- [ ] Picking up an item causes no position correction or rubber-band under simulated latency.
- [ ] Dropping items immediately restores speed and drain rate.
- [ ] A maximum carry weight is enforced and behaves sensibly at the limit.
- [ ] The HUD shows carried weight or a clear encumbrance state.
- [ ] Item weight is visible before pickup.
- [ ] Weight resets correctly on death, on dropping everything, and between rounds.
- [ ] The system works against a debug weight value before the inventory exists.
