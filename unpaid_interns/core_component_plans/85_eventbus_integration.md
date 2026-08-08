# 85 — EventBus Integration

**Source:** [`core_components.md`](../core_components.md) §11 — Technical Foundations, §14 — Shared Package Integration
**Status:** ❌ Not present in this project · **[MVP]**
**Depends on:** acquiring the package from another repository
**Blocks:** Noise Emission, phase events, damage events, quota events — the cross-system wiring of most of the game

## Summary

The shared event bus that project convention requires for cross-system communication, and which this project does not have.

`core_components.md` names it as required *"for all event-driven communication"* and lists the traffic it is meant to carry: noise, damage, item banked, phase changed, quota met. That is most of the game's inter-system wiring, which makes this a foundational dependency rather than a utility — and it is currently **an acquisition problem before it is an integration problem.** The package lives at `C:\Users\nicky\repo\HiddenObject\Assets\Packages\EventBus`, in a different repository, and nothing under `Packages/` or `Assets/` in this project references it.

It also has a real architectural obstacle that `core_components.md` flags directly: **it must interoperate with ECS systems, which cannot hold managed references.** An `ISystem` cannot subscribe to a managed event bus, and the highest-frequency publisher in the game — player movement noise — lives on exactly that side of the boundary.

That combination — a package that is not here, and a boundary it was not designed to cross — is why several plans already hedge against it. [`54_noise_emission_system.md`](54_noise_emission_system.md) says to define the interface it needs and implement it directly until the bus arrives, so swapping it in later is a change of transport rather than a rewrite. That posture is correct and this component should preserve it rather than block on the package.

## How to Build

**Acquire it, deliberately**

- Decide how the package arrives: git submodule, embedded package under `Packages/`, or a copy. [`06_session_persistence.md`](06_session_persistence.md) raises the same decision for `SaveSystem` and notes the trade — a submodule keeps them shared and updatable, a copy will drift. **Make one decision for both packages**, since `SaveSystem.asmdef` references `Packages.EventBus` and they arrive together.
- EventBus is the **leaf dependency**: it has no references of its own, so it comes first and `SaveSystem` follows.
- Both are `autoReferenced: true`, so gameplay assemblies will see them once present. Verify they compile against this project's Unity version before writing any bridge code — an incompatible package discovered after ten systems depend on it is a bad week.
- Read the package before designing against it. Everything below assumes a conventional publish/subscribe surface with a generic `Subscribe<T>()`, which is what `SaveSystem`'s template uses (`EventBusProvider.Instance.EventBus`), but the actual API governs.

**Design the ECS boundary first, because it decides everything else**

This is the component's real work.

- ECS systems cannot hold managed references, so an `ISystem` can neither subscribe nor publish directly. The bridge lives at the `GhostMonoBehaviour` layer, which is exactly where `GhostGameObject` already sits and where the project already crosses this boundary.
- **Two directions, and they are not symmetric.**
  - *ECS → managed* is the common case: movement systems raise noise, the damage entry point raises damage events. Have ECS systems write into a **native queue or event stream** (an `EntityCommandBuffer`-like append-only buffer, or a `NativeQueue` on a singleton), and a MonoBehaviour drain it once per frame and publish to the bus.
  - *Managed → ECS* is rarer: a phase change or quota event that ECS systems need. Prefer **replicated state over events** here — a system that needs to know the round phase should read the ghost field, not subscribe. [`23_shared_session_state_sync.md`](23_shared_session_state_sync.md) already establishes that every consequential event must be derivable from replicated state, which makes this direction largely unnecessary.
- **Do not allocate per event.** Player footsteps, item impacts, and monster state changes are per-tick traffic; a managed allocation per event will show up in the profiler as GC pressure long before anyone suspects the bus. [`54_noise_emission_system.md`](54_noise_emission_system.md) already carries a no-per-event-allocation criterion.

**Be honest about where it does not fit**

- If the bus proves unworkable for high-frequency per-tick events, **say so and use a direct server-side queue for those**, as [`54_noise_emission_system.md`](54_noise_emission_system.md) explicitly permits. Convention is a good default and a bad reason to ship a frame-rate problem.
- The natural split: **low-frequency, cross-cutting, presentation-facing events go on the bus** — phase changed, quota met, item banked, run failed, roster changed. **High-frequency simulation events stay in native queues** — noise, damage, perception.
- Record that split here once it is decided, so it is a design position rather than a series of individual exceptions.

**Do not let it become a hidden control flow**

- An event bus makes it easy to write systems that communicate invisibly, and a game where any system can trigger any other is one where a bug's cause is unfindable. Keep publishers few and named.
- **Never route authoritative state changes through the bus.** Credits, quota, roster state, and item ownership are server-authoritative and mutated through their owners' guarded mutators ([`63_currency_system.md`](63_currency_system.md), [`19_crew_roster.md`](19_crew_roster.md), [`20_networked_interaction_authority.md`](20_networked_interaction_authority.md)). The bus announces that something happened; it does not make it happen.
- Events are **server-raised** for anything with gameplay meaning, matching the rule [`54_noise_emission_system.md`](54_noise_emission_system.md) enforces. A client-raised event that a server system trusts is a cheat vector.
- Add a development-mode event log with a `ConfigVar` filter. Debugging a bus without one means reading call sites.

**Clean up per round**

- Subscriptions must be released on teardown. A leaked subscription on a destroyed `GhostMonoBehaviour` is the classic event-bus memory leak, and in a game that loads and unloads a location every round it will accumulate quickly.
- The `[ResetOnPlayMode]` pattern already used in `GameLeaderboard.cs` for static state is the right shape for anything static here.
- Verify across five consecutive round transitions that subscriber counts return to baseline — the same teardown check every per-round system in the plan carries.

## Acceptance Criteria

- [ ] The acquisition method is decided, documented, and identical for EventBus and SaveSystem.
- [ ] EventBus is present in this project, compiles against its Unity version, and is referenced before SaveSystem.
- [ ] A bridge publishes from ECS to the bus by draining a native queue once per frame.
- [ ] No managed allocation occurs per event in the steady state.
- [ ] The managed-to-ECS direction is either implemented or documented as unnecessary because consumers read replicated state.
- [ ] The split between bus-carried events and native-queue simulation events is decided and recorded in this file.
- [ ] High-frequency per-tick traffic does not measurably affect frame time.
- [ ] No authoritative state change is performed by an event handler; the bus announces, it does not mutate.
- [ ] Gameplay-meaningful events are raised server-side; no server system trusts a client-raised event.
- [ ] A development event log exists with a `ConfigVar` filter and works in a build.
- [ ] All subscriptions are released on teardown, and five consecutive round transitions return subscriber counts to baseline.
- [ ] Static state uses the `[ResetOnPlayMode]` pattern and does not leak across Editor play sessions.
- [ ] Systems that consumed a direct interface before the bus arrived switch to it without changing publishers or consumers.
- [ ] A dedicated-server build raises identical events to a host.
