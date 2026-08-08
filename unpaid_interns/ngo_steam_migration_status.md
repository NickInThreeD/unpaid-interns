# Migration status — NGO + Steam

**Branch:** `feature/ngo-steam-migration` (from `origin/main`)
**Commit:** `103053d` — 161 files changed, +2360 / −10128
**Date:** 2026-08-08
**Plan:** [`ngo_steam_migration.md`](ngo_steam_migration.md)

**State:** Phases 0–2 complete. Phase 3 and Phase 5 substantially written but not
compiling — the shooter gameplay layer still references deleted ECS types.
**106 compile errors remain, confined to 13 files, all listed below.**

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

## What remains

### 1. Fix the 106 compile errors

All in the shooter/ghost layer. Nothing else in the project errors — audio,
input, weapons registry, addressables, URP and every `.uxml` are clean.

| Errors | File | What to do |
|---:|---|---|
| 34 | `Gameplay/Player/Movement/FirstPersonController.cs` | **Phase 4.** Partial salvage, not a port — see below. |
| 18 | `Gameplay/Player/Projectile/Projectile.cs` | Shooter. Delete unless props reuse it. |
| 11 | `Gameplay/Player/PlayerGhost/PlayerGhost.cs` | Ghost layer. Delete. |
| 8 | `Gameplay/Player/PlayerGhost/PlayerGhostManager.cs` | Ghost layer. Delete. |
| 8 | `Gameplay/UI/LeaderboardUi.cs` | Deathmatch UI. Delete. |
| 6 | `Gameplay/UI/InGameHUD.cs` | Rewrite against `NetworkVariable`. |
| 6 | `Gameplay/VisualEffects/VisualEffectManager.cs` | Strip ECS; the VFX themselves are fine. |
| 5 | `Gameplay/UI/RespawnScreen.cs` | Deathmatch. Delete or repurpose. |
| 3 | `Gameplay/Weapon/WeaponData.cs` | Strip `[GhostField]`. |
| 3 | `UI/SessionInfo/SessionInfo.cs` | Rewrite; the code display is dead under overlay-only invites. |
| 2 | `Gameplay/VisualEffects/GhostVisualEffect.cs` | Rename off "Ghost"; strip ECS. |
| 1 | `UI/Game/NetworkStatus.cs` | Point at `NetworkManager`. |
| 1 | `Gameplay/Player/PlayerGhost/PlayerGhostMonoBehaviour.cs` | Delete with the ghost layer. |

The plan says *"Delete the shooter — removing it is not a loss."* Most of this
table is that deletion.

### 2. Phase 4 — Player controller and physics

Not started. `FirstPersonController.cs` is untouched and still carries its
`[GhostField]` attributes. Follow the plan's salvage list exactly: keep
`AccumulateJumpAndGravity`, `AccumulateGravity`, `AccumulateJump`,
`CalculateMovementFromInput`, `UpdateGround`/`GroundedCheck` and the
`SmoothDamp`/angle helpers — these already run on built-in PhysX and carry over
directly. Discard every `[GhostField]`, `HandleAnimationEvents`,
`ApplyInterpolatedClientState` and `SpawnPredictedProjectile`.

**Do not rebuild rollback prediction.** Host-authoritative with client-side
interpolation is what both reference games ship.

Then prove one grabbable prop end to end. That gates plans 12, 40, 41 and 47.

### 3. Scene and prefab wiring — needs the Unity Editor

Nothing has been done in-scene. Required:

- A `NetworkManager` GameObject in `Persistents.unity`, carrying **both**
  `SteamNetworkingSocketsTransport` and `UnityTransport` — `GameConnection` swaps
  `NetworkConfig.NetworkTransport` between them, and throws a clear error if
  either component is missing.
- A `SteamManager` component in `Persistents.unity`. **Nothing works without it**
  — it pumps `SteamAPI.RunCallbacks()`, which drives lobby callbacks *and* the
  transport's own connection-status callback.
- Player prefab as a `NetworkObject` + `NetworkBehaviour`, registered in the
  `NetworkManager` prefab list.
- `GameScene.unity` still references the two deleted subscenes; clean that up.
- Add `GameScene` to build settings if the subscene removal disturbed it.

### 4. Main menu — Phase 5 tail

`MainMenu.cs` compiles but still offers *Create Game* / *Join by Code* / *Direct
Connect*. It needs **Host** and **Invite Friends** (`SteamLobby.OpenInviteOverlay()`),
with direct-connect moved behind a debug flag. `CreationType` has already been
renamed to `HostSteam` / `JoinSteam` / `HostDirect` / `JoinDirect`.

### 5. Phase 6 — verification, and the plan documents

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
