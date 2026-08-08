# 14 — Death & Body System

**Source:** [`core_components.md`](../core_components.md) §2 — Player Character
**Status:** ⚠️ Auto-respawn exists and must be replaced · **[MVP]**
**Depends on:** [Health & Injury](13_health_and_injury.md), [Inventory](40_inventory_item_bar.md), [Crew Roster](19_crew_roster.md), [Spectator Mode](22_spectator_mode.md)
**Blocks:** monster tuning, death penalties, rescue gameplay

## Summary

What happens when an intern dies. Currently: they respawn five seconds later, in a competitive-shooter loop. `ServerGameSystem.HandlePlayerDeathAndRespawn` detects `CurrentHealth <= 0`, destroys the player and input entities, attaches a `PendingRespawn { RespawnTimer = 5f }` to the connection, and rebuilds the character when the timer expires.

For Unpaid Interns that is wrong in the most fundamental way available: **it removes all consequence from dying.** Monsters cannot be tuned against a player who returns in five seconds. The quota cannot be threatened. The entire risk calculation collapses.

Death must be permanent for the round, drop what was carried, leave a recoverable body, and move the player to spectating. The body itself then becomes gameplay — a decision for survivors about whether a corpse is worth the trip, which is exactly the kind of grim arithmetic the premise is built on.

**This is a rework, not an addition.** It should be completed before serious monster work begins, because monsters cannot be balanced against a respawning target.

## How to Build

**Replace the respawn path**

- Rewrite `HandlePlayerDeathAndRespawn` in `Assets/Scripts/Networking/Server/ServerGameSystem.cs`. Keep the death *detection* — the query over `PredictedPlayerGhost` and `GhostOwner` is fine — and replace everything after it.
- Remove the `PendingRespawn` attachment for in-round deaths. Retain the component only if it is still wanted for the hub or a between-rounds path; otherwise delete it and `Assets/Scripts/Networking/Shared/PendingRespawn.cs` along with it.
- Do not destroy the player entity immediately. Sequence it: mark dead → drop items → spawn body → transition to spectator → then clean up. Destroying first loses the position and inventory the other steps need.

**Drop the inventory**

- Spawn every carried item as a world item ghost at the death position with a small scatter, so a pile is recoverable rather than interpenetrating.
- Release every item claim held by the dying player, per [`20_networked_interaction_authority.md`](20_networked_interaction_authority.md). An item still claimed by a dead player's `NetworkId` is unpickable by anyone and looks exactly like a lost item.
- Decide whether any category is destroyed rather than dropped. A "your body was consumed" case for certain monsters creates memorable, feared encounters — but define it explicitly rather than letting it emerge from a bug.
- Items must be recoverable by teammates immediately, with no ownership lock.

**Spawn the body**

- Create a body ghost at the death position: a physics ragdoll that can be picked up and carried, as a two-handed item so recovery has a real cost. Build it on the item ghost path in [`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md) and the rule set in [`42_two_handed_item_rule.md`](42_two_handed_item_rule.md) rather than as a parallel implementation — every carry, drop, claim, and contention rule then applies for free, and a bespoke corpse will diverge from them.
- The body's colliders need the same role-separated layers as items, or a host will see two corpses in one physics scene and the extraction zone will register recovery twice.
- If the death occurred out of bounds or in unreachable geometry, spawn the body at the nearest valid position instead — otherwise the crew has been handed a recovery objective inside a wall ([`34_out_of_bounds_handling.md`](34_out_of_bounds_handling.md)).
- Carry the identity of the dead player and, ideally, the cause of death — it is cheap to store and adds enormous flavour when a teammate finds the corpse.
- Depositing the body in the extraction zone counts as recovery, using that zone's inside test rather than a second one ([`43_loot_banking_deposit.md`](43_loot_banking_deposit.md)). Leaving the location without it does not.

**Apply the penalties**

- A death costs the crew credits; an unrecovered body costs more. Percentage-based penalties scale with wealth and stay relevant late in a run.
- Apply penalties at round settlement in the Day Cycle Controller, not at the moment of death — the player needs the rest of the round to fix it, and that recovery window is the point.
- Coordinate with [`08_late_join_rejoin_policy.md`](08_late_join_rejoin_policy.md): a disconnect must not be silently treated as a death unless that rule was chosen deliberately.

**Move the player to spectating**

- Transition the client to spectator on death rather than leaving a black screen. The full treatment — camera ownership, the `AudioListener` problem, follow versus free cam, and repurposing `RespawnScreen.cs` — is [`22_spectator_mode.md`](22_spectator_mode.md). Build it before or alongside this component; permanent death without spectating is eight minutes of nothing for the player who died.
- Update the Crew Roster so `Dead` is distinct from `Disconnected` and `Extracted` ([`19_crew_roster.md`](19_crew_roster.md)), and so the Day Cycle Controller's total-crew-loss check reads it correctly.
- Restore all dead players to alive at the start of the next round — death is permanent for the round, not for the run. That reset happens on the hub transition ([`04_hub_between_rounds_state.md`](04_hub_between_rounds_state.md)), in one place with the other per-round resets.
- Destroy the player character entity rather than keeping it inert. A live-but-disabled character still collides, still replicates, and monsters will still target it.

## Acceptance Criteria

- [ ] A player who dies mid-round does **not** respawn during that round.
- [ ] All carried items drop at the death position and are immediately recoverable by teammates.
- [ ] A carryable body ghost spawns at the death position and can be transported to the extraction zone.
- [ ] Depositing a body registers it as recovered; leaving without it registers as unrecovered.
- [ ] The unrecovered-body penalty is larger than the recovered-body penalty, and both apply at settlement rather than at death.
- [ ] The death sequence runs in the correct order — items and body appear at the right position, not at the origin.
- [ ] The dead player enters spectator mode with a working camera, not a black screen.
- [ ] The Crew Roster distinguishes `Dead` from `Disconnected` and `Extracted`.
- [ ] All interns dying ends the round via the Day Cycle Controller's total-crew-loss path.
- [ ] All dead players return alive at the start of the next round.
- [ ] `PendingRespawn` is either removed or its remaining purpose is documented.
- [ ] Two players dying simultaneously produces two bodies and two correct item drops with no roster corruption.
- [ ] Dying while holding a two-handed item behaves correctly.
- [ ] The body reuses the item ghost and two-handed carry paths, with no duplicated carry, claim, or contention logic.
- [ ] A death out of bounds places a recoverable body at the nearest valid position.
- [ ] Recovery on a host registers exactly once, not once per world.
- [ ] Every item claim held by the dying player is released, and all dropped items are immediately claimable by anyone.
- [ ] The dead player's character entity is destroyed; it does not collide, replicate, or attract monster targeting.
- [ ] Dying while climbing detaches cleanly and drops items at the correct position.
