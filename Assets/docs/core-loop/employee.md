# Employee

**Source:** https://lethal-company.fandom.com/wiki/Employee

## Overview

Employees are the playable characters of *Lethal Company*. Their job is to land on moons, collect scrap, and sell it to The Company to meet the profit quota; failing the quota results in the entire crew being discharged. This page documents the player character's core systems — health, critical injury, damage sources, stamina, fear, death causes, and the built-in Echo Scanner.

## Key Points

- Employees have **100 health**.
- Damage comes from entities, other employees, map hazards, and falling.
- **Stamina** governs sprinting and jumping and is heavily affected by carry weight.
- Death produces a **Player Body** ragdoll whose cause of death can be scanned.
- Every employee has a built-in **Echo Scanner**.

## Health and Critical Injury

Falling below **20 HP** puts the employee into a **critically injured** state, shown by a HUD overlay. While critically injured:

- Health regenerates at **1 HP per second**, but only up to 20.
- Falling below **10 HP** forces a limp — greatly reduced speed and no sprinting — until the critical state ends.
- Limping employees leave a blood trail. This is purely cosmetic.

There is an important survival rule: if an employee takes a single instance of damage that would kill them, and that instance is **less than 50 damage** and they are **not already critically injured**, their health drops to **5** instead of 0. This effectively grants one free survival from any sub-50 hit at full-ish health.

### Damage values

- **Turret:** 50 per hit.
- **Stun Grenade / DIY-Flashbang:** 20, if the employee is holding it when it detonates.
- **Shovel** (employee vs. employee): 30. **Kitchen Knife:** 40.
- **Falling:** 30 damage at 35–40 units of fall value, 50 at 40–45, 80 at 45–48, and **instant death above 48**.
- **Double-Barrel:** instant kill under 15 units, 40 damage at 15–23 units, 20 damage at 24–29 units.
- **Landmine:** instant kill within 5.7 units, 50 damage at 5.7–6.4 units.
- **Jetpack explosion:** instant kill within 5 units, 50 damage at 5–7 units.
- **Lightning strike:** instant kill within 2.4 units, 50 damage at 2.4–5 units.
- **Drowning:** instant kill after 10 seconds underwater.
- **Ceiling fan:** instant kill on contact.

## Stamina

Stamina is stored internally as a value from 0 to 1 that maps to the HUD's orange bar. **The indicator only displays part of the real value** — an apparently empty bar may still hold a little stamina.

- Dropping below **0.1** causes exhaustion: no sprinting or jumping, and the character holds an arm to their chest.
- Recovery to **0.2** restores jumping; recovery to **0.3** restores sprinting.

### Depletion

- A jump costs **0.08** stamina.
- Sprinting drains **0.2 per second, multiplied by the carry weight value**, where `weight = weightInLb / 105 + 1`. Carrying nothing allows a maximum of about **5 consecutive seconds** of sprinting. Maximum possible carry weight is 315 lb.
- While being stunned by a Zap Gun, walking drains stamina at half the normal rate.

### Regeneration

Stamina regenerates whenever the employee is not sprinting:

- **Walking:** ~0.07/second — roughly 14 seconds to full.
- **Standing still:** ~0.11/second — roughly 9 seconds to full.

Taking a hit above 19 damage **refunds** roughly 0.2–0.8 stamina, calculated as `damageAmount / 125` — a deliberate mechanic that gives you a burst of sprint after being injured.

The **TZP-Inhalant** substantially reduces stamina depletion and increases regeneration, at the cost of a "drunkenness" effect.

## Fear

Fear level drives only the visual overlay and audio cues — it has no direct mechanical penalty. Numerous entity conditions raise it, each with its own fear value and increase rate. The highest-intensity sources (value 1.0) include a Snare Flea clinging to you, a Thumper attacking, a Kidnapper Fox dragging you, and a Coil-Head attacking. Looking at a dead body is also a strong source (0.9 within 10 units, 0.55 beyond). Other contributors include the Bracken, Forest Keeper, Hoarding Bug, Eyeless Dog, Ghost Girl, and Barber, generally scaled by distance and whether the entity is actively hunting.

## Death and Causes of Death

A dead employee becomes a Player Body, which can be scanned to reveal the cause of death. The end screen lists dead employees as **"DECEASED"**, or **"MISSING"** if abandoned.

Documented causes: **Abandoned** (left behind alive), **Blast** (Company Cruiser, Easter Egg, Jetpack, or Landmine), **Bludgeoning** (Shovel), **Burning** (Old Bird flamethrower), **Crushing** (Dropship, deploying Extension Ladder, Spike Trap), **Drowning**, **Electrocution** (Circuit Bees, Mask Hornets, lightning), **Fan** (Factory main-entrance fan), **Gravity** (bottomless pit, great height, or a dying Forest Keeper falling on you), **Gunshots** (Turret, or a Double-Barrel held by a Nutcracker or employee), **Inertia** (Company Cruiser crash), **Kicking** (Nutcracker), **Mauling** (generic entity kill), **Scratching** (Feiopar), **Snipped** (Barber), **Stabbing** (Butler or Kitchen Knife), **Strangulation** (Bracken or Coil-Head), **Suffocation** (Cadaver Bloom, Snare Flea, or using a Dramatic Mask at The Company), and **Unknown** (Baboon Hawk impalement, Ghost Girl decapitation, or Hygrodere consumption).

## Echo Scanner

The Echo Scanner is a built-in ability usable at any time. It highlights main entrances, the ship, logs, scrap, and entities with a green overlay, and the blue scan wave provides slight illumination in darkness. If any scanned objects have scrap value, the combined total is displayed in the bottom right.

Scanning an entity for the first time in a run pops a message for all employees: **"New creature data sent to terminal!"** — this is how Bestiary entries are unlocked.

## Version History

- **Version 50:** added "Burning", "Fan", and "Stabbing" causes of death; reworked movement on slopes.
- **Version 55:** added "Inertia" and "Snipped".
- **Version 80:** added "Scratched".

## Related Concepts

Player Body, Fear, Profit Quota, Scrap, Scanner, Bestiary, TZP-Inhalant, Item Bar, Suits, The Company, Moons

## Tags

lethal-company, employee, player-character, health, critical-injury, stamina, carry-weight, fear, death, cause-of-death, echo-scanner, damage-table, fall-damage, drowning, tzp-inhalant

---

Summary generated from: https://lethal-company.fandom.com/wiki/Employee
