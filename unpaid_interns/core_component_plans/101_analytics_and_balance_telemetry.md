# 101 — Analytics / Balance Telemetry

**Source:** [`core_components.md`](../core_components.md) §13 — Onboarding, Performance & Long Tail
**Status:** ❌ Not started
**Depends on:** [Performance Report](70_performance_report.md), [Deterministic Generation Seed](29_deterministic_generation_seed.md)
**Blocks:** balancing the game with evidence rather than opinion

## Summary

Measuring what actually happens, because this game is mostly balance work.

`core_components.md` gives both the argument and the tools: quota success rates, average haul per location, death causes, and round durations, with the observation that **balancing without data is guesswork** and that `com.unity.services.analytics` integrates with the UGS project already linked.

The argument is stronger here than in most projects. Nearly every component plan in this repository ends with numbers someone has to choose — the quota curve, the spawn budget curve, loot density per location, monster power costs, penalty percentages, sell rates. Those are all judgement calls made against an imagined player, and the imagined player is always better at the game than the real one and always makes different mistakes. [`64_quota_system.md`](64_quota_system.md) already asks for the curve to be modelled before implementation; telemetry is how the model gets corrected.

There is also a specific reason it is worth more here than usual: **the game is already instrumented.** Most of what needs measuring is computed anyway. The performance report grades every round, the loot spawner knows the map's total value, settlement itemises every credit, and the seed makes any round reproducible. Telemetry is largely a matter of *sending* numbers the game already has.

## How to Build

**Measure the decisions, not the events**

The temptation is to log everything and sort it out later, which produces volume and no answers. Instrument the questions the balance work actually asks:

- **Is the quota curve right?** Quota success rate per cycle number, and the cycle at which runs typically end ([`64_quota_system.md`](64_quota_system.md)). If most runs die at cycle three regardless of skill, the curve is too steep — the failure that reads as the game being broken rather than hard.
- **Are the locations differentiated?** Haul per location, round duration, and death rate per location. [`26_location_catalogue.md`](26_location_catalogue.md) warns that a high loot ceiling with a low monster budget is a free-money exploit that will be found in one session; this is how it is found first internally.
- **Is the risk gradient working?** Value banked versus time spent, and how often crews make a second trip. If nobody ever goes back in, the carry limit and the escalation curve are not producing the intended decision.
- **Are monsters differentiated?** Encounter survival rate per monster, time-to-escape, and how often the intended counterplay was used ([`58_monster_variety_set.md`](58_monster_variety_set.md) requires exactly this instrumentation). A monster whose counterplay is never used has not been taught.
- **What kills people?** Death cause, which [`57_attack_and_damage_application.md`](57_attack_and_damage_application.md) already records and carries to the body ghost and the summary. Deaths to falls or hazards vastly outnumbering monster deaths means the threat layer is not the threat.
- **Does onboarding work?** First-run quota success and first-run unbanked value at round end ([`98_tutorial_and_onboarding.md`](98_tutorial_and_onboarding.md) requires both).

**Always send the seed**

- [`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md) makes any round reproducible from its seed, and that turns an aggregate outlier into an investigation. A round with an anomalously low haul is a curiosity; the same round with its seed is a generator bug you can load.
- Send the seed with every round-level event, alongside the location id, day number, quota cycle, and crew size.
- Also send the **version stamp** ([`95_client_server_build_parity.md`](95_client_server_build_parity.md)), so data from different builds can be separated. Mixing pre- and post-tuning data is how a balance change gets evaluated against itself.

**Send from the host, once per round**

- The host owns the authoritative state ([`86_savesystem_integration.md`](86_savesystem_integration.md)), so it is the only machine that can report a round correctly. Four clients each sending their view produces four inconsistent records of one round.
- Send at settlement, when every number is final — the same moment [`70_performance_report.md`](70_performance_report.md) computes the grade. Most of the payload is what that report already assembled.
- Batch and send asynchronously. A telemetry call that hitches the host during settlement is worse than no telemetry.
- Never let a telemetry failure affect gameplay. Fire and forget, fail silently in release, log in development.

**Respect the player**

- Analytics needs consent where the platform requires it, and an opt-out regardless. Put it in the settings menu ([`78_settings_options_menu.md`](78_settings_options_menu.md)).
- Send **gameplay** data, not identity. Aggregate round outcomes, not who played with whom. The stable player id from [`19_crew_roster.md`](19_crew_roster.md) exists for rejoin matching, not for tracking.
- Note that `UnityConnectSettings.asset` currently has `m_Enabled: 0`. [`90_relay_and_lobby_service_enablement.md`](90_relay_and_lobby_service_enablement.md) is explicit that this flag governs legacy Analytics and is **not** relevant to UGS Relay linkage — it becomes relevant here, and enabling it is a deliberate decision belonging to this component, not something to flip while doing service setup.

**Start with the local version**

- Full cloud analytics is a service integration with consent, dashboards, and a schema. Before that, **write the same records to a local file** on the host.
- That covers internal playtesting entirely, which is where most balance data will come from anyway, and it costs almost nothing. The debug tooling's transaction log and spawn decision log ([`88_debug_and_cheat_tooling.md`](88_debug_and_cheat_tooling.md), [`63_currency_system.md`](63_currency_system.md), [`50_spawn_director.md`](50_spawn_director.md)) are already most of it.
- Design the record schema once and use the same shape for both sinks, so switching from local to cloud is a transport change.
- Pair it with the headless harnesses ([`89_automated_tests.md`](89_automated_tests.md)) — those produce simulated data about generation and loot distribution, and telemetry produces observed data about what players do with it. Together they answer "is the map generous enough" and "did anyone find it".

## Acceptance Criteria

- [ ] A round-level record is defined covering quota cycle, success, location, duration, haul, deaths and causes, and crew size.
- [ ] Quota success rate per cycle is measurable, and the cycle at which runs typically end is reported.
- [ ] Haul, duration, and death rate are reported per location and differentiate destinations.
- [ ] Second-trip frequency and value-banked-versus-time are measurable.
- [ ] Per-monster encounter survival, time-to-escape, and counterplay usage are recorded.
- [ ] Death cause is recorded for every death.
- [ ] First-run quota success and first-run unbanked value are recorded.
- [ ] Every round record includes the round seed, location id, day, cycle, crew size, and version stamp.
- [ ] Records are sent by the host only, once per round, at settlement.
- [ ] Sending is asynchronous and produces no frame hitch.
- [ ] A telemetry failure never affects gameplay and fails silently in release.
- [ ] An opt-out exists in the settings menu, and platform-required consent is handled.
- [ ] No identity data is sent; records are gameplay-only.
- [ ] Enabling legacy analytics is a deliberate decision recorded here and not a side effect of service setup.
- [ ] A local file sink produces the same records for internal playtesting before any cloud integration.
- [ ] Local and cloud sinks share one record schema.
- [ ] At least one balance change has been made on the basis of collected data rather than opinion.
