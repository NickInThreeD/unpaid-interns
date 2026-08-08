# 89 — Automated Tests

**Source:** [`core_components.md`](../core_components.md) §11 — Technical Foundations
**Status:** ❌ `com.unity.test-framework` 1.6.0 is installed; there is not one test in the project
**Depends on:** nothing — start immediately
**Blocks:** confidence in the systems that are most expensive to verify by hand

## Summary

`com.unity.test-framework` 1.6.0 is in `Packages/manifest.json` and a search across `Assets` finds **no test assembly and no test file**. Zero.

`core_components.md` identifies the right starting point: *"generation connectivity, quota math, and loot-value rolls are all pure logic and cheap to cover."* That is the whole strategy. This project should not attempt broad coverage — a networked ECS game with procedural generation is expensive to test end-to-end and the effort is better spent elsewhere. What it should do is cover the handful of systems that are **pure logic, high consequence, and slow to verify by hand**, and leave the rest to playtesting and the debug tooling in [`88_debug_and_cheat_tooling.md`](88_debug_and_cheat_tooling.md).

Three properties make a system worth testing here:

1. **It is deterministic and has no Unity dependency** — quota curves, value rolls, settlement arithmetic.
2. **Its failures are silent** — a generation seed that produces an unreachable exit does not throw; it produces a round where someone dies for no reason.
3. **Verifying it by hand is slow** — reaching quota cycle five honestly takes an hour.

Almost every plan in this project that requires a test already requires one against a system with all three properties. That is not a coincidence; it is the filter.

## How to Build

**Set the assemblies up so tests can exist at all**

- Create an EditMode test assembly definition referencing the gameplay assemblies. Nothing exists today, so this is the actual first step and it is fifteen minutes.
- Keep the tested logic **free of Unity types** where possible. A quota curve that takes ints and returns an int is trivially testable; one that reads a ScriptableObject in a `MonoBehaviour` is not. This constraint should shape the code, not the tests — [`87_data_driven_configuration.md`](87_data_driven_configuration.md) already separates tuning configs from the logic that consumes them, which is what makes this achievable.
- PlayMode tests exist and are much slower. Reserve them for things that genuinely need a running world, and expect the count to stay small.
- Wire the suite into whatever CI exists, or at minimum make it a documented pre-merge step. A test suite nobody runs is worse than none, because it implies coverage that is not being checked.

**Cover the harnesses the plans already specify**

Three components already define their tests in detail, and they are the highest-value work:

- **Generation** — [`28_procedural_interior_generator.md`](28_procedural_interior_generator.md) requires a headless harness running **at least 1,000 seeds** asserting connectivity, extraction reachable from every room, no overlapping footprints, room count within tolerance, at least one fire exit, and loot point counts within range. It calls this *"the highest-value place in the project to start"*, and it is right: generation fails silently, fails rarely, and fails expensively.
- **Navigation** — [`30_runtime_navmesh_baking.md`](30_runtime_navmesh_baking.md) extends that harness to sample a path from every emergence point to the extraction zone, because **geometric connectivity is not navigational connectivity** and the failure presents as "the monsters never came", which nobody reports as a bug.
- **Loot** — [`39_loot_spawner.md`](39_loot_spawner.md) requires per-location reporting of total value, count, distribution by distance band, and unreachable items across many seeds, with assertions that no worst-case seed fails to cover travel cost and no best-case seed clears a full quota in one trip.

These three share one harness: **generate a location from a seed, headless, and assert over the result.** Build it once with a seed as its input, as [`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md) specifies, and let each component add its assertions.

**Cover the arithmetic that decides whether the crew lives**

- **Quota curve** — [`64_quota_system.md`](64_quota_system.md) requires monotonicity, no overflow at high cycle counts, and no target that exceeds what any unlocked location can supply.
- **Settlement** — [`66_bonus_and_penalty_rules.md`](66_bonus_and_penalty_rules.md) requires a matrix over payouts, deaths, recoveries, disconnects, and quota excess, asserting the net against hand-computed expectations. This is the single most valuable non-generation test in the project: it is pure arithmetic, it decides run survival, and its edge cases (a player who dies then disconnects) are exactly the ones hand-testing misses.
- **Purchasing concurrency** — [`67_store_purchasing.md`](67_store_purchasing.md) requires N simultaneous requests against a balance affording one to produce exactly one success.
- **Currency invariant** — balance equals starting balance plus the sum of logged transactions ([`63_currency_system.md`](63_currency_system.md)).
- **Targeting** — [`56_threat_interest_targeting.md`](56_threat_interest_targeting.md) requires a matrix of target properties against expected selection per archetype, because a targeting bug is otherwise reported as "it went for the wrong person", which is unfalsifiable.

**Test determinism directly, because everything downstream assumes it**

- [`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md) requires that each consuming system draws from its own derived stream, so **adding a draw in one system does not change another's output for the same seed.** That is a property test: record outputs, add a draw, assert the others are unchanged.
- Assert that generation contains no `UnityEngine.Random`, `System.Random`, `DateTime`, or unordered-collection iteration. A static analysis or reflection check is more reliable than reviewer discipline and catches the regression a year later.
- Same-seed-same-output across runs is the cheapest and most important single assertion in the suite.

**Know what not to test**

- **Not networked behaviour end-to-end.** Two-client scenarios are slow, flaky, and better covered by the network simulator available through `EntityDriverConstructor` in manual testing, which many plans already require ("verify under simulated latency").
- **Not prediction and reconciliation.** Correctness there is a felt property; an automated test that asserts no position correction will be either trivially true or permanently flaky.
- **Not presentation.** UI layout, audio mixing, and visual effects are verified by looking and listening.
- **Not anything the debug tooling already answers faster.** [`88_debug_and_cheat_tooling.md`](88_debug_and_cheat_tooling.md)'s invariant checks run continuously in development builds and catch state corruption in real play, which is a different and complementary safety net.

**Keep the suite honest**

- Every test must be **deterministic and fast**. A generation harness over 1,000 seeds should run in seconds; if it does not, the generator has a performance problem worth knowing about.
- Fix or delete a flaky test immediately. One intermittent failure teaches the team to ignore red, and then the suite is decorative.
- When a bug is found by hand in one of the covered systems, add the case. That is how coverage grows in the places that actually break.

## Acceptance Criteria

- [ ] An EditMode test assembly exists, references the gameplay assemblies, and contains passing tests.
- [ ] The suite runs in CI or is a documented pre-merge step.
- [ ] A headless generation harness takes a seed and runs at least 1,000 seeds per location.
- [ ] The harness asserts connectivity, extraction reachability from every room, no overlapping footprints, room count tolerance, exit counts, and loot point counts.
- [ ] The harness asserts a valid navigation path from every emergence point to the extraction zone.
- [ ] The harness reports loot total value, count, distribution by distance band, and unreachable item count per location.
- [ ] No location's worst-case seed fails to cover travel cost; no best-case seed clears a full quota in one trip.
- [ ] The quota curve is tested for monotonicity, overflow, and achievability.
- [ ] Settlement arithmetic is tested across a matrix of payouts, deaths, recoveries, disconnects, and quota excess.
- [ ] A player who dies and then disconnects is covered explicitly and charged exactly one penalty.
- [ ] Simultaneous purchase requests against an insufficient balance yield exactly one success.
- [ ] The currency invariant — balance equals starting balance plus logged transactions — is asserted.
- [ ] Targeting selection is tested per archetype across a matrix of target properties.
- [ ] Adding a random draw in one seeded system does not change any other system's output for the same seed.
- [ ] Generation code is verified to use no `UnityEngine.Random`, `System.Random`, wall-clock time, or unordered-collection iteration.
- [ ] The same seed produces identical output across separate runs.
- [ ] The full suite runs in seconds and contains no flaky tests.
- [ ] Every bug found by hand in a covered system results in a new test case.
