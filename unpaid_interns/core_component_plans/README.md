# Component Plans — Index

One plan per component in [`core_components.md`](../core_components.md). Every component not already marked ✅ there has a plan here; the ✅ items (player controller, networking layer, prediction, ECS/GhostBridge, Addressables, object pooling, spatial audio, main menu) are working today and need none.

**[MVP]** marks the minimum set for a first playable loop. Build order guidance is in [`core_components.md`](../core_components.md) §15.

## §1. Game Loop & Session State

| # | Component | Status | MVP |
| --- | --- | --- | --- |
| 01 | [Run Manager](01_run_manager.md) | ❌ Not started | ✓ |
| 02 | [Day Cycle Controller](02_day_cycle_controller.md) | ❌ Not started | ✓ |
| 03 | [Round Timer / Clock](03_round_timer_clock.md) | ❌ Not started | ✓ |
| 04 | [Hub / Between-Rounds State](04_hub_between_rounds_state.md) | ❌ Not started | ✓ |
| 05 | [Location Load / Unload Flow](05_location_load_unload_flow.md) | ❌ Not started | ✓ |
| 06 | [Session Persistence](06_session_persistence.md) | ❌ Not started |  |
| 07 | [Game Over / Win Resolution](07_game_over_win_resolution.md) | ❌ Not started | ✓ |
| 08 | [Late Join / Rejoin Policy](08_late_join_rejoin_policy.md) | ⚠️ Partial — plumbing exists, policy does not | ✓ |

## §2. Player Character

| # | Component | Status | MVP |
| --- | --- | --- | --- |
| 09 | [Sprint](09_sprint.md) | ⚠️ Constants exist, never applied | ✓ |
| 10 | [Crouch](10_crouch.md) | ❌ Not started | ✓ |
| 11 | [Stamina System](11_stamina.md) | ❌ Not started | ✓ |
| 12 | [Carry Weight](12_carry_weight.md) | ❌ Not started | ✓ |
| 13 | [Health & Injury System](13_health_and_injury.md) | ⚠️ Health exists, injury layer does not | ✓ |
| 14 | [Death & Body System](14_death_and_body_system.md) | ⚠️ Auto-respawn exists and must be replaced | ✓ |
| 15 | [Fear / Stress Feedback](15_fear_and_stress_feedback.md) | ⚠️ Damage vignette exists and can be extended |  |
| 16 | [Player Scanner / Ping Tool](16_player_scanner_ping_tool.md) | ❌ Not started |  |
| 17 | [Climbing & Verticality](17_climbing_and_verticality.md) | ❌ Not started |  |
| 18 | [Player-vs-Player Collision & Friendly Fire Policy](18_pvp_collision_and_friendly_fire.md) | ❌ No stated rule — but the current code has an accidental one |  |

## §3. Multiplayer & Team

| # | Component | Status | MVP |
| --- | --- | --- | --- |
| 19 | [Crew Roster](19_crew_roster.md) | ⚠️ Connection tracking exists, crew state does not | ✓ |
| 20 | [Networked Interaction Authority](20_networked_interaction_authority.md) | ❌ Not started | ✓ |
| 21 | [Proximity Voice / Comms](21_proximity_voice_comms.md) | ❌ Not started — no voice package is installed |  |
| 22 | [Spectator Mode](22_spectator_mode.md) | ⚠️ Respawn screen exists, spectator camera does not | ✓ |
| 23 | [Shared Session State Sync](23_shared_session_state_sync.md) | ❌ Not started | ✓ |
| 24 | [Mid-Round Disconnect Handling](24_mid_round_disconnect_handling.md) | ⚠️ Cleanup exists, gameplay semantics do not | ✓ |
| 25 | [Reconnection](25_reconnection.md) | ❌ Not started — Netcode for Entities provides nothing here |  |

## §4. Location & World Generation

| # | Component | Status | MVP |
| --- | --- | --- | --- |
| 26 | [Location Catalogue](26_location_catalogue.md) | ❌ Not started | ✓ |
| 27 | [Location Selection / Assignment](27_location_selection_assignment.md) | ❌ Not started | ✓ |
| 28 | [Procedural Interior Generator](28_procedural_interior_generator.md) | ❌ Not started | ✓ |
| 29 | [Deterministic Generation Seed](29_deterministic_generation_seed.md) | ❌ Not started | ✓ |
| 30 | [Runtime NavMesh Baking](30_runtime_navmesh_baking.md) | ❌ Not started | ✓ |
| 31 | [Entry Point / Extraction Zone](31_entry_point_extraction_zone.md) | ⚠️ Static spawn points exist; the zone does not | ✓ |
| 32 | [Alternate Exits](32_alternate_exits.md) | ❌ Not started |  |
| 33 | [Exterior / Approach Area](33_exterior_approach_area.md) | ❌ Not started |  |
| 34 | [Out-of-Bounds Handling](34_out_of_bounds_handling.md) | ❌ Not started |  |
| 35 | [Environmental Conditions / Weather](35_environmental_conditions_weather.md) | ❌ Not started |  |
| 36 | [Lighting & Power Grid](36_lighting_and_power_grid.md) | ⚠️ Static lighting configured; nothing dynamic or networked · **[MVP-adjacent]** |  |

## §5. Items, Loot & Inventory

| # | Component | Status | MVP |
| --- | --- | --- | --- |
| 37 | [Item Definition / Data Model](37_item_definition_data_model.md) | ❌ Not started | ✓ |
| 38 | [Item Ghost / Networked Item State](38_item_ghost_networked_item_state.md) | ❌ Not started | ✓ |
| 39 | [Loot Spawner](39_loot_spawner.md) | ❌ Not started | ✓ |
| 40 | [Inventory / Item Bar](40_inventory_item_bar.md) | ❌ Not started | ✓ |
| 41 | [Interaction System](41_interaction_system.md) | ❌ Not started | ✓ |
| 42 | [Two-Handed Item Rule](42_two_handed_item_rule.md) | ❌ Not started |  |
| 43 | [Loot Banking / Deposit](43_loot_banking_deposit.md) | ❌ Not started | ✓ |
| 44 | [Tool & Equipment Items](44_tool_and_equipment_items.md) | ❌ Not started |  |
| 45 | [Weapons as Tools](45_weapons_as_tools.md) | ⚠️ A complete predicted weapon stack exists and is wired the wrong way round |  |
| 46 | [Storage / Hub Inventory](46_storage_hub_inventory.md) | ❌ Not started |  |
| 47 | [Physics Props & Throwing](47_physics_props_and_throwing.md) | ❌ Not started |  |

## §6. Monsters & AI

| # | Component | Status | MVP |
| --- | --- | --- | --- |
| 48 | [Monster Data Definitions](48_monster_data_definitions.md) | ❌ Not started | ✓ |
| 49 | [Monster Ghost & Replication](49_monster_ghost_and_replication.md) | ❌ Not started | ✓ |
| 50 | [Spawn Director](50_spawn_director.md) | ❌ Not started | ✓ |
| 51 | [Difficulty Escalation](51_difficulty_escalation.md) | ❌ Not started | ✓ |
| 52 | [Spawn Points / Vents](52_spawn_points_and_vents.md) | ❌ Not started |  |
| 53 | [Perception System](53_perception_system.md) | ❌ Not started | ✓ |
| 54 | [Noise Emission System](54_noise_emission_system.md) | ❌ Not started | ✓ |
| 55 | [Chase & Pathfinding](55_chase_and_pathfinding.md) | ❌ Not started | ✓ |
| 56 | [Threat / Interest Targeting](56_threat_interest_targeting.md) | ❌ Not started |  |
| 57 | [Attack & Damage Application](57_attack_and_damage_application.md) | ⚠️ A player-damage path exists; monster→player and player→monster do not | ✓ |
| 58 | [Monster Variety Set](58_monster_variety_set.md) | ❌ Not started | ✓ |

## §7. Hazards & Environment Interaction

| # | Component | Status | MVP |
| --- | --- | --- | --- |
| 59 | [Static Map Hazards](59_static_map_hazards.md) | ❌ Not started |  |
| 60 | [Door System](60_door_system.md) | ❌ Not started | ✓ |
| 61 | [Fall & Environmental Damage](61_fall_and_environmental_damage.md) | ❌ Not started | ✓ |
| 62 | [Hazard Control / Remote Disable](62_hazard_control_remote_disable.md) | ❌ Not started |  |

## §8. Economy & Progression

| # | Component | Status | MVP |
| --- | --- | --- | --- |
| 63 | [Currency System](63_currency_system.md) | ❌ Not started | ✓ |
| 64 | [Quota System](64_quota_system.md) | ❌ Not started | ✓ |
| 65 | [Selling / Payout](65_selling_payout.md) | ❌ Not started | ✓ |
| 66 | [Bonus & Penalty Rules](66_bonus_and_penalty_rules.md) | ❌ Not started |  |
| 67 | [Store / Purchasing](67_store_purchasing.md) | ❌ Not started | ✓ |
| 68 | [Upgrades](68_upgrades.md) | ❌ Not started |  |
| 69 | [Rank / Progression](69_rank_and_progression.md) | ❌ Not started — explicitly safe to defer |  |
| 70 | [Performance Report](70_performance_report.md) | ⚠️ Working replicated-scoreboard plumbing exists with the wrong semantics |  |

## §9. UI & Feedback

| # | Component | Status | MVP |
| --- | --- | --- | --- |
| 71 | [HUD](71_hud.md) | ⚠️ Exists, built for a shooter | ✓ |
| 72 | [Quota & Deadline Display](72_quota_and_deadline_display.md) | ❌ Not started | ✓ |
| 73 | [Interaction Prompts](73_interaction_prompts.md) | ❌ Not started | ✓ |
| 74 | [Terminal / Hub Interface](74_terminal_hub_interface.md) | ❌ Not started | ✓ |
| 75 | [Monitoring / Camera System](75_monitoring_camera_system.md) | ❌ Not started |  |
| 76 | [End-of-Round Summary](76_end_of_round_summary.md) | ❌ Not started | ✓ |
| 77 | [Action Feed](77_action_feed.md) | ⚠️ Works, announces the wrong things |  |
| 78 | [Settings / Options Menu](78_settings_options_menu.md) | ❌ Not started — **there is no options screen of any kind** | ✓ |
| 79 | [Accessibility](79_accessibility.md) | ❌ Not started | ✓ |
| 80 | [Teammate Identification](80_teammate_identification.md) | ❌ Not started |  |
| 81 | [Pause Semantics in Multiplayer](81_pause_semantics_in_multiplayer.md) | ⚠️ `PauseMenu` exists and has never been tested against a live session |  |

## §10. Audio

| # | Component | Status | MVP |
| --- | --- | --- | --- |
| 82 | [Monster Audio Cues](82_monster_audio_cues.md) | ❌ Not started | ✓ |
| 83 | [Ambience & Time Cues](83_ambience_and_time_cues.md) | ❌ Not started |  |
| 84 | [Player Audio](84_player_audio.md) | ⚠️ Footsteps exist, are local-only and context-blind | ✓ |

## §11. Technical Foundations

| # | Component | Status | MVP |
| --- | --- | --- | --- |
| 85 | [EventBus Integration](85_eventbus_integration.md) | ❌ Not present in this project | ✓ |
| 86 | [SaveSystem Integration](86_savesystem_integration.md) | ❌ Not present in this project |  |
| 87 | [Data-Driven Configuration](87_data_driven_configuration.md) | ⚠️ The pattern exists and carries a defect that must not be copied | ✓ |
| 88 | [Debug & Cheat Tooling](88_debug_and_cheat_tooling.md) | ⚠️ `ConfigVar` and Play Mode support exist; no gameplay commands do | ✓ |
| 89 | [Automated Tests](89_automated_tests.md) | ❌ `com.unity.test-framework` 1.6.0 is installed; there is not one test in the project |  |

## §12. Build & Release Readiness

| # | Component | Status | MVP |
| --- | --- | --- | --- |
| 90 | [Relay & Lobby Service Enablement](90_relay_and_lobby_service_enablement.md) | ❌ The one remaining setup step, and it cannot be done from the codebase |  |
| 91 | [Join by Code](91_join_by_code.md) | ❌ Implemented but unreachable — dead code | ✓ |
| 92 | [Session Lifecycle for a Round-Based Game](92_session_lifecycle.md) | ⚠️ Session works; its lifecycle assumes a deathmatch |  |
| 93 | [Addressables Content Build](93_addressables_content_build.md) | ⚠️ Works in the Editor; one missing step breaks the shipped build |  |
| 94 | [Entity Subscene Baking](94_entity_subscene_baking.md) | ⚠️ Correct today for two subscenes; the process does not scale |  |
| 95 | [Client/Server Build Parity](95_client_server_build_parity.md) | ⚠️ No parity guarantee of any kind exists |  |
| 96 | [Editor vs Build Test Paths](96_editor_vs_build_test_paths.md) | ⚠️ Editor tooling exists and is being trusted for more than it can prove |  |
| 97 | [Build Verification Pass](97_build_verification_pass.md) | ❌ No evidence in the repo of a client-vs-client build ever having been run |  |

## §13. Onboarding, Performance & Long Tail

| # | Component | Status | MVP |
| --- | --- | --- | --- |
| 98 | [Tutorial / Onboarding](98_tutorial_and_onboarding.md) | ❌ Not started |  |
| 99 | [Performance Budget](99_performance_budget.md) | ❌ No budget established | ✓ |
| 100 | [Network Bandwidth Budget](100_network_bandwidth_budget.md) | ❌ No budget, no measurement, and no relevancy rules |  |
| 101 | [Analytics / Balance Telemetry](101_analytics_and_balance_telemetry.md) | ❌ Not started |  |
| 102 | [Localization](102_localization.md) | ❌ Not started — safe to defer, expensive to retrofit |  |
| 103 | [Build Versioning & Mismatch Rejection](103_build_versioning_and_mismatch_rejection.md) | ❌ No version stamp and no handshake check exist |  |
| 104 | [Crash / Error Reporting](104_crash_and_error_reporting.md) | ❌ Not started — Cloud Diagnostics is available through the linked UGS project and is disabled |  |

