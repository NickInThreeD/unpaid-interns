# 13 — Health & Injury System

**Source:** [`core_components.md`](../core_components.md) §2 — Player Character
**Status:** ⚠️ Health exists, injury layer does not · **[MVP]**
**Depends on:** [Stamina](11_stamina.md) (injury gates sprinting)
**Blocks:** [Death & Body System](14_death_and_body_system.md), [PvP / Friendly Fire](18_pvp_collision_and_friendly_fire.md), [Attack & Damage Application](57_attack_and_damage_application.md), [Fall & Environmental Damage](61_fall_and_environmental_damage.md), [Static Map Hazards](59_static_map_hazards.md), [Out-of-Bounds Handling](34_out_of_bounds_handling.md)

> **This component's single damage entry point is a prerequisite for six others.** Every damage source in the game — projectiles, monsters, falls, drowning, hazards, out-of-bounds — routes through `ApplyDamage(target, amount, source)`, and each of those plans says so. Consolidating the two existing direct writes in `Projectile.cs` **before** adding any new source is what keeps injury rules, the friendly-fire multiplier, and death penalties applied in one place instead of seven.

## Summary

Health already works. `PredictedPlayerGhost` carries `CurrentHealth` and `MaxHealth`, the server spawns players at 100, damage flows through `LastHitTick` and `LastDamageAmount`, and `DamageVisualsController` shows a first-person damage vignette while remote players see a hit animation.

What is missing is the **injury layer** — the state between "hurt" and "dead". Right now damage is a number that goes down and then you respawn. In an extraction game, surviving an encounter should leave a mark that changes the rest of the round: slower, unable to sprint, bleeding, forced to decide whether to keep scavenging or cut losses and walk out.

That lasting consequence is what makes a near-miss meaningful. A player who escapes a monster at low health and is then fully functional has learned nothing and risked nothing.

## How to Build

**Add the critical state**

- Define a critical-injury threshold below which the player enters an injured state, stored on `PredictedPlayerGhost` — derive it from `CurrentHealth` rather than adding a separate replicated flag, so the two cannot desynchronize.
- While critically injured: regenerate slowly up to the threshold but no further, block sprinting regardless of stamina, and reduce base movement speed.
- Add a second, lower threshold that forces a heavy limp — a further speed reduction that makes crossing open ground genuinely dangerous.
- Require an item or a hub visit to heal above the threshold, so injury persists as a decision rather than resolving itself if you wait. The mid-round item is the medical item in [`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md) — until it exists, injury has no counterplay and the "persists as a decision" framing does not hold, so the two should land together.

**Consider a survival grace rule**

- A single large hit killing a healthy player outright feels arbitrary, especially with latency. A common and effective rule: a hit that would kill a player who is *not already critically injured*, and is below some damage magnitude, leaves them at minimal health instead.
- This grants exactly one escape from a bad moment and shifts the failure from "you were unlucky" to "you did not leave when you should have". Decide explicitly whether to adopt it — it substantially changes the game's lethality.

**Add environmental damage sources**

- **Fall damage is nearly free to implement.** `ControllerState.FallHeight` is already tracked and replicated in `FirstPersonController` — `ShouldUpdateFallHeight` accumulates it during falls and `CachedFallHeight` exposes it — but **nothing consumes it**. Apply banded damage on landing in `GroundedCheck`, where the fall-to-standing transition is already detected.
- Add drowning and instant-death volumes as separate simple damage sources. Drowning is triggered by flood water and terrain water ([`35_environmental_conditions_weather.md`](35_environmental_conditions_weather.md)); note `LayerIndex.Water = 4` already exists and is unused, and is the ready-made volume mechanism.
- Out-of-bounds kill volumes are a further source and must carry their own classification so they are never attributed to a teammate ([`34_out_of_bounds_handling.md`](34_out_of_bounds_handling.md)).
- Route everything through one server-side damage entry point so injury rules, penalties, and death handling exist in exactly one place.
- **Enumerate the sources now**, because the classification is what the friendly-fire policy switches on: `Projectile` (teammate or self), `Monster`, `Fall`, `Drowning`, `Hazard`, `OutOfBounds`. Adding a source later must mean adding an enum value and a multiplier, never a new write site.

**Consolidate the existing damage writes first**

- There is no single entry point today. `Assets/Scripts/Gameplay/Player/Projectile/Projectile.cs` writes `CurrentHealth`, `ControllerState.IsHit`, `LastDamageAmount`, and `LastHitTick` directly in **two separate branches** — the area-of-effect path and the direct-damage path — and each one also calls `LeaderboardManager.AddKill` on a lethal hit. Adding monster damage as a third such site, and fall damage as a fourth, is how the injury rules end up applied inconsistently.
- Build `ApplyDamage(target, amount, source)` on the server, move both projectile branches onto it, and make every future source use it: monsters, falls, drowning, hazards.
- The `source` argument is not decoration. [`18_pvp_collision_and_friendly_fire.md`](18_pvp_collision_and_friendly_fire.md) needs it to classify teammate damage and apply the friendly-fire multiplier — which is currently impossible, because the projectile code damages any player who is not the shooter with no policy check at all.
- Remove the `AddKill` call while consolidating. Kill scoring is deathmatch semantics; §8 repurposes that plumbing into a performance report and a teammate kill must not enter it.

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
- [ ] All damage flows through a single server-side entry point, including both existing projectile branches.
- [ ] The entry point classifies the damage source, so the friendly-fire policy can be applied in one place.
- [ ] No damage path writes `CurrentHealth` directly, and no damage path records a kill to a scoring system.
- [ ] A client cannot alter its own health; the server value always wins.
- [ ] Hit reactions fire exactly once per hit, with no duplicates under latency.
- [ ] Injury state is visible to the injured player and to teammates.
- [ ] Injured breathing is audible and registers in the noise system.
- [ ] Injury persists across the round and does not silently reset on load or phase change.
