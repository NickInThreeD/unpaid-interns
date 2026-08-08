# Migration — Netcode for Entities → NGO + Steam

**Status:** ❌ Not started — plan verified and ready to implement
**Scope:** Replaces the entire networking layer. Does not touch art, audio, UI, input, or docs.
**Out of scope:** Voice chat. See [`21_proximity_voice_comms.md`](core_component_plans/21_proximity_voice_comms.md) — unaffected by this migration except that its transport options change once off UGS.
**Blocks:** every multiplayer component plan in [`core_component_plans/`](core_component_plans/)
**Supersedes:** [`90`](core_component_plans/90_relay_and_lobby_service_enablement.md), [`91`](core_component_plans/91_join_by_code.md), [`94`](core_component_plans/94_entity_subscene_baking.md); reshapes [`92`](core_component_plans/92_session_lifecycle.md), [`25`](core_component_plans/25_reconnection.md)

---

## Goals

- **Run the same networking architecture as Lethal Company and R.E.P.O.** — GameObject-based netcode over Steamworks P2P, with Steam Lobby for hosting and invites.
- **Get off Unity Gaming Services entirely.** No Relay, no Lobby service, no Multiplay, no dashboard enablement, no bandwidth billing. Steam P2P is free and needs no cloud account.
- **Make the friends-list the matchmaking system.** Host creates a Steam lobby; friends join via the Steam overlay. No public server browser, no 6-character codes.
- **Unblock networked physics.** Carrying, throwing, and ragdolling props is the core of the genre and the single hardest thing to do on Netcode for Entities. NGO + PhysX `NetworkRigidbody` is the documented path.
- **Delete the shooter.** The current netcode layer is a competitive deathmatch sample. Removing it is not a loss — it is work that would have to be undone anyway.
- **Do not start a new project.** Art, audio, UI, input actions, Addressables, URP config, and all documentation survive untouched.

---

## The stack — resolved and pinned

| Layer | Current | Target |
|---|---|---|
| Editor | Unity **6000.3.11f1** | unchanged |
| Netcode | `com.unity.netcode` 1.10.0 (Entities) | **`com.unity.netcode.gameobjects` 2.x** (latest is 2.13) |
| Steam wrapper | — | **Steamworks.NET** (`com.rlabrecque.steamworks.net`) |
| Transport | Unity Transport + UGS Relay | **`com.community.netcode.transport.steamnetworkingsockets`**, vendored |
| Steam API | — | `SteamNetworkingSockets` (current; **not** the deprecated `SteamNetworking`) |
| Session / discovery | UGS Sessions, 6-char code | Steam Lobby, Steam overlay invite |
| Dedicated servers | `ServerBootstrap`, UGS Multiplay | None — host is a player |
| Physics | Unity Physics (DOTS) | PhysX + `NetworkRigidbody` |
| Player identity | Ephemeral NGE connection id | **SteamID64** — stable across reconnects |

**Package URLs:**

```
com.unity.netcode.gameobjects              (Unity Registry, 2.x)
https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net#<pin-a-version>
https://github.com/Unity-Technologies/multiplayer-community-contributions.git?path=/Transports/com.community.netcode.transport.steamnetworkingsockets
```

The transport is **vendored** (copied into the repo), not referenced by git URL — see the verification section for why.

---

## Verified findings

Everything in this section was checked against upstream sources on **2026-08-08**. These are the facts the plan rests on; re-verify if picking this up much later.

### NGO 2.x is mandatory — downgrading is not an option

The project is on **Unity 6000.3.11f1**. Unity's official position: *"Unity versions 6000.3+ LTS will only support Netcode for GameObjects v2.x."* NGO 1.x remains supported for 6000.2 LTS and below only. Pinning NGO 1.x to match the community transports' declared dependency would mean downgrading the **Editor**, cascading into URP 17.3.0, Addressables 2.9.1, and Cinemachine 3.1.5. Not viable.

### The transport is API-compatible with NGO 2.x

This was the migration's biggest open risk. It is now closed at the compile level.

`SteamNetworkingSocketsTransport` (namespace `Netcode.Transports`) implements **all nine** abstract members of NGO 2.13's `NetworkTransport`, with signatures that match exactly:

| NGO 2.13 abstract member | Transport | Match |
|---|---|---|
| `ulong ServerClientId { get; }` | `public override` | exact |
| `void Initialize(NetworkManager networkManager = null)` | `public override` | exact, incl. default arg |
| `void Shutdown()` | `public override` | exact |
| `bool StartClient()` | `public override` | exact |
| `bool StartServer()` | `public override` | exact |
| `void DisconnectLocalClient()` | `public override` | exact |
| `void DisconnectRemoteClient(ulong)` | `public override` | exact |
| `void Send(ulong, ArraySegment<byte>, NetworkDelivery)` | `public override` | exact (param names differ — irrelevant) |
| `NetworkEvent PollEvent(out ulong, out ArraySegment<byte>, out float)` | `public override` | exact |
| `ulong GetCurrentRtt(ulong)` | `public override` | exact |
| `bool IsSupported` *(virtual)* | `public override` | fine |

NGO 2.x's **new** virtuals — `OnCurrentTopology`, `OnEarlyUpdate`, `OnPostLateUpdate`, `GetDisconnectEventMessage` — all carry defaults, so not overriding them is correct.

Decisively: **the transport references neither `NetworkTransform` nor `NetworkBehaviour`** — precisely the two surfaces where NGO 2.0's documented breaking changes landed (e.g. `NetworkTransform.Update` → `OnUpdate`). It touches only `NetworkTransport`, `NetworkManager.Singleton`, `NetworkDelivery`, and `NetworkEvent`, all of which are stable across the 1.x → 2.x boundary.

**Confidence: high that it compiles; runtime behaviour still needs a live two-machine test** (Phase 1). The declared `package.json` dependency on NGO 1.x is a *minimum-version* constraint in Unity's package manager, not a pin, so it does not block installation alongside 2.x.

### Choose `steamnetworkingsockets`, not `facepunch`

| | `…transport.facepunch` | `…transport.steamnetworkingsockets` ✅ |
|---|---|---|
| Steam API | `SteamNetworking` — **deprecated by Valve** | `SteamNetworkingSockets` — current |
| Wrapper | Facepunch.Steamworks, **bundled** | Steamworks.NET, **supplied separately** |
| Known blocker | [#219](https://github.com/Unity-Technologies/multiplayer-community-contributions/issues/219) — `Posix`/`Win64` ambiguous-type compile failure, open and uncommented since April 2023 | structurally immune — see below |
| Architectures | P2P | P2P **and** Steam Game Server |

The Facepunch transport bundles its own Steamworks assemblies, which is the root cause of #219. The `steamnetworkingsockets` changelog explicitly records *"Removed bundled Steamworks.NET to enable use of preferred versions"* — you supply the wrapper as a separate UPM package, so that class of conflict cannot occur.

**This deliberately diverges from R.E.P.O.'s wrapper choice.** R.E.P.O. uses Facepunch.Steamworks; the evidence for that is a Thunderstore mod's existence, and in any case the *wrapper* choice is independent of the *transport* choice. Do not let that precedent push this project onto a Steam API Valve has deprecated.

### Vendor the transport

It is community-maintained, Unity disclaims support (*"We do not guarantee that any of the content in this repository will be supported by future Netcode for GameObjects versions"*), and its `package.json` says version `1.0.1` while its `CHANGELOG` top entry says `2.0.1` — the metadata is not carefully kept. It is **one file**, `Runtime/SteamNetworkingSocketsTransport.cs`, plus an asmdef. Copy it into `Assets/Plugins/` or a local package so it can be patched without a fork-and-PR cycle.

### Steamworks.NET is actively maintained

Version scheme is `2025.<SteamworksSDK>.<patch>`; latest is **2025.164.1**, tracking Steamworks SDK 1.64, with the repo building against SDK 1.65. It ships a `steam_appid.txt` containing `480` into the project root on import.

### Other verified facts

- **DOTS package removal is clean.** `Unity.Physics` / `Unity.CharacterController` namespaces appear in only three files — `ClientGameSystem`, `ServerGameSystem`, `Utils.cs` — all deleted in Phase 2.
- **`FirstPersonController.cs` already uses built-in PhysX** (`CharacterController`, `Physics.SphereCastNonAlloc`), not the DOTS character controller. The physics foundation already matches NGO.
- **Subscenes are `GameResourcesSubScene.unity` and `SpawnPointsSubScene.unity`.** `Persistents.unity` is a normal scene and stays.

---

## What goes, what stays

**Deleted (~38 files, all ECS):**

- `Assets/Scripts/GhostBridge/` — the entire directory
- `Assets/Scripts/Networking/` — `ClientGameSystem`, `ServerGameSystem`, `ClientConnectionSystem`, `EntityDriverConstructor`, `EntitNetworkHandler`, RPCs, spawn authoring
- `Assets/Scripts/Gameplay/Player/Movement/` — `ServerPlayerMovementSystem`, `ClientInterpolatedPlayerMovementSystem`, `PlayerMovementHistory`, `ResetHitFlagSystem`
- `Assets/Scripts/Gameplay/Player/Projectile/ProjectileReconciliationSystem.cs`
- `Assets/Scripts/Gameplay/Input/Client*System.cs`, `PlayerCommandInput`, `PlayerCommandTargetAuthoring`
- `Assets/Scripts/DedicatedServer/` — both bootstraps
- `MainCameraSystem`, `MainCameraAuthoring`, `GameLeaderboard`, `NetworkStatusSystem`, `NetworkStatusSingleton`
- **Scenes:** `ServerScene.unity`, `GameResourcesSubScene.unity`, `SpawnPointsSubScene.unity`
- **Most of `Utility/Utils.cs`** — `GetLocalIPAddress()` and `SetCursorVisible()` are the **only** survivors; preserve them before deleting the rest

**Rewritten:** `GameConnection.cs` · `GameManager.cs:266-305` · `ConnectionSettings.cs:29` · `MainMenu.cs:137` · `SessionInfo.cs:211` · `FirstPersonController.cs` (partial salvage — see Phase 4)

**Untouched:** `Assets/Art/` · `Assets/Scripts/Audio/` · `Gameplay/VisualEffects/` · all `Assets/Scripts/UI/` and `.uxml` · `InputSystem_Actions` · Addressables · URP + lighting · all docs

---

## Phase 0 — Prerequisites

- **Steam App ID.** Requires a Steamworks partner account and the one-time app fee. Until then develop against **480 (Spacewar)** — P2P and lobbies work, but 480 lobbies are shared with every other developer testing on it, so lobby *listing* is unusable. Invite-based joining is unaffected.
- **`steam_appid.txt`** is auto-created in the project root by Steamworks.NET containing `480`. Replace with the real App ID; save as **ASCII or UTF-8 without BOM**; **relaunch Unity** after changing it. Gitignore it and strip it before any depot upload.
- **Fix crew size.** `GameManager.cs:19` is `MaxPlayer = 32`, inherited from the deathmatch sample. Set to the real number (4–6); it feeds the Steam lobby member limit.
- **Decide the invite model.** Overlay-invite only (Lethal Company / R.E.P.O. behaviour), or additionally expose the lobby id as a copyable string. The latter keeps the existing `SessionInfo` code-display UI alive.
- **Branch this work.** Not incrementally shippable — there is a window where nothing connects.

## Phase 1 — Prove the transport (do this before deleting anything)

Throwaway scene, capsule, no gameplay, old stack still intact.

- Install NGO 2.x, Steamworks.NET (pinned), and **vendor** the transport file into `Assets/Plugins/`.
- Confirm it compiles against NGO 2.x. **Expect success** — the API diff above shows all nine abstract members matching exactly. Any breakage should be small and mechanical.
- Initialise Steam (Steamworks.NET's `SteamManager.cs` is an acceptable starting point; the transport requires no `SteamManager` dependency of its own).
- Create a lobby, join from a second machine, move a networked capsule.
- **Two machines on different networks.** Two Editor instances on one LAN prove nothing about Steam's routing.
- The compile risk is closed; the remaining risk here is **runtime behaviour**. That is what this phase exists to establish.

## Phase 2 — Strip Netcode for Entities

- Preserve `GetLocalIPAddress()` and `SetCursorVisible()` out of `Utils.cs` first.
- Remove from `Packages/manifest.json`: `com.unity.netcode`, `com.unity.services.multiplayer`, `com.unity.dedicated-server`, `com.unity.physics`, `com.unity.charactercontroller`.
- Keep `com.unity.multiplayer.playmode` (works with NGO) and `com.unity.multiplayer.center`.
- Delete the file and scene set listed above.
- Project is non-functional until Phase 3 completes. That is what the branch is for.
- Leave `cloudProjectId` in `ProjectSettings` until last — harmless to keep, misleading to leave permanently.

## Phase 3 — Rebuild core networking

- `NetworkManager` in the persistent scene, with the vendored transport component.
- Player prefab as a `NetworkBehaviour` with `NetworkObject`, registered in `NetworkManager`'s prefab list.
- Replace the world-creation block (`GameManager.cs:266-305`) with `StartHost()` / `StartClient()`. Keep the surrounding `LoadingData.UpdateLoading` reporting and `MainMenuState` popup flow — only the middle changes.
- Replace `WaitForGhostReplicationAsync` (`GameManager.cs:314-339`) with an NGO scene-synchronisation wait. The intent — don't show the world until it's populated — is right and worth preserving.
- **Key all player state on SteamID64, never on `NetworkManager.LocalClientId`.** Client ids are reassigned after a disconnect and will hand a returning player someone else's state. This is the single most important rule in the migration and is what makes [`25_reconnection.md`](core_component_plans/25_reconnection.md) tractable.
- `NetworkVariable` and `ServerRpc`/`ClientRpc` replace the deleted ghost/RPC layer. Server-authoritative by default: host owns world state, clients own only their own input.

## Phase 4 — Player controller and physics

`FirstPersonController.cs` is a **partial salvage, not a port.** It is a `MonoBehaviour` but is driven *by* the ECS prediction systems — its `ControllerState` carries `[GhostField]` attributes and it reads `PlayerGhost`, `GhostGameObject`, `PredictedPlayerGhost`, `MultiplayerRole`, and `Unity.Transforms.LocalTransform`.

- **Salvage** the pure movement math: `AccumulateJumpAndGravity`, `AccumulateGravity`, `AccumulateJump`, `CalculateMovementFromInput`, `UpdateGround` / `GroundedCheck`, and the `SmoothDamp` / angle helpers. These already run on built-in PhysX and carry over directly.
- **Discard** the shell: every `[GhostField]`, the tick-comparison animation block (`HandleAnimationEvents`), `ApplyInterpolatedClientState`, `SpawnPredictedProjectile`, and the weapon/reload triggers. Replace tick comparisons with RPCs or `NetworkVariable` change callbacks.
- Rebuild as a `NetworkBehaviour` with owner-authoritative movement and `NetworkTransform` replication.
- **Do not rebuild rollback prediction.** Lethal Company and R.E.P.O. are host-authoritative with client-side interpolation and feel fine. Rebuilding it recreates the complexity this migration exists to shed.
- Props: PhysX `Rigidbody` + `NetworkObject` + `NetworkRigidbody`, host-simulated.
- **Prove one grabbable prop end to end early** — pick up, carry, throw, watch it settle on a client. Gates [`12`](core_component_plans/12_carry_weight.md), [`40`](core_component_plans/40_inventory_item_bar.md), [`41`](core_component_plans/41_interaction_system.md), [`47`](core_component_plans/47_physics_props_and_throwing.md).
- Host-simulated everything is the recommended default; only reach for `NetworkObject.ChangeOwnership` on pickup if it demonstrably feels wrong.

## Phase 5 — Steam lobby and invite flow

- Rewrite `GameConnection` as a Steam lobby wrapper over `SteamMatchmaking`:
  - Create with lobby type **FriendsOnly** (or Private), never Public — there is no server browser in this design.
  - Store the host's `CSteamID` in lobby metadata; joiners pass it to the transport as the connect target.
  - Subscribe to lobby-entered, member-joined, member-left, and **`GameLobbyJoinRequested`** — that last one is what makes overlay invites work.
- **Remove a leaving player from the lobby promptly.** A stale member blocks their own rejoin.
- Main menu: **Host** and **Invite Friends** replace *Create Game* / *Join by Code* / *Direct Connect*. `MainMenuState.JoinCodePopUp` and its `JoinSessionStyle` binding in `GameSettings.cs` can be repurposed for a lobby-id path or deleted.
- Free wins: Steam persona names and avatars for [`19`](core_component_plans/19_crew_roster.md) and [`80`](core_component_plans/80_teammate_identification.md).
- **Keep a direct-connect path behind a debug flag** — it is how you distinguish a transport failure from a Steam failure, and it keeps offline iteration possible.

## Phase 6 — Verification

- Two machines, **different networks**, standalone builds, full round start to finish.
- Host quits mid-round: clients get a clear message and return to menu without hanging. (Host migration is out of scope — neither reference game has it.)
- Client disconnects and rejoins; state restored by SteamID64, not reassigned.
- Overlay invite from an in-game host lands the invitee in the correct lobby.
- Steam client closed / logged out produces a readable error, not an exception or hang.
- `steam_appid.txt` absent from the shipped build.

---

## Remaining risks

| Risk | Level | Mitigation |
|---|---|---|
| Transport **runtime** behaviour on NGO 2.x untested | Medium | Phase 1 gates on a live two-machine test. Compile compatibility is evidenced. |
| Transport is unmaintained community code | Medium | Vendored in-repo, one file, patchable. Unity disclaims support either way. |
| Networked physics feel over P2P latency | Medium | Unknowable in advance. Prove with one prop in Phase 4, not fifty. |
| No real Steam App ID yet | Low | 480 covers all development except lobby *listing*. |
| `NetworkManager.Singleton` coroutine usage inside the transport | Low | Present in NGO 2.x; would surface immediately at compile time. |

## Plan documents this invalidates

| Plan | Change |
|---|---|
| [`90_relay_and_lobby_service_enablement.md`](core_component_plans/90_relay_and_lobby_service_enablement.md) | **Obsolete.** Replaced by Steam App ID setup. No dashboard step, no bandwidth billing. |
| [`91_join_by_code.md`](core_component_plans/91_join_by_code.md) | **Obsolete as written.** Steam invites replace 6-char codes. |
| [`94_entity_subscene_baking.md`](core_component_plans/94_entity_subscene_baking.md) | **Obsolete.** No entities, no subscenes. |
| [`92_session_lifecycle.md`](core_component_plans/92_session_lifecycle.md) | Rewrite against the Steam lobby lifecycle. |
| [`25_reconnection.md`](core_component_plans/25_reconnection.md) | NGO provides more than NGE did; SteamID64 is the stable key. |
| [`100_network_bandwidth_budget.md`](core_component_plans/100_network_bandwidth_budget.md) | Cost dimension disappears (Steam P2P is free); performance dimension remains. |
| [`95`](core_component_plans/95_client_server_build_parity.md), [`97`](core_component_plans/97_build_verification_pass.md) | Rewrite against host/client builds, not dedicated-server builds. |
| [`19`](core_component_plans/19_crew_roster.md), [`80`](core_component_plans/80_teammate_identification.md) | Steam persona names and avatars now available for free. |
| [`21_proximity_voice_comms.md`](core_component_plans/21_proximity_voice_comms.md) | Not in scope here, but its Vivox recommendation was premised on being on UGS. Revisit after this lands. |

---

## Acceptance Criteria

- [ ] A real Steam App ID exists; `steam_appid.txt` is gitignored and absent from shipped builds.
- [ ] `MaxPlayer` reflects the real crew size and the Steam lobby enforces it.
- [ ] The transport is vendored in-repo, compiles against NGO 2.x, and carries a note recording its upstream origin and commit.
- [ ] `com.unity.netcode`, `com.unity.services.multiplayer`, and `com.unity.dedicated-server` are gone from `Packages/manifest.json`.
- [ ] `Assets/Scripts/GhostBridge/` and `Assets/Scripts/DedicatedServer/` no longer exist.
- [ ] `GetLocalIPAddress()` and `SetCursorVisible()` survive the deletion of `Utils.cs`.
- [ ] No code path references Unity Relay, UGS Sessions, or `cloudProjectId`.
- [ ] Two players on different networks connect through Steam P2P from standalone builds and play a full round.
- [ ] A Steam overlay invite from an in-game host lands the invitee in that host's session.
- [ ] All persisted player state is keyed on SteamID64; no gameplay state is keyed on `LocalClientId`.
- [ ] A disconnected player rejoins and recovers their own state, not another player's.
- [ ] A leaving player is removed from the Steam lobby and can immediately rejoin.
- [ ] One physics prop can be picked up, carried, and thrown, and settles identically on host and client.
- [ ] Steam client absent or logged out produces a readable message rather than an exception or hang.
- [ ] The direct-connect debug path still works and can isolate transport failures from Steam failures.
- [ ] Every plan document listed above has been updated or marked obsolete.
