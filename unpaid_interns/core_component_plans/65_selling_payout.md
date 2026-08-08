# 65 — Selling / Payout

**Source:** [`core_components.md`](../core_components.md) §8 — Economy & Progression
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Loot Banking](43_loot_banking_deposit.md), [Currency System](63_currency_system.md), [Quota System](64_quota_system.md), [Day Cycle Controller](02_day_cycle_controller.md)
**Blocks:** loot meaning anything, the store having input, quota progress existing

## Summary

Turning banked scrap into money.

`GAME_DESIGN.md` step 6: *"items brought back to the start point are sold once the round ends, converting loot into currency."* That is the simplest version and it works. This component's only real design question is whether to complicate it — and `core_components.md` suggests one specific complication worth considering: **a time-based sell rate, worse for selling early in a cycle**, layering a second timing tension on top of the quota.

The reference implements exactly that and the numbers are stark: 30% of value with three days left, 53% with two, 77% with one, and 100% on the deadline day ([`Assets/docs/core-loop/credits.md`](../../Assets/docs/core-loop/credits.md)). Selling early costs you two thirds of your haul.

That is a strong mechanic and it should be adopted **deliberately, not by default**, because it changes what the game is about. Without it, the crew asks "did we get enough?" With it, they also ask "can we survive holding it?" — which requires [`46_storage_hub_inventory.md`](46_storage_hub_inventory.md) to support holding loot over, and which punishes a crew that banks safely early. Both games are good. Only one of them is the game described in `GAME_DESIGN.md`'s core loop, which sells at the end of every round.

**Recommendation: ship the simple version first — sell everything at round settlement at full value — and treat the rate curve as a tuning experiment behind a config flag.** The simple version is required anyway as the fallback, and the curve is a multiplier on top of it.

## How to Build

**Sell at settlement, once, from the banked set**

- Selling happens in `Settling`, driven by [`02_day_cycle_controller.md`](02_day_cycle_controller.md), which enumerates banked items and reports the total to the Run Manager via `RecordQuotaProgress` and `AddCredits`.
- The banked set comes from [`43_loot_banking_deposit.md`](43_loot_banking_deposit.md), which guarantees it is stable at that moment and refuses banking transitions once settlement begins. Do not re-derive what counts as banked here.
- **Sell exactly once.** This is the transition that converts objects into the number that decides whether the crew lives, and a double-sale is the single most damaging economy bug available. Component 43's exactly-once guarantee covers the flag; this component must not add a second path that reads it.
- **Equipment is not sold.** Retained gear comes home and pays nothing ([`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md), [`46_storage_hub_inventory.md`](46_storage_hub_inventory.md)). Buying a flashlight and banking it must never be profitable, and the distinction lives in the item category rather than in a special case here.
- Destroy sold items after settlement. An item that survives the sale and is still in the zone next round is money printed from nothing.

**Split the two outputs correctly**

- A sale produces **two** effects: credits gained ([`63_currency_system.md`](63_currency_system.md)) and quota progress recorded ([`64_quota_system.md`](64_quota_system.md)). They are the same value at the moment of sale and they diverge immediately afterwards, because spending reduces credits and never reduces progress.
- Getting this wrong makes buying equipment lethal and nobody will ever use the store. Component 64 states the rule; this is the component that has to implement it correctly, since it is the only place both are written at once.
- Both writes go through the Run Manager's mutators, logged with the `SaleProceeds` reason, so the end-of-round summary can itemise the payout.

**If the rate curve is adopted, make it visible**

- The rate must be **displayed before the crew commits**, on the same screen as quota progress and days remaining ([`72_quota_and_deadline_display.md`](72_quota_and_deadline_display.md)). A hidden multiplier that takes 70% of a haul is experienced as theft, not as tension.
- Show both numbers at the moment of sale: gross value banked and net credits received. The gap is the mechanic and the player has to see it to learn it.
- The curve is data — a rate per day-remaining — in the same config asset as the quota curve.
- Round at the **single documented rounding point** [`63_currency_system.md`](63_currency_system.md) requires. A percentage applied per item and rounded per item produces a different total than one applied to the sum, and the difference will be reported as a bug.
- Apply the rate to the **whole sale**, not per item. Per-item rounding also invites the exploit of banking items individually across days to game the rounding.

**Decide where selling happens**

- The simple model sells at the extraction zone during settlement — no separate location, no extra trip. That matches the core loop as written and is the recommendation.
- The reference uses a **dedicated sell location** the crew must travel to, which adds a routing decision and a way to lose a full cycle's haul by mistiming the trip. It is a genuinely good mechanic and it is a much larger change: it needs a destination, a travel cost, a deadline-day rule, and a failure case where the crew never made it.
- If a sell location is adopted later, it slots in as a location in the catalogue ([`26_location_catalogue.md`](26_location_catalogue.md)) with settlement moved to arrival there. Build the sale as a **function of a banked set**, not as something wired into the extraction zone, so that move stays cheap.

**Report it in a way the crew believes**

- Itemise the payout: per item value, the rate applied if any, the gross, the penalties ([`66_bonus_and_penalty_rules.md`](66_bonus_and_penalty_rules.md)), and the net. [`70_performance_report.md`](70_performance_report.md) presents it; this component supplies the breakdown.
- Per-player attribution comes from the bank-time records in component 43 — who banked what — and feeds the summary and the crew roster's per-round stats ([`19_crew_roster.md`](19_crew_roster.md)).
- A crew that cannot reconcile the payout with what they carried will assume the game is cheating them, and in a game where the payout decides whether they live, that assumption ends sessions.

## Acceptance Criteria

- [ ] Banked items are sold exactly once, at settlement, from the set supplied by the banking component.
- [ ] No path can sell the same item twice, under lag, duplicate requests, or a disconnect during settlement.
- [ ] Equipment is never sold and never contributes credits.
- [ ] Sold items are destroyed and cannot survive into the next round.
- [ ] A sale writes both credits and quota progress, through the Run Manager's mutators, logged with a sale reason.
- [ ] Spending credits afterwards never reduces quota progress.
- [ ] The sale is implemented as a function of a banked set, not wired into the extraction zone.
- [ ] If the rate curve is enabled, the current rate is visible before the crew commits to selling.
- [ ] Gross value and net credits are both shown at the moment of sale.
- [ ] The rate curve lives in a config asset alongside the quota curve.
- [ ] The rate is applied to the whole sale, with a single documented rounding point.
- [ ] Banking items across separate days cannot exploit rounding for extra credits.
- [ ] With the rate curve disabled, every banked item sells at full value.
- [ ] The payout is itemised — per item, gross, rate, penalties, net — and the itemisation reconciles exactly with the credited amount.
- [ ] Per-player banked value is attributed correctly and matches the settlement total.
- [ ] A development assertion confirms the sum of sold item values equals the gross reported.
- [ ] Selling with an empty banked set completes cleanly and pays nothing.
- [ ] Debug commands can force a sale and set the current rate.
