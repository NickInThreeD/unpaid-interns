# 83 — Ambience & Time Cues

**Source:** [`core_components.md`](../core_components.md) §10 — Audio
**Status:** ❌ Not started
**Depends on:** [Round Timer / Clock](03_round_timer_clock.md), [Location Catalogue](26_location_catalogue.md), [Environmental Conditions](35_environmental_conditions_weather.md)
**Blocks:** the passage of time being felt when the clock is not visible

## Summary

Per-location environmental beds, and stingers at the moments the round changes.

`core_components.md` gives the second half a specific job: *"carrying the passage of time when the clock isn't visible."* That is a direct dependency on a decision made elsewhere. [`71_hud.md`](71_hud.md) recommends the clock be readable on demand rather than permanently displayed, because an always-visible countdown converts dread into arithmetic — and the moment that decision is taken, **audio becomes the primary channel for "how late is it".** This component stops being atmosphere and starts being information.

The first half — per-location ambience — is the cheapest character a destination gets. [`26_location_catalogue.md`](26_location_catalogue.md) already lists an ambience `SoundDef` set as location data, and a crew that can tell where they are with their eyes closed has a stronger sense of place than any amount of geometry provides.

The infrastructure is ready: pooled emitters, `SoundDef` assets, `SoundMixer` routing, and a headless no-op path all exist in `Assets/Scripts/Audio/`.

## How to Build

**Key everything to normalized time, not to seconds**

- [`03_round_timer_clock.md`](03_round_timer_clock.md) exposes `NormalizedTime` as the single 0→1 value the round's systems key off, and defines **named thresholds** — morning, midday, dusk, final warning — as data with a one-shot event per boundary crossing.
- Subscribe to those events. Do not re-derive time thresholds here; a second set of boundaries will drift from the spawn director's and the crew will hear "dusk" while the threat curve says midday.
- That plan already handles the two hard cases: **fire each boundary exactly once**, and **do not retroactively fire** for a client who joined after it passed. A late joiner should hear the current bed, not a sequence of stingers catching them up.
- Because the clock is `NetworkTick`-derived, every client crosses a boundary within a tick of each other — so the crew hears the dusk sting together, which is what makes it a shared moment rather than four private ones.

**Make the bed change, not just the stinger**

- A stinger marks a transition; the **bed** is what makes the player feel the state they are in. If only the stingers change, a player who missed one has no way to tell how late it is.
- Crossfade beds across boundaries rather than cutting. The transition should be noticeable in hindsight, not startling in the moment — a startle at dusk trains players to associate the time cue with danger that has not arrived yet.
- Make the late-round bed genuinely worse: sparser, lower, more space between events. [`51_difficulty_escalation.md`](51_difficulty_escalation.md) requires a player to be able to tell a late round from an early one **without reading a number**, and audio is the cheapest channel that delivers it.
- Resist scoring the round like a film. Music that tells the player how to feel undercuts the ambiguity — *is that a monster or the building?* — that the horror depends on. Diegetic and near-diegetic beds do more work than a score.

**Layer location, weather, and interior separately**

- **Location ambience** comes from `LocationData` ([`26_location_catalogue.md`](26_location_catalogue.md)) and establishes where the crew is.
- **Weather ambience** comes from `WeatherData` ([`35_environmental_conditions_weather.md`](35_environmental_conditions_weather.md)) and is primarily an exterior layer. Rain and storm also raise the ambient noise floor, which that plan makes a gameplay effect — masking player noise — so the audio and the noise system must agree about how loud the weather is.
- **Interior ambience** is the building itself, and should be distinctly different from outdoors. Crossing the main entrance ([`33_exterior_approach_area.md`](33_exterior_approach_area.md)) should be audible with your eyes closed; that transition is one of the strongest moments the game has and it costs one crossfade.
- **Power state** modifies the interior layer ([`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md)). A powered facility hums; a dead one does not, and the absence is more frightening than any added sound. The breaker box's own hum is already specified as a findable landmark in the dark.

**Do not let ambience compete with the threat channel**

This is the component's one real risk, and it is worth stating as a constraint rather than a note.

- [`82_monster_audio_cues.md`](82_monster_audio_cues.md) makes monster identification the game's primary survival skill, and every ambient sound is competing for the same attention and the same voices.
- **Ambience must occupy a different frequency space and a different mixer group** from monster cues, and monster cues must duck ambience rather than the reverse. `SoundMixer` already provides the routing.
- Keep ambient beds free of sounds that could be mistaken for a creature. A random distant clang is atmospheric exactly once and then becomes a false positive that teaches players to ignore real ones — the same failure [`52_spawn_points_and_vents.md`](52_spawn_points_and_vents.md) forbids for vent wind-ups, where the rule is that the telegraph must be **truthful**.
- If ambient one-shots are used, keep them clearly environmental — structural, weather, machinery — and never vocal.

**Cover the other states**

- The **hub** needs its own bed, and it should be the safest sound in the game. It is the only place the crew relaxes, and the contrast is what makes deploying feel like something.
- **Settling** and the end-of-round screens want their own treatment — the employer's register ([`70_performance_report.md`](70_performance_report.md)).
- Ambience stops in the main menu and does not leak across a round transition. Verify with the same teardown discipline every per-round system carries.

**Keep it cheap and headless-safe**

- Beds are long loops on pooled emitters; the cost is voices, not CPU. Cap concurrent ambient voices and give monster cues priority in the voice budget.
- The dedicated-server build swaps in `SoundSystemNull` and plays nothing. Confirm no timing or gameplay logic hangs off an ambience callback — the boundary events come from the clock, which is authoritative and audio-independent.

## Acceptance Criteria

- [ ] Ambience and stingers subscribe to the round clock's named boundary events and define no thresholds of their own.
- [ ] Each boundary stinger fires exactly once per round on every client.
- [ ] A client joining after a boundary hears the current bed and no retroactive stingers.
- [ ] All clients cross a boundary within one tick of each other.
- [ ] The ambient bed changes at each boundary, not only the stinger, so a player who missed a sting can still tell the time of day.
- [ ] Beds crossfade rather than cutting.
- [ ] The late-round bed is audibly worse, and a player can tell a late round from an early one with the clock hidden.
- [ ] Location ambience comes from `LocationData` and differs audibly between destinations.
- [ ] Weather ambience comes from `WeatherData`, and its loudness agrees with the noise system's masking value.
- [ ] Interior and exterior beds are distinctly different, and crossing the main entrance is audible without visuals.
- [ ] Power state modifies the interior bed, and an unpowered facility is audibly dead.
- [ ] Ambience occupies a separate mixer group from monster cues and ducks beneath them.
- [ ] No ambient sound can be mistaken for a creature; ambient one-shots are environmental and never vocal.
- [ ] The hub has its own distinct, safe-sounding bed.
- [ ] Ambience stops cleanly at round end and in the main menu, with no leakage across transitions.
- [ ] Concurrent ambient voices are capped and yield priority to monster cues.
- [ ] A dedicated-server build runs with no audio and identical round timing.
- [ ] A designer can add a location's ambience set with no code change.
