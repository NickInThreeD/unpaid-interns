# 71 — HUD

**Source:** [`core_components.md`](../core_components.md) §9 — UI & Feedback
**Status:** ⚠️ Exists, built for a shooter · **[MVP]**
**Depends on:** [Stamina](11_stamina.md), [Carry Weight](12_carry_weight.md), [Inventory](40_inventory_item_bar.md), [Round Timer](03_round_timer_clock.md)
**Blocks:** every decision the player makes while in a location

## Summary

The four or five things a player needs to know without stopping.

`InGameHUD.cs` already works and already uses the right pattern. It resolves the client world, builds an `EntityQuery` over `PredictedPlayerGhost` + `GhostOwnerIsLocal`, and drives UI Toolkit elements found by name from `PlayerHUD.uxml` — health bar, ammo bar and label, reloading label, and a reticle with four class-swapped styles. That ECS-query-from-a-`UIDocument` approach is exactly what new elements should extend.

What is wrong is the **priority**. It is a shooter HUD: ammo and a reticle are the largest, most prominent things on screen, and they are the two least relevant pieces of information in a game where players spawn empty-handed and weapons are rare defensive tools ([`45_weapons_as_tools.md`](45_weapons_as_tools.md)). The information a player actually needs — how much can I still carry, how tired am I, how late is it, what am I holding — is entirely absent.

The design constraint that shapes everything here is that this is a **horror game**, and a HUD that answers every question removes the uncertainty the tension depends on. [`03_round_timer_clock.md`](03_round_timer_clock.md) already suggests the clock might not be always visible; [`11_stamina.md`](11_stamina.md) suggests deliberately under-representing the true stamina value. Those are not decorative choices — the HUD is where the game decides how much it is willing to tell you.

## How to Build

**Re-rank what is on screen**

In rough order of how often a player needs it:

- **Item bar** — what is carried, what is selected, how many slots are free. The most-consulted element in the game and currently absent ([`40_inventory_item_bar.md`](40_inventory_item_bar.md)).
- **Stamina** — gates sprinting and jumping, changes second to second, and is the number a fleeing player is actually reading ([`11_stamina.md`](11_stamina.md)).
- **Health and injury state** — health exists; the injured state does not, and a player who cannot sprint needs to know *why* ([`13_health_and_injury.md`](13_health_and_injury.md)).
- **Carry weight or encumbrance** — required by [`12_carry_weight.md`](12_carry_weight.md), because value-per-pound decisions cannot be made against a hidden number.
- **Time of day** — subject to the visibility decision below.
- **Held item context** — ammo when holding a weapon, charge when holding a tool ([`44_tool_and_equipment_items.md`](44_tool_and_equipment_items.md)).
- **Interaction prompt** — its own component ([`73_interaction_prompts.md`](73_interaction_prompts.md)), but it lives in this space and must not collide with anything above.

Ammo and reticle become **conditional**, appearing only while a weapon is held, as §9 requires. That is a small edit to `InGameHUD.LateUpdate` and it changes the screen's whole character.

**Decide what the HUD refuses to tell you**

- The quota and deadline are the crew's central pressure and get their own persistent treatment ([`72_quota_and_deadline_display.md`](72_quota_and_deadline_display.md)) — but *inside a location*, consider whether the full figure belongs on screen at all times or only on demand.
- The clock is the sharpest version of this question. An always-visible countdown converts dread into arithmetic. **Recommended: readable on demand or in specific conditions** — outdoors, or by looking at a wrist device — rather than permanently displayed.
- Stamina should be a bar, not a number. A player who can see `0.34` will optimise against it; a player watching a bar drain will panic, which is correct.
- Whatever is hidden must be **discoverable on demand**. Hidden and unavailable are different things, and the second is just missing information.

**Keep it cheap and allocation-free**

- `InGameHUD.LateUpdate` currently queries every frame. That is fine for four elements and will not stay fine — cache the query, cache element references (it already does in `OnEnable`), and only touch a `VisualElement` when its value has actually changed. UI Toolkit style writes are not free.
- Several plans already carry a no-per-frame-allocation criterion for their HUD element ([`03_round_timer_clock.md`](03_round_timer_clock.md), [`11_stamina.md`](11_stamina.md), [`40_inventory_item_bar.md`](40_inventory_item_bar.md)). Enforce it centrally here rather than four times.
- Guard against the local player not existing. `RespawnScreen.cs` already detects death by the **absence** of a local `PredictedPlayerGhost` singleton, and once death is permanent for the round ([`14_death_and_body_system.md`](14_death_and_body_system.md)) the HUD must hide cleanly rather than throwing or freezing on stale values.

**Handle the states the HUD lives in**

- **In a location** — the full field HUD.
- **In the hub** — most of it is meaningless. `GlobalGameState.Hub` does not exist yet ([`04_hub_between_rounds_state.md`](04_hub_between_rounds_state.md) adds it) and `InGameHUD` is one of the consumers that file flags as assuming `InGame` means "in a dangerous place". Hide the field HUD, show the crew's status.
- **Spectating** — [`22_spectator_mode.md`](22_spectator_mode.md) specifies what a dead player sees: who they are following, crew alive, quota progress, time remaining. That is a different layout, not the field HUD with elements missing.
- **Loading** — show an explicit loading state, never zeros. [`23_shared_session_state_sync.md`](23_shared_session_state_sync.md) requires this for the window between connecting and the ghost linking, and a player who reads "credits: 0" for two seconds believes it.

**Respect the accessibility requirements**

- §9 elevates accessibility to required, and the HUD is where most of it lands. No element may convey its state by **colour alone** — the health bar's `k_HealthBarColor`, injury state, slot selection, and the reticle's four styles all need shape, position, or text as well.
- Scalable HUD size and a configurable safe-area margin. Both are settings, and there is no settings menu yet ([`78_settings_options_menu.md`](78_settings_options_menu.md) is blocking several components including this one).
- The fear overlay must never obscure health, stamina, or the item bar — [`15_fear_and_stress_feedback.md`](15_fear_and_stress_feedback.md) already makes this an acceptance criterion, and this is the component that has to reserve the space for it.

**Build it as one layout, not as accreted elements**

- `PlayerHUD.uxml` was authored for four elements and will not survive being extended eight more times by addition. Re-lay it out once, with named regions — bottom-centre for the item bar, corners for status, centre for reticle and prompt — and let each component fill its region.
- That also solves the collision problem: interaction prompts, hands-full warnings, and scan results all want the centre of the screen, and without reserved regions they will overlap in exactly the moment they matter.

## Acceptance Criteria

- [ ] The item bar, stamina, health with injury state, encumbrance, and held-item context are all present in the field HUD.
- [ ] Ammo and reticle appear only while a weapon is held.
- [ ] `PlayerHUD.uxml` is re-laid out with named regions, and each element occupies a reserved region without overlapping another.
- [ ] The clock's visibility rule is implemented as decided and documented in this file.
- [ ] Any information deliberately hidden is available on demand.
- [ ] Stamina is presented as a bar, not a numeric value.
- [ ] The HUD performs no per-frame allocation and writes to a `VisualElement` only when its value changes.
- [ ] The HUD hides cleanly when the local player entity does not exist, with no exceptions or stale values.
- [ ] The field HUD is hidden in the hub, and hub status is shown instead.
- [ ] Spectator mode shows its own layout, not the field HUD with gaps.
- [ ] An explicit loading state is shown between connecting and ghost link; zeros are never displayed as real values.
- [ ] No HUD element conveys its state by colour alone.
- [ ] HUD scale and safe-area margin are configurable once the settings menu exists.
- [ ] The fear overlay never obscures health, stamina, or the item bar.
- [ ] Every element reflects replicated or predicted state and never caches a value across frames.
- [ ] The HUD reads correctly at the lowest supported resolution and on the Android build profile.
- [ ] A full field HUD with every element active holds the client frame budget on the lowest-spec target.
