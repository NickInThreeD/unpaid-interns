# 47 — Physics Props & Throwing

**Source:** [`core_components.md`](../core_components.md) §5 — Items, Loot & Inventory
**Status:** ❌ Not started
**Depends on:** [Item Ghost](38_item_ghost_networked_item_state.md), [Noise Emission System](54_noise_emission_system.md), [Interaction System](41_interaction_system.md)
**Blocks:** distraction as a verb, dropping loot down a shaft, hazard triggering at range

## Summary

Dropped items behaving like objects rather than like markers.

The question this component answers is narrow and consequential: **is "throw the noisy item to distract the monster" possible at all?** In a game whose threat model is audio-first, a thrown object is the cheapest and most satisfying counterplay available — it costs no gear, no ammo, and no cooldown, and it turns a piece of scrap the player was already carrying into a decision. Without physics props, the crew's only tools against a monster are running and hiding, and the sound system is something that happens *to* them rather than something they can use.

The secondary uses are almost as good. Dropping loot down a stairwell to shorten a haul, knocking an item into a hazard to see what it does, and a corpse that falls convincingly rather than sliding are all free once objects have mass.

**Correcting an earlier assumption:** `core_components.md` previously described `com.unity.physics` (DOTS physics) as being used for character collision. It is not. The player is `CharacterController`-based and every gameplay query in the project uses built-in PhysX — `Physics.SphereCastNonAlloc` in `FirstPersonController`, `Physics.SphereCast` and `OverlapSphere` in `Projectile.cs`, `Physics.OverlapSphereNonAlloc` in `ServerGameSystem`, which imports `using Collider = UnityEngine.Collider;` specifically to disambiguate the two. Item physics belongs on **built-in `Rigidbody`**, in the same simulation everything else already uses. Mixing in DOTS physics for items would mean two simulations that cannot collide with each other.

## How to Build

**Simulate on the server, replicate the result**

- Physics is server-authoritative. The server simulates the rigidbody; clients see the outcome. This is the same posture as monsters ([`49_monster_ghost_and_replication.md`](49_monster_ghost_and_replication.md)) and for the same reason — the alternative is four machines disagreeing about where the payday landed.
- Enable `GhostGameObject.RequireTransformSync` **only while an item is in motion**, which is exactly the rule [`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md) already sets. Motion is the expensive state and it is brief; rest is the common state and it is free.
- The transform pipeline is already built for this: `ServerGhostTransformRetrieveSystem` and `ClientGhostTransformApplySystem` batch transforms through a `TransformAccessArray` in jobs, and `GhostGameObjectTransformSync` carries `ErrorOffset` and `ErrorBlendTime` for smoothing a correction. Use that blending rather than snapping — a thrown object that teleports on correction looks worse than one that arrives a frame late.
- **Sleep aggressively.** A settled item's rigidbody must go to sleep and stay asleep; a hundred awake bodies on a loot-dense map is a server frame-time problem before it is a bandwidth problem. Tune sleep thresholds deliberately rather than trusting defaults.
- On the client, item rigidbodies should be **kinematic or absent entirely** — the client is being told where things are, not deciding. Two simulations running the same objects in the same PhysX scene on a host is the doubled-collider problem from [`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md) with momentum added.

**Make throwing a real verb**

- Throw is the `Drop` verb with force applied, not a separate input. Recommended: a **short hold on drop throws**, so the common case (put it down) stays instant and the deliberate case is available without another binding.
- Direction and force come from the camera and a fixed impulse; the **client never chooses the resulting position**. It sends the intent, the server applies the impulse and simulates ([`20_networked_interaction_authority.md`](20_networked_interaction_authority.md) already requires drop positions to be server-validated — this is the same rule with velocity).
- Predict the release animation and the item leaving the hand locally so it feels immediate, and let the server's simulation own where it lands. A throw that pauses before leaving the hand is unusable in a chase, which is the only time it matters.
- Cap throw force by item weight. Hurling a two-handed generator across a room is comedy the physics will not survive.

**Wire it to noise — this is the payoff**

- An item landing raises a noise event with range and volume, consumed by the perception system ([`54_noise_emission_system.md`](54_noise_emission_system.md)). This is the entire point of the component and it must not be an afterthought.
- Scale the noise by impact speed and by the item's material or category, so a thrown object is louder than a dropped one and a metal thing is louder than a soft one. The reference design's tables put a dropped item at range 8 / volume 0.5 and a clown horn at range 60; that spread is what makes item choice matter.
- The noise is raised **on the server**, from the server's simulation, so a modified client cannot throw silently.
- Passive noisemakers ([`37_item_definition_data_model.md`](37_item_definition_data_model.md)) become genuinely interesting here: an item that is loud to carry is a liability you can convert into a decoy by throwing it.

**Contain the damage physics can do**

- **Items must not push players.** A rigidbody that can shove a `CharacterController` is a griefing tool and a source of prediction disagreement, since the player's movement is predicted and the item's is not. Set the collision matrix so items collide with world geometry and each other, and never impart force to characters.
- Decide whether a thrown item can damage a player or a monster. Recommended: **no damage from thrown scrap.** It turns every item into a weapon, undermines [`45_weapons_as_tools.md`](45_weapons_as_tools.md)'s scarcity, and the distraction use is more interesting than the damage use. A thrown item that triggers a *hazard* is a different thing and should be allowed ([`59_static_map_hazards.md`](59_static_map_hazards.md)).
- Items must not carve or obstruct navigation — [`30_runtime_navmesh_baking.md`](30_runtime_navmesh_baking.md) already forbids it. Monsters walk over loot.
- Out-of-bounds containment applies: an item that physics ejects through a seam is returned to a valid position rather than destroyed ([`34_out_of_bounds_handling.md`](34_out_of_bounds_handling.md)). Physics is the *reason* that rule exists.

**Budget it**

- Set a hard cap on simultaneously-awake rigidbodies and a policy for exceeding it — freeze the oldest, do not spawn a hundred-body pile-up. A crew that dumps twenty items in the extraction zone at once is a normal Tuesday.
- Items resting in the extraction zone should be frozen outright once banked. They are scenery at that point and they should cost nothing ([`43_loot_banking_deposit.md`](43_loot_banking_deposit.md) evaluates banking on rest, which is the natural moment to freeze).
- Profile with the maximum authored item count on the floor of one room. That is the worst case and it will happen.

## Acceptance Criteria

- [ ] Item physics runs on built-in `Rigidbody` in the same simulation as character collision, with no DOTS physics involved.
- [ ] Physics simulates on the server only; client-side item bodies are kinematic or absent.
- [ ] Transform sync is enabled only while an item is in motion and disabled within a second of it coming to rest.
- [ ] A moving item's replicated position is smoothed through the existing error-blend fields rather than snapping.
- [ ] Settled items sleep and stay asleep; awake-body count returns to zero when nothing is moving.
- [ ] Throwing is a variant of drop, requires no new binding, and feels immediate on the throwing client.
- [ ] The server owns the resulting trajectory and landing position; a forged client request cannot place an item anywhere it chooses.
- [ ] Throw force is capped by item weight, and two-handed items cannot be thrown far.
- [ ] Landing raises a server-side noise event scaled by impact speed and item category.
- [ ] A modified client cannot throw an item silently.
- [ ] Items never push, block, or impart force to players.
- [ ] Thrown scrap deals no damage to players or monsters, or the contrary decision is documented here.
- [ ] A thrown item can trigger a hazard from a distance.
- [ ] Items never carve or obstruct the NavMesh.
- [ ] An item ejected through geometry is returned to a valid position rather than lost.
- [ ] A cap on simultaneously-awake rigidbodies is enforced with a defined overflow policy.
- [ ] Banked items in the extraction zone are frozen and cost nothing.
- [ ] Twenty items dropped in one room at once holds the server frame budget on the lowest-spec host.
- [ ] Four players throwing items simultaneously under simulated latency produces no desync, duplication, or loss.
