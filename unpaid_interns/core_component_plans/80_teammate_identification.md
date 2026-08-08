# 80 — Teammate Identification

**Source:** [`core_components.md`](../core_components.md) §9 — UI & Feedback
**Status:** ❌ Not started
**Depends on:** [Crew Roster](19_crew_roster.md), [Player Scanner](16_player_scanner_ping_tool.md)
**Blocks:** "who is that ahead of me" being answerable

## Summary

Telling your crewmates apart in a dark building.

`core_components.md` calls it *"cosmetic on its face"* and then immediately explains why it is not: some monsters imitate players, and in a game with those, **the ability to identify a teammate is the counterplay to an entire archetype.** Without it, "there's someone ahead of me" is unresolvable, and a mimic is not a puzzle, it is a coin flip.

Even without mimics it does real work. A crew that cannot tell each other apart cannot coordinate — *"follow me"* requires knowing who is speaking and which silhouette they are, and in a corridor with one flashlight everyone is the same shape. The design puts four people in an unfamiliar dark building and asks them to split up; this is what makes rejoining possible.

The data already exists and is unused. `PlayerGhost.PlayerData.Name` is a replicated `FixedString64Bytes` set at spawn from `GameSettings.PlayerName`, which `MainMenu.cs:49` already wires from the main menu. It reaches no in-world presentation whatsoever.

## How to Build

**Give each intern a visual identity that survives darkness**

- Assign a **distinct suit colour** per crew slot, from a fixed palette. Colour is the fastest identification channel at a distance and it costs one material property.
- Colour alone is not enough — §9's accessibility requirement applies directly, and a colourblind player must still be able to tell four teammates apart. Pair colour with a **second channel**: a distinct silhouette element (helmet shape, backpack), a number or letter on the suit, or both ([`79_accessibility.md`](79_accessibility.md)).
- Assignment must be **stable for the run** and keyed on the stable player id, not `NetworkId` ([`19_crew_roster.md`](19_crew_roster.md)) — a player who reconnects and comes back a different colour has broken the one thing this component provides.
- The colour must be visible in the dark. Emergency lighting exists ([`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md)) but a blackout will still flatten hues; a small emissive element on the suit solves this and reads as diegetic equipment.

**Add name tags with a deliberate range**

- Show the replicated `PlayerData.Name` above a teammate at close-to-medium range. That range is a real design decision, not a UI parameter.
- **Recommended: short range, and require line of sight.** A name tag visible through walls at any distance is a permanent teammate radar that removes the fear of being separated, which is the tension the design is built on. A tag that appears when you can already see someone answers "who is that" without answering "where is everyone".
- Fade with distance rather than popping at a threshold.
- Include the crew's colour and identifying mark in the tag so the two channels reinforce each other rather than being learned separately.

**Decide the mimic rule before building it**

This is the component's one genuinely consequential decision, and it should be made now rather than discovered when the mimic monster is authored ([`58_monster_variety_set.md`](58_monster_variety_set.md)):

- **Tags never appear on a mimic** — identification is reliable, mimics are beaten by checking, and the archetype becomes a test of discipline rather than luck. Safe, and it makes the mimic much weaker.
- **Tags appear on a mimic** — identification is unreliable, and the player must use something else (behaviour, the roster, voice) to verify. Much scarier and it risks making the tag actively harmful.
- **Recommended: tags never appear on a mimic, but the suit colour and silhouette can be copied.** The tag is the reliable channel and it requires line of sight at close range — so verifying costs you the exact thing you do not want to spend near a mimic, which is proximity. That gets the tension without making the UI lie.
- Whatever is chosen, **record it here**, because the mimic's design depends on it entirely.

**Add a HUD roster**

- A persistent list of the crew with name, colour, and state — alive, dead, disconnected — read from [`19_crew_roster.md`](19_crew_roster.md).
- This is where the roster's states earn their keep. A crew that can see "two alive, one dead, one disconnected" makes completely different decisions than one guessing.
- It doubles as the speaking indicator surface for proximity voice ([`21_proximity_voice_comms.md`](21_proximity_voice_comms.md) requires a speaking indicator on the HUD roster, usable by a player with audio off — one element, two requirements).
- Keep it small and peripheral, in its reserved HUD region ([`71_hud.md`](71_hud.md)). It is reference information, not a focus.
- Do **not** show teammate positions or distances on it. That is the scanner's job, on a cooldown, deliberately ([`16_player_scanner_ping_tool.md`](16_player_scanner_ping_tool.md) includes teammates as a scannable category).

**Handle the states**

- Dead players' tags disappear with them; their body is identified by a different mechanism ([`14_death_and_body_system.md`](14_death_and_body_system.md) already stores the dead player's identity on the body ghost, which is exactly what a corpse's label should read from).
- Spectators see tags on everyone they are following ([`22_spectator_mode.md`](22_spectator_mode.md)) — they have no stake to protect and it makes spectating comprehensible.
- In the hub, tags can be generous: longer range, always visible. The hub is social and there is nothing to fear ([`04_hub_between_rounds_state.md`](04_hub_between_rounds_state.md)).

**Keep it cheap**

- Tags are client-side presentation reading already-replicated state; nothing new crosses the wire.
- World-space labels are easy to make expensive. Cull by distance and line of sight before doing any layout work, cap the number rendered, and do not rebuild a label when its text has not changed — the same no-per-frame-allocation discipline [`71_hud.md`](71_hud.md) enforces centrally.

## Acceptance Criteria

- [ ] Each intern has a distinct suit colour assigned from a fixed palette, stable for the whole run.
- [ ] Assignment is keyed on the stable player id; a reconnecting player keeps their colour.
- [ ] A second, non-colour identification channel exists, and four teammates are distinguishable in greyscale.
- [ ] Identification remains possible during a facility blackout.
- [ ] Name tags read the replicated `PlayerData.Name` and appear at the configured range with line of sight required.
- [ ] Name tags never appear through geometry.
- [ ] Tags fade with distance rather than popping.
- [ ] The mimic rule is implemented and documented in this file.
- [ ] A HUD roster shows every crew member with name, colour, and state from the crew roster.
- [ ] The roster distinguishes alive, dead, and disconnected.
- [ ] The roster carries the proximity-voice speaking indicator and is usable with audio off.
- [ ] The roster shows no teammate positions or distances.
- [ ] The roster occupies its reserved HUD region and does not overlap other elements.
- [ ] A body is identified as the specific intern who died, read from the body ghost.
- [ ] Spectators see tags on players they follow.
- [ ] Hub tags are longer-range and always visible.
- [ ] No new state is replicated for this component.
- [ ] Tag rendering is culled by distance and line of sight, capped in count, and allocates nothing per frame.
