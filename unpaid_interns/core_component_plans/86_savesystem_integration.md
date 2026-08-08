# 86 — SaveSystem Integration

**Source:** [`core_components.md`](../core_components.md) §11 — Technical Foundations, §14 — Shared Package Integration
**Status:** ❌ Not present in this project
**Depends on:** [EventBus Integration](85_eventbus_integration.md) (hard dependency), [Session Persistence](06_session_persistence.md)
**Blocks:** run state surviving a quit, settings persisting, unlock progression

## Summary

The shared persistence package project convention requires, and the plumbing that connects it to this game.

There is deliberate overlap with [`06_session_persistence.md`](06_session_persistence.md), and the split is worth stating: **that component owns *what* is saved and *when*** — run state in the hub, never mid-round, the host owns the save. **This one owns *the package itself*** — getting it into the project, the bridge that translates game state into its format, and the constraints its design imposes.

The reason it deserves separate treatment is that [`06_session_persistence.md`](06_session_persistence.md) surfaced a genuine architectural tension and left it as a decision rather than resolving it in passing. The package's README states under "What's Locked In":

> Single-player only (no encryption or server sync required) · File-based storage (local to device, no cloud)

Unpaid Interns is server-authoritative co-op. Those do not automatically fit together, and this is where the mismatch gets engineered around rather than discovered.

## How to Build

**Acquire both packages together**

- `SaveSystem.asmdef` references `Packages.EventBus`, so EventBus is the leaf dependency and must arrive first ([`85_eventbus_integration.md`](85_eventbus_integration.md)).
- Use the **same acquisition method for both** — submodule, embedded package, or copy — decided once. A submodule keeps them shared and updatable; a copy will drift and the drift will be found during a bug hunt.
- Both are `autoReferenced: true`, so gameplay assemblies see them once present. Verify they compile against this project's Unity version before writing bridge code.
- Read both READMEs before designing. The notes below reflect what [`06_session_persistence.md`](06_session_persistence.md) recorded — `SaveGameData` with `intData`/`stringData` dictionaries, `SaveGameAsync`, `SaveGameFailedEvent` with `SaveErrorData.ErrorType`, `DataCorruptedEvent`, `DataRepairedEvent`, and a `GameBootstrapper` pattern — but the package governs.

**Resolve the single-player mismatch explicitly**

- **The host owns the save.** The run lives on whichever machine runs the server world; clients persist nothing but their own settings. This is the decision [`06_session_persistence.md`](06_session_persistence.md) recommends and it is the one that makes a single-player-oriented package viable.
- **Instantiate the bridge only on the host**, guarded on the server world existing. A pure client must never create a run file — and must not crash, warn, or degrade because it did not.
- The consequence is real and must be stated to players: if the host stops hosting, the run is gone for everyone. [`08_late_join_rejoin_policy.md`](08_late_join_rejoin_policy.md) and [`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md) both require a host departure to be communicated clearly rather than as a transport error, and this is why.
- Encryption and tamper resistance are out of scope by the package's own design. In a host-authoritative co-op game the host can already alter the run, so a local plaintext save costs nothing that was not already conceded.

**Write the bridge at the GhostMonoBehaviour layer**

- The package deliberately excludes the game-specific bridge; every game writes its own. Place it at `Assets/Scripts/Gameplay/Run/UnpaidInternsSaveBridge.cs`, created alongside the save service in a bootstrapper.
- `SaveDataService` is a MonoBehaviour holding managed types, and ECS systems cannot reference it. **The Run Manager is already exactly at that boundary** and already holds nearly everything worth saving — read its ghost component data on save, write into it on load. No new bridging layer is needed.
- Subscribe to the package's events through `EventBusProvider.Instance.EventBus` with the generic `Subscribe<T>()` form, per the template.
- On load, write into the Run Manager **on the server before any client connects or the round starts**, so replication carries restored state outward rather than a client's defaults overwriting it. [`06_session_persistence.md`](06_session_persistence.md) makes this an acceptance criterion; it is the ordering most likely to be got wrong.

**Map the game's state onto the package's format**

- The package stores `intData` and `stringData` dictionaries, which is a constraint worth planning around rather than fighting. Most of what is saved is integral — day, credits, quota, quotas completed, run seed, unlocked location ids, upgrade ids.
- Collections need an encoding decision. Storage contents ([`46_storage_hub_inventory.md`](46_storage_hub_inventory.md)) are a list of `(itemId, rolledValue, instanceState)`, and the crew roster's per-run fields ([`19_crew_roster.md`](19_crew_roster.md)) are a list of `(stableId, name, deaths)`. Serialise each as a single string field with a documented format, or as indexed keys — **pick one convention and apply it everywhere**, because two encodings in one save file is how a loader silently drops half the data.
- **Version the save.** A schema version key from the first write, and an explicit path for loading an older version. Ghost serialisation is already layout-sensitive (§12); a save file adds a second compatibility surface, and a run that cannot be loaded after a patch is worse than one that was never saved.

**Keep settings out of the run save**

- Settings are per-client and local ([`78_settings_options_menu.md`](78_settings_options_menu.md)); the run save is host-owned. They must be **separate slots**.
- The consequence is specific: [`07_game_over_win_resolution.md`](07_game_over_win_resolution.md) deletes the run save on failure, and a player who loses their mouse sensitivity every time the crew misses quota will think the game is broken.
- Rank is the same shape — per-player, local, and the only thing that survives a failed run ([`69_rank_and_progression.md`](69_rank_and_progression.md)). It belongs beside settings, not in the run file.
- `SaveGameData` already carries `musicVolume` and `sfxVolume`, which is the settings slot's natural home.

**Handle failure loudly**

- Subscribe to `SaveGameFailedEvent` and surface `SaveErrorData.ErrorType` as a player-readable message. A silent failed save that discards hours of a run is the worst outcome this component can produce.
- Handle `DataCorruptedEvent` and `DataRepairedEvent` — the service auto-repairs where it can, and the player should be told when it did rather than discovering a quietly altered run.
- Use `SaveGameAsync` rather than the synchronous form, so the host does not hitch while three other people are standing in the hub.
- **Do not enable the package's periodic autosave.** Its entire purpose conflicts with the no-mid-round-save rule, which exists so players cannot quit to escape a bad situation and reload ([`06_session_persistence.md`](06_session_persistence.md)).

## Acceptance Criteria

- [ ] EventBus and SaveSystem are present, compile against this project's Unity version, and use the same documented acquisition method.
- [ ] EventBus is referenced first as the leaf dependency.
- [ ] The save bridge exists at the `GhostMonoBehaviour` layer and reads and writes Run Manager ghost data directly.
- [ ] The bridge is instantiated only on the host; a pure client creates no run file and produces no errors or warnings.
- [ ] Restoring writes into the Run Manager on the server before any client connects.
- [ ] Day, credits, quota, quotas completed, run seed, unlocked locations, upgrades, storage contents, and per-run roster fields all persist and restore correctly.
- [ ] Collections use one documented encoding convention throughout the save.
- [ ] The save carries a schema version from the first write, with a defined path for loading an older version.
- [ ] Settings and rank persist in separate local slots and survive deletion of the run save.
- [ ] No save occurs during an active round, and the package's periodic autosave is disabled.
- [ ] `SaveGameAsync` is used; saving produces no visible frame hitch for any player.
- [ ] A failed save surfaces a specific, player-readable message derived from `SaveErrorData.ErrorType`.
- [ ] A corrupted save is either repaired with the player informed, or fails clearly without loading partial state.
- [ ] The host-owns-the-save consequence is communicated to players somewhere they will encounter it.
- [ ] A host departure returns clients to the main menu with a clear message explaining the run's fate.
- [ ] Quitting mid-round and reloading resumes from the last hub state and recovers no unbanked loot.
