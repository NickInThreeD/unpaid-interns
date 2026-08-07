# 16 — Player Scanner / Ping Tool

**Source:** [`core_components.md`](../core_components.md) §2 — Player Character
**Status:** ❌ Not started
**Depends on:** Item Ghost / Networked Item State, Entry Point / Extraction Zone
**Blocks:** navigability of procedural interiors, informed risk decisions

## Summary

A pulse that highlights nearby items, exits, the extraction point, and teammates, and reports the visible value of what it found.

This exists for one reason: **procedural interiors are disorienting by design, and a player who is lost is not making decisions.** `GAME_DESIGN.md` puts the whole game on "how long do I stay" — a question you cannot answer without knowing roughly what is left worth taking and roughly how far you are from the exit. Without a scanner, players wander, and wandering is neither tense nor fun.

It is also the main way loot value reaches the player before pickup. A scanner that reports "four items, 320 credits, that way" turns exploration into routing.

Build it as an always-available player ability rather than a purchasable tool. The design already has a money sink in gear; making basic navigation cost credits punishes new crews at exactly the moment they can least afford it.

## How to Build

**Add the input and the pulse**

- Follow the input-flag pattern documented in [`09_sprint.md`](09_sprint.md): add a `Scan` flag to `PlayerInput.InputFlag` and read it in `ClientInputReaderSystem.ProcessGameplayInput`. Unlike Sprint and Crouch there is **no existing binding** in `InputSystem_Actions`, so the action must be added to the generated input asset as well.
- Rate-limit the pulse with a cooldown. Without one, players will hold the key down and the scan becomes an always-on wallhack, which destroys the darkness the game depends on.
- The scan is a **client-side query for presentation**, with one server-authoritative exception described below. Highlighting an item the client already has a ghost for needs no round trip and should feel instantaneous.

**Define what is scannable**

- Add a small `IScannable` surface (or a marker component on the ghost) carrying: a category (`Loot` / `Exit` / `Extraction` / `Teammate` / `Hazard` / `Body`), a display label, an optional value, and a maximum scan range.
- Range must differ per category. The extraction point should be scannable from much further away than a piece of loot — it is the navigation anchor, and losing it is how a run ends badly.
- **Do not make monsters scannable.** A scanner that reveals threats replaces the audio-perception skill the whole threat layer is built around. If a monster-detection tool is wanted, it belongs in §5 as a purchasable, limited, noisy item with a real cost.
- Scanning through walls versus requiring line of sight is the single biggest tuning decision here. Recommended: **line of sight required for loot, ignored for the extraction point.** Loot scanning through walls turns the map into a checklist; exit scanning through walls prevents the unfun failure of dying twenty metres from the door.

**Report the value honestly — from the server**

- Total scanned value is the one part that must not be client-computed from client-visible data, because rolled item values are server-authoritative (see the Item Ghost component in §5) and a client that can read them all can trivially build a loot radar.
- Two options: replicate rolled value only on items within genuine scan range using ghost relevancy, or have the client send a scan request RPC and the server reply with an aggregate. The relevancy approach is cheaper per-scan and aligns with the bandwidth work in §13; pick it unless profiling says otherwise.
- Report the aggregate ("3 items · 240 credits") rather than per-item values on the HUD by default. It reads faster, and it keeps the player deciding about a *room* rather than pixel-hunting individual props.

**Render the highlights**

- Use the existing `VisualEffectManager` and `GhostVisualEffect` pipeline (`Assets/Scripts/Gameplay/VisualEffects/`) plus the URP renderer features already in place — `DepthNormalsFeature` and `FullScreenPassWrapper` are both present and are the natural mechanism for an outline or through-geometry marker.
- Highlights must fade out on a timer, not persist. A permanent overlay is a map, and a map removes the fear of a place.
- Follow the accessibility requirement in §9: highlight colours must be colourblind-safe and distinguishable by **shape or label as well as hue**, because category is the information, not the colour.
- Add a distinct, quiet scan sound through the existing `SoundSystem`. Decide deliberately whether it is loud enough for monsters to hear — a scanner that makes noise is a genuinely interesting cost, and it should be a design decision rather than an oversight. If it does make noise, it must go through the noise-emission system, not just the audio mixer.

**Make it work in the dark**

- Highlights must be legible with the power cut and no flashlight — that is the case where the scanner matters most.
- Verify against the Lighting & Power Grid component in §4: a scan during a blackout should still find the exit.

## Acceptance Criteria

- [ ] A scan input exists, is bound in `InputSystem_Actions`, flows through `PlayerInput.InputFlag`, and is rate-limited by a cooldown.
- [ ] Scanning highlights loot, exits, the extraction point, teammates, and bodies within their per-category ranges.
- [ ] Monsters are never revealed by the scanner.
- [ ] The line-of-sight rule is implemented as decided, and the extraction point remains findable through geometry.
- [ ] Scanned value is derived from server-authoritative rolled values; a modified client cannot enumerate item values outside scan range.
- [ ] The HUD reports an aggregate count and value that matches what was actually highlighted.
- [ ] Highlights fade after a fixed duration and never persist through the round.
- [ ] Highlight categories are distinguishable without relying on colour alone.
- [ ] Highlights are legible in total darkness and during a power cut.
- [ ] The scan sound either registers in the noise system or is documented as silent, deliberately.
- [ ] Scanning in a fully-populated location does not spike frame time; the query is bounded by range, not by total item count.
- [ ] Two players scanning simultaneously each see only their own highlights.
