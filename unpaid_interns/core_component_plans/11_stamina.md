# 11 — Stamina System

**Source:** [`core_components.md`](../core_components.md) §2 — Player Character
**Status:** ❌ Not started · **[MVP]**
**Depends on:** Sprint
**Blocks:** Carry Weight, the entire risk/reward calculus

## Summary

A depleting resource that gates sprinting and jumping, drains faster the more you carry, and refills when you slow down.

This is the mechanic that converts *"grab one more item"* from a free choice into a real decision. Without stamina, carry weight has nothing to act on, fleeing a monster has no cost, and the extraction trip is a formality. Almost every tension in the design routes through this one number.

It must be **client-predicted**. Stamina gates sprint, sprint is predicted, so stamina that only exists on the server would produce a visible stutter every time the bar empties. It must also be **server-authoritative**, because a client that can edit its own stamina can sprint forever.

## How to Build

**Store it as predicted, replicated state**

- Add a `Stamina` float to `PredictedPlayerGhost` in `Assets/Scripts/GhostBridge/Player/PredictionComponents.cs`, marked `[GhostField]`, beside the existing `CurrentHealth` and `MaxHealth`.
- Prefer `PredictedPlayerGhost` over `ControllerState` — the latter carries the explicit *"adding more members might break network serialisation"* warning at lines 59 and 148, while `PredictedPlayerGhost` already holds gameplay values like health and ammo and is the natural home.
- Store as a normalized 0–1 value. It is simpler to reason about, cheaper to quantize for replication, and maps directly to a UI bar.
- Update it inside the prediction loop so both `PlayerPredictionSystem` and `ServerPlayerMovementSystem` run identical arithmetic. Any divergence produces correction stutter exactly when the player is panicking, which is the worst possible moment.

**Define the curve**

- Drain while sprinting, scaled by carry weight (see [`12_carry_weight.md`](12_carry_weight.md)). A per-second rate multiplied by a weight factor is sufficient and readable.
- Charge a flat cost per jump, so jump-spamming is self-limiting.
- Regenerate when not sprinting, faster when standing still than when walking — this rewards stopping, which is exactly the behavior that makes players vulnerable and is therefore good design.
- Put every rate in a ScriptableObject config, following the `WeaponData` pattern in `Assets/Data/Weapons/`. These numbers will be retuned constantly.

**Define the exhaustion state**

- Below a low threshold, block sprinting and jumping entirely.
- Require recovery to a **higher** threshold before sprinting is allowed again. Without this hysteresis, a player at the boundary will flicker in and out of sprint every frame.
- Consider a visible and audible exhaustion cue — laboured breathing doubles as a noise source monsters can hear, which turns exhaustion into a compounding risk rather than a flat speed penalty.

**Connect it to the world**

- Feed sprint and exhaustion states into the noise-emission system: sprinting should be substantially louder than walking.
- Add a stamina bar to `PlayerHUD.uxml` and read it in `InGameHUD.cs`, which already queries `PredictedPlayerGhost` for health and ammo — the same pattern extends directly.
- Consider deliberately under-representing the true value in the UI so players cannot precisely optimize against it; uncertainty is a feature in a horror game.

**Guard it**

- Clamp to the 0–1 range on every write. An unclamped negative value will silently break the exhaustion comparison.
- The server is authoritative: its value wins on reconciliation, always.

## Acceptance Criteria

- [ ] Stamina drains while sprinting and regenerates when not, at the configured rates.
- [ ] Regeneration is faster standing still than walking.
- [ ] Jumping costs a fixed amount and is blocked when stamina is insufficient.
- [ ] Reaching the exhaustion threshold blocks sprinting and jumping until the higher recovery threshold is reached.
- [ ] There is no flicker between sprinting and exhausted at the boundary.
- [ ] Stamina is predicted client-side with no visible correction or stutter under simulated latency.
- [ ] A client cannot gain stamina by local modification; the server value wins on reconciliation.
- [ ] The value stays clamped to 0–1 under all conditions, including heavy load and repeated jumping.
- [ ] All rates and thresholds are tunable from a config asset without recompiling.
- [ ] The HUD bar reflects stamina and updates without per-frame allocation.
- [ ] Sprinting produces louder noise than walking in the noise system.
- [ ] Carry weight measurably changes drain rate once component 12 is in place.
