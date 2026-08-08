# 17 — Climbing & Verticality

**Source:** [`core_components.md`](../core_components.md) §2 — Player Character
**Status:** ❌ Not started
**Depends on:** [Crouch](10_crouch.md) (movement-state pattern), [Two-Handed Item Rule](42_two_handed_item_rule.md), [Interaction System](41_interaction_system.md)
**Blocks:** multi-level interiors, deployable-ladder gear, fall-damage counterplay

## Summary

Ladders and climbable surfaces. Without them, every generated interior is effectively single-storey, or connected only by stairwells the generator must guarantee — a hard constraint on the Procedural Interior Generator and a large loss of tension. Vertical space is where hiding, dropping loot down a shaft, and being cornered all live.

Nothing exists. `FirstPersonController.MovementType` has only `Standing`, `Jumping`, and `Falling`. A `DEBUG_RENDER_CLIMBING_MOVEMENT` symbol is referenced in the debug-rendering guard near line 232, which suggests the upstream sample once had climbing, but **no climbing code is present in this project** — treat that define as a leftover, not a starting point.

The reason this component earns its place beyond traversal is the **interaction with two-handed items**. §5 specifies that bulky, high-value items occupy both hands and lock out interactions including ladders. That rule only means something if ladders exist. A player at the bottom of a shaft holding the single most valuable object in the building, who must choose between dropping it and finding another route, is the game working exactly as designed.

## How to Build

**Add the movement state**

- Add `Climbing` to `FirstPersonController.MovementType`, following the process in [`10_crouch.md`](10_crouch.md). The same warning applies: `AccumulateJumpAndGravity`, `AccumulateMovement`, and `GetStateConsts` all have `default` branches that log errors, so a partial addition produces console spam rather than a clean failure.
- Add a `Climb` block to `ControllerConsts.StateConsts` beside `Walk` and `Sprint`, with its own speed and animation scale. Climbing is deliberately slow — the vulnerability is the point.
- **Suppress gravity while climbing.** `AccumulateJumpAndGravity` must not accumulate fall speed in this state, and `state.FallHeight` must not accumulate either, or dismounting a tall ladder will apply fall damage the player never earned (see `ShouldUpdateFallHeight`, which is the function to extend).
- Keep the state derived from a replicated attachment (the ladder ghost being climbed) rather than a new `ControllerState` member, respecting the serialization warning at lines 59 and 148. A ladder reference belongs on `PredictedPlayerGhost`, which is the established home for gameplay state.

**Define climb volumes**

- A ladder is a trigger volume with an axis, a base, a top, and a dismount point at each end. Author it as a prefab component so both hand-built and procedurally-placed ladders use one implementation.
- Entering requires an explicit interact, not a walk-in. Accidental climbing during a chase is infuriating and will happen constantly with proximity attachment.
- On attach, snap the player onto the ladder axis with a short lerp rather than a teleport, and lock horizontal input to the axis.
- Dismount conditions: reaching either end, pressing the interact/jump input, or taking damage. **Falling off when hit is a real design lever** — decide it deliberately rather than leaving it to physics.

**Get prediction right — this is the risky part**

- Climbing is movement, so it is client-predicted through `PlayerPredictionSystem` and `ServerPlayerMovementSystem`. Both must run identical arithmetic, including the attach and detach transitions.
- The attach *decision* is the hazard: if the client attaches on a frame the server does not, the player will snap. Predict attachment optimistically from the same replicated ladder state the server uses, and accept the server's correction.
- Ladder volumes generated at runtime must exist on both server and client worlds before anyone can climb — coordinate with [`05_location_load_unload_flow.md`](05_location_load_unload_flow.md)'s load barrier so no client can reach a ladder that has not finished baking.
- Test under simulated latency with the network simulator available via `EntityDriverConstructor`.

**Enforce the hands rule**

- Block climbing entirely while holding a two-handed item, with a clear prompt explaining why. Silent refusal will read as a bug.
- Decide whether a one-handed item is retained while climbing. Retaining it is more forgiving and avoids accidental loot loss; forcing a drop is more dramatic. Recommended: retain, and let the two-handed rule carry the tension.
- Coordinate with [`41_interaction_system.md`](41_interaction_system.md): the interact verb is shared between "climb this" and "pick that up", so targeting priority must be defined once, in one place — that file owns the priority order, and a ladder declares its priority as data rather than special-casing the raycast here.

**Deployable ladders as gear**

- [`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md) lists a ladder as purchasable equipment. Reuse the same climb volume, spawned at runtime by the item rather than by the generator.
- A deployed ladder is world state: it must be a ghost so every client sees it, it must survive its owner's death, and it must be cleaned up on round end.
- A deployed ladder also creates a **navigation link** that did not exist when the interior was baked. [`30_runtime_navmesh_baking.md`](30_runtime_navmesh_baking.md) requires that link to be added on placement and removed at round end, with per-monster traversal permissions — so deploying a ladder can hand a monster a route the crew did not intend to open. That is a good mechanic and a terrible surprise; decide which it is and telegraph it.

**Presentation**

- Add a climbing animation state to the 3P rig, or remote players will glide up ladders in a standing pose.
- Camera: constrain look while climbing, but not so tightly that a player cannot check behind them. Being unable to look back while something approaches is scary; being unable to look back *at all* is bad camera design.
- Add climb footstep/handhold audio and route it into the noise-emission system at a low volume — climbing should be quieter than walking, giving it a use beyond traversal.

## Acceptance Criteria

- [ ] `Climbing` exists in `MovementType` and is handled in every switch, with no `default`-branch error logs.
- [ ] Gravity and fall-height accumulation are both suppressed while climbing; dismounting from height applies no spurious fall damage.
- [ ] Attaching requires an explicit interact and never triggers by proximity alone.
- [ ] Climb speed is tunable from the controller constants without a code change.
- [ ] Dismount works at both ends, on interact, and on the chosen damage rule, which is documented.
- [ ] Climbing is blocked while holding a two-handed item, with a visible prompt explaining why.
- [ ] Attach and detach are predicted with no visible snap or rubber-band under simulated latency.
- [ ] Two players can use the same ladder simultaneously, or the rule preventing it is enforced and explained.
- [ ] Remote clients see a climbing teammate in the correct pose.
- [ ] A deployed ladder item replicates to all clients, persists after its owner dies, and is cleaned up at round end.
- [ ] A deployed ladder adds a navigation link on placement and removes it at round end, and which monsters may use it is data, not an accident.
- [ ] Procedurally-placed ladders exist on server and client worlds before the round begins.
- [ ] Climbing noise registers in the noise system at a lower level than walking.
- [ ] Dying while climbing drops the player and their items correctly, per [`14_death_and_body_system.md`](14_death_and_body_system.md).
