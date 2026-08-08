# 97 — Build Verification Pass

**Source:** [`core_components.md`](../core_components.md) §12 — Build & Release Readiness
**Status:** ❌ No evidence in the repo of a client-vs-client build ever having been run
**Depends on:** [Addressables Content Build](93_addressables_content_build.md), [Entity Subscene Baking](94_entity_subscene_baking.md), [Client/Server Build Parity](95_client_server_build_parity.md), [Editor vs Build Test Paths](96_editor_vs_build_test_paths.md)
**Blocks:** knowing whether the game works at all outside the Editor

## Summary

Actually running two builds against each other, on purpose, and writing down what happened.

`core_components.md` says to **do this early, before the codebase grows**, and gives the reason: the failure modes it catches — Addressables, subscenes, UGS — are *"all cheap to fix now and expensive to diagnose later."* That asymmetry is the entire argument. A missing subscene found today is a one-line build-settings change. The same missing subscene found in six months, after twenty systems have been built assuming those entities exist, is a day of confused debugging by someone who was not there when it broke.

The status is the striking part: there is **no evidence in the repository that a client-vs-client build has ever been run.** Everything that works, works in the Editor, and §12 is explicit that this proves nothing about a shipped game.

This is not a system. It is a **checklist that gets executed and recorded**, and its output is a document saying what was tested, on what builds, with what result.

## How to Build

**Run the first pass now, against what exists**

Do not wait for the game to be finished. The current build is a working networked FPS shell, which is enough to exercise every packaging failure mode. The first pass should answer:

- Does a standalone client build launch, connect to another standalone build, and play?
- Do ghost prefabs, projectiles, and player prefabs resolve — or does packed Addressables content come up null ([`93_addressables_content_build.md`](93_addressables_content_build.md))?
- Do `GameResourcesSubScene` and `SpawnPointsSubScene` produce populated entity worlds in a build ([`94_entity_subscene_baking.md`](94_entity_subscene_baking.md))?
- Does the Relay path work between two machines on **different networks** ([`90_relay_and_lobby_service_enablement.md`](90_relay_and_lobby_service_enablement.md))?
- Does the `FPS2 Windows Server` dedicated build boot, listen, and accept a client?
- Does the Android client build run and connect?

Any of those failing today is a small fix. All of them are currently unknown.

**Define the standing checklist**

The pass should be repeatable, so write it once and re-run it at milestones. Grouped by what it proves:

- **Packaging** — every ghost prefab spawns (the smoke test [`93_addressables_content_build.md`](93_addressables_content_build.md) requires); every subscene populates; every build profile's scene list is complete.
- **Connection** — Relay create-or-join, join by code ([`91_join_by_code.md`](91_join_by_code.md)), direct connect, and dedicated server, each verified from a build.
- **Parity** — a deliberately mismatched pair is refused with the right message; a content-only mismatch is likewise refused ([`95_client_server_build_parity.md`](95_client_server_build_parity.md) requires both to be part of this pass).
- **Lifecycle** — a full round completes; two consecutive rounds run with no leaked entities or memory ([`05_location_load_unload_flow.md`](05_location_load_unload_flow.md)); a disconnect and reconnect behave ([`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md), [`25_reconnection.md`](25_reconnection.md)).
- **Failure paths** — service unavailable, wrong code, full session, host departure. Each must produce its specific message rather than a hang; these are the paths nobody tests because they require deliberately breaking something.
- **Platform** — every configured build profile launches and plays: `Windows Client`, `Android Client`, `FPS2 Windows Server`.

**Record the result, not just the outcome**

- Write down the date, the commit hash, the build profiles used, the network topology, and the result per checklist item. A pass that leaves no artefact cannot be compared against the next one.
- Record the **version stamp** ([`95_client_server_build_parity.md`](95_client_server_build_parity.md)) so a later regression can be bisected against a known-good build.
- Record round seeds for anything gameplay-related, since a seed makes a Tier 3 finding reproducible in Tier 1 ([`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md), [`96_editor_vs_build_test_paths.md`](96_editor_vs_build_test_paths.md)).
- Keep the artefacts in the repository next to these plans, so the history of what worked when is versioned with the code that produced it.

**Automate the parts that can be automated**

- The **smoke test** — spawn one of every registered ghost prefab and assert none resolve to null — is pure logic and can run headlessly in a built player. That single test catches most Addressables and registry failures without a human.
- The **build-time validations** from [`93_addressables_content_build.md`](93_addressables_content_build.md) and [`94_entity_subscene_baking.md`](94_entity_subscene_baking.md) fail the build rather than the verification pass, which is strictly better — they move the failure earlier.
- What cannot be automated is the two-machine, two-network Relay test, and that is fine. Keep the manual checklist short enough that its manual nature is not the reason it gets skipped.

**Tie it to a cadence**

- Before every playtest, without exception. A playtest that fails on packaging wastes everyone's evening and produces no design data.
- At every milestone.
- After any change to build profiles, Addressables groups, subscenes, or the connection layer.
- The rule from [`96_editor_vs_build_test_paths.md`](96_editor_vs_build_test_paths.md) covers the day-to-day; this pass is the periodic full sweep.

**Expect the first pass to fail, and treat that as the point**

- Something will be wrong. Addressables content will not have been built, or a subscene will be missing from a profile, or Relay will not be enabled on the dashboard.
- That is the value being delivered — finding those now, cheaply, rather than during a playtest. The pass has done its job when it fails and the fixes are one-liners.

## Acceptance Criteria

- [ ] A first verification pass has been run against the current build and its results recorded.
- [ ] Two standalone client builds connect and play a full session together.
- [ ] Packed Addressables content resolves in a build; a smoke test spawns every registered ghost prefab with no nulls.
- [ ] Every subscene produces a populated entity world in a build.
- [ ] Relay connection succeeds between two machines on different networks.
- [ ] Join by code, direct connect, and the dedicated-server path each work from a build.
- [ ] The `FPS2 Windows Server` build boots, listens, and accepts a client.
- [ ] The `Android Client` build launches and connects.
- [ ] A deliberately mismatched build pair is refused with the correct message.
- [ ] Two consecutive rounds complete with no leaked entities or memory.
- [ ] A disconnect and reconnect behave per their plans.
- [ ] Every defined connection-failure path produces its specific message rather than a hang.
- [ ] The checklist is written down and repeatable.
- [ ] Each pass records date, commit hash, version stamp, build profiles, topology, seeds, and per-item results.
- [ ] Pass artefacts are committed to the repository alongside the component plans.
- [ ] The ghost-prefab smoke test runs headlessly in a built player.
- [ ] The pass is run before every playtest, at every milestone, and after any build-configuration change.
