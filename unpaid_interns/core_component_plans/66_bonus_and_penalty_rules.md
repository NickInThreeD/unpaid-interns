# 66 — Bonus & Penalty Rules

**Source:** [`core_components.md`](../core_components.md) §8 — Economy & Progression
**Status:** ❌ Not started
**Depends on:** [Currency System](63_currency_system.md), [Selling / Payout](65_selling_payout.md), [Death & Body System](14_death_and_body_system.md), [Crew Roster](19_crew_roster.md)
**Blocks:** individual recklessness having a shared cost

## Summary

The modifiers on top of the payout: a bonus for exceeding quota, a fee per death, a larger fee per body left behind.

`core_components.md` states the purpose in one line — this is *"what makes individual recklessness a shared cost."* That is the design goal, and it is worth being honest that it is also the component most likely to make a session unpleasant. A penalty that makes one player's death visibly cost everyone else money is a mechanic that generates blame, and blame between four friends is the failure mode this genre has to actively manage.

So the rules here need to be tuned for a specific feeling: **the crew should regret a death, not resent the person who died.** That difference is mostly about magnitude and about where the recovery window sits. A death that costs 10% and can be partly recovered by carrying the body home produces "let's go get him". A death that costs 40% and cannot be undone produces silence on voice for the rest of the round.

[`14_death_and_body_system.md`](14_death_and_body_system.md) already establishes the structure: penalties are percentage-based, they are applied **at round settlement rather than at the moment of death**, and the unrecovered-body penalty is larger than the recovered one. This component defines the numbers and the remaining rules.

## How to Build

**Apply everything at settlement, in one place, in a fixed order**

- Penalties and bonuses are computed during `Settling` by [`02_day_cycle_controller.md`](02_day_cycle_controller.md), after the sale total is known and before the day advances.
- **The recovery window is the point.** Applying a death penalty the instant someone dies removes the crew's chance to fix it, and body recovery — the mechanic [`14_death_and_body_system.md`](14_death_and_body_system.md) exists to create — stops being a decision.
- Fix the order and document it, because the order changes the result: gross sale → sell rate if any → **bonuses** → **penalties** → net. Applying a percentage penalty before or after a percentage bonus produces different numbers, and an undocumented order is a bug report waiting.
- Every adjustment goes through the Run Manager's mutators with its own reason enum value ([`63_currency_system.md`](63_currency_system.md)), so the summary can itemise it.

**Set the death penalty low enough to survive socially**

- Percentage-based, so it stays relevant late in a run rather than becoming a rounding error — that is component 14's reasoning and it holds.
- Percentage **of what** must be decided explicitly. Recommended: of the **round's payout**, not of the crew's total balance. A penalty against the balance can wipe savings accumulated over several successful days because of one bad round, which is disproportionate and produces exactly the resentment this component should avoid.
- Recommended magnitudes to start from and tune: a recovered death costs a modest fraction of the round's payout; an **unrecovered** body costs meaningfully more. The gap between the two is the actual mechanic — it is what prices the trip back into the building.
- **Left behind is a third magnitude, not a synonym for the second.** [`105_departure_and_extraction_resolution.md`](105_departure_and_extraction_resolution.md) makes `LeftBehind` a distinct outcome from `Dead`, and by construction such an intern's body can never have been recovered — so if it is priced identically to an unrecovered death, the two are the same line with two names. Decide whether being left behind costs *more* than dying and being abandoned (defensible: the crew left with the door open) or exactly the same (simpler, and one fewer number to tune). Recommended: **the same**, and say so here, so the summary can report the cause honestly while charging one consistent fee.
- **Cap the total penalty.** A round where three people die should not produce a negative payout; a crew that comes home with nothing has been punished enough by losing the haul. Floor the net at zero and never let penalties push the balance down ([`63_currency_system.md`](63_currency_system.md) forbids a negative balance anyway, but the floor should be explicit here rather than discovered by the currency system refusing a spend).

**Get the disconnect rule right — it is already decided elsewhere**

- [`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md) recommends that a disconnect **does not count as a death** for the credit penalty, on the grounds that the alternative punishes people for their internet, while the unbanked-loot loss already removes the alt-F4 exploit's upside.
- This component must implement that rule and no other. [`02_day_cycle_controller.md`](02_day_cycle_controller.md) already requires that a player who dropped is **not double-charged** as both a death and a disconnect, and the crew roster's state precedence ([`19_crew_roster.md`](19_crew_roster.md)) — a player who dies and then disconnects reads `Disconnected` but still counts as a death — is the rule that decides the ambiguous case.
- Test the ambiguous case specifically. It is the one that will be got wrong, and it is the one a player will notice.

**Make the bonus reward the behaviour the design wants**

- The overtime bonus for exceeding quota is what makes earning above the target worthwhile, alongside the store. Without it, a crew that hits quota should stop playing carefully, which flattens the last day of every cycle.
- Recommended: a bonus proportional to the **excess over quota**, so there is no cliff and no incentive to sit precisely at the target. A flat bonus for merely meeting quota rewards nothing beyond what meeting quota already rewards.
- Consider a **survival bonus** — everyone came home — as a positive counterweight to the death penalty. It gives the crew something to protect rather than only something to lose, and psychologically that lands very differently for the same net arithmetic.
- Resist bonuses for individual performance. `GAME_DESIGN.md` makes the quota collective, and a most-valuable-intern bonus reintroduces the competitive framing the project is deliberately removing from `LeaderboardManager`'s semantics.

**Show the arithmetic**

- The end-of-round summary must itemise every line: gross, rate, bonus, each penalty with the name of who it was for, and the net ([`70_performance_report.md`](70_performance_report.md)).
- Naming the deceased in a penalty line is a tone decision. It is exactly the callous corporate register the premise is built on — *"deduction: recovery of asset, J. Fournier — not recovered"* — and the comedy is what stops it from reading as an accusation. Get it wrong and it is a blame machine; get it right and it is the funniest screen in the game.
- Every value the summary shows must reconcile exactly with the credited amount. A discrepancy here is the fastest way to lose a crew's trust in the entire economy.

**Keep the numbers in data and prove them**

- All rates, caps, and thresholds in the same config asset as the quota and sell curves. These will be retuned constantly and against playtest feeling rather than arithmetic.
- Add a pure-logic test over the full settlement calculation: a matrix of payouts, deaths, recoveries, disconnects, and quota excess, asserting the net matches a hand-computed expectation. This is exactly the cheap, high-value test §11 asks for and it protects the number that decides whether the run continues.

## Acceptance Criteria

- [ ] All bonuses and penalties are applied at settlement, after the sale total and before the day advances.
- [ ] The order of operations is documented here and implemented as documented.
- [ ] Every adjustment goes through the Run Manager's mutators with its own reason enum value.
- [ ] Death and body penalties are percentages of the round's payout, not of the crew's total balance.
- [ ] The unrecovered-body penalty is meaningfully larger than the recovered-death penalty.
- [ ] Recovering a body during the round measurably reduces the penalty, and the recovery window is real.
- [ ] Total penalties are capped; a round's net payout can reach zero but never goes negative.
- [ ] A disconnect does not incur a death penalty, per the rule in [`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md).
- [ ] A player who dies and then disconnects is charged exactly one death penalty and no disconnect penalty.
- [ ] No player is ever double-charged under any combination of death, disconnect, and reconnect.
- [ ] An overtime bonus scales with the excess over quota, with no cliff at the target.
- [ ] A survival bonus exists, or its absence is a documented decision.
- [ ] No bonus rewards individual performance over crew outcome.
- [ ] The summary itemises gross, rate, each bonus, each penalty with its cause, and the net.
- [ ] Every itemised figure reconciles exactly with the credited amount.
- [ ] All rates, caps, and thresholds live in a config asset and are tunable without a recompile.
- [ ] An automated test covers a matrix of payouts, deaths, recoveries, disconnects, and quota excess against hand-computed expectations.
- [ ] Playtesting confirms crews respond to a death with a rescue attempt rather than with blame; if not, magnitudes are reduced.
