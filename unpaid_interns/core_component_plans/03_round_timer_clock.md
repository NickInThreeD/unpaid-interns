# 03 — Round Timer / Clock

**Source:** [`core_components.md`](../core_components.md) §1 — Game Loop & Session State
**Status:** ❌ Not started · **[MVP]**
**Depends on:** Day Cycle Controller (for phase context)
**Blocks:** Spawn Director, difficulty escalation, ambience and time cues, HUD clock

## Summary

A compressed in-round clock. A full workday runs in roughly ten real minutes, and the clock is the shared sense of time pressure that makes staying longer feel expensive.

Its most important output is not the displayed time but **normalized time** — a 0-to-1 float representing how far through the day the round is. Monster spawning, difficulty escalation, lighting, and audio all key off this rather than raw seconds, which means the day length becomes a single tunable number rather than a constant scattered through a dozen systems.

The clock must be driven by `NetworkTick`, not `Time.deltaTime`. Netcode for Entities already maintains a synchronized tick across server and clients; deriving time from it means everyone agrees without extra replication. A clock built on local delta time will drift, and drift here means one intern sees a monster spawn window that another does not.

## How to Build

**Derive time from the network tick**

- Add `Assets/Scripts/Gameplay/Run/RoundClock.cs`. It can live on the Day Cycle Controller ghost rather than needing its own — it is a small amount of state and the two are always read together.
- Replicate only `RoundStartTick` as a `[GhostField]`. Every other value is derived, which keeps bandwidth at a single value regardless of how often the clock is read.
- Compute elapsed time as `(currentTick - roundStartTick) * tickInterval`, reading the tick rate from the netcode configuration rather than hardcoding it.
- Expose `NormalizedTime` as `clamp(elapsed / dayLength, 0, 1)` and a `DisplayTime` that maps normalized time onto the in-fiction workday range.

**Make it configurable**

- Put `DayLengthSeconds` and the in-fiction start and end hours in a ScriptableObject config asset, following the `WeaponData` pattern in `Assets/Data/Weapons/`. Day length is the single most-tuned number in the game and must not require a recompile.
- Allow the day length to be overridden per location, so harder destinations can run shorter or longer days.

**Expose phase boundaries**

- Define named thresholds on the normalized range — morning, midday, dusk, final warning — as data, not hardcoded constants.
- Fire a one-shot event on each boundary crossing through the shared EventBus, so audio stingers and lighting changes hook in without polling.
- Guard boundary firing against being triggered twice if the tick jumps, and against firing on a client that joined after the boundary passed.

**Surface it to players**

- Add a HUD clock element to `PlayerHUD.uxml`, extending `InGameHUD.cs`, which already demonstrates querying ECS state from a `UIDocument`.
- Consider making the clock readable only in certain conditions (outdoors, or via an item) rather than always visible — the design's tension depends on uncertainty, and an always-visible countdown removes it.

**Add debug control**

- Expose `ConfigVar` commands to set normalized time directly, freeze the clock, and multiply its speed. Testing a ten-minute loop without these is prohibitively slow, and every downstream system needs to be testable at arbitrary times of day.

## Acceptance Criteria

- [ ] Normalized time advances smoothly from 0 to 1 over exactly the configured day length.
- [ ] Host and all clients report the same normalized time within one tick's tolerance, verified under simulated latency using the Network Simulator already available in `EntityDriverConstructor`.
- [ ] A client joining mid-round computes the correct current time from `RoundStartTick`, not from zero.
- [ ] Changing `DayLengthSeconds` in the config asset changes round length with no code edit.
- [ ] A per-location day length override is respected.
- [ ] Each phase boundary event fires exactly once per round, on every client.
- [ ] A client joining after a boundary has passed does not retroactively fire that boundary's event.
- [ ] The clock stops advancing when the round is not in an active phase, and does not advance in the hub.
- [ ] Debug commands to set, freeze, and accelerate time all work in a build.
- [ ] The HUD clock matches the underlying normalized time and updates without allocating per frame.
