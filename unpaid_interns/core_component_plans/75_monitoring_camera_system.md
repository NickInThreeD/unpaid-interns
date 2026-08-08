# 75 — Monitoring / Camera System

**Source:** [`core_components.md`](../core_components.md) §9 — UI & Feedback
**Status:** ❌ Not started
**Depends on:** [Terminal / Hub Interface](74_terminal_hub_interface.md), [Hazard Control / Remote Disable](62_hazard_control_remote_disable.md), [Crew Roster](19_crew_roster.md)
**Blocks:** "someone stays behind" being a role rather than a sacrifice

## Summary

Letting a hub-bound player watch the crew and call out what they cannot see.

`core_components.md` describes the goal as turning *"someone stays behind"* into a real role, and that framing is exactly right — the role currently does not exist. A player who does not deploy is a player with nothing to do, and the crew slot they occupy is a slot not carrying loot. Nobody volunteers for that twice.

This component and [`62_hazard_control_remote_disable.md`](62_hazard_control_remote_disable.md) are the two halves of making the role worth playing: one gives the operator **information** the field team lacks, the other gives them **actions** the field team cannot take. Neither works alone. An operator who can see a monster but do nothing is a spectator with anxiety; one who can disable a turret but cannot see which turret matters is a button nobody knows when to press.

It is also the component that makes voice comms a mechanic rather than a convenience. The operator's knowledge is useless until it is spoken, and speaking it badly — *"there's something in the room, no, the other room"* — is some of the best material the genre produces.

**This is post-MVP.** The loop works without it, and it should be built once the hub, the terminal, and the threat layer are real enough to be worth watching.

## How to Build

**Decide how much the operator can see — this is the whole balance**

- The operator's view is a **power level**, and it is easy to set far too high. A full live feed of every room with monster positions marked replaces the field team's judgement entirely and makes the game easier in a way that is not fun for anyone.
- Recommended: **fixed cameras at authored positions**, one view at a time, with **no monster highlighting**. The operator sees what a camera sees — a corridor, a room, sometimes a shape moving through it — and has to interpret it, describe it, and be believed.
- Camera positions come from room modules, authored like loot points and vents ([`28_procedural_interior_generator.md`](28_procedural_interior_generator.md)), so coverage varies per layout and the operator has blind spots that matter.
- **Never show what the field team could not have known.** No monster names, no health bars, no through-wall markers. The operator's advantage is *vantage*, not omniscience.
- Player positions are the reasonable exception: a coarse marker showing where crewmates are is what makes the operator able to say "west of you" rather than "somewhere". [`62_hazard_control_remote_disable.md`](62_hazard_control_remote_disable.md) already allows a rough player marker on its schematic and forbids monster positions; use the same rule.

**Solve the rendering problem honestly**

- A live camera feed means **rendering the scene from a second viewpoint**, which is a real cost — a second render target, a second culling pass, and a second set of shadow draws in an interior whose lighting budget is already tight ([`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md)).
- Mitigate deliberately: render **one camera at a time**, at reduced resolution, at a reduced frame rate, with a deliberately degraded post-process look. A low-frame-rate grainy security feed is cheaper *and* better — it is more atmospheric than a clean feed and it makes the operator's uncertainty diegetic.
- The operator is in the hub, so their client is not also rendering a location full of monsters — the budget is more available there than it would be for a field player. Verify that assumption holds once the hub and a location are loaded simultaneously ([`05_location_load_unload_flow.md`](05_location_load_unload_flow.md)).
- Note the `AudioListener` constraint that [`22_spectator_mode.md`](22_spectator_mode.md) documents: `MainCameraSingleton` is `[RequireComponent(typeof(Camera), typeof(AudioListener))]`, so a second camera built from that prefab means a second listener. The monitor camera must **not** carry an `AudioListener` — the operator hears the hub, not the feed, unless audio is a deliberate feature.

**Consider giving it sound, carefully**

- A feed with audio is dramatically more useful and dramatically more powerful. Hearing a monster through a camera lets the operator warn the crew about something nobody has seen.
- If adopted, it should be **bad audio** — mono, filtered, no spatialisation — so it conveys presence without direction. That preserves the operator's need to guess and the crew's need to interpret.
- It must route through the existing `SoundSystem` rather than a second audio path, and it must never let the operator hear something the noise system would not have made audible at that position ([`54_noise_emission_system.md`](54_noise_emission_system.md)).

**Make it a terminal view, not a second machine**

- [`74_terminal_hub_interface.md`](74_terminal_hub_interface.md) requires the terminal to be built as tabbed views over a shared frame precisely so this slots in. Monitoring is a tab; remote control is a tab; the store is a tab.
- One operator at a time, using the terminal's existing claim ([`20_networked_interaction_authority.md`](20_networked_interaction_authority.md)). The screen is visible to anyone else in the hub, which is what makes two people watching together possible.
- Camera selection is a client-side view change and needs no server round trip — the server is not being asked to do anything, only to have already replicated what the camera sees. That means **ghost relevancy has to cover the operator's camera position, not just their player position**, or the feed will show an empty corridor while a monster stands in it.

That relevancy point is the component's one genuine networking trap. [`49_monster_ghost_and_replication.md`](49_monster_ghost_and_replication.md) sets distance-based relevancy from the player; a hub-bound operator is far from everything. Either extend relevancy to include the active camera's position, or accept that the feed only shows what some crewmate is already near — which is a legitimate design choice and considerably cheaper.

**Give the operator a reason to keep watching**

- Rotating through empty corridors is boring, and a bored operator alt-tabs. Give the view something to do: highlight where the crew is, show which rooms have been visited, mark the extraction zone.
- The strongest version pairs with remote control — the operator watching a corridor *because* they are about to disable the turret in it.
- Consider letting the operator ping a location for the field team, surfacing through the same highlight pipeline as the scanner ([`16_player_scanner_ping_tool.md`](16_player_scanner_ping_tool.md)). That converts their knowledge into something actionable even when voice fails.

**Handle the states**

- Spectators must not use it. [`22_spectator_mode.md`](22_spectator_mode.md) requires dead players to be unable to influence the world, and although watching is not influence, an operator seat for the dead removes the cost of dying and duplicates the spectator camera badly.
- Nobody in the hub is the common case, and the system must simply be idle — no cost, no cameras rendering, nothing.
- Camera state is per-round and torn down with the location.

## Acceptance Criteria

- [ ] Monitoring is a view within the terminal, not a separate in-world machine.
- [ ] Only one operator at a time, using the terminal's existing claim, with the display visible to others in the hub.
- [ ] Cameras are at authored positions from room modules, with per-layout coverage and genuine blind spots.
- [ ] The feed shows exactly one camera at a time, at reduced resolution and frame rate.
- [ ] No monster is ever named, highlighted, marked, or shown through geometry.
- [ ] A coarse crewmate position marker is available; nothing finer.
- [ ] The monitor camera carries no `AudioListener`; exactly one listener remains active at all times.
- [ ] If feed audio is implemented, it is mono, unspatialised, and derived from the same noise the world actually produced.
- [ ] Camera selection requires no server round trip.
- [ ] Ghost relevancy either covers the active camera position or the limitation is documented and the feed's blind spots are accepted deliberately.
- [ ] With no player in the hub, no camera renders and the system costs nothing.
- [ ] Spectators cannot use monitoring.
- [ ] The operator can mark or ping a location for the field team, surfacing through the scanner's highlight pipeline.
- [ ] Rendering one feed alongside a loaded hub holds the client frame budget on the lowest-spec target.
- [ ] All camera state is torn down with the location, with no leaked render targets across five consecutive rounds.
- [ ] The feed is legible during a facility blackout, or its uselessness in darkness is a documented, deliberate limitation.
