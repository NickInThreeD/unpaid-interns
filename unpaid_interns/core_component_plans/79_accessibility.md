# 79 — Accessibility

**Source:** [`core_components.md`](../core_components.md) §9 — UI & Feedback
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Settings / Options Menu](78_settings_options_menu.md), [Noise Emission System](54_noise_emission_system.md), [Monster Variety Set](58_monster_variety_set.md)
**Blocks:** an entire class of player being able to play at all

## Summary

`core_components.md` marks this **elevated from optional to required by this game's design**, and that is not a courtesy. It follows from a specific mechanical fact: monster detection in this game is primarily an **audio skill**. [`53_perception_system.md`](53_perception_system.md) builds a threat layer where hearing something before seeing it is the survival mechanic, [`58_monster_variety_set.md`](58_monster_variety_set.md) requires monsters to be identifiable *without line of sight*, and §10 asks for distinct per-monster sounds for idle, alerted, and chasing.

Every one of those requirements, stated without an accessibility counterpart, describes a game a deaf or hard-of-hearing player cannot play. Not "plays worse at" — cannot play, because the information needed to survive is only ever emitted as sound.

That makes this component structurally different from most accessibility work. It is not a polish pass applied to a finished game; it is a **parallel output channel for the game's primary information stream**, and it has to be designed alongside the audio rather than retrofitted after. Retrofitting means finding every sound that carried meaning and inventing a visual for it in isolation, which produces an inconsistent mess.

## How to Build

**Build the visual sound channel as a system, not as effects**

- The requirement is directional indicators for sounds that matter — footsteps, growls, vent wind-ups, doors, distant impacts. Build it as **one consumer of the noise system** ([`54_noise_emission_system.md`](54_noise_emission_system.md)), not as a per-sound feature.
- That is the key structural decision. Noise events already carry position, range, volume, and category — everything an indicator needs. A single system that renders any noise event within the player's audible range as a directional cue is complete by construction: a new sound that raises a noise event gets an indicator for free, and a sound that does not raise one was never gameplay-relevant.
- It also enforces the honesty rule the noise system already carries: what a player sees must match what a monster hears. An indicator derived from the same events cannot drift from the audio.
- Encode **direction, distance, and category** — not just "a sound happened". Category is what lets a player distinguish a teammate's footsteps from something else's, which is exactly the discrimination the audio gives a hearing player.
- Client-side presentation only, reading events the client legitimately receives. This must not become a way to perceive noises the audio system would not have played.

**Subtitle everything that speaks**

- Audio warnings, employer announcements, monster vocalisations, and any voice lines. §9 asks for subtitles specifically for audio warnings and voice.
- Subtitle **non-speech sounds too**, in the standard closed-caption style — *"[vent rattling, nearby]"*, *"[something breathing, behind]"*. That is where the horror information actually lives.
- Include speaker identification for proximity voice ([`21_proximity_voice_comms.md`](21_proximity_voice_comms.md) already requires a speaking indicator showing *who* is talking and roughly where, and calls it usable by a player with audio off). Speech-to-text is out of scope; the indicator is not.
- Size, background opacity, and position all configurable ([`78_settings_options_menu.md`](78_settings_options_menu.md)).

**Enforce the no-colour-alone rule everywhere, once**

Nearly every UI plan already carries this as an acceptance criterion — [`71_hud.md`](71_hud.md), [`72_quota_and_deadline_display.md`](72_quota_and_deadline_display.md), [`73_interaction_prompts.md`](73_interaction_prompts.md), [`76_end_of_round_summary.md`](76_end_of_round_summary.md), [`77_action_feed.md`](77_action_feed.md), [`16_player_scanner_ping_tool.md`](16_player_scanner_ping_tool.md), [`40_inventory_item_bar.md`](40_inventory_item_bar.md). Rather than checking seven files independently:

- Establish a **colourblind-safe palette** and require every state indicator to carry a second channel — shape, position, icon, or text.
- The scanner is the sharpest case: [`16_player_scanner_ping_tool.md`](16_player_scanner_ping_tool.md) requires highlight categories to be distinguishable by shape or label as well as hue, because **category is the information, not the colour**.
- Add a **screenshot-in-greyscale check** to the review process. It takes seconds and catches every violation, and it is far more reliable than asking each component's author to remember.

**Cover motion and vision comfort**

- FOV control and head-bob reduction, both flagged in §9 and both exposed by [`78_settings_options_menu.md`](78_settings_options_menu.md). First-person horror with camera movement is a common motion-sickness trigger, and the crouch camera transition ([`10_crouch.md`](10_crouch.md)) and landing dip ([`61_fall_and_environmental_damage.md`](61_fall_and_environmental_damage.md)) both add to it.
- The fear overlay must scale to zero and must never obscure health, stamina, or the item bar — [`15_fear_and_stress_feedback.md`](15_fear_and_stress_feedback.md) already carries both requirements.
- Brightness/gamma, honoured by the blackout state ([`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md)). Emergency lighting exists partly for this reason: total darkness must not be the unpowered state.
- HUD scale and a safe-area margin ([`71_hud.md`](71_hud.md)).

**Hold the line on the two rules that are easy to violate**

Two constraints appear across the threat-layer plans and are the ones most likely to be quietly broken by a later feature:

1. **No critical information may be available only through vision.** [`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md) states it directly. Anything that must be seen must also be findable by scan or sound.
2. **No critical information may be available only through audio.** The mirror rule, and the reason this component exists. It binds [`52_spawn_points_and_vents.md`](52_spawn_points_and_vents.md)'s vent wind-up, [`57_attack_and_damage_application.md`](57_attack_and_damage_application.md)'s attack telegraph, [`59_static_map_hazards.md`](59_static_map_hazards.md)'s arming cue, and [`60_door_system.md`](60_door_system.md)'s monster-at-the-door sound — each of those plans already requires a non-audio equivalent, and this is the component that verifies they all shipped one.

Make both rules part of the review checklist for any new gameplay cue. A telegraph without a visual equivalent should fail review the way an untelegraphed attack would.

**Test it the only way that works**

- Play a full round with **audio muted** and confirm the crew's survival information is still available. Then play one with the **screen in greyscale**. Both are cheap and both find things no checklist does.
- Add the audio-off round to the playtest protocol rather than doing it once. Regressions here are silent — a new monster with no visual cue breaks nothing that any automated test would notice.
- [`58_monster_variety_set.md`](58_monster_variety_set.md) already requires each monster to be identifiable with audio disabled; that criterion is verified here, per monster, as the roster grows.

## Acceptance Criteria

- [ ] Visual sound indicators are implemented as a single consumer of the noise system, not per-sound.
- [ ] A new gameplay sound that raises a noise event gets an indicator with no additional work.
- [ ] Indicators convey direction, distance, and category.
- [ ] Indicators never reveal a noise the audio system would not have played to that player.
- [ ] Subtitles cover speech, employer announcements, and gameplay-relevant non-speech sounds in closed-caption style.
- [ ] Proximity voice shows who is speaking and roughly where, usable with audio off.
- [ ] Subtitle size, opacity, and position are configurable.
- [ ] A colourblind-safe palette is established, and every state indicator carries a second non-colour channel.
- [ ] Scanner highlight categories are distinguishable by shape or label as well as hue.
- [ ] A greyscale screenshot review is part of the process for any new UI.
- [ ] FOV, head-bob reduction, brightness/gamma, HUD scale, and safe-area margin are all configurable.
- [ ] Fear overlay intensity scales to zero and never obscures health, stamina, or the item bar.
- [ ] Full input rebinding is available.
- [ ] Every vent wind-up, attack telegraph, hazard arming cue, and monster-at-the-door sound has a verified non-audio equivalent.
- [ ] No critical information is available only through vision, and none only through audio.
- [ ] A full round played with audio muted leaves the crew's survival information available.
- [ ] A full round played in greyscale leaves all UI state distinguishable.
- [ ] Each monster in the roster is identifiable and its state readable with audio disabled.
- [ ] The audio-off round is part of the recurring playtest protocol, not a one-off check.
