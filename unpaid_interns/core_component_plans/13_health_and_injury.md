# 13 — Health & Injury System

**Source:** [`core_components.md`](../core_components.md) §2 — Player Character
**Status:** ⚠️ Health exists, injury layer does not · **[MVP]**
**Depends on:** Stamina (injury gates sprinting)
**Blocks:** Death & Body System, monster threat targeting, fall damage

## Summary

Health already works. `PredictedPlayerGhost` carries `CurrentHealth` and `MaxHealth`, the server spawns players at 100, damage flows through `LastHitTick` and `LastDamageAmount`, and `DamageVisualsController` shows a first-person damage vignette while remote players see a hit animation.

What is missing is the **injury layer** — the state between "hurt" and "dead". Right now damage is a number that goes down and then you respawn. In an extraction game, surviving an encounter should leave a mark that changes the rest of the round: slower, unable to sprint, bleeding, forced to decide whether to keep scavenging or cut losses and walk out.

That lasting consequence is what makes a near-miss meaningful. A player who escapes a monster at low health and is then fully functional has learned nothing and risked nothing.

## How to Build

**Add the critical state**

- Define a critical-injury threshold below which the player enters an injured state, stored on `PredictedPlayerGhost` — derive it from `CurrentHealth` rather than adding a separate replicated flag, so the two cannot desynchronize.
- While critically injured: regenerate slowly up to the threshold but no further, block sprinting regardless of stamina, and reduce base movement speed.
- Add a second, lower threshold that forces a heavy limp — a further speed reduction that makes crossing open ground genuinely dangerous.
- Require an item or a hub visit to heal above the threshold, so injury persists as a decision rather than resolving itself if you wait.

**Consider a survival grace rule**

- A single large hit killing a healthy player outright feels arbitrary, especially with latency. A common and effective rule: a hit that would kill a player who is *not already critically injured*, and is below some damage magnitude, leaves them at minimal health instead.
- This grants exactly one escape from a bad moment and shifts the failure from "you were unlucky" to "you did not leave when you should have". Decide explicitly whether to adopt it — it substantially changes the game's lethality.

**Add environmental damage sources**

- **Fall damage is nearly free to implement.** `ControllerState.FallHeight` is already tracked and replicated in `FirstPersonController` — `ShouldUpdateFallHeight` accumulates it during falls and `CachedFallHeight` exposes it — but **nothing consumes it**. Apply banded damage on landing in `GroundedCheck`, where the fall-to-standing transition is already detected.
- Add drowning and instant-death volumes as separate simple damage sources.
- Route everything through one server-side damage entry point so injury rules, penalties, and death handling exist in exactly one place.

**Keep the server authoritative**

- All damage application happens on the server. Clients predict movement, never health.
- Preserve the existing `LastHitTick` pattern for one-shot effects — `FirstPersonController.HandleAnimationEvents` compares it against a locally cached tick to fire hit reactions exactly once, which is the correct approach and should be reused for any new injury feedback.

**Make the state legible**

- Extend `DamageVisualsController` for a persistent injured overlay, distinct from the transient damage flash.
- Add audible injured breathing, and route it into the noise system — an injured player being louder is a meaningful compounding cost.
- Show injury state on the HUD clearly enough that a player knows why they cannot sprint.
- Make injury visible on teammates, so the crew can make decisions about each other.

## Acceptance Criteria

- [ ] Dropping below the critical threshold enters the injured state on server and all clients.
- [ ] Injured players cannot sprint even with full stamina, and move slower.
- [ ] The lower limp threshold applies a further, distinct speed penalty.
- [ ] Health regenerates only up to the critical threshold, never above it, without healing.
- [ ] Healing above the threshold requires the defined item or hub action.
- [ ] The survival grace rule is either implemented or explicitly rejected, and documented.
- [ ] Fall damage is applied in bands from the existing `FallHeight`, with a height above which it is fatal.
- [ ] Drowning and instant-death volumes apply damage correctly.
- [ ] All damage flows through a single server-side entry point.
- [ ] A client cannot alter its own health; the server value always wins.
- [ ] Hit reactions fire exactly once per hit, with no duplicates under latency.
- [ ] Injury state is visible to the injured player and to teammates.
- [ ] Injured breathing is audible and registers in the noise system.
- [ ] Injury persists across the round and does not silently reset on load or phase change.
