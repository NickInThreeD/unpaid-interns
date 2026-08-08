# Unpaid Interns — Core Components

The systems that must exist to have a playable, working game of *Unpaid Interns* as described in [`GAME_DESIGN.md`](../GAME_DESIGN.md).

Component scope is informed by the Lethal Company mechanics reference in [`Assets/docs/`](../Assets/docs/README.md) — used as a structural touchstone for which systems a quota-loop extraction game needs — and by an audit of the existing Unity project.

**Status legend:** ✅ Exists — ⚠️ Partial / needs rework — ❌ Missing
Items marked **[MVP]** are the minimum set required for a first playable loop.

---

## 0. Current State of the Project — Read This First

The repo is a working **networked multiplayer first-person shooter deathmatch shell**, derived from Unity's `Unity.MP_FPS` sample. Understanding what it actually is changes how everything below should be built.

### Networking architecture (important correction)

The project uses **Netcode for Entities (DOTS/ECS)**, not Netcode for GameObjects:

- `Packages/manifest.json:16` — `com.unity.netcode` 1.10.0 (NGO would be `com.unity.netcode.gameobjects`).
- Also present: `com.unity.entities`-based physics (`com.unity.physics`), `com.unity.charactercontroller`, `com.unity.services.multiplayer`.
- A custom **GhostBridge** layer (`Assets/Scripts/GhostBridge/`) marries MonoBehaviours to ECS ghost entities, so gameplay can be authored on GameObjects while state replicates through ghosts.

**Consequence for every component below:** networked state is declared as `[GhostField]` on `IComponentData` structs (see `Assets/Scripts/GhostBridge/Player/PredictionComponents.cs`), mutated by server-side `ISystem`s, and read by client MonoBehaviours. Persistent singletons (quota, run state, day clock) follow the `GhostMonoBehaviour` + `IGhostManager` pattern registered in `ManagerGhostsSpawner` — `Assets/Scripts/Gameplay/Leaderboard/GameLeaderboard.cs` is the working reference implementation.

### What already exists and is reusable

- **Session/connection layer** — Relay-based create-or-join, direct host/connect, and dedicated server bootstrap (`Assets/Scripts/Networking/`, `Assets/Scripts/DedicatedServer/`, `GameConnection.cs`). Joining and playing together works today.
- **First-person controller with client prediction** — `Assets/Scripts/Gameplay/Player/Movement/FirstPersonController.cs`, driven by `PlayerPredictionSystem` / `ServerPlayerMovementSystem`, with reconciliation and error smoothing. `CharacterController`-based, walk/jump/fall states, ground detection, footstep SFX, 1P and 3P animator rigs.
- **Health and damage** — `CurrentHealth` / `MaxHealth` / `LastHitTick` on `PredictedPlayerGhost`, with a 1P damage vignette (`DamageVisualsController`) and 3P hit reactions.
- **Weapon framework** — `WeaponData` ScriptableObjects (`Assets/Data/Weapons/`), a `WeaponRegistry`, ammo/reload/cooldown state, and predicted projectiles with server reconciliation (`Projectile.cs`, `ProjectileReconciliationSystem.cs`).
- **Sound system** — Pooled emitters, `SoundDef` assets, mixer routing, headless no-op implementation (`Assets/Scripts/Audio/`).
- **UI Toolkit shell** — Main menu, in-game HUD, pause menu, loading screen, connection status, action feed, respawn screen (`Assets/Scripts/UI/`, `Assets/Scripts/Gameplay/UI/`).
- **Server-authoritative spawning** — `ServerGameSystem.cs` handles join requests, spawn-point selection with overlap avoidance, disconnect cleanup.
- **Replicated manager singleton pattern** — `LeaderboardManager` demonstrates ghost dynamic buffers, broadcast RPCs, and server/client update split.

### What exists but actively conflicts with the design

- ⚠️ **Auto-respawn on death** — `ServerGameSystem.HandlePlayerDeathAndRespawn` destroys the player entity and respawns after a 5-second timer. Unpaid Interns needs death to be *permanent for the round*, leaving a recoverable body and a spectating player. This is a rework, not an addition.
- ⚠️ **Deathmatch scoring** — `LeaderboardManager` tracks kills/deaths and broadcasts a kill feed. The design needs a *shared* team objective, not competitive scoring. The class is a good structural template for a `RunStateManager`, but its semantics are wrong.
- ⚠️ **Combat-first player kit** — The player spawns holding a rifle or shotgun (`ServerGameSystem.SpawnPlayerCharacter`, character index 0/1). Interns should spawn empty-handed, with weapons as rare, expensive, mostly-defensive tools.

### Notable gaps in the existing player controller

- ⚠️ **Sprint is defined but never applied.** `ControllerConsts.Sprint` exists, but `FirstPersonController.GetStateConsts` assigns `consts.Walk` in every branch — sprint constants are dead code. Sprint must be wired before stamina means anything.
- ❌ **No crouch.** `MovementType` has only Standing / Jumping / Falling. Crouch is central to stealth against sight-based monsters.
- ❌ **No interact verb.** `PlayerInput.InputFlag` carries only Jump / Shoot / Reload, and `ClientInputReaderSystem.ProcessGameplayInput` wires only those three. `Interact`, `Crouch`, and `Sprint` exist as *bindings* in the generated `InputSystem_Actions.cs` but reach no gameplay code.
- ❌ **No stamina, no carry weight, no drop.**

### Infrastructure present but unused

- **`com.unity.ai.navigation` 2.0.11** is installed, but there is zero AI or NavMesh code in `Assets/Scripts`. Monster navigation starts from nothing.
- **Addressables** is set up and already used for runtime prefab loading (projectiles, ghost prefabs) — the right mechanism for loot and monster prefabs too.
- **No EventBus and no SaveSystem** exist in this project. Per project convention, both should come from the shared packages rather than being reinvented (see §12).

### The honest summary

The **multiplayer foundation, movement, and presentation shell are done and solid.** Essentially **none of the Unpaid Interns game loop exists**: no items, no inventory, no interaction, no monsters, no AI, no round/day structure, no quota, no economy, no procedural generation, no extraction. The build order in §13 reflects that.

---

## 1. Game Loop & Session State

- ❌ **Run Manager [MVP]** — Owns the contract from start to game-over: current day, days remaining, cumulative earnings, win/loss state. Build as a `GhostMonoBehaviour` implementing `IGhostManager`, registered in `ManagerGhostsSpawner.ManagersToSpawn`, with server-authoritative state exposed via `[GhostField]`. Model it on `LeaderboardManager`.
- ❌ **Day Cycle Controller [MVP]** — Drives one round: deploy → scavenge → extract → settle. Advances the in-round clock and fires phase events. Must run as a server-side system with phase state replicated to clients, since all players share one clock.
- ❌ **Round Timer / Clock [MVP]** — A compressed in-round clock exposing *normalized time* (0→1), which monster spawning and escalation key off. Drive from `NetworkTick` rather than `Time.deltaTime` so server and clients agree.
- ❌ **Hub / Between-Rounds State [MVP]** — The safe planning state: review earnings, buy gear, pick the next destination, launch. Requires a new global state alongside the existing `GlobalGameState` enum (`MainMenu` / `Loading` / `InGame` in `GameSettings.cs`), which currently has no concept of a between-rounds phase.
- ❌ **Location Load / Unload Flow [MVP]** — Streaming a chosen location in and out between rounds. `ScenesLoader` handles the one-time gameplay scene load today; it needs to support repeated per-round loads for both server and client worlds, including subscene baking for ECS.
- ❌ **Session Persistence** — Saves run state (day, money, quota, gear, unlocks) so a contract survives quitting. Route through the shared SaveSystem package (§12).
- ❌ **Game Over / Win Resolution [MVP]** — Evaluates quota at the deadline: continue to the next cycle, or terminate the crew and wipe the run.
- ⚠️ **Late Join / Rejoin Policy** — `ServerGameSystem.HandleJoinRequests` spawns anyone who connects, at any time. A round-based game needs a rule: join as spectator mid-round, join at the hub between rounds, or lock the session once deployed.
- ❌ **Departure & Extraction Resolution [MVP]** — Step 5 of the core loop — *deciding when to leave* — and the only step that had no owner. Who may start the departure and whether it can be aborted; the announced grace window in which the crew can still run for it and still bank; the point of no return that freezes outcomes; and what each intern is at that instant — extracted, left behind, dead, or disconnected. Also the rule for what is forfeited, which the design states for unbanked loot and leaves undefined for everything else. Five existing components reference this mechanic and none implements it. See [`105_departure_and_extraction_resolution.md`](core_component_plans/105_departure_and_extraction_resolution.md).
- ❌ **Round Teardown & State Reset [MVP]** — Step 7 of the core loop — *repeat* — as an owned, ordered sequence rather than nineteen independent cleanup implementations. Nothing in this codebase has ever run a second round: `ScenesLoader` loads one hardcoded scene once, and unload exists only for full session teardown. Nineteen plans already carry a "nothing leaks into the next round" criterion with no shared ordering, no registration mechanism, and no leak detection. See [`106_round_teardown_and_state_reset.md`](core_component_plans/106_round_teardown_and_state_reset.md).

## 2. Player Character

- ✅ **Player Controller [MVP]** — Exists and is prediction-correct (`FirstPersonController.cs`). Reusable as-is for the base movement layer.
- ⚠️ **Sprint [MVP]** — Constants exist but are never applied (`GetStateConsts` always selects `Walk`). Needs a `Sprint` input flag, a state branch, and stamina gating.
- ❌ **Crouch [MVP]** — New `MovementType`, collider height change, speed penalty, and a visibility modifier consumed by monster perception. The single most important stealth verb.
- ⚠️ **Health & Injury System [MVP]** — Health exists on `PredictedPlayerGhost`; the *injury* layer does not. Add a critically-injured state below a threshold: slow regeneration, forced limp, no sprinting — so surviving an encounter carries a cost for the rest of the round.
- ❌ **Stamina System [MVP]** — Drains on sprint and jump, scaled by carry weight; regenerates when walking or standing still. This is the mechanic that makes "grab one more item" a real decision. Must live on the predicted ghost state so the client can predict it.
- ❌ **Carry Weight [MVP]** — Total held weight modifies movement speed and stamina drain, turning value-per-pound into a genuine optimization problem.
- ⚠️ **Death & Body System [MVP]** — Currently the player entity is destroyed and respawned after 5s. Replace with: drop carried items at the death position, spawn a carryable body ghost, move the player to spectator for the remainder of the round, and apply a credit penalty that is larger if the body is never recovered.
- ⚠️ **Fear / Stress Feedback** — `DamageVisualsController` provides a damage vignette that can be extended into a fear overlay driven by proximity, darkness, and being hunted. Presentation rather than mechanics, but it is what makes the horror land.
- ❌ **Player Scanner / Ping Tool** — Highlights nearby items, exits, and the extraction point and reports visible value. The primary navigation aid in dark procedural interiors, and a natural fit for the existing Addressables/VFX pipeline.
- ❌ **Climbing & Verticality** — Ladders and climbable surfaces. `FirstPersonController` has no climbing state (a `DEBUG_RENDER_CLIMBING_MOVEMENT` define is referenced at line 232 but no climbing code exists). Needed for multi-level interiors, deployable ladders as purchasable gear, and the two-handed rule that blocks ladder use.
- ❌ **Player-vs-Player Collision & Friendly Fire Policy** — Whether interns can block each other in doorways, push one another, or deal damage. All three are design decisions with real consequences in a co-op horror game: body-blocking during a chase is either a beloved emergent mechanic or a griefing vector, and there is currently no stated rule either way.

## 3. Multiplayer & Team

- ✅ **Networking Layer [MVP]** — Netcode for Entities with GhostBridge, working relay and direct connection, dedicated server bootstrap. **Foundational and already done.**
- ✅ **Client Prediction & Reconciliation [MVP]** — `PlayerPredictionSystem`, `PlayerMovementHistory`, `ProjectileReconciliationSystem`. Any new predicted state (stamina, crouch, held item) must plug into this pipeline rather than bypassing it.
- ⚠️ **Crew Roster [MVP]** — `ClientsMap` and `JoinedClient` track connections and player entities. Needs extending with per-round survival state: alive / dead / left-behind / extracted. The quota is collective, so this is what failure is applied to.
- ❌ **Networked Interaction Authority [MVP]** — A server-authoritative rule for who may pick up, drop, or use a given object, so two players grabbing the same item resolves cleanly. Needs an ownership/claim field on item ghosts plus client-side prediction of the common case.
- ❌ **Proximity Voice / Comms** — Distance-based voice plus a radio item for long range. In a game about splitting up in a dangerous building, communication *is* a mechanic — and voice monsters can hear is a design lever. Not currently present in any form.
- ⚠️ **Spectator Mode [MVP]** — `RespawnScreen.cs` exists for the respawn flow but there is no free/follow spectator camera. Required once death becomes permanent for the round: dead players need something to do, plus optionally a vote-to-leave-early.
- ❌ **Shared Session State Sync [MVP]** — Quota, money, day count, and destination must be identical for all clients. Use ghost fields on the Run Manager, not per-client local state.
- ⚠️ **Mid-Round Disconnect Handling [MVP]** — `ServerGameSystem.RefreshClientsMap` destroys the player entity and its input entity on disconnect, which is correct plumbing but has **no gameplay semantics**. Unanswered: does a dropped player's carried loot fall to the floor or vanish? Do they count as dead for the death penalty? Does the team keep earning on their behalf? In a game where one disconnect can cost a shared quota, this needs an explicit rule.
- ❌ **Reconnection** — Rejoining a session after a drop and recovering your character or spectator state. Netcode for Entities does not provide this for free; without it, a brief network blip ends someone's entire run.

## 4. Location & World Generation

- ❌ **Location Catalogue [MVP]** — The set of destinations, each with difficulty tier, size multiplier, loot count range, monster budget, and travel cost. Author as ScriptableObjects, mirroring the existing `WeaponRegistry` pattern.
- ❌ **Location Selection / Assignment [MVP]** — Whether the employer assigns a location at random or the team picks from an unlocked list. Determines whether the game is about routing strategy or adapting to what you're given.
- ❌ **Procedural Interior Generator [MVP]** — Assembles interiors from modular room prefabs, sized by the location's multiplier, guaranteeing connectivity and a reachable extraction point. **This is the largest single piece of new work in the project.**
- ❌ **Deterministic Generation Seed [MVP]** — The server rolls one seed per round and replicates it; every client generates the identical layout. Without this, clients disagree about geometry. A `FixedRandom` singleton exists in `ServerGameSystem.OnCreate`, but **it should not be repurposed as the round seed**: it is created from `DateTime.Now.Millisecond`, is never replicated, and is drawn from by `FindSpawnPoint` on every join, so its state depends on how many people joined and when. Add a separate replicated round seed with per-system derived streams and leave `FixedRandom` alone.
- ❌ **Runtime NavMesh Baking [MVP]** — Procedural interiors need navigation baked at runtime for monster pathfinding. `com.unity.ai.navigation` is installed but entirely unused; `NavMeshSurface.BuildNavMeshAsync` on the server after generation is the intended path. **Do not defer this — it constrains how the generator may assemble rooms.**
- ⚠️ **Entry Point / Extraction Zone [MVP]** — `SpawnPointAuthoring` and `SpawnPointsSubScene` provide static spawn points today. Needs to become a per-location extraction volume that both spawns players and detects deposited loot — the position that defines the map's whole risk gradient.
- ❌ **Alternate Exits** — Secondary exits dropping into random parts of the map, shortening hauling routes at the cost of unpredictable placement.
- ❌ **Exterior / Approach Area** — Outdoor space between drop-off and building entrance: a decompression zone and a habitat for a distinct threat set.
- ❌ **Out-of-Bounds Handling** — Boundaries and kill volumes that keep players in the play space without breaking immersion.
- ❌ **Environmental Conditions / Weather** — Per-location modifiers (fog, rain, storm, flooding, blackout) changing visibility, movement, and audio without changing loot value. Cheap replayability; `LightingProfile` / `LightingProfilerApplier` already provide a per-scene lighting-swap mechanism to build on.
- ⚠️ **Lighting & Power Grid** — URP lighting and baked probe volumes are configured for the existing `GameScene`. Needs dynamic, runtime-generated lighting plus a breaker/fuse box that can cut power to an area, making darkness a real tactical state for both players and monsters.

## 5. Items, Loot & Inventory

- ❌ **Item Definition / Data Model [MVP]** — ScriptableObject per item: value range, weight, two-handed flag, passive noise, special properties, per-location rarity. Follow the `WeaponData` + `WeaponRegistry` pattern already in `Assets/Data/Weapons/`, including the numeric-ID lookup, since ghost fields must carry IDs rather than object references.
- ❌ **Item Ghost / Networked Item State [MVP]** — Every world item needs a replicated identity: item ID, rolled value, position, held-by, and banked flag. Build on `GhostGameObject` + `GhostSpawner`, which already handle Addressable prefab spawning with a network GUID.
- ❌ **Loot Spawner [MVP]** — Populates each generated location with items per round, honoring count ranges and rarity weights so the same map plays differently every visit. Server-authoritative, seeded from the same round seed.
- ❌ **Inventory / Item Bar [MVP]** — A hard-limited set of carry slots (e.g. four) with scroll-select, pick up, and drop. Slot state belongs on the predicted player ghost so the local client can predict pickups. The limit is what forces repeated trips to the extraction point.
- ❌ **Interaction System [MVP]** — Raycast-based "look at and press E" targeting with contextual prompts. Requires adding an `Interact` flag to `PlayerInput.InputFlag` and wiring it in `ClientInputReaderSystem.ProcessGameplayInput` — neither exists today.
- ❌ **Two-Handed Item Rule** — Bulky, high-value items occupy both hands, block further pickups, and lock out interactions (ladders, doors, terminals). Turns the biggest payday in the room into a deliberate vulnerability.
- ❌ **Loot Banking / Deposit [MVP]** — Registers items inside the extraction zone as secured. Anything not banked when the round ends is lost — the rule the entire "when do we leave" decision rests on.
- ❌ **Tool & Equipment Items** — Purchasable gear that changes how a run plays: flashlight, radio, ladder, defensive weapon, key/lockpick, medical item. The money sink that makes earning above quota worthwhile.
- ⚠️ **Weapons as Tools** — A full predicted weapon and projectile stack already exists and works. It should be **repurposed and de-emphasized**: weapons become rare, expensive, mostly-defensive, and are not part of the starting kit as they are today.
- ❌ **Storage / Hub Inventory** — Persistent storage for gear and unsold loot between rounds, plus a way to query total stored value before deciding to sell.
- ❌ **Physics Props & Throwing** — Dropped items behaving as physics bodies that can be thrown, dropped down stairwells, or used to trigger hazards from a distance. **Correction to an earlier draft:** `com.unity.physics` (DOTS physics) is installed but is *not* what gameplay collides against — the player is `CharacterController`-based and every gameplay query in the project uses built-in PhysX (`Physics.SphereCastNonAlloc` in `FirstPersonController`, `Physics.SphereCast` / `OverlapSphere` in `Projectile.cs`, `Physics.OverlapSphereNonAlloc` in `ServerGameSystem`, which imports `using Collider = UnityEngine.Collider;` to disambiguate). Item physics should therefore be built on built-in rigidbodies, which is also what `NavMeshSurface` bakes from. No item physics exists today. Determines whether "throw the noisy item to distract the monster" is possible at all.

## 6. Monsters & AI

> Nothing in this section exists in any form. This is the largest greenfield area after world generation.

- ❌ **Monster Data Definitions [MVP]** — Per-monster stats: health, damage, speed, senses used, indoor/outdoor category, spawn weight, max count, and a "power cost" for budgeting. ScriptableObjects with numeric IDs, mirroring `WeaponRegistry`.
- ❌ **Monster Ghost & Replication [MVP]** — Monsters simulate on the server and replicate to clients as interpolated ghosts. Use `GhostGameObject` for the MonoBehaviour bridge; monsters should **not** be client-predicted, unlike players.
- ❌ **Spawn Director [MVP]** — Runs on a periodic cycle through the round, weighted by time of day and location, capped by a per-category power budget. The single most important pacing knob.
- ❌ **Difficulty Escalation [MVP]** — Ramps threat as the round progresses and as the deadline nears, so lingering is always more dangerous than leaving. Should also react to team success to prevent risk-free farming.
- ❌ **Spawn Points / Vents** — Fixed emergence locations with a telegraphed audio wind-up, so spawns are readable and avoidable rather than arbitrary — and never on top of a player.
- ❌ **Perception System [MVP]** — Sight (visibility modified by crouching, standing still, distance) and hearing (noise range and volume). Must be inspectable and consistent, because counterplay depends on players learning the rules.
- ❌ **Noise Emission System [MVP]** — Every action and item publishes a noise event with range and volume: walking, sprinting, landing, dropping, voice, tools, passive noisemakers. The perception system consumes these. Route through the shared EventBus (§12). Note the existing `SoundSystem` is **presentation-only** — it plays audio, it does not model what entities can hear. These are two separate systems that must stay in sync.
- ❌ **Chase & Pathfinding [MVP]** — NavMesh pursuit with last-known-position search, line-of-sight breaking, and give-up conditions. Depends entirely on runtime NavMesh baking (§4).
- ❌ **Threat / Interest Targeting** — Monsters choose between valid targets by how dangerous and how appealing each is (armed, injured, loot-laden). Lets players manipulate aggro deliberately — the difference between a chase system and an AI system.
- ⚠️ **Attack & Damage Application [MVP]** — A server-authoritative damage path already exists for weapons hitting players (`PredictedPlayerGhost.CurrentHealth`, `LastHitTick`, `LastDamageAmount`). It needs extending to monster→player and player→monster, with hitboxes, telegraphs, and per-monster kill behavior.
- ❌ **Monster Variety Set [MVP]** — A starting roster covering distinct counterplay archetypes: one that hunts by sound, one by sight, one stationary that blocks a route, one unavoidable that must be fled. Three or four well-differentiated monsters beat ten similar ones.

## 7. Hazards & Environment Interaction

- ❌ **Static Map Hazards** — Non-moving lethal dangers placed during generation: mines, turrets, crushing traps. They punish careless movement without requiring AI and give the map itself a personality.
- ❌ **Door System [MVP]** — Openable, closable, lockable doors with per-monster open speeds and a key/lockpick counter. Doors are the primary tool players have for buying time in a chase, and they must be networked state, not local animation.
- ❌ **Fall & Environmental Damage [MVP]** — Height-based damage bands, drowning, instant-death pits. `ControllerState.FallHeight` is **already tracked and replicated** in `FirstPersonController` but nothing consumes it — fall damage is close to free to add.
- ❌ **Hazard Control / Remote Disable** — A way for someone at the hub to temporarily disable hazards for the field team. Gives the stay-behind role something meaningful to do.

## 8. Economy & Progression

- ❌ **Currency System [MVP]** — A persistent team-wide balance carried across rounds, wiped only on run failure. Server-authoritative; a ghost field on the Run Manager.
- ❌ **Quota System [MVP]** — The target that must be sold within each cycle of days, escalating each time one is met so difficulty compounds. Missing it at the deadline ends the run for everyone.
- ❌ **Selling / Payout [MVP]** — Converts banked items into currency at round end or at a dedicated sell location. Consider a time-based sell rate (worse payout for selling early in a cycle) to layer a second timing tension on top of the quota.
- ❌ **Bonus & Penalty Rules** — Overtime bonus for exceeding quota, a fee per death, a larger fee per body left behind. What makes individual recklessness a shared cost.
- ❌ **Store / Purchasing [MVP]** — Spend currency on tools and upgrades between rounds, delivered at the start of the next round. Without a spend, earning above quota has no purpose.
- ❌ **Upgrades** — Persistent purchases improving the hub or team capability — strategy-shaping, not just numeric bumps.
- ❌ **Rank / Progression** — Long-term progression across runs (intern → senior → up the corporate ladder). Retention and flavor; safe to defer.
- ⚠️ **Performance Report** — `LeaderboardManager` + `LeaderboardUi` provide a working replicated-scoreboard pattern that should be **repurposed** into an end-of-day report grading haul, deaths, and efficiency. The plumbing is right; the semantics (competitive kills/deaths) are wrong.

## 9. UI & Feedback

- ⚠️ **HUD [MVP]** — `InGameHUD.cs` renders health, ammo, reload, and reticle by querying `PredictedPlayerGhost` directly. Needs stamina, the inventory bar, held item, and time of day; ammo and reticle become secondary. The ECS-query pattern it uses is the right one to extend.
- ❌ **Quota & Deadline Display [MVP]** — Persistent visibility of earned / required / days remaining, so players can answer "is this trip necessary?" at a glance.
- ❌ **Interaction Prompts [MVP]** — Contextual pick-up / open / use prompts with clear affordances, including a hands-full state.
- ❌ **Terminal / Hub Interface [MVP]** — Choosing a destination, buying gear, reviewing status. Diegetic (an in-world computer) supports the tone better than a menu. Build in UI Toolkit alongside the existing screens.
- ❌ **Monitoring / Camera System** — Lets a hub-bound player watch teammates and the map and call out threats, turning "someone stays behind" into a real role.
- ❌ **End-of-Round Summary [MVP]** — Items banked, money earned, who survived, quota progress. The moment the round's decisions get judged.
- ✅ **Main Menu & Lobby [MVP]** — `MainMenu.cs`, `StartHostPopUp`, `DirectConnectPopUp`, `ConnectionStatusScreen`, `LoadingScreen`, `PauseMenu` all exist and work. Player name entry is already wired (`MainMenu.cs:49` → `GameSettings.PlayerName`).
- ⚠️ **Action Feed** — `ActionFeed.cs` broadcasts kill/join messages via RPC. Repurpose for team-relevant events: player died, item banked, quota met, ship leaving.
- ❌ **Settings / Options Menu [MVP]** — **There is no options screen of any kind.** No `Settings.uxml` exists among the UI Toolkit assets, and mouse sensitivity is a hardcoded `const float sensitivity = 3.7f` at `ClientInputReaderSystem.cs:78`. Required to ship: look sensitivity, invert-Y, audio volume sliders per mixer group, graphics quality, FOV, and input rebinding (the Input System supports rebinding, but nothing exposes it). This is a genuine omission from the earlier draft, not a nice-to-have.
- ❌ **Accessibility [MVP]** — **Elevated from optional to required by this game's design.** Monster detection is primarily an audio skill, so without visual sound indicators (directional cues for footsteps, growls, vent noise) and subtitles for audio warnings, a deaf or hard-of-hearing player cannot meaningfully play. Also: colorblind-safe HUD and scanner highlights, FOV control and head-bob reduction for motion sensitivity, and subtitle support for any voice lines.
- ❌ **Teammate Identification** — Distinguishing crewmates in dark interiors: suit colors, name tags at range, or a HUD roster. Player names exist in data (`PlayerGhost.PlayerData.Name`) but are not surfaced in-world. Cosmetic on its face, but it directly affects whether "who is that ahead of me" is answerable — which matters when some monsters imitate players.
- ⚠️ **Pause Semantics in Multiplayer** — `PauseMenu` exists, but pausing cannot stop a networked simulation. The menu must be explicit that the world keeps running, and must not imply safety. Currently untested against a live session.

## 10. Audio

- ✅ **Spatial Audio System [MVP]** — Pooled emitters, `SoundDef` assets, mixer routing, and a headless no-op path all exist (`Assets/Scripts/Audio/`). Verify occlusion support — in a game where hearing a monster before seeing it is the primary survival skill, occlusion is a gameplay system, not polish.
- ❌ **Monster Audio Cues [MVP]** — Distinct, learnable per-monster sounds for idle, alerted, and chasing. Players should identify a threat and its state without line of sight.
- ❌ **Ambience & Time Cues** — Per-location environmental beds plus stingers at phase transitions, carrying the passage of time when the clock isn't visible.
- ⚠️ **Player Audio [MVP]** — Footsteps exist (`HandleFirstPersonFootstepSFX`) but fire **only for the locally owned client** and are unaware of surface, speed, or crouch. They must also feed the noise-emission system, so what players hear matches what monsters hear.

## 11. Technical Foundations

- ✅ **ECS / GhostBridge Architecture [MVP]** — Established and working. All new networked gameplay must follow it: `[GhostField]` state, server `ISystem` mutation, `GhostMonoBehaviour` presentation.
- ✅ **Addressables [MVP]** — Configured and already used for runtime prefab loading. The right mechanism for loot, monster, and room-module prefabs.
- ❌ **EventBus Integration [MVP]** — Cross-system communication (noise, damage, item banked, phase changed, quota met) should route through the shared EventBus package at `C:\Users\nicky\repo\HiddenObject\Assets\Packages\EventBus` rather than direct references or a new event system. **Not currently present in this project.** Note it must interoperate with ECS systems, which cannot hold managed references — likely a thin bridge at the MonoBehaviour boundary.
- ❌ **SaveSystem Integration** — All persistence (run state, unlocks, settings) should go through the shared SaveSystem package at `C:\Users\nicky\repo\HiddenObject\Assets\Packages\SaveSystem`. **Not currently present.**
- ⚠️ **Data-Driven Configuration [MVP]** — `WeaponData` / `WeaponRegistry` / `LightingProfile` establish the ScriptableObject-with-numeric-ID pattern. Extend it to items, monsters, locations, and quota curves so designers can tune without code changes — essential given how much of this game is balance work.
- ✅ **Object Pooling** — `SoundGameObjectPool` exists for audio. Loot, monsters, and VFX will need equivalent pooling as procedural maps spawn many objects.
- ⚠️ **Debug & Cheat Tooling [MVP]** — `ConfigVar`, `PlayModeSettings`, and Multiplayer Play Mode support exist. Add console commands to skip time, force spawns, grant money, teleport, and toggle god mode — without these, testing a 10-minute round loop is prohibitively slow.
- ❌ **Automated Tests** — `com.unity.test-framework` is installed with no tests written. Generation connectivity, quota math, and loot-value rolls are all pure logic and cheap to cover.

## 12. Build & Release Readiness

Multiplayer that works in the Editor does not automatically work in a shipped build. These are the gaps between the two, verified against the project's current configuration.

**Chosen connection strategy: Relay + Lobby.** The host is a player's machine, so there is no per-hour server cost — a good fit for small co-op sessions.

- ✅ **Unity Gaming Services is already linked** — `ProjectSettings/ProjectSettings.asset:934` carries `cloudProjectId: bc8406a5-fddf-4bb6-b45f-ac19f6f0df6e` under `organizationId: nickinthreed`, and `ProjectSettings/Packages/com.unity.services.core/Settings.json` pins the `production` environment. (Note: `UnityConnectSettings.asset` having `m_Enabled: 0` is **not** relevant — that flag governs legacy Unity Analytics/Ads/Crash Reporting, not UGS project linkage.)
- ✅ **The Relay transport path is fully implemented** — `EntityDriverConstructor.cs:67,92` applies `WithRelayParameters` to both client and server drivers; `EntityNetworkHandler.cs:46-49` resolves Relay host and client endpoints; `GameConnection.cs:49` requests `.WithRelayNetwork()`; anonymous authentication is wired at `GameConnection.cs:122`. `MainMenu.cs:137` already routes the "Create Game" button to the Relay path.
- ❌ **Confirm Relay and Lobby are enabled in the Unity Cloud dashboard** — Linking a project does not enable individual services. This is the one remaining setup step and it cannot be done from the codebase.
- ❌ **Join-by-code is dead code [MVP]** — `GameConnection.JoinGameAsync` (line 58) implements joining a specific session by code, but no `CreationType` maps to it and no menu button invokes it (`ConnectionSettings.CreationType` has only `CreateOrJoin`, `Host`, `ConnectAndJoin`). Today `CreateorJoinGameAsync` matches players by **session *name*** via `GameSettings.Instance.SessionName`, so two unrelated groups choosing the same name collide, and there is no way to join a specific friend's session. `SessionInfo.cs:211` already surfaces a copyable session code, so only the join half is missing. **Wire this before the first real playtest.**
- ⚠️ **Session lifecycle for a round-based game** — `SessionOptions.MaxPlayers` is `GameManager.MaxPlayer` (32, a deathmatch number). Set it to the real crew size, and decide whether the session locks once the team deploys or stays joinable between rounds.
- ✅ **Direct connect remains available as a fallback** — `HostGameAsync` (line 95), `ConnectGameAsync` (line 105), and `GetServerConnectionSettings` (line 75) never touch Unity Services, and `GameManager.StartGameAsync:277` handles the no-session case. Useful for LAN testing and for diagnosing whether a failure is transport-level or service-level.
- ✅ **UGS-free dedicated server path also exists** — `ServerBootstrap.cs` (guarded by `UNITY_SERVER`) reads a `port=` CLI argument, defaults to 7979, and listens directly. Retained as an option if hosting ever needs to move off player machines.
- ⚠️ **Addressables content build** — Ghost prefabs, projectiles, and player prefabs load through Addressables at runtime. Addressables content must be built **before** the player build, or these resolve to null in the shipped game while working fine in the Editor. Only a "Default Local Group" exists today; loot, monster, and room-module prefabs will need group organization as they are added.
- ⚠️ **Entity subscene baking** — `GameResourcesSubScene` and `SpawnPointsSubScene` are baked entity scenes and are correctly listed in `EditorBuildSettings`. Any new subscene (procedural room modules, per-location content) must be added to build settings and to every relevant build profile, or the entity world comes up empty at runtime.
- ⚠️ **Client/server build parity** — Ghost serialization is layout-sensitive; `FirstPersonController.ControllerState` carries an explicit warning that adding members can break network serialization. **A client and server built from different code revisions will fail to communicate, often subtly.** Version-stamp builds and reject mismatched connections.
- ✅ **Build profiles exist** — `Windows Client`, `Android Client`, and `FPS2 Windows Server` (dedicated server subtarget, boots `ServerScene`) are configured. `GameManager` detects headless via `-batchmode` or Linux standalone and swaps in `SoundSystemNull`.
- ⚠️ **Editor-only test paths** — Multiplayer Play Mode and thin clients are `#if UNITY_EDITOR` only. **Editor multiplayer testing does not prove a build works** — every networking change needs verification with two real builds, or a build against an Editor host.
- ❌ **Build verification pass** — There is no evidence in the repo of a client-vs-client build having been run. Do this early, before the codebase grows: the failure modes above (Addressables, subscenes, UGS) are all cheap to fix now and expensive to diagnose later.

## 13. Onboarding, Performance & Long Tail

Items that don't belong to a single gameplay system but block a finished product.

- ❌ **Tutorial / Onboarding** — This genre is unusually opaque: quota timing, carry limits, monster counterplay, and extraction rules are all learned through expensive failure. At minimum, an in-fiction orientation (the employer briefing new interns) covering the loop, plus contextual first-time hints. The tone supports this better than most games — a patronizing corporate induction is free comedy.
- ❌ **Performance Budget [MVP]** — Procedurally generated geometry, multiple active monsters, dynamic lighting, physics props, and pooled audio, all replicated to several clients. Establish frame and memory budgets early and profile against them; `com.unity.profiling.core` and Unity's profiler are available. Procedural interiors in particular can silently destroy batching and lightmapping assumptions.
- ❌ **Network Bandwidth Budget** — Relay bills on bandwidth, and ghost snapshot size scales with replicated entity count. Every item, monster, and door on the map is a potential ghost. Set per-ghost importance and relevancy rules deliberately rather than replicating everything to everyone, or a loot-dense map will be both expensive and laggy.
- ❌ **Analytics / Balance Telemetry** — Quota success rates, average haul per location, death causes, and round durations. This game is mostly balance work, and balancing without data is guesswork. `com.unity.services.analytics` would integrate with the UGS project already linked.
- ❌ **Localization** — Not needed for a playable build, but retrofitting it after UI text is scattered across UXML and C# is far more expensive than planning for it. Safe to defer if consciously decided.
- ❌ **Build Versioning & Mismatch Rejection** — Ghost serialization is layout-sensitive, so a client and server on different revisions fail in confusing ways. Stamp builds with a version and reject mismatched connections at handshake with a clear message.
- ❌ **Crash / Error Reporting** — Procedural generation and networked state produce bugs that are hard to reproduce from a verbal report. Cloud Diagnostics is available through the linked UGS project and is currently disabled.

## 14. Shared Package Integration

These are the same two items listed in §11 and are planned there — see [`85_eventbus_integration.md`](core_component_plans/85_eventbus_integration.md) and [`86_savesystem_integration.md`](core_component_plans/86_savesystem_integration.md). They are repeated here because acquiring them is a prerequisite that sits outside any one system.

- ❌ **EventBus** — Required by project convention for all event-driven communication. Must be read and referenced before designing the noise, damage, or phase-event systems.
- ❌ **SaveSystem** — Required by project convention for all persistence. Must be read and referenced before implementing run-state saving.

**Both live in a different repository** (`HiddenObject`), so this is an acquisition problem before it is an integration problem. `SaveSystem.asmdef` references `Packages.EventBus`, so EventBus is the leaf dependency and arrives first; the acquisition method — submodule, embedded package, or copy — should be decided once for both.

## 15. Suggested Build Order

The multiplayer foundation is already done, which reorders the plan substantially from a greenfield project.

1. **Player verbs** — Wire `Interact`, `Crouch`, and `Sprint` through `PlayerInput` → `ClientInputReaderSystem` → `FirstPersonController`; add stamina and carry weight to the predicted ghost state. Everything downstream depends on these.
2. **Items and extraction** — Item data model → item ghosts → interaction/pickup → inventory → extraction zone → loot banking → **departure and extraction resolution**. At this point there is a loop, on a hand-built map, with no threats. Departure belongs in this step rather than later: without it there is no way to *end* a round, so nothing downstream can be tested.
3. **Round and economy** — Run Manager → day cycle and clock → **round teardown** → selling → quota → game over → end-of-round summary. This is a complete, playable, monsterless game. Teardown belongs here, before content accumulates: every system added after it is one more thing to retrofit into a sequence that already works.
4. **Death rework** — Replace auto-respawn with permanent-for-the-round death, bodies, spectating, and death penalties. Do this before monsters, or monsters cannot be tuned meaningfully.
5. **Threat layer** — Noise emission → perception → runtime NavMesh → chase/pathfinding → one monster → spawn director. Runtime NavMesh baking may need to come first if procedural generation lands before this step.
6. **Procedural generation** — Room modules → deterministic seeded generator → runtime NavMesh bake → per-round location load/unload.
7. **Depth** — Monster variety, hazards, doors, store and upgrades, weather, hub terminal.
8. **Presentation and retention** — Full audio pass, fear feedback, performance reports, ranks, cosmetics.

## 16. Open Questions

Each question below now has an **owner** — the plan file where the decision is argued and where the answer must be recorded when it is taken. A question is only closed once that file states the decision as a fact rather than as a recommendation; several already carry a strong recommendation and are marked as such.

| Question | Owner | Status |
| --- | --- | --- |
| Is the round on a hard timer, or purely player-ended? | [`02`](core_component_plans/02_day_cycle_controller.md), [`105`](core_component_plans/105_departure_and_extraction_resolution.md) | **Answered.** A hard outer limit with player-chosen early exit; both triggers enter the same departure sequence |
| What is the target crew size? | [`19`](core_component_plans/19_crew_roster.md) | Recommended **4**; `GameManager.MaxPlayer = 32` must be replaced by one configured value |
| Is the quota per-cycle-escalating or a fixed target? | [`64`](core_component_plans/64_quota_system.md) | **Answered.** Escalating, with the curve as data |
| Are locations chosen or assigned? | [`27`](core_component_plans/27_location_selection_assignment.md) | Recommended **chosen**, with quota escalation supplying the pressure — a deliberate reversal of the elevator pitch's wording |
| Do weapons survive as a pillar, or become rare tools? | [`45`](core_component_plans/45_weapons_as_tools.md) | Recommended **rare defensive tools**; the weapon is currently baked into the player prefab, which is the real work |
| How much is predicted versus server-only? | [`20`](core_component_plans/20_networked_interaction_authority.md), [`49`](core_component_plans/49_monster_ghost_and_replication.md) | **Answered.** Movement, stamina, and pickups predicted; monsters interpolated, never predicted |
| Can interns hurt each other? | [`18`](core_component_plans/18_pvp_collision_and_friendly_fire.md) | Recommended **soft collision, heavily reduced friendly fire**; note the inherited code has already answered *yes, at full damage* |
| What happens when someone disconnects mid-round? | [`24`](core_component_plans/24_mid_round_disconnect_handling.md) | Recommended **no death penalty, loot drops, grace window**; [`25`](core_component_plans/25_reconnection.md) owns the return path |
| What happens to an intern still inside when the round ends? | [`105`](core_component_plans/105_departure_and_extraction_resolution.md) | Recommended **lost with the location**, reported as missing rather than deceased; requires a `LeftBehind` crew state |
| Does a total crew loss forfeit loot already banked? | [`105`](core_component_plans/105_departure_and_extraction_resolution.md) | **Answered.** No — anything resting in the extraction zone has already arrived; only what is on the interns is lost |
