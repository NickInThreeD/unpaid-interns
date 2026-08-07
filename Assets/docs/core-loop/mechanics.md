# Mechanics

**Source:** https://lethal-company.fandom.com/wiki/Mechanics

## Overview

This page is the technical reference for *Lethal Company*'s underlying systems — global game variables, how time is normalized, how animation curves drive randomness, and the full entity spawning algorithms for daytime, outdoor, and indoor entities. It is aimed at experienced employees and anyone reverse-engineering the game's behavior.

> **⚠ Version caveat — read before using these numbers.** The source page states its values are accurate **as of Version 45**, and its editors describe the article as incomplete. The game has since shipped through v80+. The **spawn-cycle times** in this document are separately dated to **Version 49**. The algorithmic *structure* (curve evaluation, power gating, deviation ranges, vent assignment) is the most reliable part; the **constants** are the least. Verify any specific number against current game data before depending on it.

## Key Points

### Global game variables

These are static values shared by every game session; only a game update changes them.

- `hourLength` = 60
- `hoursCount` = 18
- `timeSpeedMultiplier` = 1.4
- Ship leave time = 12 AM
- `scrapValueMultiplier` = 0.4
- `scrapAmountMultiplier` = 1.0
- `mapSizeMultiplier` = 1.5
- `spawningCooldown` = 2 hours

Moon-, entity-, and scrap-specific variables are referenced throughout the wiki in camelCase (e.g. a moon's "Map Size Multiplier" becomes `mapSizeMultiplier`).

### Normalized time

Most systems don't use raw clock time — they use *normalized time*, a value between 0 and 1 representing the fraction of the day elapsed. It is calculated as current time divided by total time (`hourLength * hoursCount`).

### Animation curves

Many mechanics are built on Unity Animation Curves: mathematical functions mapping one value (such as time of day) to another (such as spawn chance). *Lethal Company* interpolates these with Hermite cubic spline interpolation, using keyframes plus slopes that define how the curve bends between points. The wiki writes curve lookups as `curve.eval(Y)`.

## Entity Spawning

### The spawn cycle

A spawning cycle runs once at the start of each round and then periodically until the day ends or the ship leaves early. The interval is set by `spawningCooldown` (2 hours). The first cycle is special — it fires at the moment the round begins, and each day starts at **7:40 AM**.

As of Version 49, cycles occur at: **7:40 AM, 9:00 AM, 11:00 AM, 1:00 PM, 3:00 PM, 5:00 PM, 7:00 PM, 9:00 PM, and 11:00 PM.**

### Power counts gate everything

An entity category's spawning algorithm only runs if that category's **Power Count** has not reached its maximum. For example, if a Bracken (power level 3) and a Thumper (power level 3) are already present on Assurance, the moon's Max Indoor Power of 6 is met and no further indoor entities spawn until the count drops.

### Common eligibility rules

For all three categories, each entity slot builds a weighted probability list. An entity is ineligible (chance set to 0) if any of these fail:

- **Max Count** — spawning it would exceed the entity's own maximum count.
- **Power Level** — adding its power level would exceed the moon's maximum power for that category.
- **Latest Spawn Time** — the entity's cutoff has passed (Roaming Locusts, for example, stop spawning after 7:41 PM).

Eligible entities have their moon rarity multiplied by their own Spawn Probability Curve evaluated at the current time of day. Entities with a **Spawn Count Falloff** curve are further multiplied by that curve, evaluated against how many of that same entity already exist — this is what prevents one species from flooding the map. The game then picks a random weighted index from the list.

### Daytime entities

Spawn count comes from the moon's Daytime Spawn Probability Curve and Daytime Spawn Deviation:

- Min = `daytimeCurve.eval(normalizedTime) - daytimeSpawnDeviation`
- Max = `daytimeCurve.eval(normalizedTime) + daytimeSpawnDeviation`

The result is clamped between 0 and 20 — never more than 20 daytime entities at once. Selected entities spawn immediately at predefined daytime spawn points.

### Outdoor entities

Same structure, but the minimum is inflated by how far into the quota cycle the crew is:

- Min = `outdoorCurve.eval(normalizedTime) + abs(daysUntilDeadline - 3) / 1.6 - outdoorSpawnDeviation`
- Max = `outdoorCurve.eval(normalizedTime) + outdoorSpawnDeviation`

The result is clamped between `minOutsideEnemiesToSpawn` and 20. `minOutsideEnemiesToSpawn` defaults to 0 but is raised to a moon-specific value during **Eclipsed** weather. Selected entities spawn immediately at predefined outdoor spawn points.

**The practical consequence: the more days that pass in a quota cycle, the more entities spawn.**

### Indoor entities

The most complex case, because indoor entities do not appear instantly — each is assigned a **vent** and a **timer**.

- Min = `indoorCurve.eval(normalizedTime) + abs(daysUntilDeadline - 3) / 1.6 - indoorSpawnDeviation`
- Max = `indoorCurve.eval(normalizedTime) + indoorSpawnDeviation`

Clamped between `minEnemiesToSpawn` and either 20 or the number of free vents on the map — every selected entity must have a vent available. As with outdoor entities, `minEnemiesToSpawn` defaults to 0 and rises during eclipses (using the same per-moon value as `minOutsideEnemiesToSpawn`).

**Additional forced-spawn conditions.** With two or more players, at least one day since the previous quota fulfillment, and `minEnemiesToSpawn` currently 0, the game raises it to 1 if any of these hold:

- Scrap collected this round exceeds 80% of the profit quota, and it is after 11:24 AM.
- Scrap collected this round exceeds 65% of the total scrap value on the map.
- All players have survived 5+ consecutive days while collectively collecting more than 30 credits of scrap each round. (When landing on the Company moon, the counter uses the previous day's amount instead.)

This is effectively an anti-farming / escalation system: performing well makes the game spawn more.

Separately, **removing the Apparatus from its socket has a 70% chance to set `minEnemiesToSpawn` to 2**, if it is currently below 2.

### Vent spawn delay

A selected indoor entity is assigned a random free vent plus a random `spawningDelay`, clamped between the current time and the start of the next spawn cycle. This delay determines how far in advance the **vent crawling sound** plays — it starts quiet and grows louder. When the delay expires the sound stops, the vent opens, and the entity spawns in front of it. The vent then resets and can be reassigned next cycle.

## Related Concepts

Time, Profit Quota, Weather, Moons, Interior, Entity Targeting, Apparatus, Danger Level, Scrap, Guide: List of Spawn Chance

## Tags

lethal-company, mechanics, entity-spawning, spawn-cycle, animation-curves, normalized-time, power-level, power-count, vents, spawn-deviation, eclipsed, game-variables, unity, hermite-spline, apparatus

---

Summary generated from: https://lethal-company.fandom.com/wiki/Mechanics
