# 96 — Editor vs Build Test Paths

**Source:** [`core_components.md`](../core_components.md) §12 — Build & Release Readiness
**Status:** ⚠️ Editor tooling exists and is being trusted for more than it can prove
**Depends on:** [Debug & Cheat Tooling](88_debug_and_cheat_tooling.md)
**Blocks:** knowing whether a change actually works

## Summary

The gap between "it works in the Editor" and "it works".

`core_components.md` states the problem in one line: Multiplayer Play Mode and thin clients are `#if UNITY_EDITOR` only, so **Editor multiplayer testing does not prove a build works.** Every networking change needs verification with two real builds, or a build against an Editor host.

This is not a component that produces code so much as one that produces a **testing discipline**, and it earns a plan because the discipline is what makes several other components' acceptance criteria meaningful. A dozen plans in this project carry criteria of the form "verified under simulated latency" or "works in a standalone build", and without an agreed way to run those, they are aspirations.

The gap is real and specific. The Editor is a single process, a single revision, a single asset database, and a single set of loaded content. Four whole classes of failure are **invisible** there by construction:

- **Addressables** — the Editor resolves references from the asset database, never from packed content ([`93_addressables_content_build.md`](93_addressables_content_build.md)).
- **Subscene registration** — the Editor loads subscenes regardless of build profile membership ([`94_entity_subscene_baking.md`](94_entity_subscene_baking.md)).
- **Build parity** — one revision cannot mismatch itself ([`95_client_server_build_parity.md`](95_client_server_build_parity.md)).
- **UGS services** — a service failure may not surface in the same way, and the dedicated-server path is a different profile entirely ([`90_relay_and_lobby_service_enablement.md`](90_relay_and_lobby_service_enablement.md)).

## How to Build

**Define the three test tiers and what each one proves**

Naming the tiers is most of the value, because it stops "I tested it" from being ambiguous:

- **Tier 1 — Editor Multiplayer Play Mode.** Fast, iterative, run constantly. Proves gameplay logic, prediction behaviour, and server/client role splits. Proves **nothing** about packaging.
- **Tier 2 — standalone build against an Editor host.** The cheap middle ground. Catches Addressables failures, missing subscenes, and content mismatches, because the build side is genuinely packaged. Fast enough to run on every meaningful change.
- **Tier 3 — two standalone builds, different machines, different networks.** The only tier that proves the shipped game works. Catches Relay-specific behaviour, real latency, and dedicated-server issues. Slow; run at milestones and before every playtest.

Record which tier each acceptance criterion needs. A criterion saying "works in a build" means Tier 2 at minimum, and several — [`90_relay_and_lobby_service_enablement.md`](90_relay_and_lobby_service_enablement.md)'s different-networks requirement, [`95_client_server_build_parity.md`](95_client_server_build_parity.md)'s mismatch rejection — need Tier 3.

**Make Tier 2 cheap enough to actually happen**

- The determining factor is build time. A ten-minute build means Tier 2 gets skipped; a two-minute one means it gets used.
- Keep a fast build configuration — development build, no compression, minimal profile — distinct from the shipping one, and make it one click or one command.
- Automate the Addressables content build into it ([`93_addressables_content_build.md`](93_addressables_content_build.md) requires this anyway), so the fast path still exercises packed content. A fast build that skips content build proves nothing.

**Use the network simulator deliberately**

- `EntityDriverConstructor` exposes a **network simulator** alongside the Relay parameters, and it is what makes "verify under simulated latency" a realistic criterion rather than a wish. A dozen plans depend on it — sprint, stamina, crouch, climbing, interaction authority, carry weight, doors, and the shared-state hash check among them.
- Define **standard profiles** — a good connection, a poor one, and a lossy one — with fixed parameters, so "tested under latency" means the same thing to everyone and results are comparable across changes.
- Latency testing belongs in Tier 1, where it is cheap. That is the one thing the Editor does better than a build, since the simulator is trivially configurable there.

**Do not let thin clients imply more than they prove**

- Thin clients are useful for load and bandwidth shape, and they do not run full presentation. A round that works with four thin clients has not proven that four real clients hold the frame budget ([`99_performance_budget.md`](99_performance_budget.md)).
- Use them for what they are good at — snapshot size and server cost under player count ([`100_network_bandwidth_budget.md`](100_network_bandwidth_budget.md)) — and do not substitute them for real clients when the question is about the client's experience.

**Make the tiers reachable, not just defined**

- The scenario launcher in [`88_debug_and_cheat_tooling.md`](88_debug_and_cheat_tooling.md) — start directly into a location with a given seed, day, quota, and loadout — is what makes Tier 2 and Tier 3 practical. Reaching day four honestly to test a bug takes an hour and nobody will do it twice.
- Debug commands must work **in a build**, which [`88_debug_and_cheat_tooling.md`](88_debug_and_cheat_tooling.md) already requires and which this component is the main consumer of. An Editor-only console makes Tiers 2 and 3 blind.
- Seeds make a Tier 3 finding reproducible in Tier 1 ([`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md)), which is how a slow-tier bug gets fixed on the fast tier.

**Write the pre-merge expectation down**

- Networking or serialisation change → Tier 2 minimum.
- Content, prefab, or subscene change → Tier 2 minimum.
- Anything else → Tier 1.
- Milestone or playtest → Tier 3, plus the full build verification pass ([`97_build_verification_pass.md`](97_build_verification_pass.md)).

A rule this simple is followed; a longer checklist is not.

## Acceptance Criteria

- [ ] Three test tiers are defined, documented, and named consistently across the plans.
- [ ] Each tier's coverage and blind spots are recorded, including the four failure classes invisible in the Editor.
- [ ] Every acceptance criterion elsewhere that requires a build states which tier satisfies it.
- [ ] A fast development build configuration exists and completes quickly enough to be run routinely.
- [ ] The fast build includes an Addressables content build.
- [ ] Standard network simulator profiles — good, poor, lossy — are defined with fixed parameters and used for all latency verification.
- [ ] Latency testing is routinely performed in Tier 1.
- [ ] Thin clients are used for bandwidth and server-load measurement only, never as a substitute for real clients in frame-budget testing.
- [ ] Debug commands and the scenario launcher work in a standalone build.
- [ ] A Tier 3 finding can be reproduced in Tier 1 from its seed.
- [ ] The pre-merge tier expectation is documented and short enough to be followed.
- [ ] Tier 2 is run against every networking, serialisation, content, prefab, or subscene change.
- [ ] Tier 3 is run before every playtest and at every milestone.
