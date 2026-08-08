# 43 — Loot Banking / Deposit

**Source:** [`core_components.md`](../core_components.md) §5 — Items, Loot & Inventory
**Status:** ❌ Not started · **[MVP]**
**Depends on:** Item Ghost, Entry Point / Extraction Zone, Inventory, Networked Interaction Authority
**Blocks:** Selling / Payout, Quota progress, End-of-Round Summary, the entire "when do we leave" decision

## Summary

The moment loot stops being a liability and becomes money.

`GAME_DESIGN.md` states the rule in one line — *"they don't count until they're back"* — and every tension in the game hangs off it. A player holding 400 credits of scrap deep in a building is holding nothing; the same scrap on the extraction pad is quota progress. That gap is what makes the return trip matter, what makes dying expensive, and what makes the decision to go back in for one more armful the central act of the game.

This component is small, and it converts objects into the currency that decides whether the crew lives. That makes it the place where correctness matters most in the whole of §5. **An item banked twice is money created from nothing; an item banked and then lost is a run destroyed by a bug the crew will never diagnose.** Exactly-once is not a nice property here, it is the requirement.

**Scope boundary:** [`31_entry_point_extraction_zone.md`](31_entry_point_extraction_zone.md) owns the volume, its placement, and the departure control. This component owns what being inside it means. Conversion of banked value into credits at settlement belongs to [`02_day_cycle_controller.md`](02_day_cycle_controller.md) and the Selling component in §8.

## How to Build

**Bank on rest, not on trigger enter**

- The condition is: **a dropped, unheld item whose resting position is inside the extraction volume is banked.** Not "an item that crossed the boundary", and not "an item a player was holding when they stood in the zone".
- Trigger enter/exit events are the wrong foundation, for three reasons the zone plan already anticipates: an item spawned inside the volume fires no enter event, an item resting on the boundary fires enter and exit repeatedly, and a thrown item that passes through the volume and out the other side fires an enter it did not earn. Use the zone's explicit **inside test** against the item's settled position.
- Evaluate on the server when an item comes to rest — the same settle moment that turns off transform sync in [`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md). One event, one evaluation, no polling every item every frame.
- Also evaluate once when the round enters `Settling`, as a backstop, so an item that came to rest during a physics edge case is still counted.

**Make it exactly-once and make it sticky**

- `Banked` is a `[GhostField]` on the item ghost and is set **once**, on the server, by a transition that also clears `HeldByNetworkId`.
- Guard the write: if `Banked` is already true, do nothing and log. Duplicate requests under lag are expected, not exceptional.
- **Picking a banked item back up clears the flag and removes its value from the running total.** Without this rule a player banks an item, picks it up, walks out, and the crew is paid for scrap that is now on the floor of a building they have left. With it, the total is always a true statement about what is currently in the zone.
- That makes the running total a **derived sum over items currently banked**, not an accumulator that only goes up. Deriving it is slightly more work per change and removes an entire class of drift bug — it is the same replicate-one-thing-and-derive-the-rest discipline [`23_shared_session_state_sync.md`](23_shared_session_state_sync.md) asks for everywhere else.
- Consider whether re-taking a banked item should be allowed at all. Recommended: **allowed**, because forbidding it needs a rule the player cannot see, and because taking back a tool you banked by accident is reasonable.

**Do not require an interaction**

- Dropping an item in the zone banks it. Do not add a "deposit" verb — it is one more thing to press while being chased, and a player who drops their haul and runs has done the right thing and should be rewarded for it.
- The reverse is the useful affordance: make the deposit surface obvious enough that dropping anywhere sensible in the zone works. A precise deposit target is a source of frustration and of lost value at the worst possible moment.

**Give it the feedback the game is built on**

- **Reveal the value on bank.** This is the single most important feedback loop in the game: the crew learns what a room was worth at the exact moment they are deciding whether to go back for the rest of it. A silent bank makes the quota abstract.
- Show the item's value, the running banked total, and the distance to quota, together. [`27_location_selection_assignment.md`](27_location_selection_assignment.md) makes the same argument about the terminal — a player forced to hold two numbers in their head across two screens will get it wrong.
- Announce banks through the repurposed `ActionFeed` (§9) so the crew inside the building hears the haul growing. That is what turns individual scavenging into a shared score.
- Give it a sound and a visible pile. Loot accumulating physically in the zone is free feedback and it looks like progress.

**Cover everything else that gets deposited**

- **Bodies** — depositing a corpse in the volume registers recovery and reduces the death penalty ([`14_death_and_body_system.md`](14_death_and_body_system.md)). It uses this component's inside test, not a second one, and its value is a penalty reduction rather than credit.
- **Equipment** — must **not** pay out. An item's category and sell value come from its definition ([`37_item_definition_data_model.md`](37_item_definition_data_model.md)); a flashlight banked at its purchase price is a money printer and will be found immediately. Equipment left in the zone is *retained* for the next round, which is the actual intent, and that is a different transition from banking.
- Keep the two transitions distinct in the data — `Banked` for scrap that will be sold, `Retained` for gear that comes home — so settlement never has to guess.

**Settle it exactly once**

- At `Settling`, the Day Cycle Controller enumerates banked items, sums their rolled values, and reports to the Run Manager ([`02_day_cycle_controller.md`](02_day_cycle_controller.md)). This component supplies the enumeration and must guarantee the set is stable at that moment.
- Reject every banking transition once `Settling` has begun — component 02 already requires that state changes during settlement be held or refused, and a bank landing mid-sum is precisely the corruption it is protecting against.
- Unbanked items are destroyed with the location, per the design's loss condition. Verify none survive into the next round; a leaked banked flag on a pooled item instance would credit the next round for last round's scrap ([`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md)).
- Per-player attribution — who banked what — feeds the crew roster's per-round stats and the end-of-round summary ([`19_crew_roster.md`](19_crew_roster.md)). Record it at bank time; it cannot be reconstructed afterwards.

**Prove it**

- Log every bank and unbank server-side with tick, item id, rolled value, and the player responsible. This log plus the round seed is the only way a value discrepancy is ever diagnosed.
- Add a development-only audit at settlement: the sum of banked item values must equal the running total the HUD has been displaying. A mismatch means the derived total and the item set have diverged, and it must be loud.
- Test the ugly cases deliberately: banking on the same tick as the round ends, banking while a player is disconnecting, two players dropping the same contested item into the zone, and an item thrown into the zone from outside.

## Acceptance Criteria

- [ ] An unheld item resting inside the extraction volume is banked, evaluated on rest rather than on trigger crossing.
- [ ] An item spawned already inside the volume is banked without needing to cross the boundary.
- [ ] An item thrown through the volume without stopping is not banked.
- [ ] Banking sets a `[GhostField]` on the item exactly once; duplicate requests under lag change nothing and are logged.
- [ ] Picking a banked item back up clears the flag and reduces the running total accordingly.
- [ ] The running banked total is derived from currently-banked items, never accumulated.
- [ ] No interaction is required to bank; dropping anywhere sensible in the zone works.
- [ ] Banking reveals the item's value and updates the displayed total and distance to quota immediately.
- [ ] Banks are announced to the whole crew, including players inside the building.
- [ ] Banked loot is physically visible accumulating in the zone.
- [ ] Depositing a body registers recovery via this component's inside test and reduces the death penalty rather than paying credits.
- [ ] Equipment deposited in the zone is retained for the next round and pays nothing; buying and banking an item is never a net gain.
- [ ] `Banked` and `Retained` are distinct states in the data.
- [ ] Every bank records which player was responsible, and per-player banked value matches the settlement total.
- [ ] Banking transitions are refused once settlement begins, and the settled total is unaffected by a bank attempted during it.
- [ ] Unbanked items are destroyed at round end and no banked flag survives on a pooled instance into the next round.
- [ ] A development audit at settlement confirms the summed item values equal the displayed running total.
- [ ] Every bank and unbank is logged with tick, item id, value, and player.
- [ ] Four players repeatedly banking, un-banking, and contesting items under simulated latency for a minute produce a correct final total with no duplication or loss.
