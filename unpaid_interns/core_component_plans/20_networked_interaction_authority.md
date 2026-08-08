# 20 — Networked Interaction Authority

**Source:** [`core_components.md`](../core_components.md) §3 — Multiplayer & Team
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Item Ghost / Networked Item State](38_item_ghost_networked_item_state.md), [Interaction System](41_interaction_system.md)
**Blocks:** Inventory, Loot Banking, Door System, body recovery, every co-op interaction

## Summary

The rule for who may pick up, drop, open, or use a given object, and what happens when two players try at the same moment.

In single player this component does not exist. In co-op it is the difference between a working game and one where the highest-value item in the building duplicates because two people grabbed it on the same frame — a bug that is invisible in Editor testing with one player and catastrophic in an economy where items convert directly to quota progress.

The project has no interaction of any kind yet, so this is greenfield. But the surrounding architecture already dictates the answer: gameplay state is server-authoritative and replicated through ghosts, and movement is client-predicted with reconciliation. Interaction should follow the same shape — **predict optimistically on the client, resolve authoritatively on the server, correct visibly when they disagree.**

The hard requirement is that a failed prediction must be *cheap and legible*. A player who reaches for an item and gets it 80 ms later feels responsive. A player who sees the item in their hands and then watches it teleport away needs to understand instantly that someone else got there first.

## How to Build

**Put a claim on the item ghost**

- Add to the item's replicated state ([`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md)) a `HeldByNetworkId` field and a `ClaimTick`, both `[GhostField]`. An unheld item has an invalid owner id.
- The server resolves contention by tick: the earliest `ClaimTick` wins, and ties break on the lower `NetworkId` so the result is deterministic rather than dependent on iteration order.
- Never resolve contention on the client. A client may *predict* it won, but the server decides.
- Keep the claim on the item, not on the player. An item is the contended resource, and putting the claim anywhere else means reconstructing it from inventory state on every query.
- **`NetworkId` is a routing key, not an identity** — [`19_crew_roster.md`](19_crew_roster.md) establishes this, and netcode reassigns ids as connections come and go. A claim keyed on `NetworkId` alone has a real failure mode: a player drops while holding an item, a new connection is later assigned the same id, and inherits a claim it never made. Mitigate by clearing every claim held by a `NetworkId` **at the moment of disconnect** rather than at grace-window expiry ([`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md) already requires exactly this timing, and this is why). `NetworkId` remains the right field on the wire — it is compact and it is what the server resolves against live connections — but nothing may assume it survives a disconnect.

**Define the request path**

- The client raycasts, finds a target, and sends an interaction request. Send it as a **command on the input stream**, not as a fire-and-forget RPC — the input stream is already tick-stamped and replayed by the prediction pipeline, which is exactly what tick-ordered resolution needs. `PlayerInput.InputFlag` plus a target ghost id is sufficient.
- The server validates every request before granting it: does the item still exist, is it unclaimed, is the requester close enough, is the requester alive, does the requester have a free slot, is the item two-handed and are both hands free.
- **Validate distance server-side.** Without it, a modified client picks up items across the map. This is the single most important check in the component.
- Grant by writing the claim, then let replication carry it. Do not send a separate "you got it" RPC — the ghost field is the answer, and two sources of truth will drift.

**Predict the common case**

- On sending the request, the client immediately shows the item in hand. The common case — nobody else is reaching for it — is overwhelmingly the majority, and waiting a round trip makes every pickup feel broken.
- On the correcting snapshot, if the claim went to someone else, return the item to the world at its replicated position and play a short, clear rejection cue. No silent failure.
- Predicted pickup changes carry weight, which changes movement speed, which is predicted — so the client must apply the weight change on the same tick the server does. [`12_carry_weight.md`](12_carry_weight.md) already calls this out; this is the component that has to honour it.
- Design the failure to be *visible but not punishing*: a snap of the item back to the floor plus a sound is enough. Do not add a cooldown or a stagger.

**Cover the other interaction types**

- **Drop and throw** — the reverse claim. The server clears `HeldByNetworkId` and spawns the item at a validated position; a client cannot choose an arbitrary drop location.
- **Doors** — shared state, not per-player. Two players opening a door on the same tick must produce one open door, not a toggle that lands closed. Make door state absolute (`Open` / `Closed`), never a toggle command, so simultaneous requests converge. See the Door System in §7.
- **Bodies** — recovery is a pickup of a two-handed item, so it inherits this whole path. Verify it against the case of two players grabbing the same corpse.
- **Banking** — depositing in the extraction zone clears the holder and sets the banked flag ([`43_loot_banking_deposit.md`](43_loot_banking_deposit.md)). This is the only transition that must be exactly-once, because it converts to money.

**Handle the ugly cases**

- **Holder disconnects or dies while holding.** The item must not become permanently claimed by a `NetworkId` that no longer exists. The server clears claims on death and disconnect and drops the item into the world — coordinate with [`14_death_and_body_system.md`](14_death_and_body_system.md) and [`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md).
- **Item destroyed between request and resolution.** Validate existence on the server every time; never assume the ghost the client named still exists.
- **Claims must not survive a round.** Clear all claims at round teardown so nothing leaks into the next location. "At teardown" now has a precise meaning: step 4 of [`106_round_teardown_and_state_reset.md`](106_round_teardown_and_state_reset.md), deliberately ordered *before* the despawn that would otherwise leave a claim pointing at a destroyed ghost, and *after* the settlement pass that still needs to know who held what.
- Apply the theft rules decided in [`18_pvp_collision_and_friendly_fire.md`](18_pvp_collision_and_friendly_fire.md): world items and corpses are free to anyone, items held by a living player are not. This component is where that policy is enforced.

**Make it observable**

- Log every grant and rejection server-side with tick, requester, and target. Item duplication and item loss are both nightmares to diagnose from a verbal report, and this log is what makes them tractable.
- Add a debug overlay showing claim state on nearby items. It costs an hour and saves days.

## Acceptance Criteria

- [ ] Two clients requesting the same item on the same tick result in exactly one holder, deterministically resolved, with no duplication and no loss.
- [ ] The loser sees the item return to the world within one snapshot, with a clear rejection cue.
- [ ] Pickup feels immediate on the acquiring client — no round-trip delay in the uncontended case.
- [ ] A client cannot pick up an item beyond the configured interaction range, verified by sending a forged request.
- [ ] A client cannot pick up an item that is already held, already banked, or destroyed.
- [ ] Pickup respects free slots and the two-handed rule, refusing with a legible prompt rather than silently.
- [ ] Carry weight updates on the same tick on client and server; a contested pickup causes no movement correction.
- [ ] Dropping clears the claim and places the item at a server-validated position.
- [ ] Two players opening the same door on the same tick leaves it open, not closed.
- [ ] Banking an item is exactly-once — an item cannot be counted twice under lag or duplicate requests.
- [ ] A holder dying or disconnecting releases the claim and leaves the item recoverable in the world.
- [ ] Claims are cleared at the moment of disconnect, and a later connection assigned the same `NetworkId` inherits no claim.
- [ ] No claim survives a round transition.
- [ ] Grants and rejections are logged server-side with enough detail to reconstruct a contested pickup.
- [ ] Four players repeatedly grabbing at one item under simulated latency for a minute produces no duplicates, no orphans, and no roster or inventory corruption.
