# 23 — Shared Session State Sync

**Source:** [`core_components.md`](../core_components.md) §3 — Multiplayer & Team
**Status:** ❌ Not started · **[MVP]**
**Depends on:** Run Manager, Day Cycle Controller, Crew Roster
**Blocks:** trust in every number the game displays

## Summary

The guarantee that quota, money, day count, phase, and destination are **identical on every machine**, and the discipline that keeps them that way as the game grows.

This is not a class. It is a rule plus a test harness, and it earns a component of its own because the failure it prevents is silent. A client that believes the quota is 1,200 when the server believes it is 1,500 does not crash — it plays a whole round making wrong decisions and then discovers the crew is dead. That is the worst kind of multiplayer bug: invisible until it is expensive.

The architecture already supplies the mechanism. [`01_run_manager.md`](01_run_manager.md) specifies `[GhostField]` state mutated only on the server; the working reference is `LeaderboardManager`, which replicates a dynamic buffer and pushes transient events over broadcast RPCs. What this component adds is: an explicit inventory of what counts as shared state, the rules for how it is allowed to be read and written, the late-joiner guarantee, and the tests that catch a violation before a playtest does.

## How to Build

**Inventory the shared state**

- Write the list down and keep it in this file. If a value is on it, it is server-owned and replicated; if it is not on it, no UI may display it as authoritative.
- Currently: `CurrentDay`, `DaysUntilDeadline`, `TeamCredits`, `CurrentQuota`, `QuotaProgress`, `QuotasCompleted`, `RunState`, `RoundPhase`, `RoundStartTick`, `SelectedLocationId`, `WeatherId` ([`35_environmental_conditions_weather.md`](35_environmental_conditions_weather.md)), `RunSeed` and `RoundSeed`, crew roster entries, power zone flags ([`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md)), and the banked total.
- Note the banked total is **derived from currently-banked items rather than accumulated** ([`43_loot_banking_deposit.md`](43_loot_banking_deposit.md)), which is this file's derive-rather-than-replicate rule applied to the number the crew cares about most. Per-item banked flags are item ghost state, not session state; only the aggregate belongs on this list.
- Everything else is either per-player predicted state (stamina, held items) or pure presentation (fear intensity, scan highlights). Those must **never** be promoted to shared state without appearing on this list first.

**Enforce the write rule**

- One writer: the server. Every mutator on a shared-state manager is guarded by `Role != MultiplayerRole.Server` with a warning, and by `GhostGameObject.IsGhostLinked()`, exactly as `LeaderboardManager.AddKill` does. A rejected write must log — silent rejection is how "why is my money wrong" becomes unanswerable.
- Clients read via `GhostGameObject.ReadGhostComponentData<T>()` and never cache the result across frames. A cached value that stops updating is the same bug as a desynced one and is harder to see.
- **No client-side arithmetic on shared state.** A HUD that computes `quota - progress` locally is fine; a HUD that computes "what my credits will be after this sale" and then displays it as the balance is not. Derived display values must be visibly derived.

**Separate durable state from transient events**

- Ghost fields carry *state*: what the quota is right now. They are eventually consistent and a client that misses a snapshot catches up on the next one.
- Broadcast RPCs carry *events*: "quota met", "the ship is leaving", "an item was banked". A client that misses one never sees it.
- The failure mode is using the wrong one. A phase transition sent only as a ghost field can be missed entirely if a client's snapshot arrives after the phase has already moved on — which is why [`02_day_cycle_controller.md`](02_day_cycle_controller.md) specifies both. Conversely, a quota value sent only as an RPC is lost forever for a late joiner.
- **Rule: every event that has a lasting consequence must also be derivable from replicated state.** RPCs are for presentation and timing, never the sole record of a fact.

**Guarantee the late joiner**

- A client joining mid-session must receive current values, not defaults. Ghost fields do this automatically once the ghost is linked — the trap is any manager that pushes its state on a one-time RPC at spawn.
- The state the client sees between connecting and the ghost linking is *not* zero-cost: `LeaderboardManager.UpdateServer` already logs "not linked yet" and returns. UI must render a clear loading state during that window rather than displaying zeros, because a player who reads "credits: 0" for two seconds will believe it.
- Test this deliberately. It is the single most under-tested path in any networked game, because the developer is always the host.

**Watch the bandwidth**

- Shared session state is small and changes rarely — quantize aggressively and set a low `GhostImportance` so it never competes with player and monster snapshots. §13's bandwidth budget applies here, and this is the cheapest state in the game to get right.
- Derive rather than replicate wherever possible. [`03_round_timer_clock.md`](03_round_timer_clock.md) replicates only `RoundStartTick` and computes everything else — that is the pattern to copy, not the exception.

**Build the desync test**

- Add a debug command that dumps every value on the inventory list, with the current `NetworkTick`, on whichever machine runs it.
- Add an automated check: the server periodically broadcasts a hash of its shared state; each client hashes its own and logs loudly on mismatch. Ship it enabled in development builds and behind a `ConfigVar` in release.
- This is the component's real deliverable. Everything above is a rule; the hash check is what proves the rule is being followed after six months of new features.

## Acceptance Criteria

- [ ] The inventory of shared state is written down in this file and matches the `[GhostField]`s actually declared.
- [ ] Every shared-state mutator rejects client calls with a logged warning and changes nothing.
- [ ] No client-side cache of shared state persists across frames.
- [ ] Host and all clients report identical values for every listed field, verified under simulated latency and packet loss.
- [ ] A client joining mid-round receives correct current values for every listed field, never defaults.
- [ ] UI shows an explicit loading state, not zeros, between connection and ghost link.
- [ ] Every consequential event is both broadcast as an RPC and derivable from replicated state.
- [ ] A client that drops the RPC for a phase change still converges to the correct phase from ghost state.
- [ ] Shared session state uses a low ghost importance and does not measurably contribute to snapshot size.
- [ ] The state-dump debug command works on host and client and is usable in a build.
- [ ] The periodic state-hash comparison runs in development builds and logs loudly on mismatch.
- [ ] A deliberately injected desync is caught by the hash check within a few seconds.
- [ ] Two full round cycles with a mid-round join and a disconnect end with every client agreeing on every listed value.
