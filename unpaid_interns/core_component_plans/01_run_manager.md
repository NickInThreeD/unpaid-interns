# 01 — Run Manager

**Source:** [`core_components.md`](../core_components.md) §1 — Game Loop & Session State
**Status:** ❌ Not started · **[MVP]**
**Depends on:** nothing — this is the root of the game loop
**Blocks:** Day Cycle Controller, Hub State, Quota, Selling, Game Over

## Summary

The Run Manager owns a single contract from first deployment to the moment the crew succeeds or dies. It is the authoritative record of *where the team is in the run*: which day it is, how many days remain before the deadline, how much money the team has, what the current quota is, and whether the run is still alive.

Everything else in the game loop reads from it. The Day Cycle Controller asks it whether another round should start; the store asks it how much money there is; the end-of-round summary asks it whether quota was met. It holds no per-round state — that belongs to the Day Cycle Controller — and it holds no per-player state.

Because the quota is collective and the failure condition applies to the whole crew, this state must be **server-authoritative and identical on every client**. It is built as a ghost-replicated manager singleton, following the `LeaderboardManager` pattern already working in the project.

## How to Build

**Create the manager ghost**

- Add `Assets/Scripts/Gameplay/Run/RunManager.cs` as a `GhostMonoBehaviour` implementing `IGhostManager`, `IUpdateServer`, and `IUpdateClient` — mirror `Assets/Scripts/Gameplay/Leaderboard/GameLeaderboard.cs`, which is the working reference for this pattern.
- Give it a `static Instance` set in `Awake`, with the duplicate-instance guard used by `LeaderboardManager`.
- Apply `[ResetOnPlayMode(resetMethod: "ResetStaticState")]` and clear any static queues in that method, so entering play mode repeatedly in the Editor does not leak state.
- Create `Assets/Prefabs/ActorGhosts/RunManager.prefab` alongside `Leaderboard.prefab`, and register it in the `ManagerGhostsSpawner.ManagersToSpawn` list on its prefab in the scene. `ManagerGhostsSpawner` asserts the prefab implements `IGhostManager`, so the interface is mandatory.

**Define the replicated state**

- Declare an `IComponentData` struct with `[GhostField]` on each replicated value: `CurrentDay`, `DaysUntilDeadline`, `TeamCredits`, `CurrentQuota`, `QuotaProgress`, `QuotasCompleted`, and a `RunState` enum (`NotStarted` / `InProgress` / `Failed` / `Succeeded`).
- Keep every field a blittable value type — no strings, no managed references. Use `FixedString` types if text is ever required.
- Read on clients via `GhostGameObject.ReadGhostComponentData<T>()`; write only on the server, guarded by `Role == MultiplayerRole.Server`.

**Implement the operations**

- `StartRun()` — resets to day 1, seeds starting credits and the first quota, sets state to `InProgress`. Server only.
- `AdvanceDay()` — decrements `DaysUntilDeadline`, increments `CurrentDay`, and evaluates the deadline.
- `AddCredits(int)` / `SpendCredits(int)` — the only mutation path for money; reject spends that exceed the balance and return success so callers can react.
- `RecordQuotaProgress(int)` — accumulates the value banked and sold this cycle.
- `EvaluateDeadline()` — on reaching the deadline, either roll to the next quota cycle or set state to `Failed`.
- Guard every mutator with the server-role check and an `IsGhostLinked()` check, exactly as `LeaderboardManager.AddKill` does — calls arriving before the ghost links must fail loudly, not silently corrupt state.

**Wire it up**

- Publish state transitions (day advanced, quota met, run failed) through the shared EventBus so UI and audio can react without holding a reference to the Run Manager.
- Add debug commands via the existing `ConfigVar` system: grant credits, force-advance the day, set the quota, force a run failure.

## Acceptance Criteria

- [ ] A `RunManager` ghost spawns automatically on the server at session start and links on all clients, verified by log output on both roles.
- [ ] Host and every connected client display identical values for day, credits, quota, and days remaining at all times.
- [ ] A client that joins mid-session receives the correct current values, not defaults.
- [ ] Calling any mutator from a client is rejected with a warning and does not change server state.
- [ ] Credits never go negative; a spend larger than the balance is refused and reports failure.
- [ ] Advancing past the deadline with quota unmet sets `RunState` to `Failed` on all clients.
- [ ] Advancing past the deadline with quota met increments `QuotasCompleted`, raises the quota, and resets the cycle.
- [ ] State survives a full round transition — values are not reset by loading or unloading a location.
- [ ] Entering and exiting play mode repeatedly in the Editor produces no leaked static state and no duplicate instances.
- [ ] Debug commands for granting credits and forcing day advance work and are usable from a build.
