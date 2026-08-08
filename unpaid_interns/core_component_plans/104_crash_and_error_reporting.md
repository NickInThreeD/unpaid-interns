# 104 — Crash / Error Reporting

**Source:** [`core_components.md`](../core_components.md) §13 — Onboarding, Performance & Long Tail
**Status:** ❌ Not started — Cloud Diagnostics is available through the linked UGS project and is disabled
**Depends on:** [Deterministic Generation Seed](29_deterministic_generation_seed.md), [Build Versioning & Mismatch Rejection](103_build_versioning_and_mismatch_rejection.md)
**Blocks:** fixing the bugs players actually hit

## Summary

Finding out what broke, on a machine you do not have, in a round you did not play.

`core_components.md` gives the reason directly: **procedural generation and networked state produce bugs that are hard to reproduce from a verbal report.** That is the defining characteristic of this project's bug population. A player saying "I fell through the floor" or "the exit was walled off" or "my items disappeared" is describing an outcome, not a cause, and without machine-side context there is nothing to act on.

What makes this component unusually cheap here is that **the hardest part is already solved.** [`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md) makes any round reproducible from a single value, and calls this *"the cheapest mitigation available"* for exactly the two bug classes §13 names. A crash report carrying a seed is not a report — it is a repro.

Cloud Diagnostics is available through the already-linked UGS project (`cloudProjectId: bc8406a5-fddf-4bb6-b45f-ac19f6f0df6e`) and is currently disabled.

## How to Build

**Attach the context that makes a report actionable**

A stack trace alone is worth much less here than in a single-player game. Every report should carry:

- **The round seed**, and the run seed it derived from ([`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md)). This is the single most valuable field.
- **The version stamp** ([`103_build_versioning_and_mismatch_rejection.md`](103_build_versioning_and_mismatch_rejection.md)) — code revision, content catalogue, subscenes, registries — so a report is attributed to a build rather than to the game in general.
- **Role and topology**: host, client, or dedicated server; Relay or direct connect. A bug that only occurs on the host is a different bug, and role is what separates the doubled-collider class of failure ([`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md)) from everything else.
- **Round state**: location id, day, quota cycle, round phase, crew size, and normalized time. A crash at 0.9 normalized time with a full spawn budget is a different investigation from one at deploy.
- Keep it to fields the game already has. Everything above is either replicated shared state ([`23_shared_session_state_sync.md`](23_shared_session_state_sync.md)) or a build constant.

**Report errors, not only crashes**

- A hard crash is the rarest failure this project will produce. The common ones are **exceptions that do not kill the process** — a null Addressable reference, a missing subscene entity, a ghost that failed to link — and those currently produce a log line nobody sees.
- Capture unhandled exceptions and error-level logs, deduplicated by stack signature, and report them with the same context.
- Report the **invariant violations** too. Several plans specify development-mode assertions — shared-state hash mismatch ([`23_shared_session_state_sync.md`](23_shared_session_state_sync.md)), layout hash mismatch ([`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md)), credits not matching the transaction log ([`63_currency_system.md`](63_currency_system.md)), inventory and item-ghost disagreement ([`40_inventory_item_bar.md`](40_inventory_item_bar.md)). [`88_debug_and_cheat_tooling.md`](88_debug_and_cheat_tooling.md) routes them into one reported surface; that surface should also feed this one. **A desync detected in the wild is more valuable than a crash**, because it is the class of bug that otherwise never gets reported at all.

**Give players a way to report what did not throw**

- The worst bugs here produce no exception. "The exit was walled off" is a generator failure that raises nothing, and it is precisely the case the seed makes tractable.
- Add an in-game report action that captures the current context and the seed. [`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md) already requires the seed to be visible to testers *without reading a log file*, on the loading screen or the end-of-round summary — this is the button next to it.
- A free-text field plus automatic context beats either alone. The player describes the outcome; the game supplies the reproduction.

**Enable Cloud Diagnostics deliberately**

- It is available through the linked project and disabled. Note that `UnityConnectSettings.asset` has `m_Enabled: 0`, which [`90_relay_and_lobby_service_enablement.md`](90_relay_and_lobby_service_enablement.md) is explicit **does not** affect Relay linkage — enabling it is this component's decision and [`101_analytics_and_balance_telemetry.md`](101_analytics_and_balance_telemetry.md)'s, not something to flip during service setup.
- Verify per-platform support against the configured build profiles, including `Android Client` and `FPS2 Windows Server`. A dedicated server that crashes silently is the worst version of this problem, because nobody is looking at it.
- Check symbol upload so stack traces are readable rather than addresses.

**Start local, as with telemetry**

- Before any cloud integration, write the same records to a **local file on the host**, with rotation. That covers internal playtesting entirely, which is where most bugs will be found anyway.
- [`101_analytics_and_balance_telemetry.md`](101_analytics_and_balance_telemetry.md) takes the same approach for the same reason and defines one record schema across both sinks. Share the context-gathering code between them — the fields overlap almost entirely.
- The debug tooling's existing logs — server-side item lifecycle ([`38_item_ghost_networked_item_state.md`](38_item_ghost_networked_item_state.md)), spawn decisions ([`50_spawn_director.md`](50_spawn_director.md)), interaction grants and rejections ([`20_networked_interaction_authority.md`](20_networked_interaction_authority.md)) — are the detail a report should be able to reference. Keep a rolling buffer of recent log lines and attach it.

**Respect the player and the budget**

- Consent where the platform requires it, and an opt-out in the settings menu ([`78_settings_options_menu.md`](78_settings_options_menu.md)), consistent with telemetry.
- No identity data. Seeds, versions, and state — not who was playing.
- Reporting must never affect gameplay: asynchronous, rate-limited, deduplicated, and silently failing in release. A crash reporter that hitches the host during a chase has caused a worse problem than it solves.

## Acceptance Criteria

- [ ] Crash and unhandled-exception reporting is enabled and verified on every configured build profile, including Android and the dedicated server.
- [ ] Every report carries the round seed and run seed.
- [ ] Every report carries the version stamp, role, topology, location, day, quota cycle, round phase, crew size, and normalized time.
- [ ] Non-fatal errors and error-level logs are captured and deduplicated by stack signature.
- [ ] Development invariant violations — shared-state hash, layout hash, currency, inventory consistency — are reported through the same channel.
- [ ] An in-game player-initiated report captures full context plus free text.
- [ ] The round seed is visible to a player without opening a log file.
- [ ] A rolling buffer of recent server-side log lines is attached to reports.
- [ ] Cloud Diagnostics enablement is a deliberate decision recorded here, separate from Relay setup.
- [ ] Symbols are uploaded and stack traces are readable rather than raw addresses.
- [ ] A local file sink with rotation produces the same records before any cloud integration.
- [ ] Context gathering is shared with telemetry rather than duplicated.
- [ ] Consent and an opt-out exist in the settings menu.
- [ ] No identity data is transmitted.
- [ ] Reporting is asynchronous, rate-limited, and never causes a frame hitch or affects gameplay.
- [ ] A reported bug has been reproduced from its seed and version stamp alone, without further information from the reporter.
