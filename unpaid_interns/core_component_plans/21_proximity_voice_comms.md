# 21 — Proximity Voice / Comms

**Source:** [`core_components.md`](../core_components.md) §3 — Multiplayer & Team
**Status:** ❌ Not started — no voice package is installed
**Depends on:** Crew Roster, Noise Emission System (for the monster-hearing lever)
**Blocks:** the "split up" playstyle, the radio item, hub-bound roles

## Summary

Distance-attenuated voice chat, plus a radio item that beats the distance limit.

`GAME_DESIGN.md` describes a team scattered through an unfamiliar dangerous building deciding independently how much risk is worth it. That only works if they can talk, and it only produces the genre's characteristic panic if talking has a *range*. Voice that carries everywhere is a Discord call with a game attached; voice that fades as you walk away from your crew is a mechanic — it is what makes splitting up feel like a decision and reuniting feel like relief.

Two design levers make it more than plumbing:

1. **A radio as purchasable gear.** Long-range comms become something the crew spends quota money on, and losing the radio holder becomes a real loss.
2. **Monsters that hear voice.** Routing voice into the noise-emission system means shouting a warning can be the thing that gets you found. That is the best kind of horror mechanic — the correct action carries the risk.

Nothing exists. There is no voice package in `Packages/manifest.json`; `com.unity.services.multiplayer` 2.1.3 covers sessions, Relay, and Lobby, but not voice.

## How to Build

**Choose the transport — this is the decision, the rest is wiring**

- **Vivox** (`com.unity.services.vivox`) is the path of least resistance. It runs on the UGS project that is already linked (`cloudProjectId: bc8406a5-…` under `organizationId: nickinthreed`), supports 3D positional channels natively, and does not put voice traffic through Relay. It has its own dashboard enablement step and its own pricing, both of which need checking before committing.
- **Rolling voice over the netcode transport** is possible and is almost always a mistake. Ghost snapshots and voice have opposite requirements — voice wants low latency and tolerates loss, ghosts want reliability — and Relay bills on bandwidth, so voice would compete directly with the snapshot budget flagged in §13.
- Verify the platform surface first. Build profiles exist for `Windows Client` and `Android Client`; microphone permissions on Android are a runtime request, and a voice solution that has not been tested there will fail at exactly the wrong moment.
- **Decide before building any gameplay on it.** The radio item and the monster-hearing lever both depend on the transport exposing per-speaker position and per-speaker speaking state.

**Model channels, not connections**

- Two logical channels: **proximity** (3D, range-limited, everyone in the location) and **radio** (2D, all holders of a live radio).
- Which channels a player is in is server-authoritative and derived from crew state and held items. The client asks for nothing — it is told.
- The dead belong in their own channel. Spectators talking into the live channel removes all consequence from dying, which [`14_death_and_body_system.md`](14_death_and_body_system.md) is specifically built to create. Recommended: **the dead can hear the living but the living cannot hear the dead**, which preserves the spectator experience without letting a corpse call out monster positions. Coordinate with [`22_spectator_mode.md`](22_spectator_mode.md).
- Hub voice is unrestricted — no range limit, everyone hears everyone. The hub is the social state.

**Make position work**

- Positional voice needs the speaker's world position on the listener's machine every update. That already exists: player ghosts replicate transforms, and `MainCameraSingleton` carries the `AudioListener`.
- Update the voice system's listener pose from the same source the audio system uses, or spatialized voice will disagree with spatialized footsteps and both will feel wrong.
- Occlusion: §10 flags occlusion as a gameplay system rather than polish. Voice should be occluded by the same rules as other sound, or a teammate through a wall will be clearer than a monster in the same room.

**Route voice into the noise system**

- Publish a noise event while a player is transmitting on proximity voice, with volume derived from input level and range from the noise config. [`54_noise_emission_system.md`](54_noise_emission_system.md) consumes it exactly like footsteps, using the same position/range/volume shape — voice is simply the noisiest and most variable emitter, spanning a far wider range than any movement sound.
- **Raise the event on the server** from replicated speaking state, not on the speaking client. A client that suppresses its own noise event would be silently invisible to monsters.
- Radio transmission should produce noise at the *receiving* end too if the radio plays out loud — a squawking radio in a quiet corridor is a great way to die and a great story.
- Push-to-talk versus open mic changes this substantially: open mic means background noise constantly attracts monsters. Recommended default is push-to-talk with open mic available in settings, and the noise consequence applying identically to both.

**Handle the human factors**

- Per-player volume and mute, persisted locally. This is a co-op game played with strangers; mute is a safety feature, not a nicety.
- A visible speaking indicator on the HUD roster, which doubles as the answer to "is my mic working" — the most common support question in any game with voice.
- Microphone device selection belongs in the Settings menu (§9), which does not exist yet and is already blocking.
- Accessibility (§9): the deaf-and-hard-of-hearing requirement covers voice too. At minimum, a speaking indicator that shows *who* is talking and roughly where they are. Speech-to-text is out of scope but the indicator is not.

**Fail gracefully**

- Voice must be entirely optional. No microphone, denied permission, or a Vivox outage must degrade to a silent-but-playable game, never a failed join.
- Surface a clear status ("voice unavailable") rather than silence, or players will assume the game is broken.

## Acceptance Criteria

- [ ] The transport choice is made, documented in this file, and its dashboard/enablement and pricing implications are recorded.
- [ ] Proximity voice attenuates with distance and is inaudible beyond the configured range.
- [ ] Voice is spatialized consistently with the existing sound system, and occluded by the same rules.
- [ ] Radio holders can hear each other at any distance within the location; non-holders cannot.
- [ ] Losing or dropping the radio removes the player from the radio channel immediately.
- [ ] Dead players are in a separate channel per the documented rule, and cannot transmit to living players.
- [ ] Hub voice is unrestricted for all connected players.
- [ ] Channel membership is server-driven; a client cannot join a channel it was not assigned.
- [ ] Transmitting on proximity voice raises a noise event on the server that monsters can perceive.
- [ ] A client cannot suppress its own voice noise event.
- [ ] Per-player volume and mute work and persist across sessions.
- [ ] A speaking indicator shows who is talking, and is usable by a player with audio off.
- [ ] A player with no microphone or denied permission joins and plays normally, with a clear status message.
- [ ] Voice traffic does not measurably increase Relay bandwidth or ghost snapshot latency.
- [ ] Voice works in a standalone build on every configured build profile, including Android.
