# Turret

**Source:** https://lethal-company.fandom.com/wiki/Turret

## Overview

Turrets are stationary, tripod-mounted guns that shoot employees on sight — one of the three map hazards in *Lethal Company*. They deal **50 damage per shot** as hitscan area-of-effect damage, meaning two hits kill a healthy employee. Unlike the other hazards they can be escaped once triggered, but they can also be provoked into a far more dangerous berserk state.

## Key Points

- **Damage:** 50 per shot, hitscan (AOE).
- **Fire rate:** one ray every **0.21 seconds** while firing.
- **Turrets only target employees — never entities.**
- Can be temporarily disabled from the ship's terminal.

## Behavior — Five Modes

### Deactivated

Deactivating from the terminal lasts **4.5 seconds**. After the timer expires there is a **cooldown during which the turret cannot be disabled again**. On reactivating it returns to detection mode. The short window and cooldown mean a terminal operator must time deactivation precisely rather than hold a turret off indefinitely.

### Detection (default)

The turret rotates slowly at speed 28, **reversing direction every 7 seconds** and checking for employees **every 0.25 seconds**. Detecting an employee promotes it to charging.

### Charging

The turret snaps toward the employee (rotation speed increases to 95) and, after a **1.5-second delay**, switches to firing — **unless the employee has moved out of the way**. Breaking line of sight during this window returns the turret to detection mode. This 1.5-second grace period is the single most important survival window.

### Firing

Fires a ray every 0.21 seconds for 50 damage per bullet. Crucially, the **firing radius is larger than the detection radius**, so once shooting it can track an employee beyond the range at which it originally spotted them. It continues firing for **2 seconds after the target leaves line of sight or dies**, then returns to detection if no one is visible.

### Berserk

Triggered when an employee hits the turret with a **Shovel, Stop Sign, or Yield Sign** while it is **not already firing**. After a **1.3-second windup** it fires continuously while rotating at speed 77 for **9 seconds**, hitting anything in radius for 50 damage per shot. This is far more lethal than normal operation and is almost never worth provoking deliberately.

## Occurrences

Turret counts vary per moon and the distribution is **not linear** — each moon has a custom spawn curve.

| Moon | Min | Max | Average |
|---|---|---|---|
| Experimentation | 0 | 7 | 2 |
| Vow | 0 | 7 | 2 |
| Assurance | 0 | 11 | 3 |
| Offense | 0 | 9 | 3 |
| Adamance | 0 | 12 | 3 |
| Artifice | 0 | 10 | 3 |
| Embrion | 0 | 9 | 3 |
| Dine | 0 | 18 | 3 |
| March | 0 | 15 | 4 |
| **Titan** | 0 | **35** | **12** |
| **Rend** | 0 | **0** | **0** |

**Titan is by far the most turret-dense moon**, averaging 12 and capable of 35. **Rend never spawns turrets at all.**

## Notes and Exploits

- **Placing an Extension Ladder in front of a turret blocks its fire** — anything behind the ladder is protected. The ladder appears to count as a solid plane, which is believed to be **unintentional behavior**.
- **You can crouch under the turret's bullets**, but it will keep firing at you regardless.
- The weapon model is very similar to, and likely a modified, Bren light machine gun.

## Version History

- **Launch:** Turrets added.
- **Version 45:** Mansion procedural generation updated, allowing more turrets to spawn there.
- **Version 50:** minimum turret count on Dine reduced from 3 to 0; average reduced from 6 to 3.

## Related Concepts

Map Hazard, Landmine, Spike Trap, Terminal, Extension Ladder, Shovel, Employee, Interior, Guide:Camera duty

## Tags

lethal-company, turret, map-hazard, hitscan, berserk-mode, detection-mode, terminal-deactivation, extension-ladder-exploit, titan, rend, 50-damage, line-of-sight

---

Summary generated from: https://lethal-company.fandom.com/wiki/Turret
