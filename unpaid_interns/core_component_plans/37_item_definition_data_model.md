# 37 — Item Definition / Data Model

**Source:** [`core_components.md`](../core_components.md) §5 — Items, Loot & Inventory
**Status:** ❌ Not started · **[MVP]**
**Depends on:** Data-Driven Configuration
**Blocks:** Item Ghost, Loot Spawner, Inventory, Carry Weight, Loot Banking, Tool & Equipment Items, Store

## Summary

What an item *is*, as data. Every other component in §5 reads from this one, and nothing in §5 can start before it exists.

The design puts the whole game on the value of what you carry versus what it costs you to carry it. That comparison only exists if items carry both numbers, and it only stays tunable if a designer can add a new item without a programmer. `core_components.md` §11 flags data-driven configuration as essential precisely because *"how much of this game is balance work"* — and items are where most of that work lands.

The pattern is already in the project and so is the trap. `WeaponData` ScriptableObjects live in `Assets/Data/Weapons/` behind a `WeaponRegistry` that resolves ids — but `WeaponRegistry.GetWeaponData(uint weaponID)` returns `Weapons[(int)weaponID]`, so **the id is the list position.** Reordering the list silently reassigns every id. That is survivable with two weapons and catastrophic with a hundred items, a save file recording purchased gear, and ghost fields carrying item ids across a version boundary. [`26_location_catalogue.md`](26_location_catalogue.md) already mandates explicit ids for locations; the same rule applies here and matters more.

## How to Build

**Author the type and the registry**

- Add `Assets/Scripts/Gameplay/Items/ItemData.cs` as a ScriptableObject with `[CreateAssetMenu]`, and `ItemRegistry.cs` beside it with `GetItemData(uint itemId)`.
- Store assets under `Assets/Data/Items/`, beside `Assets/Data/Weapons/`.
- **Explicit serialized `Id`, dictionary built at load, assert loudly on duplicates.** Never list position. Once an id is assigned to a shipped item it is permanent — write that down in the asset's tooltip, because the person who reuses a retired id will not have read this file.
- The registry must be **identical on server and client**. An item present in one build and not the other resolves to null on one side, and the symptom is an invisible or unpickable object rather than an error. Version-stamp it alongside the build-version check in §12, the same requirement the location registry carries.

**Choose the fields**

- **Identity** — id, display name, a short description for the scanner and the store, and a category.
- **Economy** — minimum and maximum value. Value is a **range on the definition and a roll on the instance**; the definition never carries a concrete price. [`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md) owns the rolled value.
- **Physical** — weight, and a two-handed flag. These two are what [`12_carry_weight.md`](12_carry_weight.md) and [`42_two_handed_item_rule.md`](42_two_handed_item_rule.md) consume, and they are the fields the whole risk/reward curve is tuned on.
- **Presentation** — the prefab, as a `GhostSpawner.GhostReference`. That type already wraps an `AssetReferenceGameObject` with a serialized `Hash128` GUID and is exactly how `WeaponData` references projectile and VFX prefabs; reuse it rather than inventing an item-loading path.
- **Behaviour** — passive noise (range and volume, consumed by the noise system in §6), light emission, and an activation behaviour for tools ([`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md)).
- **Store** — purchasable flag and price, so store items and scrap share one type rather than two parallel hierarchies.

**Keep rarity out of the item**

- Spawn weight belongs to the **location**, not the item: [`26_location_catalogue.md`](26_location_catalogue.md) already specifies a per-location loot table with rarity weights, and the reference design is explicit that the same object is common on one destination and rare on another ([`Assets/docs/items/scrap.md`](../../Assets/docs/items/scrap.md)).
- Putting a global rarity on `ItemData` and a per-location weight on `LocationData` gives two knobs for one thing, and they will disagree. Pick the location's table and delete the other.
- What the item may legitimately carry is an *eligibility* hint — indoor / outdoor / anywhere — so a location's table cannot accidentally place a rusted girder on a rooftop.

**Define the categories deliberately**

Four, following the reference's split ([`Assets/docs/items/items.md`](../../Assets/docs/items/items.md)), because they differ in **loss rules** rather than in flavour:

- **Scrap** — found in the field, sells for its rolled value, lost if not banked.
- **Equipment** — bought in the store, has a function, sells for little or nothing, and is expected to come home ([`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md)).
- **Weapon** — equipment that does damage, and the category monster threat targeting reads when deciding who is dangerous (§6).
- **Special** — bodies, keys, quest-shaped objects, anything with a bespoke rule.

The important consequence: **an item's category decides whether banking it pays.** Buying a flashlight for 60 credits and selling it for 60 is a money printer, and it will be found in the first session. Equipment must have a sell value of zero or near it, and that rule lives here, in data, so [`43_loot_banking_deposit.md`](43_loot_banking_deposit.md) has one field to read rather than a special case to maintain.

**Make the numbers honest against each other**

- Value and weight must be correlated by design, not by hope. The interesting item is the heavy one that pays, and the trap item is the heavy one that does not.
- Publish **value per unit weight** in the authoring inspector as a computed, read-only field. It is the statistic players will actually optimise against, and a designer who cannot see it will produce items that are strictly dominated without noticing.
- Keep the range wide enough that a rolled value is a small surprise and narrow enough that an item's identity is stable. An item worth 10–400 credits is not an item, it is a lottery ticket.

**Ship few, tune them, then add**

- Author eight to twelve scrap items across three weight bands and two or three pieces of equipment, and tune those against a real round before adding more. A catalogue of near-identical items adds inventory-management friction without adding a decision.
- Add a debug item with fixed value and weight for testing every downstream system deterministically.
- Add an editor validation pass — every item has a prefab, a non-zero id, a sane value range, a weight, and a category — and fail the build on a violation. Silent nulls in item data surface as invisible objects in the world, which is the hardest possible bug to trace back to a missing asset field.

## Acceptance Criteria

- [ ] `ItemData` and `ItemRegistry` exist under `Assets/Scripts/Gameplay/Items/`, with assets in `Assets/Data/Items/`.
- [ ] Item ids are explicit serialized values; reordering the registry changes no id.
- [ ] The registry builds a dictionary at load and asserts loudly on duplicate ids.
- [ ] A registry mismatch between client and server is detected at connect, not as a null at spawn time.
- [ ] Every item carries a value range, a weight, a two-handed flag, a category, and a prefab reference.
- [ ] Prefabs are referenced through `GhostSpawner.GhostReference` and load through Addressables.
- [ ] The definition never carries a concrete value — only a range.
- [ ] Spawn rarity exists only on the location's loot table; `ItemData` carries no global rarity.
- [ ] Item categories are implemented and each has a documented loss and sell rule.
- [ ] Equipment cannot be sold for a profit; buying and immediately banking an item is not a net gain.
- [ ] Value per unit weight is visible in the authoring inspector.
- [ ] Passive noise, light emission, and activation behaviour are data fields with named consumers, or are marked as unused.
- [ ] An editor validation pass rejects items with missing prefabs, zero ids, inverted value ranges, or missing categories, and fails the build.
- [ ] A fixed-value debug item exists for deterministic testing.
- [ ] A designer can add a new item and see it spawn in a round with no code change and no recompile.
- [ ] At least eight scrap items exist across at least three weight bands and play measurably differently to carry.
