# 74 — Terminal / Hub Interface

**Source:** [`core_components.md`](../core_components.md) §9 — UI & Feedback
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Hub State](04_hub_between_rounds_state.md), [Location Selection](27_location_selection_assignment.md), [Store / Purchasing](67_store_purchasing.md), [Quota & Deadline Display](72_quota_and_deadline_display.md)
**Blocks:** the between-rounds phase being playable at all

## Summary

The in-world computer where the crew decides what happens next.

Three separate systems need a surface — picking a destination ([`27_location_selection_assignment.md`](27_location_selection_assignment.md)), spending money ([`67_store_purchasing.md`](67_store_purchasing.md)), and reading the crew's position against the quota ([`72_quota_and_deadline_display.md`](72_quota_and_deadline_display.md)) — and all three are consulted together, in the same minute, by people making one combined decision. Splitting them across three screens means the crew answers a single question with three partial views and gets it wrong.

`core_components.md` argues for a **diegetic** implementation: an in-world computer rather than a menu, because *"diegetic supports the tone better"*. That is correct and it is also mechanically load-bearing. A terminal is a **physical object in a shared space**, which means only one person is at it, the others are watching over their shoulder, and the decision is made out loud. A menu each player opens privately produces four silent people and a destination that changes without discussion — which is exactly the small betrayal [`27_location_selection_assignment.md`](27_location_selection_assignment.md) warns about.

## How to Build

**Put it in the world and let one person drive**

- A physical terminal in the hub, entered through the normal interaction verb ([`41_interaction_system.md`](41_interaction_system.md)) and exited with a clear action.
- **One operator at a time.** Claim the terminal server-side using the same authority pattern as any other contended interactable ([`20_networked_interaction_authority.md`](20_networked_interaction_authority.md)) — an item ghost's claim rules generalise directly, and a second player attempting to use it gets a legible "in use by Priya" refusal.
- Everyone else can **see the screen**. Render the terminal's display in-world, not only to the operator, so the crew is looking at one shared surface. This is the single decision that makes the terminal social rather than administrative.
- The operator's own view can be a fuller, readable overlay; the shared in-world render is what keeps the others involved.

**Present the whole decision on one screen**

The destination list is the primary view, and per destination it must show what the crew needs to choose between ([`27_location_selection_assignment.md`](27_location_selection_assignment.md)):

- Difficulty tier, travel cost, rough loot expectation, known threats, and the current weather forecast ([`35_environmental_conditions_weather.md`](35_environmental_conditions_weather.md)).
- Locked destinations **visible and marked as locked**, because the expensive place you cannot afford is motivation.
- And on the same screen, always: **credits, quota shortfall, and days remaining.** Both component 27 and component 67 make the same argument independently — forcing players to carry a number across screens just makes them wrong.

Loot expectation must be **deliberately imprecise**. An exact expected-value figure turns the choice into arithmetic and removes the gamble, which is the thing being sold.

**Keep selection cheap and deploy expensive**

- [`27_location_selection_assignment.md`](27_location_selection_assignment.md) and [`04_hub_between_rounds_state.md`](04_hub_between_rounds_state.md) already agree on the rule: **any intern may change the destination, and it is free and reversible; deploy is a separate, deliberate, shared commitment.** The terminal must make that distinction obvious in its layout — selection is a list, departure is a different physical control.
- Show **who** changed the selection, in the terminal and in the action feed. A destination that silently changes while someone is shopping is the failure mode component 27 names.
- Travel cost is deducted at deploy, not at selection ([`27_location_selection_assignment.md`](27_location_selection_assignment.md)). Browsing must never cost credits, and the terminal must not imply otherwise.

**Write it as the employer, not as a UI**

- The tone is the cheapest content in the project. `GAME_DESIGN.md` describes an employer that treats interns as expendable labour, and every string here is an opportunity — enthusiastic upselling of safety equipment, a destination description that describes a lethal site as "a light retrieval assignment", a quota reminder that gets less friendly each day ([`72_quota_and_deadline_display.md`](72_quota_and_deadline_display.md)).
- Resist making it a command line for its own sake. The reference's typed-command terminal is characterful and it is also a barrier: a crew fumbling a text command under time pressure is comedy exactly once. **Recommended: navigable lists with a terminal aesthetic** — the look and voice without the typing.
- Keep every string in one table, per [`73_interaction_prompts.md`](73_interaction_prompts.md)'s argument, so deferring localisation stays cheap (§13).

**Build it in UI Toolkit alongside the existing screens**

- `Assets/UI Toolkit/GameUI/` already holds `MainMenu.uxml`, `PauseMenu.uxml`, `PlayerHUD.uxml` and the rest, with `MainMenu.cs` and `PauseMenu.cs` demonstrating the pattern. Follow it rather than introducing a second UI system.
- Rendering a `UIDocument` onto an in-world surface is the piece that is not yet done anywhere in the project — prototype it early, because "the terminal is a world-space screen" is an assumption several plans now rest on.
- Hide it outside the hub. `GlobalGameState.Hub` does not exist yet ([`04_hub_between_rounds_state.md`](04_hub_between_rounds_state.md) adds it), and that file already requires auditing every `GameState` consumer — this is a new one and should use the declarative display-style binding pattern the existing screens use.

**Validate everything on the server**

- The terminal sends requests; the server decides. Selection validity, affordability, phase, storage capacity, and unlock state are all checked server-side, per components 27 and 67. A modified client operating a terminal must achieve nothing it could not achieve legitimately.
- Every refusal is explained specifically — the same rule as interaction prompts. "Insufficient credits" and "destination locked" are different problems and the crew should not have to guess which.

**Leave room for what plugs in later**

- The monitoring/camera system (§9) and remote hazard control ([`62_hazard_control_remote_disable.md`](62_hazard_control_remote_disable.md)) are both terminal-adjacent and are what make the stay-behind role real. Build the terminal as **tabbed views over a shared frame** so those become additional views rather than a second in-world computer.
- Terminal-controlled doors ([`60_door_system.md`](60_door_system.md)) target the same request path as hazard control. One authority check, two target types.

## Acceptance Criteria

- [ ] The terminal is a physical object in the hub, entered and exited through the normal interaction verb.
- [ ] Only one player may operate it at a time, enforced server-side, with a legible refusal naming the current operator.
- [ ] The terminal display is visible in-world to every player in the hub, not only to the operator.
- [ ] The destination list shows tier, travel cost, imprecise loot expectation, known threats, and weather forecast per destination.
- [ ] Locked destinations are visible and clearly marked.
- [ ] Credits, quota shortfall, and days remaining are shown on the same screen as the destination list and the store.
- [ ] No exact expected-value figure is displayed for any destination.
- [ ] Changing the destination is free, reversible, and attributed to the player who changed it.
- [ ] Travel cost is deducted at deploy, never at selection.
- [ ] Deploy is a separate physical control, visually and spatially distinct from selection.
- [ ] The store is a view within the terminal, not a separate screen.
- [ ] All requests are server-validated for phase, affordability, unlock state, and storage capacity; a forged request achieves nothing.
- [ ] Every refusal states a specific reason.
- [ ] The terminal is built in UI Toolkit alongside the existing screens and rendered to a world-space surface.
- [ ] The terminal is hidden and inoperable outside the hub, using the declarative state-binding pattern.
- [ ] All terminal strings live in one table.
- [ ] The interface is structured as tabbed views so monitoring and remote control can be added without a second terminal.
- [ ] Text is legible at the in-world viewing distance and at reduced UI scale, and conveys no state by colour alone.
- [ ] A client joining while the crew is in the hub sees correct terminal state, never defaults.
