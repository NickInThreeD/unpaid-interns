# 46 — Storage / Hub Inventory

**Source:** [`core_components.md`](../core_components.md) §5 — Items, Loot & Inventory
**Status:** ❌ Not started
**Depends on:** [Hub State](04_hub_between_rounds_state.md), [Item Ghost](38_item_ghost_networked_item_state.md), [Loot Banking](43_loot_banking_deposit.md), [Session Persistence](06_session_persistence.md)
**Blocks:** gear surviving between rounds, deciding when to sell, hub as a place worth being

## Summary

Where the crew's things live when they are not in a location.

Two distinct jobs share one system. **Gear storage** is what makes equipment purchases worth making — a flashlight bought on day two must be there on day three, or the store is a per-round rental and the money sink stops mattering ([`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md)). **Loot storage** is what makes selling a *decision* rather than an automatic settlement step: if the crew can hold scrap over, then "sell now or hold for a better rate" becomes a real question, and the Selling component in §8 gets a second axis of tension.

That second job is optional and should be treated as such. The design's core loop ends each round with *"items brought back to the start point are sold once the round ends"*, which is the simplest thing that works. Holding loot over is a deliberate extension, and it is worth building the storage system so it *can* support it without committing to it on day one.

The hub is also where storage does its most underrated work: giving the between-rounds state something physical to be about. A crew standing in a room full of the things they nearly died for is a better scene than a menu with a number on it.

## How to Build

**Decide what storage actually is — and make it physical**

- **Recommended: a real space with real objects.** Items sitting on shelves in the hub, pickable, droppable, countable by looking. It costs almost nothing beyond what [`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md) already provides, and it makes the hub a place instead of a lobby.
- The alternative — an abstract list behind a UI — is simpler to persist and worse in every other way. It also produces the classic co-op failure where nobody knows what the team owns.
- A hybrid is the practical answer: physical objects for gear that gets carried out, plus a **queryable total** for value, because "how much is our stored loot worth" must be answerable without walking around adding it up ([`27_location_selection_assignment.md`](27_location_selection_assignment.md) makes the same argument about the destination screen).

**Reuse the item ghost, do not invent a container format**

- Stored items are item ghosts in the hub scene, with `Retained` set rather than `Banked` ([`43_loot_banking_deposit.md`](43_loot_banking_deposit.md)). One representation, one set of claim and pickup rules, no serialization format for "an item in a box".
- Storage therefore inherits interaction authority for free: two players grabbing the same stored flashlight resolves exactly as it does in the field ([`20_networked_interaction_authority.md`](20_networked_interaction_authority.md)).
- Cap it. Unbounded storage is unbounded ghosts in a scene that is loaded for the whole session, and a crew that hoards two hundred objects will find the bandwidth floor. A capacity limit is also a decision — what do we keep? — which is worth more than the convenience it costs.

**Get the transfer moments right**

Storage is populated and drained at exactly three moments, and each one must be a single explicit transition:

- **Round end, from the extraction zone.** Retained equipment comes home; banked scrap is sold, unless the hold-over rule is adopted. Nothing else transfers.
- **Round start, into the field.** Gear taken from storage is carried out in inventory slots. There is no "loadout screen" — a player physically picks up what they want, which enforces the slot limit at the only moment it can be reasoned about calmly.
- **Store delivery.** Purchases arrive into storage at the start of the next round ([`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md), §8).

The rule that keeps this honest: **an item exists in exactly one place at a time.** Storage is not a copy of anything. The duplication risk here is the same one [`40_inventory_item_bar.md`](40_inventory_item_bar.md) guards against, and in an economy where items are money it is the most damaging bug available.

**Persist it**

- Storage contents are run state: they belong on the save alongside credits and quota ([`06_session_persistence.md`](06_session_persistence.md)), and they are wiped with the run on failure ([`07_game_over_win_resolution.md`](07_game_over_win_resolution.md)).
- Serialise as a list of `(itemId, rolledValue, instanceState)` — never as ghost references, which are meaningless across sessions. Instance state covers ammo and charge, which must survive a save or a player will lose a full flashlight to a reload.
- Restore on the server before any client connects, so replication carries the correct contents outward rather than overwriting them ([`06_session_persistence.md`](06_session_persistence.md) already requires this ordering for the Run Manager; storage rides the same path).
- On total crew loss, apply the rule recorded in [`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md): **hub storage survives, everything carried into the field is lost.** Both files must agree, and this is the one that owns the storage half.

**Answer the questions the crew will ask**

- Total stored value, at a glance, and what fraction of the current quota it represents. This is the input to "do we sell now or push for one more day", and it must be visible on the same screen as quota progress.
- What gear is available and how much of it — four flashlights and no radio is a fact that should be discoverable in two seconds, not by inventory archaeology.
- Who took what, if the hold-over rule is adopted and stored loot has value. A crew that loses stored gear and cannot tell whether it was lost in the field or taken by a teammate has a social problem the game created.

**Keep it out of the way when it should be**

- Storage is hub-only. There is no access to it from a location — that would collapse the carry limit, which is the mechanic the entire §5 is built around.
- No storage interaction during a round, and none during settlement, for the same reason [`02_day_cycle_controller.md`](02_day_cycle_controller.md) freezes banking during `Settling`.

## Acceptance Criteria

- [ ] Stored items are item ghosts in the hub with `Retained` state, using the same pickup, claim, and drop paths as field items.
- [ ] An item exists in exactly one place at any moment; no transfer produces a copy.
- [ ] Equipment left in the extraction zone at round end arrives in hub storage.
- [ ] Gear is taken into the field by physically picking it up, subject to the normal slot limit.
- [ ] Store purchases are delivered into storage at the start of the next round.
- [ ] Storage capacity is capped, and reaching the cap is communicated clearly rather than silently dropping items.
- [ ] Total stored value is queryable and displayed alongside quota progress.
- [ ] Available gear and its quantities are readable at a glance.
- [ ] Storage contents persist across a save and reload, including per-instance ammo and charge.
- [ ] Contents are restored on the server before clients connect, and clients receive them by replication.
- [ ] Storage is wiped with the run on failure.
- [ ] The total-crew-loss rule matches [`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md): storage survives, field-carried items do not.
- [ ] Storage cannot be accessed from within a location or during settlement.
- [ ] Two players taking from storage simultaneously resolve to one holder per item with no duplication.
- [ ] A full storage of the maximum capacity does not measurably affect hub snapshot size or frame time.
- [ ] Two consecutive runs leave no items leaked from the previous run's storage.
