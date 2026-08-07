# Weapon

**Source:** https://lethal-company.fandom.com/wiki/Weapon

## Overview

Weapons in *Lethal Company* are a small set of store, scrap, and special items that can damage or stun entities and employees. Beyond their combat use, being **classed as a weapon in the code changes how entities perceive you** — holding one raises your threat level in Entity Targeting calculations. Notably, **not every item capable of dealing damage or stunning is classed as a weapon by the code.**

Weapons split into two classes: **damage weapons** and **stun weapons**.

## Damage Weapons

| Weapon | Source | Weight | Conductive | Damage |
|---|---|---|---|---|
| Shovel | Store, 30 credits | 14 lb | Yes | 1 HP to entities / 20 HP to employees |
| Stop Sign | Scrap | 28 lb | Yes | Identical to Shovel |
| Yield Sign | Scrap | 42 lb | Yes | Identical to Shovel |
| Kitchen Knife | Special scrap | 0 lb | Yes | 1 HP to entities / 10 HP to employees |
| Double-Barrel | Special scrap | 16 lb | No | 2, 3, or 5 HP to entities / 20, 40, or 100 HP to employees |
| Shotgun Shell | Special item | 0 lb | No | None (ammunition only) |

### Notes on each

- **Stop Sign and Yield Sign** are functionally identical to the Shovel but **twice and three times as heavy** respectively — there is no combat advantage to using them, only a carry-weight penalty. They are worth picking up as scrap, not as weapons.
- **Kitchen Knife** spawns whenever a **Butler** spawns on a moon and is obtained by killing one. It **kills Butlers in one hit**, weighs nothing, and swings faster than a Shovel — but deals only half the employee damage.
- **Double-Barrel** spawns whenever a **Nutcracker** spawns and is obtained by killing one. It consumes Shotgun Shells as ammunition and has the most complex damage model in the game, scaling sharply with range.
- **Shotgun Shells** cannot be used by themselves, but are **still classed as a weapon for threat level calculations** — simply carrying shells makes you look more dangerous to entities that check threat level.

## Stun Weapons

| Weapon | Source | Weight | Conductive |
|---|---|---|---|
| Stun Grenade | Store, 30 credits | 5 lb | No |
| Zap Gun | Store, 400 credits | 11 lb | Yes |
| Homemade Flashbang (DIY-Flashbang) | Scrap | 5 lb | No |

- **Stun Grenade** — pulling the pin starts a **3-second countdown**, during which it can be thrown. **It can be picked up again after detonation**, and used grenades are the standard tool for **safely triggering Landmines from a distance**.
- **Zap Gun** — targets a nearby entity or employee and shocks them until they break free or the wielder loses control of the beam. Battery-powered and rechargeable at the Electric Coil.
- **Homemade Flashbang** — **explodes instantly in the hands of whoever pulls the pin**, dealing 20 damage to the user, stunning nearby entities and employees, and deleting itself. Unlike the Stun Grenade it cannot be thrown first or reused.

## Threat Level Implications

Per the Entity Targeting rules, an employee **holding a defensive weapon gains +2 threat level**. Against entities that use threat level — notably **Baboon Hawks** — a higher threat level makes them keep their distance rather than attack. This means simply *holding* a Shovel is a defensive measure even if you never swing it.

## Related Concepts

Entity Targeting, Items, Scrap, Store, Shovel, Kitchen Knife, Double-Barrel, Stun Grenade, Zap Gun, Shotgun Shells, Landmine, Hitbox, Electric Coil

## Tags

lethal-company, weapon, damage-weapon, stun-weapon, shovel, kitchen-knife, double-barrel, stun-grenade, zap-gun, flashbang, threat-level, shotgun-shells, combat

---

Summary generated from: https://lethal-company.fandom.com/wiki/Weapon
