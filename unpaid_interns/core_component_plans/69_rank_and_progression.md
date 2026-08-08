# 69 — Rank / Progression

**Source:** [`core_components.md`](../core_components.md) §8 — Economy & Progression
**Status:** ❌ Not started — explicitly safe to defer
**Depends on:** [Performance Report](70_performance_report.md), [Session Persistence](06_session_persistence.md)
**Blocks:** nothing

## Summary

A number that goes up across runs, so a player has something to show for the ones they lost.

`core_components.md` files this under *"retention and flavor; safe to defer"*, and that assessment should be taken seriously rather than politely noted. This is the only component in §8 that touches nothing else, and building it early would be a straightforward misallocation — every hour here is an hour not spent on the loot loop that makes the game worth ranking anyone in.

It earns a plan anyway for one reason: **it is the component most likely to be built wrong in a way that damages the rest of the game.** The obvious version — rank grants bonuses — turns a co-op game into one where a new player is a liability and a veteran carries. In a design where the quota is collective and failure is shared, that is corrosive: nobody wants to be the intern whose presence made the crew's target harder to hit.

So the rule, stated up front and taken from the reference implementation ([`Assets/docs/core-loop/company-ranks.md`](../../Assets/docs/core-loop/company-ranks.md)): **rank is cosmetic. It grants no gameplay advantage.**

## How to Build

**Derive it from the report the game already produces**

- [`70_performance_report.md`](70_performance_report.md) grades the crew at the end of each round. Rank XP comes from that grade and from nothing else — no separate scoring system, no parallel stat tracking, no second definition of "did well".
- Good grades award XP; poor grades and failed runs **subtract** it. Rank that only rises is a playtime counter wearing a costume; rank that can fall means the badge says something.
- Award the same XP to every crew member from the crew grade. The quota is collective, the failure is collective, and the grade should be too — an individual XP split would reintroduce exactly the competitive framing the project is removing from `LeaderboardManager`'s semantics.
- The reference derives its grade from scrap recovered versus scrap available, combined with deaths. That ratio is the right shape because it measures the crew against **what was actually there**, which the loot spawner knows ([`39_loot_spawner.md`](39_loot_spawner.md)) and which makes a small map and a large one comparable.

**Persist it outside the run**

- Rank is the **only** thing in the project that survives a failed run. Everything else — credits, upgrades, storage, unlocks — is wiped ([`07_game_over_win_resolution.md`](07_game_over_win_resolution.md)).
- That makes it a different save scope: per-player and local, not part of the run save. [`06_session_persistence.md`](06_session_persistence.md) already establishes that player settings live in a separate local slot for the same reason, and rank belongs beside them.
- **Consequence worth stating:** the host owns the run save, but each player owns their own rank. A player's rank travels with them between crews, which is the correct behaviour and also the reason it cannot live in the run file.
- Key it on the stable player id from [`19_crew_roster.md`](19_crew_roster.md), with the same fallback for direct-connect sessions where UGS is unavailable.

**Keep it visible and keep it flavour**

- Display it where it costs nothing: the crew roster, the lobby, the performance report, a badge on the suit. Teammate identification (§9) already needs per-player visual distinction, and a rank badge is a natural, free contributor to it.
- Rank names are pure premise. Intern → Part-Timer → Associate → and upward through a corporate ladder that is transparently meaningless is exactly the register `GAME_DESIGN.md` describes, and a demotion notice is one of the funniest things a failed run can produce.
- **No gameplay effect. None.** No stat bonus, no starting credits, no unlocked equipment, no matchmaking gate. Write this in the file so a future contributor does not add "just a small bonus" and quietly break the co-op contract.

**Do not let it leak into balance**

- Resist rank-gated content. A destination or an item locked behind rank means a crew cannot play together until everyone has ground to the same tier, which is a retention mechanic that costs sessions.
- Resist showing rank during matchmaking or lobby selection in a way that lets crews filter by it. It is a badge, not a credential.
- If a cosmetic reward per rank is wanted — a suit colour, a hub decoration — that is fine and is the correct outlet for the impulse to make rank *do* something.

**Build it last, and build it small**

- The whole component is: an XP value, a table of thresholds, a derivation from the crew grade, a local save, and a display. If it grows past that, it has started doing something it should not.
- Because it is cheap and deferred, it is also a good candidate for the first thing built after the loop is proven — it makes a playtest feel like a game rather than a test, and that changes the quality of feedback testers give.

## Acceptance Criteria

- [ ] Rank grants no gameplay advantage of any kind, and this constraint is recorded in this file.
- [ ] XP is derived solely from the crew grade produced by the performance report; no parallel scoring exists.
- [ ] Every crew member receives the same XP from a round, with no individual split.
- [ ] Poor grades and failed runs subtract XP, and rank can fall.
- [ ] Rank persists across runs, including failed ones, and is the only state that does.
- [ ] Rank is saved per-player and locally, separate from the host-owned run save.
- [ ] Rank is keyed on the stable player id, with a working fallback on direct-connect sessions.
- [ ] A player's rank travels with them between different crews and hosts.
- [ ] Rank is visible on the crew roster, in the lobby, and on the performance report.
- [ ] No content, destination, item, or session is gated behind rank.
- [ ] Rank cannot be used to filter or gate matchmaking.
- [ ] Any per-rank reward is purely cosmetic.
- [ ] Rank thresholds and names live in a config asset and are tunable without a recompile.
- [ ] A new player with no rank plays a full run with no mechanical disadvantage relative to a high-ranked player.
- [ ] A debug command can set rank and XP directly.
