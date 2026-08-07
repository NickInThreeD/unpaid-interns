# Single Item Day

**Source:** https://lethal-company.fandom.com/wiki/Single_Item_Day

## Overview

Single Item Day (SID), also called Single Scrap Day, is a game event introduced in **Version 60** in which every piece of scrap on the moon is the same item type for that day. The event includes value clamping and total-value correction rules that keep the day from being either worthless or absurdly profitable. This page documents the trigger chance, the item selection filter, and the value adjustment maths.

## Key Points

- **5.2% chance per day** as of the current version.
- All scrap on the moon becomes a **single randomly chosen item** from that moon's loot pool.
- The item is picked **without regard to normal spawn chances** — a rare item is as likely to be chosen as a common one.
- Individual scrap values are clamped to the range **50–170 credits**.

## Item Selection Rules

The chosen item is re-rolled if either of these is true:

- Its **rarity is below 5**, or
- It is a **two-handed item**.

This re-roll happens **up to 2 times**. If the final rolled item still meets one of those criteria, there is a **60% chance the SID event is skipped entirely** for that day. The net effect is that low-rarity and two-handed items are heavily disfavored but not impossible.

## Value Adjustment

Individual item values are clamped into **[50, 170]** — no scrap can be worth more than 170 or less than 50 credits during a SID.

Two corrective multipliers then apply to the moon's resulting total:

- If the total scrap value exceeds **4500 credits**, every item's value is multiplied by **0.7×** (a 30% reduction).
- If the total scrap value is under **600 credits** — or under **1500 credits** when the picked item is two-handed, such as the Large Axle — every item's value is multiplied by **1.4×** (a 40% increase).

The higher floor for two-handed items compensates for the fact that they can only be carried one at a time.

## Notes

- The usual rule that **caves spawn more scrap than normal** still applies during a SID.
- **Gift Boxes**, if selected as the SID item, still open normally and drop a random piece of scrap each — making a Gift Box SID effectively a normal-variety day with extra steps.
- The **Large Axle** and **V-type Engine** are the lowest average-value SID outcomes, because two-handed items occupy your hands one at a time while one-handed items can fill all four inventory slots.
- Conversely, two-handed items benefit from the broader value-compensation threshold (1500 rather than 600 credits before the 1.4× multiplier applies).

## Version History

- **Version 60 (August 17, 2024):** Single Scrap Day added at an **8.6%** chance.
- **Version 62 (August 20, 2024):** chance reduced to **6.8%**.
- **Version 64 Beta 1 (September 2, 2024):** chance reduced to **5.2%**.

## Related Concepts

Scrap, Item Bar, Moons, Interior, Gift Box, Infestations, Mechanics, Profit Quota

## Tags

lethal-company, single-item-day, single-scrap-day, sid, event, scrap, value-clamping, two-handed, gift-box, rarity, version-60, loot-pool

---

Summary generated from: https://lethal-company.fandom.com/wiki/Single_Item_Day
