# 72 — Quota & Deadline Display

**Source:** [`core_components.md`](../core_components.md) §9 — UI & Feedback
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Quota System](64_quota_system.md), [Shared Session State Sync](23_shared_session_state_sync.md), [HUD](71_hud.md)
**Blocks:** the crew being able to answer "is this trip necessary?"

## Summary

Making the number that kills everyone visible enough to act on.

`core_components.md` gives the requirement as a question the player must be able to answer at a glance: *"is this trip necessary?"* That is the decision the whole game is built around — `GAME_DESIGN.md` puts the central tension in *"how long to stay"* — and it is not a decision anyone can make against a number they have to go and look up.

This is a small presentation component with one genuinely important design job: choosing **what to show and where**, such that the pressure is constant without the game becoming a spreadsheet. The quota is the source of dread; a dread you have to open a menu to feel is not doing its work, and a dread rendered as a live progress bar in the corner stops being dread and becomes a progress bar.

## How to Build

**Show the shortfall, not the fraction**

- Display **how much more is needed**, not progress against a total. "410 short" is actionable. "190 / 600" requires arithmetic performed by someone who is being chased, and they will get it wrong.
- Pair it with days remaining, always. Either number alone is meaningless: 410 short is comfortable with three days and fatal with none.
- [`64_quota_system.md`](64_quota_system.md) supplies both as cheap, never-stale values for exactly this reason.
- Both come from replicated state and are never computed client-side ([`23_shared_session_state_sync.md`](23_shared_session_state_sync.md)). A crew whose members disagree about the shortfall makes four different decisions.

**Split it across two contexts, because they are different questions**

- **In the hub**, the crew is planning: show the full picture. Shortfall, days remaining, current credits, stored value, and the cost of the destination they are considering — all on one screen ([`74_terminal_hub_interface.md`](74_terminal_hub_interface.md), [`27_location_selection_assignment.md`](27_location_selection_assignment.md)). This is where arithmetic is appropriate, because nothing is chasing them.
- **In a location**, the crew is deciding whether to take another trip: show the minimum that answers it. Shortfall and this round's banked total, small and peripheral.
- The number that matters mid-round is actually **"how much have we banked today"** measured against the shortfall — that is what makes "one more trip" a calculable risk. [`43_loot_banking_deposit.md`](43_loot_banking_deposit.md) already requires banking to reveal value and update the running total on the spot; this component is where that total lives on screen.

**Escalate the presentation as the deadline approaches**

- The display should feel different on the last day than on the first. Colour, size, and audio all work, and this is the cheapest atmosphere in the project.
- The premise supplies the tone. An employer whose messaging becomes progressively less friendly as the deadline nears — encouraging, then pointed, then openly threatening — is free comedy and free tension in the same element ([`64_quota_system.md`](64_quota_system.md) already flags this).
- Fire the transitions off the round clock's phase boundaries where they are in-round ([`03_round_timer_clock.md`](03_round_timer_clock.md)) and off the day counter where they are per-cycle.
- Do not let escalation become noise. A display that flashes constantly on the final day is a display players learn to ignore, which is the opposite of the intent.

**Be honest about what has and has not been earned**

- Banked-but-unsold value and sold value are different things, and conflating them will get a crew killed. If the sell-rate curve is adopted ([`65_selling_payout.md`](65_selling_payout.md)), a haul worth 500 gross might be 150 in credits — showing the gross as quota progress is a lie the crew will discover at settlement.
- Show gross banked value **and** what it is currently worth, whenever the two differ.
- Never show a projected total as if it were real. [`63_currency_system.md`](63_currency_system.md) requires derived previews to be visibly derived, and this is the display most tempted to violate it.

**Make it work for the players who cannot act on it**

- Spectators need it: [`22_spectator_mode.md`](22_spectator_mode.md) lists quota progress as one of the four things a dead player should see, because it is their remaining stake in the round.
- A crew member in the hub while others deploy ([`62_hazard_control_remote_disable.md`](62_hazard_control_remote_disable.md)) needs the full hub view, not the field view.
- Accessibility (§9): the escalation must not be conveyed by colour alone, and the display must remain legible at reduced HUD scale.

**Keep it in the reserved region**

- [`71_hud.md`](71_hud.md) requires named HUD regions precisely so elements like this do not collide with interaction prompts and scan results. Claim a corner and stay in it.
- No per-frame allocation, and no writes when the value has not changed — the shortfall changes a handful of times per round.

## Acceptance Criteria

- [ ] The display shows the shortfall in absolute terms, not a fraction or a percentage.
- [ ] Days remaining is always shown alongside the shortfall.
- [ ] Both values come from replicated state and are identical on every client.
- [ ] The hub view shows shortfall, days remaining, credits, stored value, and prospective travel cost together on one screen.
- [ ] The in-location view shows the shortfall and the round's banked total, and nothing else.
- [ ] Banking an item updates the displayed round total immediately.
- [ ] Where gross banked value and its current sale worth differ, both are shown.
- [ ] No projected or derived figure is presented as an actual balance or as actual progress.
- [ ] Presentation escalates measurably as the deadline approaches, in more than one channel.
- [ ] Escalation does not become constant visual noise on the final day.
- [ ] Escalation is not conveyed by colour alone.
- [ ] Spectators see quota progress and days remaining.
- [ ] A hub-bound player sees the hub view while the crew is deployed.
- [ ] The display occupies its reserved HUD region and never overlaps interaction prompts or scan results.
- [ ] No per-frame allocation, and no element writes when values are unchanged.
- [ ] The display is legible at reduced HUD scale and at the lowest supported resolution.
- [ ] A client joining mid-round shows correct values, never zeros or a stale shortfall.
