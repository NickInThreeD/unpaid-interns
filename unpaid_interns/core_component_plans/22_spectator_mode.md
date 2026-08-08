# 22 — Spectator Mode

**Source:** [`core_components.md`](../core_components.md) §3 — Multiplayer & Team
**Status:** ⚠️ Respawn screen exists, spectator camera does not · **[MVP]**
**Depends on:** Crew Roster, Death & Body System
**Blocks:** permanent-for-the-round death, mid-round join handling, vote-to-leave

## Summary

Something for dead players to do.

This is not a comfort feature — it is a **prerequisite for the death rework**. [`14_death_and_body_system.md`](14_death_and_body_system.md) makes death permanent for the round, which means a player who dies two minutes into a ten-minute round has eight minutes of nothing. Without spectating, the correct play for a dead intern is to alt-tab, and a co-op horror game where a quarter of the crew is in a browser has lost.

Spectating also keeps dead players *invested*. Watching the survivors decide whether to come back for your body is the payoff of the entire death system, and it only exists if you can see it happening.

What is there today is the deathmatch remnant. `Assets/Scripts/Gameplay/UI/RespawnScreen.cs` detects death by the absence of a local `PredictedPlayerGhost` singleton, enables a `RespawnCamera` GameObject, and counts down a hardcoded `RESPAWN_DURATION = 5.0f` to match `ServerGameSystem`'s respawn timer. The camera-swap plumbing is reusable; the countdown and its premise are not.

## How to Build

**Fix the camera ownership**

- `MainCameraSystem` runs in `PresentationSystemGroup` and drives `MainCameraSingleton.Instance` from the `MainCamera` singleton entity, which lives on the local player. When the player entity is destroyed that singleton disappears, `RequireForUpdate<MainCamera>()` stops the system, and the camera is left wherever it was — which is why the current code needs a whole second camera.
- Prefer keeping **one camera**. Rather than enabling a separate `RespawnCamera`, give the spectator controller a `MainCamera` component on its own entity so `MainCameraSystem` keeps driving the same camera with no special case. One camera means one `AudioListener`, and two active listeners is a real bug that produces doubled or dead audio.
- Whichever approach is chosen, verify the `AudioListener` follows the spectated position — spectating with your ears still at your corpse is disorienting and breaks the audio-first threat design in §10.

**Do not keep the player entity alive**

- The tempting shortcut is to leave the dead player's character in the world with input disabled. Resist it: a live character still collides, still occupies a spawn slot, still replicates a full predicted ghost, and monsters will target it.
- Instead, on death the server marks the roster entry `Dead` ([`19_crew_roster.md`](19_crew_roster.md)) and the client switches to a local spectator controller. Spectating is a **client presentation state**, not a networked pawn.
- The one piece of server state needed is *who* a spectator is following, and only if follow-cam is implemented as a shared feature. If it is purely local, the server needs nothing beyond the roster flag.

**Build the two camera modes**

- **Follow** — attach to a living crewmate, cycle with a key. This is the default and the one that keeps players engaged, because it is a story rather than a view.
- **Free** — detached flight through the location. Useful, and a cheating vector: a free spectator can scout the map and call out monster positions on voice. Coordinate with [`21_proximity_voice_comms.md`](21_proximity_voice_comms.md)'s rule that the living cannot hear the dead. If that rule is not adopted, free-cam must be cut or restricted.
- Constrain free-cam to the location bounds so spectators cannot fly out of the level and see the generator's seams. Use the bounds volume defined in [`34_out_of_bounds_handling.md`](34_out_of_bounds_handling.md) rather than a second definition — that component computes the assembled interior's real extent inside the load barrier, which a spectator constraint authored before generation cannot know.
- Handle the case where **nobody is alive to follow**: the round is ending via the total-crew-loss path, so hold the last position and show the round-end state rather than dropping to a null camera.

**Replace the respawn screen**

- Repurpose `RespawnScreen.cs` rather than adding a parallel screen: it already resolves the client world, builds the local-player query, and hides itself when `GameSettings.Instance.GameState != GlobalGameState.InGame`.
- Delete `RESPAWN_DURATION` and the countdown label entirely. A countdown that never completes is worse than no countdown.
- Show instead: who you are following, how many crew are still alive, quota progress, and time remaining — the dead player's remaining stake in the round.
- Note the state check will need updating once `GlobalGameState.Hub` exists ([`04_hub_between_rounds_state.md`](04_hub_between_rounds_state.md)); spectating must not persist into the hub.

**Give spectators something to do**

- Optional but high value: let dead players see and ping the map, or use the monitoring/camera system in §9. A dead intern who can still call out "the exit is west of you" is participating.
- The design's vote-to-leave-early hook is now settled, and settled this file's way: [`105_departure_and_extraction_resolution.md`](105_departure_and_extraction_resolution.md) records that **the dead get no vote and no departure control**, for the reason given here — they have lost their stake and the living carry the risk. That component also explains why the reference design's spectator vote is not needed: any living intern can start departure alone, and any living intern can abort it, so a crew is never held hostage by one person's indecision.
- What the dead *should* get instead is visibility into the decision: who is still alive, where they are, and — once departure starts — the countdown. Watching the timer while your surviving crewmate is three rooms too deep is a far better spectator experience than a vote button.

**Clear it properly**

- On round end, every spectator returns to a normal player in the hub, per the death system's rule that death is permanent for the round, not the run.
- Verify no spectator state, camera, or input mapping leaks into the next round — this is the most likely failure and the easiest to miss, because it only shows on the second round.

## Acceptance Criteria

- [ ] A player who dies enters spectator mode immediately, with a working camera and no black screen.
- [ ] Exactly one `AudioListener` is active at all times, and it is positioned at the spectated view.
- [ ] Follow mode attaches to a living crewmate and cycles between them.
- [ ] Free mode moves smoothly and cannot leave the location bounds.
- [ ] With no living crewmates, the camera holds a valid position and the round-end path proceeds normally.
- [ ] The dead player's character entity is destroyed; monsters do not target it and it occupies no spawn point.
- [ ] Spectators cannot influence the world — no interaction, no damage, no noise events.
- [ ] The voice rule for dead players is implemented consistently with the free-cam decision.
- [ ] Dead players cannot start, abort, or vote on departure, and the departure countdown is visible to them.
- [ ] The respawn countdown is gone, and the screen shows crew alive, quota progress, and time remaining.
- [ ] Spectator UI hides in the hub and in the main menu.
- [ ] All spectators return to playable characters at the start of the next round.
- [ ] No spectator camera, input mapping, or UI state leaks into the following round, verified across three consecutive rounds.
- [ ] A player who disconnects while spectating and returns is handled per [`25_reconnection.md`](25_reconnection.md) rather than spawning alive.
