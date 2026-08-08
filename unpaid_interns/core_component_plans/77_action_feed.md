# 77 — Action Feed

**Source:** [`core_components.md`](../core_components.md) §9 — UI & Feedback
**Status:** ⚠️ Works, announces the wrong things
**Depends on:** [Crew Roster](19_crew_roster.md), [Loot Banking](43_loot_banking_deposit.md), [Day Cycle Controller](02_day_cycle_controller.md)
**Blocks:** a split-up crew knowing what is happening to each other

## Summary

The running line of text that tells four people in four different rooms what just happened.

`ActionFeed.cs` already works. It resolves a container from `ActionFeedUi.uxml`, creates a styled `Label` per entry, appends it, and schedules its removal — driven by `LeaderboardManager` broadcasting `KillFeedEntryRpc` and `PlayerJoinedEntryRpc` and consuming them client-side. The mechanism is sound and the RPC pattern is the correct one for transient events.

What it announces is a deathmatch's business: *"X killed Y"* and *"Z joined"*. `core_components.md` asks for it to be repurposed to team-relevant events — died, item banked, quota met, ship leaving — and that repurposing is where its real value is.

The value is specific to this design. `GAME_DESIGN.md` puts the crew in *"random, unfamiliar locations"* deciding independently how much risk is worth it, which in practice means four people who cannot see each other. Voice covers most of it ([`21_proximity_voice_comms.md`](21_proximity_voice_comms.md)) — but voice is range-limited by design, people talk over each other, and someone is always the person who missed it. The feed is the **reliable, silent, always-legible channel** underneath the voice channel, and it is the accessibility fallback for a game whose most important information is otherwise spoken.

## How to Build

**Replace the event set**

Announce things that change what another player should do:

- **An intern died** — with cause where known ([`57_attack_and_damage_application.md`](57_attack_and_damage_application.md) records it). The most important line in the game.
- **An intern disconnected** — [`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md) requires it explicitly, because *"a crew that does not know someone left will wait for them"*.
- **An item was banked** — with value, so the crew hears the haul growing ([`43_loot_banking_deposit.md`](43_loot_banking_deposit.md) requires this announcement). This is what turns individual scavenging into a shared score.
- **A body was recovered.**
- **Quota met**, and **the round is ending** — the departure warning ([`31_entry_point_extraction_zone.md`](31_entry_point_extraction_zone.md) requires it to be loud and immediate; the feed is one of the channels). [`105_departure_and_extraction_resolution.md`](105_departure_and_extraction_resolution.md) requires four channels to fire at once — feed, HUD countdown, audio, and a non-audio visual — because this is the one announcement a player cannot afford to miss. It is also the one feed line that must **never** be coalesced or rate-limited away.
- **Departure started, by whom, and departure aborted.** A crew that cannot find out who ended the round will invent an answer, and an abort that goes unannounced leaves people sprinting back for nothing.
- **Between rounds**: a purchase was made, the destination changed, an upgrade was bought ([`67_store_purchasing.md`](67_store_purchasing.md), [`27_location_selection_assignment.md`](27_location_selection_assignment.md), [`68_upgrades.md`](68_upgrades.md) all require shared-money and shared-decision actions to be visible).
- **Optionally**: a teammate damaged another teammate ([`18_pvp_collision_and_friendly_fire.md`](18_pvp_collision_and_friendly_fire.md) suggests it, so friendly fire is visible rather than mysterious).

Delete the kill feed. [`13_health_and_injury.md`](13_health_and_injury.md) and [`45_weapons_as_tools.md`](45_weapons_as_tools.md) both require the `LeaderboardManager.AddKill` call to be removed from the damage path; the feed entry it produced goes with it.

**Keep the transport, fix its source**

- Keep `IRpcCommand` structs broadcast via `GhostGameObject.BroadcastRPC` and consumed with `ConsumeRPC` — the `GameLeaderboard.cs` pattern. Transient notifications are exactly what RPCs are for.
- But respect the rule in [`23_shared_session_state_sync.md`](23_shared_session_state_sync.md): **every event with a lasting consequence must also be derivable from replicated state.** An RPC can be missed and is lost forever. The feed announcing a bank is presentation; the banked total is a ghost field. A player who misses the line still sees the right total.
- That makes the feed **purely advisory**, which is the correct posture and simplifies everything: no acknowledgement, no ordering guarantee, no late-joiner replay.
- Move the broadcasts off `LeaderboardManager` as it is retired ([`70_performance_report.md`](70_performance_report.md)) and onto the systems that own each event, or onto the Run Manager as a single announcement channel. One channel is easier to rate-limit.

**Design against the failure mode: too much text**

- A loot-dense round with four players banking items will produce a wall of scrolling text, and a feed that is always full is a feed nobody reads — which means the *death* line scrolls past unseen.
- Rate-limit and coalesce. Several items banked in quick succession become one line with a count and a total. Repeated low-value events collapse; deaths never do.
- **Tier the events.** Deaths, disconnects, and the round ending are high priority and should be visually distinct, persist longer, and never be coalesced away. Bank announcements are ambient.
- Cap the visible entries and the lifetime. `ActionFeed` already schedules removal per label — keep that and add a maximum count so a burst does not fill the screen.
- Consider suppressing bank announcements for the player who banked. They watched it happen.

**Fix the presentation problems it already has**

- Entries are built as `new Label(...)` per event, which allocates per announcement. Under a burst that is a measurable cost; pool the labels the way `SoundGameObjectPool` pools emitters (§11 establishes the pattern).
- The feed shares screen space with interaction prompts and scan results. [`71_hud.md`](71_hud.md) requires named HUD regions precisely so these do not collide; claim one and stay in it.
- The current strings are built by concatenation. Move them to the shared string table [`73_interaction_prompts.md`](73_interaction_prompts.md) establishes, so deferring localisation stays cheap (§13).

**Make it carry the tone**

- The employer's voice is free here. *"J. Fournier has been removed from the payroll."* *"Asset recovered: 340 credits. Well done, team."* The premise is a company that treats interns as expendable, and the feed is where that lands per-event rather than only on the report screen.
- Keep death lines readable first and funny second. A player scanning for "who died" must not have to parse a joke to find out.

**Serve accessibility, because it is doing that job whether or not it is designed to**

- §9 makes accessibility required and notes that monster detection is primarily an audio skill. The feed is a **text channel for events that are otherwise audio or voice**, which makes it load-bearing for a deaf or hard-of-hearing player.
- Scalable text size, adequate contrast, and no meaning conveyed by colour alone — a high-priority line must read as high-priority in monochrome.
- Consider a configurable verbosity setting rather than a single fixed set, once the settings menu exists ([`78_settings_options_menu.md`](78_settings_options_menu.md)).

## Acceptance Criteria

- [ ] The kill feed is removed, along with the `AddKill` call that fed it.
- [ ] Deaths, disconnects, banked items, body recoveries, quota met, and round ending are all announced.
- [ ] Between-rounds purchases, upgrades, and destination changes are announced.
- [ ] Every announced event with a lasting consequence is also derivable from replicated state; the feed is advisory only.
- [ ] A client that misses a feed RPC still converges to correct state.
- [ ] Events are tiered, with deaths, disconnects, and round-ending visually distinct, longer-lived, and never coalesced.
- [ ] Rapid repeated events coalesce into a single line with a count and total.
- [ ] Visible entry count and lifetime are capped; a burst of banking never fills the screen.
- [ ] A death line remains visible and findable during a burst of ambient announcements.
- [ ] Label objects are pooled; no per-announcement allocation in the steady state.
- [ ] The feed occupies its reserved HUD region and never overlaps prompts or scan results.
- [ ] All feed strings live in the shared string table.
- [ ] Announcement text is written in the employer's voice, with death lines legible before they are funny.
- [ ] Text is scalable, high-contrast, and conveys priority without relying on colour.
- [ ] The feed alone is sufficient to follow the round's key events with audio disabled.
- [ ] Announcements are correct for a client that joined mid-round and never replay stale events to them.
