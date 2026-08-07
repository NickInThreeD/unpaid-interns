# 25 — Reconnection

**Source:** [`core_components.md`](../core_components.md) §3 — Multiplayer & Team
**Status:** ❌ Not started — Netcode for Entities provides nothing here
**Depends on:** Crew Roster, Mid-Round Disconnect Handling, Spectator Mode, Session Persistence
**Blocks:** runs surviving real-world networks

## Summary

Getting a player back into the run they were already in.

A contract in this design runs for several rounds across an escalating quota. That is a long time to hold a connection, and a design where a brief network blip permanently ejects someone from a shared run is a design that will be abandoned after the first time it happens to a friend group.

**Netcode for Entities gives you none of this.** A reconnecting client is a new connection with a new `NetworkId`, no memory of what it was, and — through `ServerGameSystem.HandleJoinRequests` as written today — a brand-new character spawned wherever a spawn point is free. The server has to hold the state and hand it back deliberately.

This is the third of four components covering the connection lifecycle, and the division matters: [`08_late_join_rejoin_policy.md`](08_late_join_rejoin_policy.md) decides *who may connect and when*, [`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md) decides *what a drop costs*, [`19_crew_roster.md`](19_crew_roster.md) holds *the state*, and this one is *the mechanics of restoring a returning player*.

**If reconnection is going to be deferred, say so explicitly here and make the disconnect rules match** — a grace window with nothing to return to is dead code, and players told "you can come back" who then cannot will be angrier than players told the truth.

## How to Build

**Recognize the returning player**

- Match on the stable player id established in [`19_crew_roster.md`](19_crew_roster.md) — the UGS authentication id, sent in `ClientJoinRequestRpc` — never on `NetworkId`, which is reassigned, and never on player name, which is user-supplied and duplicable.
- The client must send the same id it used originally. `AuthenticationService` persists its session token across app restarts, which covers a game crash as well as a network drop; verify that behaviour rather than assuming it, and have a fallback for the direct-connect path where UGS is not involved.
- On match, re-key the roster entry to the new `NetworkId` and clear the `Disconnected` state. Do not create a second entry — a run that ends with six roster rows for four humans has already lost the plot.
- Reject an id that is already connected. Two live connections claiming one crew slot is a duplication bug waiting to happen, and it is also how someone hijacks a teammate's slot.

**Restore to the right state, not to a default**

- The restored state is a function of the current round phase, not of what the player was doing when they dropped:
  - **Hub** — spawn them as a normal player in the hub with their gear and inventory intact.
  - **Round in progress, inside the grace window, they were alive** — return them to play. Where they appear is a real decision: their drop position is generous and can be exploited (drop while cornered, return safely); the extraction point is safe and costs them the walk back. **Recommended: the extraction point**, because it never rewards a disconnect and it is trivially explainable.
  - **Round in progress, grace window expired, or they were dead** — spectator, per [`22_spectator_mode.md`](22_spectator_mode.md), and they deploy normally next round.
  - **Round settling** — hold them out until the hub, then restore. Injecting a player into settlement is how loot accounting gets corrupted.
- Never leave the decision to `HandleJoinRequests`' current behaviour, which spawns anyone who asks. Gate the spawn on the Day Cycle Controller's phase, as [`08_late_join_rejoin_policy.md`](08_late_join_rejoin_policy.md) requires.

**Decide what comes back with them**

- **Run state** — credits, quota, day, gear, storage — all replicate from the Run Manager automatically. This part is free, provided the roster entry survived; it is the reason [`19_crew_roster.md`](19_crew_roster.md) forbids deleting entries on disconnect.
- **Carried inventory** — only restorable if the loot rule in [`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md) held the items rather than dropping them, and only inside the grace window. If items were dropped, they are on the floor and the player walks to them like anyone else. Do not do both — restoring items that were also dropped duplicates them, and in an economy where items are money, duplication is the most damaging bug the project can ship.
- **Health and injury** — restore the state they had. A player who returns at full health after fleeing a monster at 15 HP has been rewarded for dropping.
- **Per-round stats** — banked value and deaths must survive, or the end-of-round summary and the death penalties will be wrong.

**Make the client side work**

- The client needs to re-run the full connection path: Relay allocation or direct endpoint, `NetworkStreamDriver` connect, join request, subscene load, and the load barrier from [`05_location_load_unload_flow.md`](05_location_load_unload_flow.md). This is not a resume — it is a fresh connection carrying an old identity.
- Reuse `ConnectionStatusScreen` and `LoadingScreen` rather than building a bespoke reconnect UI. Extend `LoadingData.LoadingSteps` with a reconnect step so the player can see what stage they are at.
- Offer an explicit **"rejoin session"** action rather than reconnecting silently. Silent reconnection loops are how a player ends up in a location they were not ready to be in.
- Cap the retry attempts and fail with a clear message. An indefinite retry loop against a session that has already ended is worse than a clean failure.

**Know when there is nothing to return to**

- The session may be gone: the host left, the run failed, or the crew already finished. Detect each and say which — "the host ended the session" and "your run failed while you were away" are very different messages and the player will want the right one.
- With the host-owns-the-save decision in [`06_session_persistence.md`](06_session_persistence.md), a host that quit means the run itself is gone. Say that plainly.

## Acceptance Criteria

- [ ] A reconnecting player is matched to their existing roster entry by stable id, with no duplicate entry created.
- [ ] The roster entry is re-keyed to the new `NetworkId` and cleared of `Disconnected`.
- [ ] A second connection using an already-connected id is rejected with a clear message.
- [ ] Reconnecting in the hub restores a normal player with gear and storage intact.
- [ ] Reconnecting mid-round inside the grace window restores play at the documented position.
- [ ] Reconnecting after the grace window, or while dead, restores a spectator, not a live character.
- [ ] Reconnecting during settlement holds until the hub and does not alter the settled total.
- [ ] Run state — credits, quota, day, gear — is correct after reconnect on the returning client and unchanged for everyone else.
- [ ] Health and injury state are restored, not reset.
- [ ] Per-round banked value and deaths survive the reconnect and appear correctly in the end-of-round summary.
- [ ] Inventory is either restored or dropped, never both; no item duplication is possible via disconnect and return.
- [ ] The rejoin action is explicit, shows progress through the existing loading UI, and does not retry indefinitely.
- [ ] Failing to reconnect reports the specific reason — session ended, host left, run failed, or timeout.
- [ ] A reconnect completes the subscene load barrier before the player is placed in the world.
- [ ] Disconnecting and reconnecting three times in one round leaves the roster, inventory, and loot accounting correct.
- [ ] If reconnection is deferred, that decision is documented here and the disconnect grace window is removed to match.
