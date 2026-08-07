# Door

**Source:** https://lethal-company.fandom.com/wiki/Door

## Overview

Doors are a core interior mechanic in *Lethal Company*. Beyond simple navigation, closing a door behind you is a fundamental survival tool — every indoor entity opens doors at a different speed, and doors block several damage sources outright. This page covers facility and cottage doors; Secure Doors, main entrance/fire exits, ship pressure doors, and Curtained Doors have their own pages.

## Key Points

- Doors spawn **randomly at the junction of two rooms**. When the map generates from the seed, it reserves door positions and then decides whether to place one; if not, a door-sized gap is left for employees to pass through.
- **Outdoor doors are fixed, not random** — for example the cottage door on Rend.
- **Opening:** hold [E] for **0.3 seconds** (cottage doors take **0.5 seconds**), shown by a progress circle.
- **Closing:** hold [E] again — the door closes immediately.
- **Employees can scan scrap and entities through a closed door.**

## Door Types

### Factory Door

Rectangular iron doors with a small **transparent glass window** that lets limited light and vision through. All have a handle and a pair of hinges, though the hinges are only visible from the inner side. Handle position identifies orientation: **left = outer, right = inner.**

### Mansion Door

Smooth-edged rectangular wooden doors with shallow grooves and a copper handle on both sides. Handle position again gives orientation: **left on the inner side, right on the outer.**

**Navigation exploit:** mansion doors always open **away** from you when you are heading toward the main entrance, and **toward** you when heading away from it. (Note: the Interior page records that this heuristic was patched in v80.)

Mansion doors can also **open and shut by themselves with a loud sound**, as if haunted — see the haunted door mechanic below.

### Cottage Door

A rectangular wooden door in darker brown wood than the mansion variant, appearing **only in the cottages on Rend and Adamance**. It rotates inward when opened. The Adamance cottage door is **locked**, requiring a Key or Lockpicker — and works normally even when the cottage is submerged during flooded weather.

### House Door

Appears only in one of Artifice's garages; resembles a mansion door without the smooth edge.

### Experimentation Garage Door

Unique to Experimentation. **It closes in stages each time you pass under it** — the more trips you make between the ship and the main entrance, the more it closes, eventually blocking the route entirely.

### Artifice Garage Doors

Four lever-operated doors on Artifice, commonly used to trap large monsters inside a garage. See Curtained Door.

### Mineshaft Doors

Two categories, each consisting of two small doors — one fixed and one operable:

- **PUSH door** — has a mini cylinder with a rectangular metal sheet that employees push aside, flanked by two symmetrical cylinders.
- **PULL door** — marked "KEEP OUT"; the marked panel opens freely while the handled panel is fixed.

**Light coding:** a **blue light** above a mineshaft door marks a **cave entrance**. Non-cave mineshaft doors have a chance to spawn a **yellow light** instead.

## Locked Doors

Factory and mansion doors can spawn locked. Only two things open them:

- **Key** — consumed on use, disappearing from the item bar.
- **Lockpicker** — takes **30 seconds** to open a door, but is **reusable indefinitely**.

## Entity Interaction

**All indoor entities can open doors except Barbers and Hygroderes**, at widely varying speeds:

| Entity | Door open speed |
|---|---|
| Nutcracker | 0.5s |
| Masked | 0.5s |
| Hoarding Bug | 0.7s |
| Ghost Girl | 0.7s |
| Bracken | 0.8s |
| Maneater | 0.8s |
| Jester | 2s |
| Thumper | 3.3s |
| Spore Lizard | 3.3s |
| Snare Flea | 4.3s |
| Bunker Spider | 6.7s |
| Butler | 13.5s |
| **Coil-Head** | **16.7s** |

**Masked and Bracken are practically unimpeded by doors**, while **Thumpers and Coil-Heads are slowed enormously** — deciding when to close a door and when to simply run is a core survival skill.

**Important:** opening doors **does not reset the speed** of Thumpers or popped Jesters, so a door will not shake off a charging Thumper.

### The glass window advantage

The Factory door's window lets employees spot entities on the far side before opening — and **entities cannot see employees through it**. The one exception is the **Coil-Head, which can see through the glass** due to how it's coded. This rarely matters except on stairs, where you can see its feet (where its "eyes" are in roaming mode).

## Damage Blocking

Doors block **landmine blasts, stun grenade blasts, and turret gunshots**. They **do not** block **Double-Barrel shots**, which makes fighting Nutcrackers through doorways difficult.

## Haunted Doors

As of **Version 62**, mansion interior doors have a **20% chance to become haunted**.

Every 0–30 seconds, a haunted door has a **50% chance** to enter "haunting mode". While in that mode it opens or closes as soon as all of these are true:

- The player's sanity is at least **83%**.
- The player is within **18 units** of the door.
- The player has **line of sight** to the door.

A closed haunted door will open. An open one has an **additional 70% chance** to close.

## Related Concepts

Interior, Secure Door, Curtained Door, Fire Exit, Key, Lockpicker, Cottage, Elevator, Breaker Box, Scanner, Item Bar, The Ship

## Tags

lethal-company, door, factory-door, mansion-door, cottage-door, mineshaft-door, locked-door, key, lockpicker, door-open-speed, haunted-doors, entity-blocking, damage-blocking, navigation

---

Summary generated from: https://lethal-company.fandom.com/wiki/Door
