# Unpaid Interns — Codebase Audit

Findings from reading the Unity project before writing the component plans in [`core_component_plans/`](core_component_plans/). Everything below was verified against source, not inferred from documentation.

This complements [`core_components.md`](core_components.md) §0, which describes *what the project is*. This file records **the specific code-level facts the plans were built on**, including several that contradict or extend the earlier write-up. Where a finding changed a plan, the plan is linked.

---

## 1. Architecture — confirmed

- **Netcode for Entities, not Netcode for GameObjects.** `Packages/manifest.json` pins `com.unity.netcode` 1.10.0, alongside `com.unity.entities`-based `com.unity.physics` 1.4.4, `com.unity.charactercontroller` 1.4.2, and `com.unity.services.multiplayer` 2.1.3.
- **The GhostBridge layer is real and complete.** `Assets/Scripts/GhostBridge/` provides `GhostGameObject` (998 lines), `GhostMonoBehaviour`, `GhostSpawner` (594 lines), `ManagerGhostsSpawner`, and the client/server update split. This is the pattern all new networked gameplay must follow.
- **`LeaderboardManager` is the working reference for a replicated manager singleton.** `Assets/Scripts/Gameplay/Leaderboard/GameLeaderboard.cs` demonstrates every piece a new manager needs: `GhostMonoBehaviour` + `IGhostManager` + `IUpdateServer`/`IUpdateClient`, a ghost dynamic buffer (`PlayerScoreEntry`), server-role guards, `IsGhostLinked()` checks, `BroadcastRPC`/`ConsumeRPC`, and the `[ResetOnPlayMode]` static-state reset.
- **A deferred-write queue is needed, and the reference shows why.** `LeaderboardManager` holds `_pendingPlayers`, `_killQueue`, and `_joinedQueue` because calls arrive before the ghost links — `UpdateServer` logs `"LeaderboardManager not linked yet"` and returns. Any new manager will hit the same window.
- **Addressables is genuinely in use, not just installed.** `GhostSpawner.GhostReference` wraps an `AssetReferenceGameObject` with a serialized `Hash128` GUID, and prefabs resolve through it at runtime.

## 2. Findings that changed the plans

These are the discoveries that materially altered what was written.

### 2.1 The input pipeline is OR-accumulating and prediction-replayed

The single most consequential finding, and it is undocumented in the code.

- `PlayerInput.UpdateFrom` is `InputFlags |= input.InputFlags` (`PlayerCommandInput.cs:36-39`) — it **sets bits and never clears them**.
- `ClientInputSenderSystem` uses that in two places: on a new tick, `commandInput.SetFrom(current)` then `UpdateFrom(inProgressCommandInput)` folds in flags raised during intervening client frames; within an already-sent tick, `existingCommandData.UpdateFrom(current)` refreshes it. `inProgressCommandInput` resets to `default` each new tick, so flags do **not** latch across ticks — but within a tick a bit can only turn on.
- Consequence 1: a **held** flag releases up to one tick late.
- Consequence 2: `PlayerPredictionSystem` replays buffered ticks during reconciliation, so a tick's flags are processed **more than once** on the client. Any verb with a side effect beyond movement — interact, drop, scan, toggle-crouch — must be idempotent per tick.
- The existing code already solves this shape with server tick stamps (`LastShotTick`, `LastJumpTick`, `LastReloadTick`) compared against a cached tick in `HandleAnimationEvents`.

→ Documented in [`09_sprint.md`](core_component_plans/09_sprint.md), applied in [`10_crouch.md`](core_component_plans/10_crouch.md) (toggle crouch), [`11_stamina.md`](core_component_plans/11_stamina.md) (exhaustion threshold), [`20_networked_interaction_authority.md`](core_component_plans/20_networked_interaction_authority.md).

### 2.2 Friendly fire is already on, by accident

- `Projectile.cs` builds `_hitLayerMask = LayerMask.GetMask("ServerPlayer", "Ground", "Default")` and damages **any** player it hits, checking only that the target is not the shooter (`hitPlayerOwner.NetworkId == projectileData.OwnerNetworkId` → ignore).
- On a lethal hit it calls `LeaderboardManager.Instance.AddKill(shooterNetworkId, targetNetworkId)` — so a teammate kill currently *scores a point*.
- There is no team concept anywhere in the damage path.

→ [`18_pvp_collision_and_friendly_fire.md`](core_component_plans/18_pvp_collision_and_friendly_fire.md).

### 2.3 There is no single damage entry point

- `Projectile.cs` writes `CurrentHealth`, `ControllerState.IsHit`, `LastDamageAmount`, and `LastHitTick` **directly, in two separate branches** — the area-of-effect path and the direct-damage path — each with its own duplicated kill bookkeeping.
- Adding monster damage, fall damage, drowning, and hazards as further such sites would make injury rules and penalties inconsistent by construction.

→ [`13_health_and_injury.md`](core_component_plans/13_health_and_injury.md) now requires consolidating these **before** adding sources, and carrying a damage `source` so the friendly-fire policy has one place to live.

### 2.4 `RemovePlayer` deletes the row a rejoin would need to match

- `ServerGameSystem.RefreshClientsMap` calls `RemovePlayerFromLeaderboard(networkId)` on disconnect, and `LeaderboardManager.RemovePlayer` does `buffer.RemoveAt(i)`.
- Correct for a deathmatch. Fatal for a run-based game: the roster entry is what a reconnect matches against and what a disconnect penalty applies to.

→ [`19_crew_roster.md`](core_component_plans/19_crew_roster.md) forbids deleting entries on disconnect.

### 2.5 A disconnect would have ended the round

- On `ConnectionState.State.Disconnected`, `RefreshClientsMap` destroys the player character entity (found via `GhostOwner`) and the input entity (found via `PlayerCommandTarget`), then clears the `ClientsMap` slot.
- So a "count live player entities" check reaches zero for a crew that is merely offline — and the total-crew-loss path would end the round under everyone.

→ [`02_day_cycle_controller.md`](core_component_plans/02_day_cycle_controller.md) now reads `AnyAliveInField()` from the roster instead.

### 2.6 `FixedRandom` is wall-clock seeded and shared

- `ServerGameSystem.OnCreate` creates a `FixedRandom` singleton from `Random.CreateFromIndex((uint)DateTime.Now.Millisecond)` — server-only, never replicated.
- `FindSpawnPoint` draws from it on every join to shuffle spawn points, so its state depends on **how many people joined and when**.
- Reusing it as the generation seed would make layouts non-reproducible and couple them to join order.

→ [`29_deterministic_generation_seed.md`](core_component_plans/29_deterministic_generation_seed.md) leaves it alone and adds a separate replicated round seed with per-system derived streams.

### 2.7 The camera disappears with the player entity

- `MainCameraSystem` declares `RequireForUpdate<MainCamera>()` and drives `MainCameraSingleton.Instance` from the `MainCamera` singleton entity, which lives on the local player.
- When the player entity is destroyed the singleton is gone, the system stops, and the camera is left wherever it was — which is why `RespawnScreen` needs an entire second `RespawnCamera` GameObject.
- `MainCameraSingleton` is `[RequireComponent(typeof(Camera), typeof(AudioListener))]`, so a second camera means a second `AudioListener` — a real bug in a game whose threat detection is audio-first.

→ [`22_spectator_mode.md`](core_component_plans/22_spectator_mode.md) keeps one camera and moves the `MainCamera` component to the spectator entity.

### 2.8 `WeaponRegistry` ids are list positions

- `WeaponRegistry.GetWeaponData(uint weaponID)` returns `Weapons[(int)weaponID]`. The id **is** the index, so reordering the list silently reassigns every id.
- Survivable with two weapons. A disaster once a save file stores which locations are unlocked, or a ghost field carries an item id across a version boundary.

→ [`26_location_catalogue.md`](core_component_plans/26_location_catalogue.md) copies the ScriptableObject-plus-registry pattern but mandates an explicit serialized `Id` and a dictionary built at load.

## 3. Gaps confirmed by reading, not assumed

- **Sprint constants are genuinely dead.** `ControllerConsts` defines a full `Sprint` block of `StateConsts` beside `Walk`, but `GetStateConsts` (line 662) has one switch where `Standing`, `Jumping`, and `Falling` all fall through to `stateConsts = consts.Walk`. `consts.Sprint` is never read.
- **`1 << 2` is free.** `PlayerInput.InputFlag` is `Jump = 1 << 0`, `Shoot = 1 << 1`, `Reload = 1 << 3`. The gap is available, and `ProcessGameplayInput` wires exactly those three actions.
- **`FallHeight` is tracked and consumed by nothing.** `ControllerState.FallHeight` accumulates in `AccumulateJumpAndGravity`, resets on state change, and is exposed as `CachedFallHeight` — a repo-wide grep finds **no reference outside `FirstPersonController.cs`**. Fall damage is close to free.
- **`MovementType` has three values** — `Standing`, `Jumping`, `Falling`. No crouch, no climb. Every switch over it has a `default` branch that calls `Debug.LogError`, so a partial addition spams the console rather than failing cleanly.
- **`DEBUG_RENDER_CLIMBING_MOVEMENT` is a leftover.** Referenced in the debug-rendering guard at line 232; no climbing code exists in the project.
- **The `ControllerState` serialization warning is real and doubled.** The comment *"Adding more members to this struct might break network serialisation speak to Claire/Andy B"* appears twice, at the top and bottom of the struct. New per-player gameplay state belongs on `PredictedPlayerGhost`, which already holds health, ammo, weapon id, and tick stamps.
- **Zero AI code.** `com.unity.ai.navigation` 2.0.11 is installed; a repo-wide grep for `NavMesh` across `Assets/Scripts` returns **nothing**.
- **Zero tests.** `com.unity.test-framework` 1.6.0 is installed; there is no test assembly or test file anywhere under `Assets`.
- **No voice package.** Nothing Vivox or WebRTC in the manifest. `com.unity.services.multiplayer` covers sessions/Relay/Lobby only.
- **No EventBus, no SaveSystem.** Neither is in `Packages/manifest.json` nor under `Assets`. Both are referenced by project convention as living in a *different repository* (`HiddenObject`), which is an acquisition problem before it is an integration problem.
- **No options screen.** No `Settings.uxml` among the UI Toolkit assets, and mouse sensitivity is `const float sensitivity = 3.7f` at `ClientInputReaderSystem.cs:78`.
- **`GlobalGameState` has three values** — `MainMenu`, `InGame`, `Loading` (`GameSettings.cs:9`). There is no between-rounds concept.
- **`ScenesLoader` loads one scene, once.** `LoadSceneAsync(GameManager.GameSceneName, …)` with `GameSceneName = "GameScene"` hardcoded at `GameManager.cs:21`. `UnloadGameplayScenesAsync` exists but resolves the same hardcoded name.
- **`GameManager.MaxPlayer = 32`**, consumed by `SessionOptions.MaxPlayers` in three places (`GameConnection.cs:48`, `:130`, `UGS_ServerBootstrap.cs:73`). A deathmatch number in a 4-player co-op design.
- **Respawn is 5 seconds, hardcoded twice.** `PendingRespawn { RespawnTimer = 5f }` in `ServerGameSystem`, and `RESPAWN_DURATION = 5.0f` in `RespawnScreen.cs:20` — a client-side duplicate that must be kept in sync by hand.
- **Player layers exist and are minimal.** `LayerIndex.cs`: `ServerPlayer = 3`, `ClientPlayer = 6`, `Ground = 7`, `FirstPersonOverlay = 8`. No team or monster layers yet.

## 4. The genuinely reusable parts

Worth stating plainly, because the gap list is long and the foundation is not the problem:

- **Session and connection layer.** Relay create-or-join, direct host/connect, and dedicated-server bootstrap all implemented. `EntityDriverConstructor` applies `WithRelayParameters` to both drivers and also exposes a **network simulator** — which is what makes "verify under simulated latency" a realistic acceptance criterion throughout the plans.
- **Prediction and reconciliation.** `PlayerPredictionSystem`, `PlayerMovementHistory`, `ProjectileReconciliationSystem`, and the error-smoothing fields on `PredictedPlayerGhost` (`AppliedError`, `ErrorTimeout`, `RotationError`). New predicted state plugs in here.
- **The full-screen effect pipeline.** `DamageVisualsController` builds a runtime material, wraps it in `FullScreenPassWrapper`, and injects via `RenderPipelineManager.beginCameraRendering` filtered to the owning player's camera — reusable as-is for a fear overlay, and it early-outs at zero intensity so it costs nothing when idle.
- **Subscene load synchronisation.** `WaitForAllSubScenesToLoadAsync` polls `SceneSystem.IsSceneLoaded` per `SceneReference` entity **per world**, which is the correct check and the right foundation for a per-round load barrier.
- **Audio.** Pooled emitters, `SoundDef` assets, mixer routing, and a headless no-op path. Presentation-only — it plays sound, it does not model what entities can hear.

## 5. Open questions the code cannot answer

Recorded because each one blocks a plan from being finished rather than merely written:

- Target crew size — `MaxPlayer = 32` is certainly wrong, but nothing states the intended number.
- Whether weapons remain a pillar or become rare defensive tools. A complete predicted weapon stack exists; the answer decides how much of §5 is reuse versus removal.
- Whether locations are assigned or chosen. `GAME_DESIGN.md`'s pitch implies assigned; its own open-questions section leaves it undecided.
- What happens on a mid-round disconnect — loot, penalty, body, rejoin. The cleanup code is correct and carries no gameplay meaning.
- Whether interns can hurt each other. The code currently says yes, and nothing indicates that was a decision.
