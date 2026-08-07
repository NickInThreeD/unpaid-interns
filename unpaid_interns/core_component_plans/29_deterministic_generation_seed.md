# 29 — Deterministic Generation Seed

**Source:** [`core_components.md`](../core_components.md) §4 — Location & World Generation
**Status:** ❌ Not started · **[MVP]**
**Depends on:** Run Manager (to carry the replicated value)
**Blocks:** Procedural Interior Generator, Loot Spawner, Spawn Director, weather selection, reproducible bug reports

## Summary

One number, rolled by the server each round and replicated to everyone, from which every random decision in the round derives.

Without it, clients disagree about geometry — and "disagree about geometry" in practice means a player falls through a floor that exists on someone else's machine, or walks to a door that is a wall for the host. It is a small component guarding an enormous failure.

Its second job is almost as valuable: **reproducibility**. A round that can be regenerated exactly from a seed turns "the exit was walled off" from an unfalsifiable report into a five-second repro. §13 flags procedural generation and networked state as the project's hardest bugs to reproduce; this is the cheapest mitigation available for both.

The raw material exists. `ServerGameSystem.OnCreate` already creates a `FixedRandom` singleton entity — `Random.CreateFromIndex((uint)DateTime.Now.Millisecond)` — and `FindSpawnPoint` draws from it to shuffle spawn points. It is server-only, never replicated, seeded from wall-clock time, and shared with unrelated systems. Each of those is a problem this component fixes.

## How to Build

**Roll one seed per round, on the server, and replicate it**

- Add `RoundSeed` as a `[GhostField]` on the Run Manager alongside the destination id, so it arrives with the same snapshot that tells clients where they are going ([`01_run_manager.md`](01_run_manager.md), [`23_shared_session_state_sync.md`](23_shared_session_state_sync.md)).
- Roll it when the destination is committed, before the location load begins. Every client must have it *before* generation starts, which means it must be inside the load barrier in [`05_location_load_unload_flow.md`](05_location_load_unload_flow.md) — a client that generates before the seed arrives will build the wrong building and the error will surface as physics weirdness, not as a clean failure.
- Derive the round seed from a **run seed** plus the day number, and store the run seed with the save ([`06_session_persistence.md`](06_session_persistence.md)). A whole contract then becomes reproducible from a single value, not just one round.

**Split the stream — do not share one generator**

- This is the part that is easy to get wrong and hard to debug. If the interior generator, the loot spawner, and the spawn director all draw from one `Random`, then **any change to how many numbers one of them consumes silently changes what all the others produce.** Adding a single extra draw to room placement reshuffles the entire loot table, and a bug report's seed stops reproducing after an unrelated commit.
- Give each consumer its own derived, independent stream: hash the round seed with a fixed per-system constant (`hash(roundSeed, "interior")`, `hash(roundSeed, "loot")`, `hash(roundSeed, "monsters")`, `hash(roundSeed, "weather")`) and construct a `Unity.Mathematics.Random` from each.
- Within a stream, draw order must still be fixed and deterministic — a stream protects systems from each other, not from their own nondeterminism.
- Keep the existing `FixedRandom` singleton for genuinely non-deterministic server-side uses like spawn-point shuffling. Do **not** repurpose it as the round seed: it is currently consumed by `FindSpawnPoint` on every join, so its state depends on how many people joined and when, which is exactly the kind of hidden coupling this section exists to prevent.

**Enforce determinism at the boundary**

- Everything downstream of the seed must be free of: `UnityEngine.Random`, `System.Random`, `Time.*`, `DateTime`, uninitialized memory, and iteration over `Dictionary` or `HashSet` where order can differ between runs or platforms.
- Beware float determinism across platforms. Windows and Android clients in the same session must produce the same layout, and floating-point differences accumulate. Prefer integer arithmetic for structural decisions — grid positions, room counts, connection choices — and let floats handle only presentation.
- Add an assertion pass in development builds: after generation, each machine computes a hash of the structural layout and the server compares them. This is the same mechanism as the shared-state hash check in [`23_shared_session_state_sync.md`](23_shared_session_state_sync.md) and should reuse it. A mismatch must be loud and immediate, at the loading screen, not discovered thirty seconds into a round.

**Handle late joiners and rejoins**

- A client arriving mid-round gets the seed from the replicated ghost field and generates the same layout. This is the whole reason the seed lives on the Run Manager rather than being pushed in a one-shot RPC at deploy — an RPC sent before someone connected is lost forever.
- The seed must survive for the whole round, not just the deploy moment. Do not clear it at the end of loading.

**Make it a tool**

- Add `ConfigVar` commands to set the next round seed explicitly and to log the current one. A fixed seed is what makes tuning the generator, the loot tables, and monster spawns possible at all — otherwise every test run changes two variables at once.
- Print the round seed on the loading screen or in the end-of-round summary in development builds, so a tester can copy it into a bug report without opening a log file.
- Add it to the automated generation harness described in [`28_procedural_interior_generator.md`](28_procedural_interior_generator.md): seeds are the harness's input, so the two components share one interface.

## Acceptance Criteria

- [ ] The server rolls exactly one round seed per round and replicates it as a `[GhostField]` on the Run Manager.
- [ ] Every client has the seed before generation begins, enforced by the load barrier.
- [ ] The same seed produces identical layouts, loot placement, and monster spawns on every machine, verified by hash comparison.
- [ ] Each consuming system draws from its own derived stream; adding a draw in one system does not change another system's output for the same seed.
- [ ] The round seed is derived from a persisted run seed plus day number, and a whole run reproduces from one value.
- [ ] The existing `FixedRandom` singleton is untouched by seeded generation and remains available for non-deterministic server use.
- [ ] No downstream generation code uses `UnityEngine.Random`, `System.Random`, wall-clock time, or unordered-collection iteration.
- [ ] A Windows client and an Android client in the same session generate identical layouts.
- [ ] A development-build layout-hash mismatch is reported at the loading screen, loudly, before play begins.
- [ ] A client joining mid-round generates the same layout as everyone else from the replicated seed.
- [ ] The seed persists for the whole round and is not cleared after loading.
- [ ] Debug commands can set the next seed and log the current one, and both work in a build.
- [ ] The seed is visible to testers without reading a log file.
- [ ] Re-selecting the same destination later in a run produces a different layout.
- [ ] Reloading a saved run and playing the same day produces the same layout as the original play-through.
