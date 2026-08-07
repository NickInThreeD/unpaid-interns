# 18 — Player-vs-Player Collision & Friendly Fire Policy

**Source:** [`core_components.md`](../core_components.md) §2 — Player Character
**Status:** ❌ No stated rule — but the current code has an accidental one
**Depends on:** Health & Injury (single damage entry point), Crew Roster
**Blocks:** weapon design, doorway and corridor sizing, chase tuning

## Summary

Can interns block each other in a doorway? Push each other? Shoot each other? Steal from each other?

There is no stated rule, and that is the problem — because the inherited deathmatch code has already answered, silently and wrongly. **Friendly fire is currently on.** `Projectile.cs` damages any object on the `ServerPlayer` layer, checks only that the target is not the shooter (`hitPlayerOwner.NetworkId == projectileData.OwnerNetworkId` → ignore), and on a kill calls `LeaderboardManager.AddKill(shooterNetworkId, targetNetworkId)`. In a co-op game every player is a valid target and every teammate kill scores a point.

This component is mostly a **decision** with a small implementation. It matters because the answers propagate: doorway width in the Procedural Interior Generator, whether weapons can be sold as defensive tools, how a chase resolves when two people run for the same stairwell, and how much griefing surface a public lobby has.

The recommendation, stated plainly so it can be argued with: **soft collision on, friendly fire on but heavily reduced, corpse and dropped-loot theft allowed.** A crew that cannot physically interfere with each other loses the best comedy in the genre; a crew that can instantly kill each other loses to one bad actor. Reduced friendly fire keeps a swung shovel meaningful without making murder efficient.

## How to Build

**Fix the accidental friendly fire first**

- Route all damage through the single server-side entry point required by [`13_health_and_injury.md`](13_health_and_injury.md). Today `Projectile.cs` writes `CurrentHealth` directly in three separate places (the AoE branch and the direct-damage branch, plus the kill bookkeeping), so a policy check would have to be duplicated. Consolidate before adding the rule, not after.
- At that entry point, classify the damage source: monster, environment, self, or teammate. Apply a configurable multiplier per class, sourced from a ScriptableObject config following the `WeaponData` pattern.
- Remove the `LeaderboardManager.AddKill` call from the damage path entirely. Killing a teammate is not a score event; per §8 that plumbing is being repurposed into a performance report, and a kill counter must not survive into it.
- Log teammate damage server-side. Whatever the policy, the crew should be able to find out afterwards who shot whom, and a host should be able to see griefing in a log.

**Decide and implement collision**

- Players are `CharacterController`-based and currently sit on the `ServerPlayer` / `ClientPlayer` layers (`LayerIndex.cs`). Collision between them is governed by the physics layer matrix and by `DisableCharacterDynamicContacts`, which `ServerGameSystem.OnUpdate` destroys on the first update — meaning dynamic contacts are currently enabled.
- Three options, in increasing order of work: **full solid collision** (simple, and body-blocking a doorway during a chase becomes a real event), **soft push-out** (players slowly displace each other, no hard blocking), or **no collision** (pass through teammates entirely).
- Recommended: **soft push-out.** It preserves the physical comedy of crowding a corridor while making it impossible to trap a teammate in a dead end and walk away.
- Whatever is chosen, it constrains level generation: hard collision means the generator must guarantee doorways wide enough for two people fleeing at once, and that is a rule the generator has to be told (§4).
- Collision must be identical on the predicted client and the server, or players will jitter against each other constantly. This is the most likely source of visible prediction error in the whole game — two predicted characters interacting is strictly harder than one character against static geometry.

**Decide the theft rules**

- Can a player pick up an item another player dropped? Almost certainly yes; the alternative requires ownership on world items and breaks corpse recovery.
- Can a player take an item off a corpse, or carry a body and abandon it? Yes, and this is a feature — §14 makes body recovery a decision, and being able to *not* recover is what makes it one.
- Can a player take an item directly out of a living teammate's hands? Recommended no. It is pure griefing with no interesting counterplay.
- All of this is enforced in the Networked Interaction Authority component ([`20_networked_interaction_authority.md`](20_networked_interaction_authority.md)), which is where claims on items are resolved. This component supplies the policy; that one enforces it.

**Add the safety valves**

- Make friendly-fire multiplier and collision mode **host-configurable** in the lobby. Different groups want different games, and this is cheap to expose once the multiplier already exists in config.
- Disable friendly fire entirely in the hub. There is no design reason to allow damage in the safe state, and it is where trolling costs the most goodwill.
- Consider a "damaged by teammate" feed entry through the repurposed `ActionFeed` (§9) so it is visible rather than mysterious.

**Write the decision down**

- Record the chosen answers in this file when made. The whole point of this component is that an unstated rule is worse than either explicit answer, and a plan that also leaves it unstated has not done its job.

## Acceptance Criteria

- [ ] All player damage flows through one server-side entry point that classifies the source before applying it.
- [ ] Teammate damage is scaled by a configurable multiplier, tunable without a recompile, and settable to zero.
- [ ] No kill is recorded to any scoring system when a player damages or kills a teammate.
- [ ] Teammate damage events are logged server-side with attacker, victim, and amount.
- [ ] The chosen collision mode is implemented and documented in this file.
- [ ] Two players pushing against each other in a doorway produces no jitter, snapping, or prediction correction under simulated latency.
- [ ] A player cannot permanently trap a teammate in a dead end (or, if full collision is chosen, this is accepted and documented).
- [ ] Doorway and corridor minimum widths in the generator are stated to match the chosen collision mode.
- [ ] Dropped items and corpses can be picked up by any player; items cannot be taken from a living player's hands.
- [ ] Friendly fire is disabled in the hub.
- [ ] The friendly-fire multiplier and collision mode are host-configurable before the run starts.
- [ ] Self-damage from explosives and falls is unaffected by the teammate multiplier.
- [ ] Four players standing in the same small room maintain stable positions with no accumulating drift.
