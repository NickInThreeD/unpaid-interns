# 76 — End-of-Round Summary

**Source:** [`core_components.md`](../core_components.md) §9 — UI & Feedback
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Selling / Payout](65_selling_payout.md), [Bonus & Penalty Rules](66_bonus_and_penalty_rules.md), [Crew Roster](19_crew_roster.md), [Quota System](64_quota_system.md)
**Blocks:** the round's decisions being legible in hindsight

## Summary

What we banked, what it paid, who came back, and how much further we have to go.

`core_components.md` calls it *"the moment the round's decisions get judged"*, and the important word is **decisions**. The crew spent ten minutes making choices — go deeper or leave, take the heavy thing or two light ones, go back for the body or don't — and this screen is where those choices resolve into a number. Without it, a round ends with the balance quietly changing and nobody learns anything.

It also carries a hard correctness requirement that most UI does not: **every figure must reconcile exactly with the credited amount.** [`65_selling_payout.md`](65_selling_payout.md) and [`66_bonus_and_penalty_rules.md`](66_bonus_and_penalty_rules.md) both make this an acceptance criterion for the same reason — in a game where the payout decides whether the crew survives, a crew that cannot square the summary with their balance will conclude the economy is cheating them, and that conclusion ends sessions rather than rounds.

**Scope boundary:** this is the *arithmetic* — what was banked, what it sold for, what it cost, where that leaves the quota. The **grade and the per-intern notes** are [`70_performance_report.md`](70_performance_report.md). In practice they are one screen with two sections; this file owns the ledger half, and it is the MVP half.

## How to Build

**Show the ledger in the order it happened**

- Gross banked value → sell rate if any → bonuses → penalties → net credited. That is the order [`66_bonus_and_penalty_rules.md`](66_bonus_and_penalty_rules.md) fixes, and the summary must present it in the same order it was computed. A screen that shows the same numbers in a different sequence invites the crew to recompute it wrongly and conclude there is a bug.
- Itemise the haul, or at least group it: a crew wants to know that the thing someone died carrying was worth 340. Per-item value is available at settlement and cannot be recovered later.
- Show each penalty with its cause named — *"recovery of asset, J. Fournier — not recovered"*. [`66_bonus_and_penalty_rules.md`](66_bonus_and_penalty_rules.md) argues the callous corporate register is what stops that line from reading as an accusation, and it is the funniest thing on the screen.
- End with the two numbers that drive the next decision: **new balance** and **remaining shortfall with days left** ([`72_quota_and_deadline_display.md`](72_quota_and_deadline_display.md)).

**Report survival honestly and distinctly**

- Per intern: survived, died and was recovered, died and was left behind, disconnected. The crew roster distinguishes all four ([`19_crew_roster.md`](19_crew_roster.md)) and collapsing them loses information the crew cares about — "left behind" is a different feeling from "recovered", and it is a different number.
- A disconnected player must read as **disconnected, not dead**. [`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md) decides they incur no death penalty, and a summary that lists them among the dead contradicts the ledger sitting directly above it.
- Per-player banked value comes from the bank-time attribution [`43_loot_banking_deposit.md`](43_loot_banking_deposit.md) records. It cannot be reconstructed after the fact, so if that attribution was skipped, this section cannot exist.

**Get the timing and the state right**

- Show it after settlement completes and before the hub becomes playable — the round is closed before the next decision starts ([`02_day_cycle_controller.md`](02_day_cycle_controller.md) settles, then advances the day; the summary sits between).
- **Every client sees the same summary**, driven from replicated state. A client whose snapshot arrives late shows a loading state rather than zeros, per [`23_shared_session_state_sync.md`](23_shared_session_state_sync.md) — and zeros here would read as "we earned nothing", which is a genuinely alarming thing to show someone falsely.
- Dead and spectating players see it. They have the largest stake in how the round resolved, and [`22_spectator_mode.md`](22_spectator_mode.md) already gives them quota progress for the same reason.
- **Do not block on every player dismissing it.** A ready-check holds four people hostage to one person reading slowly; a dismissable screen with a timeout is kinder and avoids a hang when someone has alt-tabbed.

**Build it as one screen with the report**

- [`70_performance_report.md`](70_performance_report.md) reuses `LeaderboardUi`'s `ListView` and `ScoreItem.uxml` binding. This component supplies the ledger section above those rows.
- Layout order: payout ledger, then quota position, then per-intern rows with status, then the grade and notes. Money first, because money is what the crew is asking about; the joke lands better after the arithmetic.
- Follow the accessibility requirement in §9 — this is the densest text in the game and the least excusable place for a colour-only status indicator. Survival status in particular must be readable in monochrome.

**Make it verifiable, not just presentable**

- Add a development-only assertion that the displayed net equals the actual change in `TeamCredits` for the round, and that the itemised lines sum to the displayed gross. [`63_currency_system.md`](63_currency_system.md) already logs every mutation with a reason enum — this screen is that log, rendered, and the assertion is what keeps the two honest.
- If they ever disagree, the log wins and the mismatch must be loud in development. A silently wrong summary is worse than no summary, because it teaches the crew a false model of the economy.
- Two consecutive rounds must produce independent summaries with nothing carried over — the standard per-round teardown check.

## Acceptance Criteria

- [ ] The ledger shows gross banked value, sell rate if any, each bonus, each penalty, and the net credited, in the order they were computed.
- [ ] The haul is itemised or grouped, with per-item values shown.
- [ ] Each penalty names its cause and the intern it relates to.
- [ ] The screen ends with the new balance, remaining shortfall, and days remaining.
- [ ] Survival status distinguishes survived, recovered death, left-behind death, and disconnected.
- [ ] A disconnected player is never listed as dead, consistent with the penalty ledger.
- [ ] Per-player banked value is shown and matches the settlement total.
- [ ] The summary appears after settlement and before the hub becomes playable.
- [ ] Every client sees identical figures, and a lagging client shows a loading state rather than zeros.
- [ ] Dead and spectating players see the summary.
- [ ] The screen does not block on input from every player and cannot hang the session.
- [ ] The summary and the performance report render as one screen, ledger first.
- [ ] All status information is readable without colour.
- [ ] A development assertion confirms the displayed net equals the round's actual change in `TeamCredits`.
- [ ] A development assertion confirms itemised lines sum to the displayed gross.
- [ ] Any mismatch between the summary and the transaction log fails loudly in development.
- [ ] Two consecutive rounds produce independent summaries with no data carried over.
- [ ] A round where nothing was banked produces a coherent summary rather than an empty or broken screen.
