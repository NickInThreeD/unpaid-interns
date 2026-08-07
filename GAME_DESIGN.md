# Unpaid Interns — Game Design Overview

## Elevator Pitch

You're an unpaid intern. Your employer sends you and your fellow interns to random, unfamiliar locations to retrieve items on their behalf. The locations are dangerous — monsters roam them and will chase and harm you. You decide how much risk is worth it, because everything you bring back gets sold to meet a quota. Miss the quota before time runs out, and the whole team dies — or gets fired, which around here amounts to the same thing.

## Core Loop

1. **Get assigned a location** — a random building/map is selected for the round.
2. **Enter and scavenge** — explore the location and pick up items scattered throughout.
3. **Avoid or evade monsters** — hostile entities inhabit the location and will chase/harm players who get too close or too loud.
4. **Return items to the start point** — collected items must be carried back to the extraction/drop-off point within the level to "bank" them; they don't count until they're back.
5. **Decide when to leave** — players choose when the risk outweighs the reward and can pull out of the location at any time, forfeiting whatever hasn't been returned yet.
6. **Sell for money** — items brought back to the start point are sold once the round ends, converting loot into currency.
7. **Repeat under a quota clock** — this cycle repeats across a limited number of days/rounds, with a cumulative money quota that must be hit.

## Risk/Reward Tension

The central decision every round is **how long to stay**. Locations get more dangerous the longer players linger (more monster encounters, more exposure), but leaving early means less loot and a slower climb toward quota. There's no forced timer inside a location — the danger itself is the pressure, not a countdown.

## Economy & Quota

- Items have sell value and are cashed in after being returned to the start point.
- The team shares a collective quota that must be met within a set number of days/rounds (or a time limit).
- Falling short of quota when time runs out is a fail state for the whole team — everyone dies or is fired (functionally the same outcome).

## Stakes & Tone

The premise leans into dark workplace-comedy horror: the "employer" treats interns as expendable labor, sending them into harm's way for unpaid work, with survival tied directly to productivity. The tension comes from balancing greed (grab more loot) against self-preservation (get out alive), with a looming quota that punishes the whole team for individual caution or recklessness.

## Reference Points

This concept draws inspiration from extraction/quota-loop games like *Lethal Company* (see `Assets/docs/` for a mechanics reference derived from that game's wiki, used here as a design touchstone rather than as documentation of this project).

## Open Design Questions

- Squad size and whether interns are player-controlled co-op or a mix of players/AI.
- Number/variety of monster types and how detection/chase mechanics work.
- How locations are generated or selected (fully random vs. a curated pool).
- Whether items have weight/carry limits that force trade-off decisions mid-run.
- What "fired" means mechanically if it's not literal death (run ends? character replaced?).
