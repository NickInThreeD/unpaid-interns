# 67 — Store / Purchasing

**Source:** [`core_components.md`](../core_components.md) §8 — Economy & Progression
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Currency System](63_currency_system.md), [Tool & Equipment Items](44_tool_and_equipment_items.md), [Storage / Hub Inventory](46_storage_hub_inventory.md), [Hub State](04_hub_between_rounds_state.md)
**Blocks:** earning above quota having a purpose

## Summary

Somewhere to spend the money.

`core_components.md` states the dependency bluntly: *"without a spend, earning above quota has no purpose."* A game where credits only ever count toward a target is a game where the correct play is to bank exactly enough and stop — and the entire risk appetite the design is built on collapses, because there is no reason to take the extra trip.

The store is small in code and it is the component that makes the other half of the economy exist. It also carries most of the game's between-rounds texture: choosing what to buy with a quota deadline three days out is the crew's only real strategic decision, and it is a decision they make while standing in a safe room reading a number that is not big enough.

The purchase itself is the part that has to be right. It is a **shared balance being spent by four people who can all press buttons at once**, which is a concurrency problem wearing a shopping list.

## How to Build

**Make the transaction atomic and server-authoritative**

- The client sends a purchase **request**; the server validates and decides. Never deduct client-side and never let the client name a price.
- Validate every time: is the item purchasable, is the phase `Hub`, is the crew's balance sufficient **at this moment**, is storage below capacity ([`46_storage_hub_inventory.md`](46_storage_hub_inventory.md)).
- **Deduct and grant in one server operation.** Two players spending the last 100 credits on the same tick must produce one purchase and one refusal, not two purchases and a negative balance. [`63_currency_system.md`](63_currency_system.md) requires `SpendCredits` to refuse rather than clamp and requires callers to check the return value — this is the highest-frequency caller and the one where an unchecked return becomes free equipment.
- The rejection must be legible: "not enough credits", "storage full", "not in the hub". A silent no-op on a shared balance is how a crew ends up arguing about whether someone bought something.

**Show everyone what is happening**

- A shared wallet spent privately is a betrayal generator. Every purchase should be **visible to the whole crew** — announced through the repurposed `ActionFeed` (§9), with who bought what.
- Show the balance updating live on every client, and show pending requests if there is any latency. Two players about to buy the same expensive item should be able to see it coming.
- This is the same argument [`27_location_selection_assignment.md`](27_location_selection_assignment.md) makes about a destination silently changing while someone is shopping: shared state changed by one person needs to be observable by the rest.

**Deliver at the start of the next round, not instantly**

- Purchases arrive in hub storage at the start of the next round ([`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md), [`46_storage_hub_inventory.md`](46_storage_hub_inventory.md)). The delay is what makes buying a **plan** rather than a reaction.
- The reference makes delivery a whole mechanic — a dropship, a 30-second window to collect, no refunds for what you leave behind ([`Assets/docs/items/store.md`](../../Assets/docs/items/store.md)). That is excellent texture and it is scope; it is also a second failure mode for a crew that is already losing things. **Recommended: simple delivery into storage for MVP**, with the dropship as a later flourish if the hub needs more to do.
- Whatever is chosen, the crew must be able to see what is on order and when it arrives. A purchase that vanishes into a queue nobody can inspect is a support ticket.

**Price against the quota, not against vibes**

- The store's prices and the quota curve are one balance problem. An item that costs less than a round's typical surplus is bought automatically and is therefore not a decision; one that costs three cycles of surplus is never bought at all.
- Aim for the band where a purchase costs a **meaningful fraction of one good round** — enough that buying two things instead of one is a real choice.
- Every item's price lives on its `ItemData` ([`37_item_definition_data_model.md`](37_item_definition_data_model.md)), so the store is a filtered view of the item registry rather than a parallel catalogue with its own ids and prices to drift out of sync.
- **Sell value must be near zero** for purchasable items, enforced by the item category. [`43_loot_banking_deposit.md`](43_loot_banking_deposit.md) and [`65_selling_payout.md`](65_selling_payout.md) both carry this rule; the store is where the exploit would be executed, so verify it from this side too.

**Consider discounts, carefully**

- The reference rotates random discounts, which gives each cycle a small character and rewards checking the terminal. It is cheap and it works.
- If adopted: roll from the **run seed** plus cycle number so it is reproducible and cannot be re-rolled by reloading a save ([`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md), [`06_session_persistence.md`](06_session_persistence.md)).
- Keep discounts modest. A steep discount makes the optimal play "wait for the sale", which turns the between-rounds phase into a lottery the crew is passively observing.

**Present it in fiction**

- The store lives in the terminal ([`74_terminal_hub_interface.md`](74_terminal_hub_interface.md)), which §9 argues should be a diegetic in-world computer rather than a menu — the tone supports it and the premise demands it.
- Show, on the same screen: current credits, quota shortfall, and days remaining. The purchase decision is a function of how far behind the crew is, and forcing them to remember that number from another screen just makes them wrong ([`72_quota_and_deadline_display.md`](72_quota_and_deadline_display.md) makes the same argument).
- The employer's copy is free comedy here — enthusiastic upselling of safety equipment to people it is about to send into a building.

**Make it testable**

- `ConfigVar` commands to grant credits ([`01_run_manager.md`](01_run_manager.md) already requires this), to force-purchase an item, and to clear storage. Testing gear-dependent systems without a way to acquire gear instantly is prohibitively slow.
- Add a test for the concurrency case explicitly: N simultaneous purchase requests against a balance that affords one, asserting exactly one succeeds and the balance is correct.

## Acceptance Criteria

- [ ] Purchases are server-validated; a client cannot name a price, deduct locally, or purchase outside the hub.
- [ ] Deduction and grant happen in one atomic server operation.
- [ ] Two players purchasing on the same tick against a balance that affords one produce exactly one purchase and one legible refusal.
- [ ] The balance never goes negative and no purchase is granted without a confirmed deduction.
- [ ] Every rejection states a specific reason.
- [ ] Purchases are announced to the whole crew with who bought what.
- [ ] The balance updates live on every client during shopping.
- [ ] Purchased items are delivered into hub storage at the start of the next round, not instantly.
- [ ] Pending orders are visible to the crew before delivery.
- [ ] Prices live on `ItemData`; the store is a filtered view of the item registry with no parallel catalogue.
- [ ] Purchasable items have near-zero sell value, and buying then banking an item is never profitable.
- [ ] If discounts are enabled, they are seeded from the run seed and cycle, and cannot be re-rolled by reloading a save.
- [ ] The store screen shows current credits, quota shortfall, and days remaining together.
- [ ] Storage capacity is respected, and a purchase that would exceed it is refused with an explanation.
- [ ] Purchased gear persists across rounds and through a save and reload.
- [ ] Debug commands can grant credits, force a purchase, and clear storage, and all work in a build.
- [ ] An automated test asserts exactly one success under N simultaneous purchase requests.
- [ ] Item prices are tuned so a purchase costs a meaningful fraction of one good round's surplus.
