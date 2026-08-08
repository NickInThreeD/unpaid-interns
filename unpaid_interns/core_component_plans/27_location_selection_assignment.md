# 27 — Location Selection / Assignment

**Source:** [`core_components.md`](../core_components.md) §4 — Location & World Generation
**Status:** ❌ Not started · **[MVP]**
**Depends on:** Location Catalogue, Hub State, Run Manager
**Blocks:** Terminal / Hub Interface, Location Load / Unload Flow, store purchasing decisions

## Summary

How the crew ends up at a particular destination each round: the employer assigns one at random, or the team chooses from what they have unlocked.

This is a small system attached to a large design decision. `GAME_DESIGN.md` says the employer "sends you and your fellow interns to random, unfamiliar locations", which reads as assignment — but it also lists "how locations are generated or selected (fully random vs. a curated pool)" as an open question, and §16 flags the same thing. The answer determines whether the game has a **strategy layer** at all.

- **Assigned at random** — the crew adapts to what they are given. Preserves the powerlessness of the premise, removes routing strategy, and makes every round a fresh problem. Cheaper to build and to balance, because the designer controls the distribution.
- **Chosen from a list** — the crew routes deliberately: cheap and safe when close to quota, expensive and rich when behind. This is where the between-rounds phase gets its teeth, and it is what makes the store, the travel cost, and the day count interact.

**Recommendation: chosen, with the employer applying pressure.** The crew picks, but the quota escalation forces increasingly dangerous choices, and the tone is preserved by making the *pressure* corporate rather than the *decision*. The premise's cruelty lands harder when the interns choose their own doom to hit a number. Note that this is a genuine reversal of the elevator pitch's wording, so it needs an explicit decision rather than a quiet drift — record it here when made.

A cheap hybrid worth considering: the employer offers **three destinations per day**, drawn from the unlocked pool with weights. The crew chooses among them, so the strategy layer exists, but they never get the whole menu — which keeps the feeling of being handed a job.

## How to Build

**Make the selection server-authoritative and shared**

- Selection happens in the hub, before deploy. The selected location id is a `[GhostField]` on the Run Manager so every client sees the same destination before it loads — required by [`23_shared_session_state_sync.md`](23_shared_session_state_sync.md), and the precondition for [`05_location_load_unload_flow.md`](05_location_load_unload_flow.md) loading the same scene on every machine.
- The client sends a *request*; the server validates and decides. Validate: is the location unlocked, can the crew afford the travel cost, is the phase actually `Hub`.
- **Deduct the travel cost at commit, not at selection.** A crew browsing destinations must not be charged for looking, and a selection that is later changed must not leak credits. Charge on deploy, in one place, through the Run Manager's `SpendCredits`.

**Decide who chooses**

- One shared decision, not a per-player one. Options: any intern may set it, a majority must agree, or a single designated role holds it.
- Recommended: **any intern may set it, but the deploy action is separate and explicit.** Changing the destination is cheap and reversible; committing is deliberate. This mirrors the departure-control rule already specified in [`04_hub_between_rounds_state.md`](04_hub_between_rounds_state.md) and keeps both interactions consistent.
- Show who changed it in the hub UI. A destination that silently changes while someone is shopping is a small betrayal that will happen constantly.

**Build the random path anyway**

- Even under the "chosen" answer, a weighted random draw is needed — for the three-offer hybrid, for a debug "give me any location" command, and as the fallback when no valid choice exists.
- Draw it on the server from the same `FixedRandom` singleton that [`29_deterministic_generation_seed.md`](29_deterministic_generation_seed.md) governs, so a run can be reproduced end to end from one seed. `ServerGameSystem.OnCreate` currently creates `FixedRandom` from `DateTime.Now.Millisecond`, which is fine as an entropy source and useless for reproduction — that plan fixes it.
- Weight by difficulty tier against the crew's current quota pressure if a difficulty curve is wanted, but do it visibly: a hidden difficulty director that quietly sends a struggling crew somewhere easier undermines the quota's threat.

**Handle unlocks**

- Decide whether all destinations are available from day one or unlock via progression. Unlocks give the Upgrades component (§8) something to sell and give a long run a shape; full availability is simpler and lets a confident crew jump straight into danger.
- Whichever is chosen, unlock state is run state: it lives on the Run Manager, persists via [`06_session_persistence.md`](06_session_persistence.md), and is wiped with the run on failure.
- Locked destinations should be *visible* and locked, not hidden. Seeing the expensive place you cannot afford yet is motivation.

**Surface it in the terminal**

- The Terminal / Hub Interface (§9) is where this is presented. Per destination, show what the crew needs to decide: difficulty tier, travel cost, rough loot expectation, known threats, and **the current forecast** ([`35_environmental_conditions_weather.md`](35_environmental_conditions_weather.md)) — in-fiction, and deliberately imprecise. An exact expected-value readout turns the choice into arithmetic and removes the gamble.
- The forecast is what makes weather a decision rather than a random punishment, and it only works if it is rolled per destination per day *before* the crew chooses. That requires the weather draw to be keyed on the run seed and day number, not rolled at deploy — a constraint that plan carries and this one depends on.
- Show current credits and quota progress on the same screen. The destination decision is a function of how far behind the crew is, and forcing players to remember the number across two screens just makes them wrong.
- Announce the committed destination through the repurposed `ActionFeed` so anyone not looking at the terminal still finds out where they are going.

## Acceptance Criteria

- [ ] The chosen model — assigned, chosen, or the three-offer hybrid — is implemented and documented in this file.
- [ ] The selected location id is replicated and identical on host and every client before the load begins.
- [ ] Selection requests are validated server-side for unlock state, affordability, and phase; a forged request changes nothing.
- [ ] Travel cost is deducted once, at deploy, through the Run Manager, and browsing costs nothing.
- [ ] Changing the destination before deploy is free and reversible, and the change is visible to the whole crew.
- [ ] Deploy is a separate, explicit action from selection.
- [ ] The weighted random draw uses the run's seeded random and reproduces identically from the same seed.
- [ ] Unlock state persists with the run and is wiped on run failure.
- [ ] Locked destinations are visible and clearly marked as locked.
- [ ] The terminal shows difficulty, cost, loot expectation, known threats, and the weather forecast alongside credits and quota progress.
- [ ] The forecast shown before committing matches the condition the crew arrives to.
- [ ] Loot expectation is presented imprecisely — no exact expected-value figure.
- [ ] The committed destination is announced to the crew.
- [ ] Selecting the same destination twice in a run produces a different layout (see the seed component), not a repeat.
- [ ] A debug command can force any destination, including locked ones.
