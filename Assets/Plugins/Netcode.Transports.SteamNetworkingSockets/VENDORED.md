# SteamNetworkingSockets Transport — vendored copy

## Origin

| | |
|---|---|
| Upstream repo | [Unity-Technologies/multiplayer-community-contributions](https://github.com/Unity-Technologies/multiplayer-community-contributions) |
| Upstream path | `Transports/com.community.netcode.transport.steamnetworkingsockets` |
| Commit | `d862504b148f6c3a31763797900eb5a54d4625a5` |
| Commit date | 2023-05-19 ("Update Steam Game Server Callbacks (#221)") |
| License | MIT — Copyright (c) 2021 Unity Technologies (see `LICENSE.md`) |
| Vendored on | 2026-08-08 |

This commit is the current `main` HEAD for this package path — the transport has
received no upstream changes since May 2023.

## Why vendored rather than referenced by git URL

Three reasons, all from [`ngo_steam_migration.md`](../../../unpaid_interns/ngo_steam_migration.md):

1. **Unity disclaims support.** The repository states: *"We do not guarantee that
   any of the content in this repository will be supported by future Netcode for
   GameObjects versions."*
2. **The package metadata is not maintained.** `package.json` declares version
   `1.0.1` while the top `CHANGELOG.md` entry says `2.0.1`, and the declared NGO
   dependency is 1.x while this project runs NGO 2.13.1. (That dependency is a
   *minimum-version* constraint in UPM, not a pin, so it does not block use.)
3. **It is one file.** `Runtime/SteamNetworkingSocketsTransport.cs` plus an
   asmdef. Vendoring lets us patch it directly instead of forking and PRing.

## Why this transport and not `…transport.facepunch`

- It targets `SteamNetworkingSockets`, the current Valve API. The Facepunch
  transport targets `SteamNetworking`, which **Valve has deprecated**.
- Facepunch bundles its own Steamworks assemblies, which is the root cause of
  [issue #219](https://github.com/Unity-Technologies/multiplayer-community-contributions/issues/219)
  (`Posix`/`Win64` ambiguous-type compile failure, open since April 2023). This
  transport's changelog records *"Removed bundled Steamworks.NET to enable use of
  preferred versions"* — we supply Steamworks.NET as a separate UPM package, so
  that class of conflict cannot occur.

## Compile compatibility with NGO 2.x — verified

Verified against **NGO 2.13.1** on 2026-08-08. The transport implements all nine
abstract members of `NetworkTransport` with exactly matching signatures, and
overrides `IsSupported`. NGO 2.x's new virtuals (`OnCurrentTopology`,
`OnEarlyUpdate`, `OnPostLateUpdate`, `GetDisconnectEventMessage`) all carry
defaults, so not overriding them is correct.

Decisively, the transport references **neither `NetworkTransform` nor
`NetworkBehaviour`** — the two surfaces where NGO 2.0's breaking changes landed.
It touches only `NetworkTransport`, `NetworkManager.Singleton`, `NetworkDelivery`
and `NetworkEvent`, all stable across the 1.x → 2.x boundary.

## Compilation guard

The source is wrapped in:

```csharp
#if !DISABLESTEAMWORKS && STEAMWORKSNET && NETCODEGAMEOBJECTS
```

`STEAMWORKSNET` and `NETCODEGAMEOBJECTS` are supplied by `versionDefines` in
`Runtime/com.community.netcode.transport.steamnetworkingsockets.asmdef`, keyed on
the presence of `com.rlabrecque.steamworks.net` and
`com.unity.netcode.gameobjects`. **If either package is missing, this file
compiles to an empty assembly and `SteamNetworkingSocketsTransport` silently does
not exist.** A ~4 KB `SteamNetworkingSockets Transport for Netcode for
GameObjects.dll` in `Library/ScriptAssemblies` is the symptom.

The asmdef references those two packages by GUID:

| GUID | Resolves to |
|---|---|
| `1491147abca9d7d4bb7105af628b223e` | `Unity.Netcode.Runtime.asmdef` (NGO) |
| `68bd7fdb68ef2684e982e8a9825b18a5` | `com.rlabrecque.steamworks.net.asmdef` |

Both were confirmed present and matching on 2026-08-08.

## Local modifications

- **2026-08-08** — Added the provenance header comment at the top of
  `Runtime/SteamNetworkingSocketsTransport.cs`. No behavioural change.

## Known upstream defects (not yet patched)

Recorded here so they are not rediscovered as mysteries. None block the
migration; revisit if the symptoms show up in playtesting.

1. **Per-message allocation in `PollEvent`.** Every received message allocates a
   fresh `byte[]` and marshals a `SteamNetworkingMessage_t`. This is GC pressure
   proportional to traffic. Relevant to
   [`100_network_bandwidth_budget.md`](../../../unpaid_interns/core_component_plans/100_network_bandwidth_budget.md).
2. **Connection iteration always restarts at the first peer.** `PollEvent`'s
   `foreach` over `connectionMapping.Values` returns after the first message it
   finds, so a consistently busy peer can starve later ones. With a 4-player crew
   this is unlikely to bite, but it is a real unfairness under load.
3. **`GetCurrentRtt` always returns 0.** Upstream left a TODO. Any RTT-based HUD
   or diagnostic will read zero and must not trust this value.
4. **`Send` appends a delivery byte that the receiver discards.** Symmetric and
   harmless, but it costs one byte per message and is not an NGO requirement.
5. **`Shutdown` calls `NetworkManager.Singleton.StartCoroutine`.** Works on NGO
   2.x, but it means shutdown ordering depends on the singleton still being
   alive; it falls back to a synchronous path when it is not.
