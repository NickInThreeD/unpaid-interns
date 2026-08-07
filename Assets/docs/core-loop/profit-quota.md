# Profit Quota

**Source:** https://lethal-company.fandom.com/wiki/Profit_Quota

## Overview

The Profit Quota is the central objective of *Lethal Company*: the minimum total scrap value a crew must collect and sell to The Company within each quota cycle. Meeting it continues the run; failing it ends the game with the entire crew jettisoned into deep space. This page documents the quota cycle structure, the quadratic formula that escalates each quota, the luck system driven by ship furniture, and the overtime bonus that rewards selling early.

> **⚠ Version caveat.** The wiki records that **Version 80 changed the quota increase evaluation**, and the source page's own randomizer-curve figures are annotated by its editors as *"not correct as of v80"*. The formula below is the documented pre-v80 model. Treat the **structure** (quadratic increase, luck-shifted random multiplier) as reliable and the **exact curve** as unverified for current versions.

## Key Points

- Each quota cycle lasts **4 days**, counting down 3 → 2 → 1 → 0 days remaining. The wiki describes this as **3 days to explore and collect**, with day 0 being the deadline day.
- You *can* land on a normal moon on day 0, but you must reach Gordion to sell before the day ends or the run is lost.
- The **first quota is always 130 credits**.
- Failing to meet a quota discharges the crew — a hard game over.
- You may land on any moon on day 0, but **not landing on Gordion (The Company) to sell on the last day is an unavoidable loss**.

## Quota Calculation

The quota escalates as a **quadratic function** of how many quotas have already been fulfilled, meaning the *increase* itself grows with every cycle:

```
quotaIncrease = 200 * (1 + timesFulfilled² / 4)
              * (randomizerCurve.eval(clamp(random(0,1) - (totalLuck * 1.5), 0, 1)) + 1)
```

The increase is multiplied by a Unity animation curve evaluated against a random input in `[0, 1]`. That random input is reduced by `totalLuck × 1.5`, lowering the expected quota jump.

Because the increase scales quadratically, **quota values scale cubically and the total money required to reach the nth quota scales quartically** — the difficulty ramp is steep by design.

### The luck system

`totalLuck` is the combined luck value of all furniture placed in the ship **at the time of the previous quota calculation**. This timing matters: except for the first quota, furniture placed during quota *n* only affects the luck of quota *n + 1*.

Owning every piece of furniture except the Signal Translator yields a maximum luck of **0.2043**, which subtracts **0.30645** from the random input and restricts the randomizer range to `[0, 0.69355]`.

Highest-value contributors are the **Disco Ball (0.06)**, **Television (0.02)**, **Shower (0.015)**, and **Jack O' Lantern (0.012)**, with most other furniture between 0.003 and 0.01. The **Signal Translator is the only item with negative luck (-0.012)** and should be excluded from a luck-optimized ship.

### Probability distribution

The randomizer curve closely resembles the integral of a normal distribution and can be approximated as a bell curve with **mean 0.018 and standard deviation 0.121**. As an illustration, the chance of rolling a value within `[-0.1, 0.1]` is about **58.71%**.

## Overtime Bonus

Selling more than the required quota grants an overtime bonus, rounded down:

```
overtimeBonus = (quotaFulfilled - profitQuota) / 5 + 15 * daysUntilDeadline
```

`daysUntilDeadline` begins at `totalDays - 2` and decrements at the end of each day, going negative on day 0. This functions as an **efficiency bonus or penalty**:

| Day | `daysUntilDeadline` | Efficiency bonus |
|---|---|---|
| 3 | 2 | **+30** |
| 2 | 1 | **+15** |
| 1 | 0 | **0** |
| 0 | -1 | **-15**, floored at 0 |

The bonus is **capped at 0**, so this term can never take credits away — it only fails to add any.

### ⚠ Do not read this as "sell early"

The efficiency term rewards early delivery **in isolation**, but it is dwarfed by the buy-rate penalty documented on the Credits page: scrap sells at **30% / 53% / 77% / 100%** of value at 3 / 2 / 1 / 0 days remaining. Selling one day early costs roughly a quarter of your entire haul's value to gain at most 15 credits. **The correct play, and the balance intent, is to sell on day 0.** The efficiency bonus is a small consolation term, not an incentive to sell early.

### ⚠ The wiki contradicts itself on these figures

The **Guide:Contract** page lists the efficiency bonus as **45 / 30 / 15 / 0** at 3 / 2 / 1 / 0 days left — one step higher than the table above at every point. The two pages use a **different day-index convention** (`daysUntilDeadline` runs 2 → -1 while Guide:Contract's "days left" runs 3 → 0). The formula-derived table above is the one consistent with the worked example below, so prefer it — but be aware the discrepancy exists in the source.

## Optimizing a Sale

To sell the minimum scrap needed to reach a target credit total. **This form has `daysUntilDeadline = -1` baked in — it is only valid for selling on day 0, the final day:**

```
quotaFulfilled = (5 * total + profitQuota + 75) / 6
```

Round **up** if the result isn't whole, to avoid falling short.

The `+ 75` term is `-75 × daysUntilDeadline` with `daysUntilDeadline = -1`. The general form is:

```
quotaFulfilled = (5 * total + profitQuota - 75 * daysUntilDeadline) / 6
```

**Example:** to reach 900 credits for a Jetpack while fulfilling the first quota of 130:

```
quotaFulfilled = (5 * 900 + 130 + 75) / 6 = 784.16  →  round up to 785
overtimeBonus  = (785 - 130) / 5 + 15 * (-1) = 131 - 15 = 116
total          = 785 + 116 = 901
```

Note this confirms `daysUntilDeadline = -1` on the selling day — the source page describes this as "0 days until deadline" in prose, which conflicts with its own table. The arithmetic above is the authority.

## Warnings

- The quota's quadratic growth means later cycles demand disproportionately more scrap per day; plan moon selection accordingly.
- Furniture luck is applied one cycle late, so buying decor for luck only pays off from the *following* quota onward.
- Deaths cost 8% (body recovered) or 20% (body lost) of total credits, which interacts directly with quota planning.
- **All scrap value figures here assume the 100% buy rate.** Combine with the Credits page before modelling income.

## Version History

- **Version 55:** luck added.
- **Version 80:** quota increase evaluation changed. The specific change is not documented on the source page.

## Related Concepts

Scrap, The Company, Credits, Store, Decor, Time, Moons, Signal Translator, Player Body, Performance report

## Tags

lethal-company, profit-quota, quota, credits, overtime-bonus, luck, furniture, decor, quadratic-scaling, randomizer-curve, gordion, selling, efficiency-bonus, game-over

---

Summary generated from: https://lethal-company.fandom.com/wiki/Profit_Quota
