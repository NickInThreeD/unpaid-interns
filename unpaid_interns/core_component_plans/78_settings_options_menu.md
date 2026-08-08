# 78 — Settings / Options Menu

**Source:** [`core_components.md`](../core_components.md) §9 — UI & Feedback
**Status:** ❌ Not started — **there is no options screen of any kind** · **[MVP]**
**Depends on:** [Session Persistence](06_session_persistence.md) (local settings slot)
**Blocks:** Accessibility, HUD scaling, interaction prompt key display, shipping at all

## Summary

The screen the project does not have.

`core_components.md` is unusually blunt about this one, and the audit confirms it: there is **no `Settings.uxml`** among the UI Toolkit assets, and mouse sensitivity is a hardcoded `const float sensitivity = 3.7f` at `ClientInputReaderSystem.cs:78`. Not a serialized field, not a config asset — a compile-time constant inside the input reader's `OnUpdate`.

That single line is the whole component in miniature. A player whose mouse feels wrong cannot fix it, cannot play well, and will not stay. Sensitivity, invert-Y, volume, and FOV are not features; they are the conditions under which a player can use the game at all, and their absence is the kind of omission that gets discovered in a public playtest rather than in development, because the developers' own settings happen to be the defaults.

It is also **blocking other components**. [`71_hud.md`](71_hud.md) needs HUD scale, [`73_interaction_prompts.md`](73_interaction_prompts.md) needs to display the bound key rather than a hardcoded "E", [`15_fear_and_stress_feedback.md`](15_fear_and_stress_feedback.md) needs a fear-intensity slider that reaches zero, and [`79_accessibility.md`](79_accessibility.md) needs somewhere for nearly all of its requirements to live. Several plans already carry acceptance criteria that cannot be met until this exists.

## How to Build

**Start by removing the hardcoded values**

- `ClientInputReaderSystem.cs:78` — `const float sensitivity = 3.7f` becomes a read from a settings service. It is applied inside the input read, so the value must be available to an ECS system, which means a blittable settings singleton rather than a managed object reference.
- Audit for the rest before building the UI. Any tuning constant a player would reasonably expect to change — look sensitivity, FOV, volumes, head-bob — should be found and routed through the same service first, so the menu is wiring up existing plumbing rather than inventing it per setting.
- `RESPAWN_DURATION = 5.0f` in `RespawnScreen.cs` and `PendingRespawn { RespawnTimer = 5f }` in `ServerGameSystem` are a **different** problem — duplicated gameplay constants, not settings — and [`14_death_and_body_system.md`](14_death_and_body_system.md) removes them. Do not sweep them in here.

**Ship the required set, not an exhaustive one**

- **Controls** — look sensitivity (separate X/Y if cheap), invert-Y, and **input rebinding**. The Input System supports rebinding and `InputSystem_Actions` is already generated with the actions in place; nothing exposes it. Rebinding is also an accessibility requirement, not a convenience.
- **Audio** — a slider per mixer group. `SoundMixer` and the `SoundSystem` already route through groups, so this is wiring rather than new work, and `SaveGameData` in the shared SaveSystem already carries `musicVolume` and `sfxVolume` fields ([`06_session_persistence.md`](06_session_persistence.md)).
- **Video** — resolution, fullscreen mode, quality level, **FOV**, and a **brightness/gamma** control. [`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md) requires brightness to be honoured by the blackout state, because "unplayably dark on my monitor" is this genre's most common complaint.
- **Gameplay/comfort** — head-bob reduction, fear overlay intensity from 0–100%, HUD scale, and a hold-versus-toggle choice for crouch ([`10_crouch.md`](10_crouch.md) recommends offering both).
- **Accessibility** — subtitles, visual sound indicators, colourblind-safe palettes. [`79_accessibility.md`](79_accessibility.md) owns what these do; this component owns the surface they are set from.

**Get persistence right, and keep it separate from the run**

- Settings are **per-client and local**. They are not part of the run save and they must survive a failed run, per [`06_session_persistence.md`](06_session_persistence.md): *"settings are per-client and belong in a separate local slot, not in the run save."*
- That separation matters more than it sounds. [`07_game_over_win_resolution.md`](07_game_over_win_resolution.md) deletes the run save on failure, and a player who loses their mouse sensitivity every time the crew misses quota will assume the game is broken.
- A pure client must be able to change settings without a server world. The menu cannot depend on being connected.
- Apply immediately on change, not on a confirm button — except for resolution changes, which need the standard revert-after-timeout confirmation so an unsupported mode does not strand someone.

**Make it reachable from both places**

- From the main menu and from the pause menu. `MainMenu.cs` and `PauseMenu.cs` both exist and both use the UI Toolkit pattern with `[CreateProperty]` display-style bindings; add one screen used from both rather than two.
- The pause menu route is the one that matters most — a player discovers their sensitivity is wrong thirty seconds into a round, not in the menu.
- [`81_pause_semantics_in_multiplayer.md`](81_pause_semantics_in_multiplayer.md) is the constraint here: **the world keeps running while the menu is open.** A player adjusting settings mid-round is standing still in a dangerous building, and the screen must not imply safety. Keep it dismissable in one key.

**Do not defer the input-rebinding half**

- It is the piece most often postponed and it is the one with the strongest accessibility case — a player who cannot use the default bindings cannot play at all.
- The Input System's rebinding API works against the same `InputSystem_Actions` asset the game already reads. Persist overrides as the JSON the API produces, in the local settings slot.
- Rebinding must not be able to leave the player unable to open the pause menu. Reserve a binding, or provide a reset-to-defaults that is reachable from the main menu.

**Keep the strings and the plumbing tidy**

- One settings service, read by both ECS systems and MonoBehaviours — the same ECS-boundary problem [`54_noise_emission_system.md`](54_noise_emission_system.md) describes. A blittable singleton for the values ECS needs, with managed access on the MonoBehaviour side.
- All labels in the shared string table ([`73_interaction_prompts.md`](73_interaction_prompts.md)), because a settings menu is the second-densest string surface after prompts and is the one localisation always starts with (§13).
- Build `Settings.uxml` alongside the existing screens in `Assets/UI Toolkit/GameUI/`.

## Acceptance Criteria

- [ ] `const float sensitivity = 3.7f` is removed from `ClientInputReaderSystem` and read from the settings service.
- [ ] A repo audit has routed every player-facing tuning constant through the settings service.
- [ ] Look sensitivity and invert-Y are configurable and take effect immediately.
- [ ] Full input rebinding is available and persists across sessions.
- [ ] Rebinding cannot leave a player unable to open the pause menu, and reset-to-defaults is reachable from the main menu.
- [ ] A volume slider exists per mixer group and affects the running game immediately.
- [ ] Resolution, fullscreen mode, quality, FOV, and brightness/gamma are all configurable.
- [ ] Brightness is honoured by the facility blackout state.
- [ ] Head-bob reduction, fear overlay intensity from 0–100%, HUD scale, and crouch hold/toggle are all exposed.
- [ ] Fear intensity at 0% fully disables the overlay and the non-diegetic tone.
- [ ] Settings persist locally, per client, and survive a failed run and a deleted run save.
- [ ] A client with no server world can open and change settings.
- [ ] Changes apply immediately, except resolution, which uses a revert-after-timeout confirmation.
- [ ] The menu is reachable from both the main menu and the pause menu, from one shared screen.
- [ ] The menu does not imply the world is paused, and is dismissable in one key.
- [ ] Settings are readable by both ECS systems and MonoBehaviours through one service.
- [ ] All labels live in the shared string table.
- [ ] `Settings.uxml` exists alongside the other UI Toolkit screens and works in a standalone build.
- [ ] The menu is usable on every configured build profile, including Android.
