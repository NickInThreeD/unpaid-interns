# 63 — Currency System

**Source:** [`core_components.md`](../core_components.md) §8 — Economy & Progression
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Run Manager](01_run_manager.md)
**Blocks:** Quota, Selling, Store, Bonus & Penalty Rules, Upgrades — everything in §8

## Summary

One number, shared by the whole crew, that survives between rounds and is wiped when the run dies.

It is the smallest component in §8 and it is the one every other component in §8 mutates, which makes its **discipline** more important than its logic. `GAME_DESIGN.md` makes the money collective — *"the team shares a collective quota"* — so there is no per-player balance, no splitting, and no individual wallet. That is a design decision worth stating up front because it removes an entire category of feature requests: there is nothing to trade, nothing to tip, and nothing to steal.

Most of the implementation is already specified. [`01_run_manager.md`](01_run_manager.md) declares `TeamCredits` as a `[GhostField]` and defines `AddCredits(int)` / `SpendCredits(int)` as **the only mutation path**, returning success so callers can react. This component's job is to make that the *actual* only path, and to define the rules around it that the rest of §8 will lean on.

The reference design starts a contract with 60 credits ([`Assets/docs/core-loop/credits.md`](../../Assets/docs/core-loop/credits.md)) — enough to buy one useful thing and not enough to be comfortable, which is the right shape for a starting balance.

## How to Build

**One writer, one path, no exceptions**

- All mutation goes through `AddCredits` and `SpendCredits` on the Run Manager, server-side, guarded by `Role != MultiplayerRole.Server` and `IsGhostLinked()`, following `LeaderboardManager.AddKill`'s shape.
- **Never expose a setter.** A public `TeamCredits` setter is how a UI screen ends up writing the balance and how a bug becomes untraceable. The audit trail depends on every change passing through two functions.
- `SpendCredits` refuses and returns false when the balance is insufficient. It never clamps to zero and never goes negative — [`01_run_manager.md`](01_run_manager.md) already makes both acceptance criteria, and a negative balance is the failure that silently poisons every subsequent comparison.
- Callers must **check the return value**. A store that deducts and delivers without checking will hand out free equipment under a race, and the store is the highest-frequency caller.

**Make every change attributable**

- Log every mutation server-side with tick, amount, resulting balance, and a **reason enum**: `SaleProceeds`, `Purchase`, `TravelCost`, `DeathPenalty`, `BodyPenalty`, `Bonus`, `Debug`.
- "Where did our money go" is the most common question a shared balance produces, and without this log it is unanswerable. It also feeds the end-of-round summary ([`70_performance_report.md`](70_performance_report.md)) and the balance telemetry §13 asks for.
- The reason enum is not decoration: [`66_bonus_and_penalty_rules.md`](66_bonus_and_penalty_rules.md) needs it to itemise the round, and the summary needs it to explain a payout the crew will otherwise dispute.

**Get the shared-state discipline right**

- `TeamCredits` is on the shared-state inventory in [`23_shared_session_state_sync.md`](23_shared_session_state_sync.md), which means: replicated, never cached across frames on a client, and **never computed client-side.**
- That last rule has a specific trap. A store UI showing "you will have 240 after this purchase" is fine as a clearly-derived preview; the same UI displaying that as *the balance* is not. The distinction has to be visible in the presentation, or players will believe a number the server does not agree with.
- Between connection and ghost link, the UI must show a loading state rather than zero. A player who reads "credits: 0" for two seconds will believe the run just ended.

**Keep integers**

- Credits are integers. Floating-point money accumulates rounding error across a long run and produces a balance that disagrees with the sum of its transactions, which is exactly the discrepancy the log exists to prevent.
- Every rate that produces a fraction — the sell-rate curve in [`65_selling_payout.md`](65_selling_payout.md), percentage penalties in [`66_bonus_and_penalty_rules.md`](66_bonus_and_penalty_rules.md) — rounds at a **single, documented point**, and the same way every time. Rounding in two places with two conventions is how a payout is off by one and nobody can find it.

**Define the lifecycle**

- **Start of run** — seeded to a configured starting balance. In a config asset, not a constant.
- **During a run** — persists across rounds, across the hub, and across a save and reload ([`06_session_persistence.md`](06_session_persistence.md)).
- **On run failure** — wiped with the rest of the run ([`07_game_over_win_resolution.md`](07_game_over_win_resolution.md)). A failed run that keeps its money is not a failure.
- Verify explicitly that a new run started immediately after a failure begins from the configured starting balance with no residue — component 07 already flags leaked state as the most common bug at that boundary.

**Make it testable**

- `ConfigVar` commands to grant, deduct, and set the balance, and to dump the transaction log. [`01_run_manager.md`](01_run_manager.md) already requires the grant command; the log dump is what makes economy bugs diagnosable.
- Add a development-only assertion that the balance equals the sum of the logged transactions plus the starting balance. It is three lines and it turns the entire class of "money is wrong somehow" into a specific failing check.

## Acceptance Criteria

- [ ] `TeamCredits` is a single team-wide `[GhostField]` on the Run Manager; there is no per-player balance anywhere.
- [ ] All mutation goes through `AddCredits` and `SpendCredits`; no setter exists.
- [ ] Both mutators are server-only, guarded by role and ghost-link checks, and reject client calls with a logged warning.
- [ ] `SpendCredits` refuses an unaffordable spend, returns false, and never clamps or goes negative.
- [ ] Every caller checks the return value; no code path delivers goods without a confirmed deduction.
- [ ] Every mutation is logged with tick, amount, resulting balance, and a reason enum.
- [ ] The reason enum covers sales, purchases, travel costs, death and body penalties, bonuses, and debug grants.
- [ ] Credits are integers, and all fractional rates round at one documented point with one convention.
- [ ] Host and every client display an identical balance at all times, verified under simulated latency.
- [ ] A client joining mid-session receives the correct balance, not zero.
- [ ] UI shows a loading state, not zero, between connection and ghost link.
- [ ] Derived previews ("you will have N after this") are visibly distinct from the actual balance.
- [ ] The starting balance comes from a config asset and is applied at run start.
- [ ] The balance persists across rounds, the hub, and a save and reload.
- [ ] Run failure wipes the balance, and a new run starts from the configured value with no residue.
- [ ] Debug commands can grant, deduct, set, and dump the transaction log, and all work in a build.
- [ ] A development assertion confirms the balance equals the starting balance plus the sum of logged transactions.
