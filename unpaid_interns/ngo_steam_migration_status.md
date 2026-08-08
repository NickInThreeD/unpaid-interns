# Migration status — NGO + Steam

**Branch:** `feature/ngo-steam-migration` (from `origin/main`)
**Commit:** `103053d` — 161 files changed, +2360 / −10128
**Date:** 2026-08-08
**Plan:** [`ngo_steam_migration.md`](ngo_steam_migration.md)

**State:** Phases 0–2 complete. Phase 3 written. The 106 compile errors across the 13 files below
have been fixed — see [Compile-error pass (2026-08-08)](#compile-error-pass-2026-08-08) for what
each fix actually did and where it narrowed scope. **This was not verified with a real compiler**:
this environment has no Unity install, no `dotnet`, and no UnityMCP bridge, so every fix here is a
careful manual read, not a green build. Opening the project in the Editor is the first thing the
next session should do.

---

## The headline result

**The vendored transport compiles against NGO 2.13.1.** This was the migration's
single biggest open risk and it is now closed with evidence, not inference:

| Assembly | Before | After |
|---|---|---|
| `SteamNetworkingSockets Transport for Netcode for GameObjects.dll` | 4,096 B (empty — `#if` stripped everything) | **14,848 B** |
| `Unity.Netcode.Runtime.dll` | absent | 669,696 B |
| `com.rlabrecque.steamworks.net.dll` | absent | 425,984 B |

Zero errors from the transport itself. The plan's API-compatibility table was
correct on every row.

**Runtime behaviour is still unproven.** That needs two machines on different
networks and cannot be done from here. It remains the top risk.

---

## Three findings that correct the plan

These cost real time to discover. Do not re-derive them.

### 1. Phase 1 as written is impossible — NGO and NGE cannot coexist

The plan says: *"Prove the transport (do this before deleting anything) … old
stack still intact."* That cannot be done. The two packages collide on assembly
name:

```
Assembly with name 'Unity.Netcode.Editor' already exists
  (Packages/com.unity.netcode.gameobjects/Editor/Unity.Netcode.Editor.asmdef)
Assembly with name 'Unity.Netcode.Editor' already exists
  (Packages/com.unity.netcode/Editor/Unity.NetCode.Editor.asmdef)
```

`Unity.Netcode.Editor` vs `Unity.NetCode.Editor` differ only in case, and the same
collision hits `.Tests`. While both are installed **all compilation is blocked** —
which presents confusingly, because the transport DLL silently stays empty rather
than erroring. Phases 1 and 2 must merge: strip first, then verify.

### 2. Steamworks.NET's UPM package does not ship `SteamManager.cs`

The plan suggests it as "an acceptable starting point". It only exists in the
standalone `.unitypackage`. The UPM package ships the API wrapper and
`CallbackDispatcher` but no lifecycle driver, so
`Assets/Scripts/Networking/Steam/SteamManager.cs` is written from scratch here.

### 3. `ConfigVar`, `Singleton<T>` and `ResetOnPlayMode` are not ECS code

They lived in `Assets/Scripts/GhostBridge/Utils/`, which the plan says to delete
in its entirety. But they are used by `Audio/SoundMixer.cs` and
`Input/InputSystemManager.cs` — both subsystems the plan lists as **untouched**.
Deleting the folder wholesale breaks audio and input.

They are preserved into `Assets/Scripts/Utility/`, exactly as `Utils.cs`'s two
survivors were. None of the three has any ECS dependency.

---

## Decisions taken

| Decision | Value | Consequence |
|---|---|---|
| Crew size | **4** | `GameManager.MaxPlayer`, down from 32. Genre default; matches `19_crew_roster.md`. Feeds the Steam lobby member limit. |
| Invite model | **Overlay-invite only** | No join codes, no server browser, nothing to type. `MainMenuState.JoinCodePopUp` and the `SessionInfo` code display are now dead and can be deleted. |
| Steamworks.NET | `2025.164.1` (pinned) | Latest; tracks Steamworks SDK 1.64. |
| Transport commit | `d862504b148f6c3a31763797900eb5a54d4625a5` | Current upstream HEAD — the package has had no changes since May 2023. |

---

## What is done

### Phase 0 — Prerequisites ✅ (except the App ID)

- `MaxPlayer` is 4.
- `steam_appid.txt` is gitignored, with a comment explaining why it must never
  reach a depot upload.
- Work is branched.
- ❌ **No real Steam App ID.** Development runs on 480 (Spacewar). Invite-based
  joining works on 480; lobby *listing* does not, but this design has no listing.

### Phase 1 — Packages + vendored transport ✅

- NGO `2.13.1` and Steamworks.NET `2025.164.1` installed and resolved.
- Transport vendored to `Assets/Plugins/Netcode.Transports.SteamNetworkingSockets/`
  with [`VENDORED.md`](../Assets/Plugins/Netcode.Transports.SteamNetworkingSockets/VENDORED.md)
  recording origin, commit, licence, the compile-guard trap, and **five known
  upstream defects** found while reading it (per-message allocation, peer
  starvation in `PollEvent`, `GetCurrentRtt` always returning 0, a wasted delivery
  byte, and the `NetworkManager.Singleton.StartCoroutine` shutdown dependency).
- Compile verified. Runtime not.

### Phase 2 — Strip Netcode for Entities ✅

- Packages removed: `com.unity.netcode`, `com.unity.physics`,
  `com.unity.charactercontroller`, `com.unity.dedicated-server`,
  `com.unity.services.multiplayer`. Verified gone from `packages-lock.json`.
  `com.unity.multiplayer.playmode` and `.center` kept.
- Deleted: `GhostBridge/`, `DedicatedServer/`, the ECS movement/input/camera/
  leaderboard/spawn systems, and `ServerScene` + both subscenes.
- `Utils.cs` reduced to `GetLocalIPAddress()` and `SetCursorVisible()`.

> ⚠️ **`packages-lock.json` does not re-resolve on an asset refresh.** After
> editing `manifest.json` outside Unity, the lockfile kept `com.unity.netcode` at
> `depth: 0` and the collision persisted. `manage_packages(action:
> "resolve_packages")` is what actually clears it.

### Phase 3 — Core networking 🟡 written, not compiling

New, and believed complete:

- **`Networking/Steam/SteamManager.cs`** — Steam lifecycle. Uses `InitEx` so every
  failure yields a player-readable reason (`FailureReason`) instead of a bare
  `false` or an exception. Satisfies the "Steam absent produces a readable
  message" criterion.
- **`Networking/Steam/SteamLobby.cs`** — friends-only lobbies, `Task`-based create
  and join, `GameLobbyJoinRequested` for overlay invites, member join/leave
  events, `SetJoinable` for locking on deploy. Host SteamID64 published in lobby
  metadata under `host_steam_id` — deliberately *not* read from
  `GetLobbyOwner`, because Steam reassigns ownership when the owner leaves and a
  client must never retarget its transport at someone who is not hosting.
- **`Networking/CrewRegistry.cs`** — the SteamID64 ↔ client-id mapping. This is the
  plan's "single most important rule" made mechanical: `Bind()` reuses the
  existing record on reconnect and reports `isReconnect`, `Unbind()` keeps the
  record so state survives the dropout.
- **`GameConnection.cs`** — rewritten. `HostSteamAsync` / `JoinSteamAsync` plus the
  retained `HostDirectAsync` / `JoinDirectAsync` debug path over `UnityTransport`.
- **`GameManager.cs`** — world creation replaced by `StartHost()`/`StartClient()`;
  loading-progress reporting and popup flow preserved as the plan asks.
- **`SceneLoader.cs`** — `WaitForGhostReplicationAsync` replaced by
  `WaitForClientSynchronizationAsync` on NGO's `OnSynchronizeComplete`. Host loads
  through `NetworkSceneManager`; clients are synchronised by NGO.
- **`ConnectionSettings.cs`** — UGS and `NetworkEndpoint` dependencies removed;
  6-character session code gone; `PendingLobbyId` added.

Both loading waits have explicit bail-outs if the host disappears mid-handshake,
so a dead host produces an error rather than an infinite loading screen.

---

## Compile-error pass (2026-08-08)

Ran in a cloud session with no Unity Editor, no UnityMCP, no `dotnet` — fixes below are a careful
manual read of every call site, cross-checked against what still exists post-Phase-2, not a
compiler run. **Verify with a real build before trusting this.**

| File | What was done |
|---|---|
| `Gameplay/Player/Projectile/Projectile.cs` | Deleted (with its `.meta` and the now-empty `Projectile/` folder). Confirmed no other script referenced it. |
| `Gameplay/Player/PlayerGhost/PlayerGhost.cs` | Deleted. Confirmed no other script referenced it. |
| `Gameplay/Player/PlayerGhost/PlayerGhostManager.cs` | Deleted. Confirmed no other script referenced it. |
| `Gameplay/Player/PlayerGhost/PlayerGhostMonoBehaviour.cs` | Deleted (abstract, never attached to a prefab). |
| `Gameplay/UI/LeaderboardUi.cs` | Deleted. |
| `Gameplay/UI/RespawnScreen.cs` | Deleted. |
| `Gameplay/Weapon/WeaponData.cs` | `GhostSpawner.GhostReference` (an ECS-ghost-GUID wrapper, deleted with `GhostSpawner.cs` in Phase 2) → plain `AssetReferenceGameObject` on all three prefab fields. `ProjectileGhostPrefab` renamed to `ProjectilePrefab`. **The `.asset` files (`Shotgun.asset`, `AssaultRifle.asset`) still have the old serialized field under the old type — those references will come up empty in the Editor and need re-assigning.** |
| `UI/Game/NetworkStatus.cs` | `ClientServerBootstrap.HasServerWorld` → `GameManager.GameConnection.IsHost`. Session-name display now shows `GameConnection.Transport` (Steam/Direct) since there's no `Session.Name` concept left post-UGS. |
| `UI/SessionInfo/SessionInfo.cs` | Dropped the session-code copy-to-clipboard UI (`ConnectionSettings.SessionCode` doesn't exist — confirmed dead under overlay-only invites, per plan). `ClientServerBootstrap`/ECS-world-based netcode status → reads `Unity.Netcode.NetworkManager.Singleton` directly. |
| `Gameplay/VisualEffects/GhostVisualEffect.cs` | Renamed to `NetworkedTimedEffect.cs` **via `git mv` on both the `.cs` and `.cs.meta`**, preserving the GUID so the 4 prefabs referencing it (`BulletImpact`, `CustomMuzzleFlash`, `MachineGunBulletHit`, `PlasmaHit`) keep resolving. Rewritten as a plain `NetworkBehaviour` that despawns itself server-side after `Lifetime` — replaces the deleted `GhostGameObject.DestroyEntity()` call. |
| `Gameplay/VisualEffects/VisualEffectManager.cs` | **Scope narrowed.** Its base classes (`GhostSingleton<T>`, `IUpdateServer`, `IUpdateClient`, `IGhostManager`) and its RPC path (`GhostGameObject.BroadcastRPC`/`ConsumeRPC`, `IRpcCommand`) were part of the deleted `GhostBridge` RPC bridge, not just a few field references. It also looked up the firing player via `PlayerGhostManager` (deleted) to find their shot origin — that registry has no replacement yet. Rebuilding a real networked "remote players see my muzzle flash" broadcast needs a player registry that doesn't exist and wasn't attempted. What's left: a plain `MonoBehaviour` singleton with `SpawnMuzzleFlash(Transform spawnPoint, uint weaponId)` — the VFX-instantiation logic is intact and callable, just not wired to any caller (no weapon-firing system exists yet either). |
| `Gameplay/UI/InGameHUD.cs` | **Scope narrowed further than "rewrite against `NetworkVariable`" suggests — there is no `NetworkVariable` anywhere in the codebase yet** (grepped for it; zero hits outside this file and the deleted `Projectile.cs`). Health/ammo/reticle all read `PredictedPlayerGhost` fields (`CurrentHealth`, `EquippedWeaponID`, `WeaponCooldown`, ...) that have no NGO equivalent — no health or weapon-equip state has been ported. Stripped to just the HUD-visibility toggle (`GameSettings.GameState`); health/ammo/reticle need a real NetworkVariable-backed player-state system before they can come back. |
| `Gameplay/VisualEffects/DamageVisualsController.cs` | **Not on the original 13-file list — found by a reference-check pass before deleting `PlayerGhost.cs`, since it did `GetComponentInParent<PlayerGhost>()` for the local player's camera.** Repointed at `FirstPersonController` (now the thing that knows about the owning player's camera) and gated on `IsOwner` instead of a `PlayerGhost.Role` check. |
| `Gameplay/Player/Movement/FirstPersonController.cs` | Phase 4's actual work — see below, not a small fix. |

The plan says *"Delete the shooter — removing it is not a loss."* Most of the file list above was
that deletion. Two files (`VisualEffectManager`, `InGameHUD`) survive but are now inert shells,
narrower than their original scope, because the state they displayed or broadcast doesn't exist in
the NGO version of this project yet. That's a real gap, not a rewrite — see "What remains" below.

## Phase 4 — Player controller and physics (this pass)

`FirstPersonController.cs` is now a `NetworkBehaviour`, salvaging exactly the functions the plan
named — `AccumulateJumpAndGravity`, `AccumulateGravity`, `AccumulateJump`, `CalculateMovementFromInput`,
`UpdateGround`/`GroundedCheck`, `SmoothDamp`/angle helpers — unchanged, still pure PhysX math.
Discarded `[GhostField]`, `HandleAnimationEvents`, `ApplyInterpolatedClientState`,
`SpawnPredictedProjectile`, and (going further than the plan's explicit list, since their only
callers were the above) the animator-driven code (`ApplyAnimatorState` and friends) and footstep SFX
— both read state (`PredictedPlayerGhost`, weapon cooldown/reload) that doesn't exist yet. Camera
setup (previously `PlayerGhost.CreateClientCamera()`, instantiating a per-player camera prefab) was
simplified to just capturing `Camera.main` on the owner in `OnNetworkSpawn` — consistent with
`CinemachineCameraTarget` already being a field here, implying a scene-level Cinemachine rig rather
than per-player camera instantiation.

**The bigger piece: an ECS type this file depended on, `PlayerInput`, no longer exists at all** — it
was defined in `PlayerCommandInput.cs`, deleted in Phase 2 along with the rest of the ECS input
systems (`ClientInputReaderSystem`, `ClientInputSenderSystem`). Recreated as a plain struct in the
new `Gameplay/Input/PlayerInput.cs` (`MoveInput`, `LookYawPitchDegrees`, `Jump`; implements
`INetworkSerializable` for RPC transport), and ported `ClientInputReaderSystem`'s mouse-look
accumulation (sensitivity `3.7`, pitch clamped ±85°) into a new `SampleAndSendInput()` that runs on
the owner every `Update()`.

**Networking model implemented:** owner samples input each frame → `[ServerRpc] SubmitInputServerRpc`
→ server stores it and, also every `Update()`, runs `AccumulateMovement` + `ApplyMovementUpdate`
against its own copy of the `CharacterController`. No other file drives this anymore — the ECS tick
systems that used to call these functions are gone, so this `Update()` loop *is* the port, not
incidental plumbing. This matches "host-authoritative, client-side interpolation, no rollback
prediction" **once a `NetworkTransform` component is added to the player prefab** (server-authoritative
mode) — that's an Editor step, not done here, see below. Until that component exists, movement will
not replicate to anyone: the server moves its own `CharacterController.Move()`, but nothing sends
that position to remote clients.

**Not done, deliberately, because it's out of this pass's scope (weapon/animation porting, not
movement):** shooting, reloading, ammo, third-person animation sync, footstep audio. `SoundDef
PlayerHitSFX` was removed since its only reader (`HandleAnimationEvents`) is gone.

`ControllerConsts` used to be authored via a deleted ECS baking component; there's no replacement
authoring path since subscenes are gone, so tuning values now live directly on
`FirstPersonController` as a `[SerializeField]` with placeholder defaults (`Walk.Speed = 5`,
`JumpHeight = 1.2`, `Gravity = -15`, ...) — **these need real tuning in the Editor**, they were picked
to be plausible, not measured.

## What remains

### 1. Verify the compile-error fixes with a real build

This whole pass was done blind. Open the project in the Unity Editor (or point UnityMCP at it) and
confirm 0 errors before trusting anything above. Given the scale of the `FirstPersonController`
rewrite in particular, budget time to fix whatever the compiler finds that manual review missed.

### 2. Re-wire the two narrowed-scope files once their dependencies exist

`VisualEffectManager` (networked muzzle-flash broadcast) and `InGameHUD` (health/ammo/reticle) both
need a player-state system — health, equipped weapon, ammo — that doesn't exist anywhere in the NGO
version of this project yet. Building that (presumably `NetworkVariable`s on a per-player
`NetworkBehaviour`) is a prerequisite, not part of either file.

### 3. Prove one grabbable prop end to end

The plan's actual Phase 4 exit criterion, not yet attempted. Gates plans 12, 40, 41 and 47. Needs
item 4 below (a `NetworkObject` player prefab with a `NetworkTransform`) to exist first, since
movement doesn't replicate without it.

### 4. Scene and prefab wiring — needs the Unity Editor

Nothing has been done in-scene. Required:

- A `NetworkManager` GameObject in `Persistents.unity`, carrying **both**
  `SteamNetworkingSocketsTransport` and `UnityTransport` — `GameConnection` swaps
  `NetworkConfig.NetworkTransport` between them, and throws a clear error if
  either component is missing.
- A `SteamManager` component in `Persistents.unity`. **Nothing works without it**
  — it pumps `SteamAPI.RunCallbacks()`, which drives lobby callbacks *and* the
  transport's own connection-status callback.
- Player prefab as a `NetworkObject` + `NetworkBehaviour`, registered in the
  `NetworkManager` prefab list. **Also needs a `NetworkTransform` (server-authoritative
  mode)** — `FirstPersonController` now runs movement server-side every frame but has no
  way to replicate the result to other clients without one; see the Phase 4 section above.
- `DamageVisualsController`'s `screenDamageMaterial` and `FirstPersonController`'s
  `m_Consts` (movement tuning) need real values — current defaults are placeholders.
- `GameScene.unity` still references the two deleted subscenes; clean that up.
- Add `GameScene` to build settings if the subscene removal disturbed it.

### 5. Main menu — Phase 5 tail

`MainMenu.cs` compiles but still offers *Create Game* / *Join by Code* / *Direct
Connect*. It needs **Host** and **Invite Friends** (`SteamLobby.OpenInviteOverlay()`),
with direct-connect moved behind a debug flag. `CreationType` has already been
renamed to `HostSteam` / `JoinSteam` / `HostDirect` / `JoinDirect`.

### 6. Phase 6 — verification, and the plan documents

Every item in Phase 6 needs two machines on different networks. None of it has
been done.

Plan docs are **not** yet updated. Per the plan, [`90`](core_component_plans/90_relay_and_lobby_service_enablement.md),
[`91`](core_component_plans/91_join_by_code.md) and [`94`](core_component_plans/94_entity_subscene_baking.md)
are obsolete; [`92`](core_component_plans/92_session_lifecycle.md),
[`25`](core_component_plans/25_reconnection.md), [`100`](core_component_plans/100_network_bandwidth_budget.md),
[`95`](core_component_plans/95_client_server_build_parity.md) and
[`97`](core_component_plans/97_build_verification_pass.md) need rewriting.

---

## Acceptance criteria

| | Criterion |
|---|---|
| ❌ | Real Steam App ID exists |
| ✅ | `steam_appid.txt` gitignored and absent from builds |
| 🟡 | `MaxPlayer` is 4 — Steam lobby enforcement written, untested |
| ✅ | Transport vendored, compiles against NGO 2.x, provenance recorded |
| ✅ | `com.unity.netcode`, `.services.multiplayer`, `.dedicated-server` gone |
| ✅ | `GhostBridge/` and `DedicatedServer/` no longer exist |
| ✅ | `GetLocalIPAddress()` and `SetCursorVisible()` survive |
| 🟡 | No Relay/UGS references — gone from code; `cloudProjectId` still in ProjectSettings |
| ❌ | Two players on different networks play a full round |
| ❌ | Overlay invite lands the invitee in the host's session |
| 🟡 | State keyed on SteamID64 — `CrewRegistry` enforces it; no gameplay state ported yet |
| ❌ | Disconnected player rejoins and recovers their own state |
| 🟡 | Leaving player removed from lobby — `SteamLobby.Leave()` on every exit path, untested |
| ❌ | One physics prop picked up, carried, thrown |
| 🟡 | Steam absent produces a readable message — written, untested |
| 🟡 | Direct-connect debug path works — written, untested |
| ❌ | Plan documents updated |

---

## Environment note

The `UnityMCP` MCP server did not attach to the Claude Code session, though the
bridge was running and healthy (`mcp-for-unity-server` 3.4.6 on
`127.0.0.1:8080`). It was driven over raw JSON-RPC instead. If the next agent
hits the same thing, `/mcp` to reconnect is the fix; failing that, a streamable
HTTP client against that endpoint works fine.

`execute_code` is **broken** on this machine — the CodeDom fallback exceeds
Windows' command-length limit (`mono.exe: The filename or extension is too
long`). Use `unity_reflect`, or inspect `Library/ScriptAssemblies` directly.
Assembly size is a reliable tell: a ~4 KB assembly means everything was
`#if`-stripped.
