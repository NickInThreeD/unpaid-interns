# 68 — Upgrades

**Source:** [`core_components.md`](../core_components.md) §8 — Economy & Progression
**Status:** ❌ Not started
**Depends on:** [Store / Purchasing](67_store_purchasing.md), [Hub State](04_hub_between_rounds_state.md), [Session Persistence](06_session_persistence.md)
**Blocks:** a long run having a shape

## Summary

Permanent purchases that change what the crew can do, rather than what they are carrying.

`core_components.md` sets the bar precisely: upgrades should be **strategy-shaping, not just numeric bumps.** That is the whole design constraint and it is worth holding to, because the default failure of this component is a list of percentages — +10% stamina, +15% carry weight — which costs the same to build as something interesting and changes nothing about how a round is played.

The structural value is different from the store's. Equipment ([`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md)) is spent and lost; upgrades accumulate. That gives a long run a **direction**: a crew twelve days in should feel materially different from a crew on day two, not merely richer. It is also what makes the escalating quota survivable in a way that feels earned rather than granted — [`51_difficulty_escalation.md`](51_difficulty_escalation.md) raises threat with each quota cycle, and upgrades are the crew's side of that curve.

This is a **post-MVP component**. The loop works without it, and it should be built once there is a real run length to shape.

## How to Build

**Make each upgrade unlock a capability or a decision**

Some that pass the "strategy-shaping" test, each hooking a system that already exists in the plans:

- **Unlock a destination** — the most valuable upgrade type in the game, because it changes where the crew can go rather than how well they do there. [`27_location_selection_assignment.md`](27_location_selection_assignment.md) already makes unlock state run state that persists and is wiped on failure, and notes that locked destinations should be **visible and locked** so the expensive place is motivation.
- **Storage capacity** — [`46_storage_hub_inventory.md`](46_storage_hub_inventory.md) caps storage deliberately. Raising the cap changes what the crew can hold over, which interacts directly with the sell-rate curve if [`65_selling_payout.md`](65_selling_payout.md) adopts it.
- **The monitoring station** — turning the hub-bound role from possible into good, by unlocking the camera system (§9) or extending what [`62_hazard_control_remote_disable.md`](62_hazard_control_remote_disable.md) can reach. This one converts a crew slot into a strategy.
- **A second deployment per day** — if the day cycle supports it, the ability to go back out is a genuinely different way to play a cycle.
- **Extraction zone improvements** — a light that carries further, an audible beacon, faster departure. Small, felt every round, and directly about the moment the game is tensest.

Each of those is a sentence a player can repeat to a friend. That is the test: if an upgrade cannot be described without a percentage, it is a balance change, not an upgrade.

**Keep the data model boring**

- `UpgradeData` ScriptableObject with an explicit serialized id and a dictionary registry, following the same pattern as items, locations, monsters, and weather — and the same correction against `WeaponRegistry`'s list-position ids ([`37_item_definition_data_model.md`](37_item_definition_data_model.md)).
- Per upgrade: id, name, in-fiction description, price, prerequisites, and the effect. Effects should be **enumerated and consumed by their owning system**, not scripted here — the storage cap is read by the storage component, the destination unlock by the selection component. This component owns purchase and persistence; it should own no gameplay logic at all.
- Purchased state is a set of unlocked ids on the Run Manager, replicated so every client agrees ([`23_shared_session_state_sync.md`](23_shared_session_state_sync.md)) and persisted with the run ([`06_session_persistence.md`](06_session_persistence.md)).

**Buy them through the store, not a parallel system**

- Upgrades are purchased in the terminal, through the same server-validated, atomic, announced path as equipment ([`67_store_purchasing.md`](67_store_purchasing.md)). Two players buying the same upgrade on the same tick must produce one purchase and one refusal, and one refund path is easier to keep correct than two.
- Unlike equipment, an upgrade **applies immediately** — there is no delivery delay, because there is nothing to deliver. State that difference explicitly so the store UI can show it.
- Purchasing an already-owned upgrade must be impossible, not merely refunded.

**Price them against the run's shape**

- An upgrade should cost **more than a good round's surplus and less than a cycle's** — expensive enough to be a real alternative to equipment, cheap enough to be reachable within a run that will not last forever.
- Prerequisites let a tier structure emerge without a tree UI. Two or three shallow chains beat one deep one; a chain nobody finishes is content nobody sees.
- Watch the interaction with the quota curve: upgrades that compound the crew's earning rate against a quota that grows cubically ([`64_quota_system.md`](64_quota_system.md)) will either be irrelevant or will break the curve. Prefer upgrades that change **options** over ones that change **income**, for exactly this reason.

**Wipe them with the run**

- Upgrades are **run-scoped**, not account-scoped. They are wiped on failure with everything else ([`07_game_over_win_resolution.md`](07_game_over_win_resolution.md)), and a new run starts with none.
- Cross-run persistence is [`69_rank_and_progression.md`](69_rank_and_progression.md)'s job, and that component is deliberately cosmetic. Keeping the two separate is what stops the game from becoming one where a new crew cannot win.
- Verify no upgrade state leaks into a new run — component 07 already flags that boundary as the most likely place for residue.

**Make them visible in the world**

- An upgrade the crew paid for should be **physically apparent in the hub**. A monitoring station that appears, shelves that extend, a light that switches on. The hub is a place ([`04_hub_between_rounds_state.md`](04_hub_between_rounds_state.md)) and watching it accumulate is the most direct feedback a long run can offer.
- Announce purchases to the whole crew, since they are spending shared money on shared capability.

## Acceptance Criteria

- [ ] `UpgradeData` and a registry exist with explicit serialized ids and a dictionary built at load.
- [ ] Every authored upgrade changes an option or a capability; none is purely a numeric modifier.
- [ ] Each upgrade's effect is consumed by its owning system; this component contains no gameplay logic.
- [ ] Purchased upgrade ids are replicated on the Run Manager and identical on every client.
- [ ] Upgrades are purchased through the same atomic, server-validated store path as equipment.
- [ ] Two players purchasing the same upgrade on the same tick produce exactly one purchase.
- [ ] An already-owned upgrade cannot be purchased.
- [ ] Upgrades apply immediately with no delivery delay, and the store UI reflects that difference.
- [ ] Prerequisites are enforced server-side.
- [ ] Locked and unaffordable upgrades are visible rather than hidden.
- [ ] Upgrade prices sit between one good round's surplus and one cycle's earnings.
- [ ] Upgrades that change options are preferred over ones that change income, and any income-affecting upgrade is checked against the quota curve.
- [ ] Upgrade state persists across rounds and through a save and reload.
- [ ] Upgrades are wiped on run failure, and a new run starts with none, verified for residue.
- [ ] No upgrade persists across runs; cross-run progression is cosmetic only.
- [ ] Every purchased upgrade is physically visible in the hub.
- [ ] Upgrade purchases are announced to the whole crew.
- [ ] A debug command can grant or revoke any upgrade.
