# Moons

**Source:** https://lethal-company.fandom.com/wiki/Moons

## Overview

Moons — also called celestial bodies, exomoons, or planets in-game — are the destinations employees route the autopilot ship to in order to collect scrap. This page is the system-level reference for how moons work: the properties that define each one (difficulty, risk level, cost, map size, power caps, scrap range, interior layout, possible weather), a full catalogue of destinations, and an average-profit analysis comparing them.

## Key Points

- Moons are grouped into three tiers in the terminal: **Easy, Intermediate, and Hard**.
- Higher-tier moons cost credits to route to; the rest are free.
- **Weather never affects a moon's scrap amount or value** — it only adds difficulty.
- Each moon's interior is procedurally generated on landing.

## Moon Properties

### Difficulty, risk level, and cost

**Difficulty** is the terminal's grouping: Easy moons are relatively safe with modest loot and fewer dangerous entities on average; Intermediate moons are harder but more rewarding; Hard moons add punishing exterior conditions such as dense fog and snowstorms on top of everything else.

**Risk level** is the in-universe hazard rating (D through S++). It is fixed per moon and has **no mechanical effect** — it exists purely as an indicator for employees.

**Cost** applies to Rend, Dine, Titan, Embrion, and Artifice. Once in orbit around a paid moon, you do not pay again until you route somewhere else — **including round trips to the Company Building**, which is the standard way to avoid paying twice.

### Map size multiplier

Determines the size of the procedurally generated interior. Experimentation and Assurance are smallest at 1.00; **Titan is largest at 2.20**. Larger maps mean bigger maze structures and more tangled catwalks and hallways — more scrap, but a much higher chance of getting lost.

### Maximum power

Every moon has separate **maximum indoor, outdoor, and daytime power** values, and every entity has a power level. The cap restricts how many entities can be present at once, weighted by how dangerous each one is.

For example, Brackens and Thumpers both have power level 3, so they cannot coexist on Experimentation, whose maximum indoor power is 4.

- **Max indoor power** governs interior entities such as Coil-Heads and Snare Fleas.
- **Max outdoor power** governs Eyeless Dogs, Forest Keepers, and similar.
- **Max daytime power** governs the daytime-only set — Manticoils, Roaming Locusts, Circuit Bees.

### Min/max scrap

Each map has a predefined minimum and maximum number of scrap items. Individual values are rolled randomly from the moon's loot table, so a high-count moon can still produce a poor run — but on average, higher max scrap means higher total value.

### Indoor map layout

The interior is one of the **Facility (Factory), Mansion, or Mineshaft**. Every moon except March has some chance of generating a non-default layout; per-moon probabilities are on the individual moon pages.

## Exomoons Catalogue

| Moon | Difficulty | Risk | Cost | Likely interior | Size | Scrap | Indoor/Outdoor power |
|---|---|---|---|---|---|---|---|
| 71-Gordion (Company) | Safe | Safe | 0 | — | — | — | — |
| 41-Experimentation | Easy | D | 0 | Factory | 1.00 | 8–11 | 4 / 8 |
| 220-Assurance | Easy | C | 0 | Factory | 1.00 | 13–15 | 6 / 8 |
| 56-Vow | Easy | C | 0 | Factory | 1.15 | 12–14 | 7 / 6 |
| 21-Offense | Intermediate | B | 0 | Mineshaft | 1.25 | 14–18 | 12 / 8 |
| 61-March | Intermediate | B | 0 | Factory | 1.75 | 13–16 | 14 / 12 |
| 20-Adamance | Intermediate | B | 0 | Factory | 1.18 | 14–16 | 13 / 11 |
| 85-Rend | Hard | A | 550 | Mansion | 1.80 | 18–25 | 10 / 6 |
| 7-Dine | Hard | S | 600 | Mansion | 1.80 | 200–249 | 10 / 9 |
| 8-Titan | Hard | S+ | 700 | Factory | 2.20 | 28–31 | 18 / 7 |
| 68-Artifice | Hard | S++ | 1500 | Mineshaft | 1.80 | 26–30 | 13 / 13 |
| 5-Embrion | Hard | S | 150 | Factory | 1.10 | 14–16 | 8 / 70 |
| 44-Liquidation (unreleased) | Hard | S++ | 700 | Mansion | 1.60 | 28–44 | 13 / 13 |

Notable outliers: **Dine's enormous 200–249 scrap count** (individually near-worthless items), **Embrion's outdoor power of 70**, and **Titan's 2.20 map size** paired with the highest indoor power of 18.

## Loot Analysis

Average expected profit from a single visit:

- **Artifice — ~1811 credits**, the richest released moon, at ~31 items averaging 58 credits each.
- **Liquidation (unreleased) — ~1828**, at ~36 items and the best value-per-weight of any standard moon (15 credits/lb).
- **Dine — ~2773**, the highest total, but from ~225 items worth ~12 credits each at effectively zero weight, giving an extraordinary **133 credits per pound** and a scrap density of ~125.
- **Titan — ~1547** and **Rend — ~1228** fill out the high tier.
- Free moons range from ~301 (Experimentation) up to ~802 (Offense).

**Density matters more than raw totals.** Titan has the largest indoor map, so clearing all its scrap takes the longest; several lower-tier moons have higher scrap density and yield loot faster per minute spent.

The wiki notes these figures **exclude** shotguns and knives dropped by entities, plus Bee Hives and the Apparatus, so they do not fully reflect each moon's true tier.

## Challenge Moons

Weekly challenge moons are procedurally generated specialized moons. Playing one locks the crew to that moon for a single day with the goal of maximizing profit; at the end of the day all scrap is deleted and the score is submitted to a leaderboard. See the Challenge Moons page for details.

## Related Concepts

Interior, Weather, Danger Level, Mechanics, Scrap, Terminal, The Company, Challenge Moons, Orbit, Profit Quota, Map Hazard

## Tags

lethal-company, moons, exomoons, destinations, difficulty, risk-level, map-size-multiplier, max-power, scrap-density, interior-layout, loot-analysis, routing, credits, challenge-moons

---

Summary generated from: https://lethal-company.fandom.com/wiki/Moons
