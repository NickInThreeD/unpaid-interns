# 06 — Session Persistence

**Source:** [`core_components.md`](../core_components.md) §1 — Game Loop & Session State
**Status:** ❌ Not started
**Depends on:** Run Manager, Hub State
**Blocks:** long-form runs, store purchases surviving a session, unlock progression

## Summary

Saving a contract so it survives quitting, and restoring it on return. Without it a run only exists while the session is live, which caps the game at whatever players finish in one sitting — a hard ceiling on a design built around escalating multi-day quotas.

What persists is the Run Manager's state (day, credits, quota, quotas completed), purchased gear and upgrades, hub storage contents, unlocked locations, and player settings. What does **not** persist is anything mid-round: a run is saved in the hub, between deployments, never inside a location.

That restriction is deliberate. Allowing mid-round saves would let players quit to escape a bad situation and reload, which destroys the extraction tension the entire game rests on. The save point *is* the hub.

## The Architectural Tension — Read Before Building

Per project convention this must use the shared SaveSystem at `C:\Users\nicky\repo\HiddenObject\Assets\Packages\SaveSystem`. Its README states plainly under "What's Locked In":

> Single-player only (no encryption or server sync required) · File-based storage (local to device, no cloud)

Unpaid Interns is server-authoritative co-op. These do not automatically fit together, and the mismatch must be resolved by an explicit decision rather than discovered halfway through:

- **The host owns the save.** The run lives on whichever machine runs the server world. Clients persist nothing but their own settings.
- **Consequence:** if the host stops hosting, the run is gone for everyone. That is a real product decision — acceptable for a friends-group game, unacceptable if players expect to continue with whoever is online.
- **Alternative** if that is unacceptable: each client keeps a shadow copy for display only, or the run is exported to a code players can carry. Both are substantially more work and should not be assumed.

## How to Build

**Bring the packages into the project**

- Both packages live in a different repository (`HiddenObject`). Decide how they arrive here: git submodule, embedded package under `Packages/`, or a copy. A submodule keeps them shared and updatable; a copy will drift. Pick deliberately.
- `SaveSystem.asmdef` references `Packages.EventBus`, so EventBus must come first — it has no references of its own and is the leaf dependency.
- Both are `autoReferenced: true`, so gameplay assemblies will see them once present. Verify they compile against this project's Unity version before writing any bridge code.

**Write the GameSaveBridge**

- The package deliberately excludes the bridge — every game writes its own. Use the template in the SaveSystem README as the starting point.
- Place it at `Assets/Scripts/Gameplay/Run/UnpaidInternsSaveBridge.cs`, created alongside `SaveDataService` in a bootstrapper, following the README's `GameBootstrapper` pattern.
- **Only instantiate it on the host.** Guard creation on the server world existing, so clients never attempt to write a run file.
- Subscribe to the SaveSystem's events via `EventBusProvider.Instance.EventBus` using the generic `Subscribe<T>()` form shown in the template.

**Bridge the ECS boundary**

- `SaveDataService` is a MonoBehaviour holding managed types; ECS systems cannot reference it. Do the translation at the `GhostMonoBehaviour` layer — the Run Manager already sits exactly there and already holds every value worth saving.
- On save, read the Run Manager's ghost component data and write it into `SaveGameData`'s `intData` / `stringData` dictionaries.
- On load, write values back into the Run Manager on the server **before** any client connects or the round starts, so replication carries correct state outward rather than overwriting it.

**Define what and when**

- Save on: entering the hub after a round settles, completing a store purchase, and meeting a quota.
- Do **not** enable the package's periodic autosave during a round — its whole purpose conflicts with the no-mid-round-save rule.
- Use `SaveGameAsync` rather than the synchronous form so the host does not hitch while other players are standing around.
- Settings (volume, sensitivity) are per-client and belong in a separate local slot, not in the run save. `SaveGameData` already carries `musicVolume` and `sfxVolume` fields for this.

**Handle failure honestly**

- Subscribe to `SaveGameFailedEvent` and surface `SaveErrorData.ErrorType` to the player. A silent failed save that discards hours of a run is the worst possible outcome.
- Handle `DataCorruptedEvent` and `DataRepairedEvent` — the service auto-repairs where it can, and the player should be told when it did.

## Acceptance Criteria

- [ ] EventBus and SaveSystem are present in this project, compile cleanly, and the acquisition method (submodule / embedded / copy) is documented.
- [ ] A run saved in the hub restores with identical day, credits, quota, quotas completed, purchased gear, and stored loot.
- [ ] Restoring writes into the Run Manager on the server before clients connect, and clients receive the restored values, not defaults.
- [ ] The save bridge is instantiated only on the host; a pure client creates no run file.
- [ ] No save occurs during an active round, verified by quitting mid-round and confirming the run resumes from the last hub state.
- [ ] Quitting mid-round and reloading does not let a player recover loot they had not banked.
- [ ] Saving does not produce a visible frame hitch for any player.
- [ ] A failed save surfaces a specific, player-readable message derived from `SaveErrorData.ErrorType`.
- [ ] A corrupted save is either repaired with the player informed, or fails with a clear message and does not silently load partial state.
- [ ] Player settings persist independently of the run and survive a run being failed or deleted.
- [ ] The host-owns-the-save consequence is documented somewhere players will encounter it.
