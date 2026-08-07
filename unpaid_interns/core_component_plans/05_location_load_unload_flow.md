# 05 — Location Load / Unload Flow

**Source:** [`core_components.md`](../core_components.md) §1 — Game Loop & Session State
**Status:** ❌ Not started · **[MVP]**
**Depends on:** Hub State, Run Manager
**Blocks:** Location Catalogue, procedural generation, every per-round system

## Summary

Streaming a chosen location in at the start of a round and out at the end, repeatedly, for the whole crew in lockstep.

This sounds like plumbing and is in fact one of the riskiest components in the project, because the existing scene flow was never designed to do it. `ScenesLoader.LoadGameplayAsync` loads exactly one hardcoded scene — `GameManager.GameSceneName`, the string `"GameScene"` — once, at session start. `UnloadGameplayScenesAsync` exists but is only called when tearing down the entire session on return to the main menu. Nothing repeats.

The hard part is not loading a scene. It is that **ECS subscenes must finish baking and replicating on both the server world and every client world before anyone can play**, and a client whose location has not finished loading while the server has already started the round will fall through the floor or see an empty world. `ScenesLoader.WaitForAllSubScenesToLoadAsync` already solves this correctly for the one-shot case and is the right foundation.

## How to Build

**Generalize the scene loader**

- Refactor `Assets/Scripts/Gameplay/GameManager/SceneLoader.cs` to take a location identifier rather than assuming `GameManager.GameSceneName`.
- Keep `WaitForAllSubScenesToLoadAsync` — it already polls `SceneSystem.IsSceneLoaded` per `SceneReference` entity per world, which is exactly the check that matters. Do not replace it with a plain scene-load await.
- Make unload symmetrical and callable mid-session, not only during session teardown.
- Verify that loading and unloading repeatedly does not leak entities — ECS subscene reload is the most likely source of a slow memory climb over a long run.

**Add a load barrier**

- The server must not start the round until every connected client reports its location fully loaded. Add an explicit ready handshake — clients send an RPC on completion, the server counts them, and only then advances the Day Cycle Controller out of `Deploying`.
- **Procedural generation happens inside this barrier.** Once [`28_procedural_interior_generator.md`](28_procedural_interior_generator.md) lands, "loaded" means subscenes baked *and* the interior assembled from the round seed — a client that reports ready before generating will be standing in an empty shell. Extend the ready condition rather than adding a second barrier.
- The round seed and location id must already be replicated when the barrier opens ([`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md)); a client that begins generating before the seed arrives builds a different building, and the symptom is physics weirdness rather than a clean error. Gate generation on having the seed, and treat a missing seed as a load failure.
- The barrier means **the slowest machine sets the deploy time for everyone**. Budget generation cost accordingly and show progress, or a long generation reads as a hang.
- Follow the RPC pattern already in use: `IRpcCommand` structs with `GhostGameObject.BroadcastRPC` and `ConsumeRPC`, as in `GameLeaderboard.cs`.
- Handle the client that never reports — a timeout with a clear failure path, not an indefinite hang.

**Reuse the existing loading UI**

- `LoadingData.LoadingSteps` and `LoadingScreen.cs` already drive a staged progress display. Extend the step enum for per-round loading rather than building a second loading screen.
- Show the loading screen on the deploy transition and hide it only once the barrier clears, so no player is ever standing in a half-built location.

**Register scenes correctly for builds**

- Every location scene and subscene must be listed in `ProjectSettings/EditorBuildSettings.asset` **and** in each build profile's scene list, including `FPS2 Windows Server`. A subscene present in the Editor but absent from a build profile produces an empty entity world at runtime with no obvious error.
- Confirm the dedicated server profile includes every location, since the server bakes the authoritative copy.

**Handle failure**

- Decide what happens when a location fails to load for one client: abort the deployment for everyone, or drop that client to the hub. Silent partial failure is the worst outcome and must be impossible.

## Acceptance Criteria

- [ ] A location loads on deploy and fully unloads on return to the hub, with no residual GameObjects or entities.
- [ ] Two different locations can be loaded in sequence within one session without a restart.
- [ ] The same location can be loaded twice in a session and behaves identically the second time.
- [ ] The round does not begin until every client reports loaded, verified by artificially delaying one client.
- [ ] A client that fails to load triggers the defined failure path rather than hanging the session.
- [ ] The loading screen is visible for the entire transition and hides only after the barrier clears.
- [ ] Entity count and memory return to baseline after unload, verified across five consecutive load/unload cycles in the profiler.
- [ ] All location scenes and subscenes are present in `EditorBuildSettings` and in every build profile.
- [ ] The flow works in a **standalone build**, not only in the Editor — this is where missing subscene registration surfaces.
- [ ] A dedicated server build loads and unloads locations correctly with no client attached.
- [ ] A client cannot report ready before it has both the round seed and a fully generated interior.
- [ ] A client that never receives the seed triggers the load-failure path rather than generating a mismatched layout.
- [ ] Loading progress is visible throughout generation, so a slow machine reads as slow rather than hung.
