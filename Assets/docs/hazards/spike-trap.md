# Spike Trap

**Source:** https://lethal-company.fandom.com/wiki/Spike_Trap

## Overview

The Spike Trap is a map hazard in *Lethal Company*: a spiked platform suspended from a support beam that slams down and **instantly kills** all employees and killable entities beneath it. Its behavior and timing are decided at spawn from the map seed and its position, producing two distinct trap types that require different counterplay — and that can also be weaponized against entities.

## Key Points

- **Damage:** instant kill on collision.
- Behavior and slam interval are fixed at spawn, derived from the **map seed and spawn position**.
- Kills both employees and killable entities.
- Can be temporarily deactivated from the ship's terminal.

## Behavior — Two Modes

### Interval Mode (80% chance)

The trap slams on a repeating timer, with the interval drawn from a weighted distribution:

- **81% chance:** slams every 0.8–10.8 seconds.
- **10% chance:** slams every 0.8–26.2 seconds.
- **9% chance:** slams every 0.8–2.1 seconds.

In this mode the trap **does not slam when an entity is within 8 units of the plate — unless a player is also within that radius.** This is what stops interval traps from harmlessly clearing entities on their own.

### Detection Mode (20% chance)

The trap does not slam on its own. It only fires when **a player crosses a 4.4-unit line in front of the support beam**. It will kill vincible entities standing underneath at the moment a player triggers it.

**Because the sensor line is so thin, an employee can jump over it and survive — even carrying heavy items.**

## Collision Timing

The trap kills on contact, but it **only detects collisions 0.75 seconds after it has started going back up**. This creates a brief window during the retraction where the spikes are physically present but harmless.

## Occurrences

Counts vary per moon on a non-linear custom curve.

| Moon | Min | Max |
|---|---|---|
| Experimentation | 0 | 0 |
| Vow | 0 | 0 |
| Assurance | 0 | 2 |
| Titan | 0 | 2 |
| Offense | 0 | 4 |
| Rend | 0 | 7 |
| Dine | 0 | 8 |
| March | 0 | 9 |
| **Embrion** | **1** | 14 |
| Artifice | 0 | 17 |
| **Adamance** | 0 | **35** |

**Experimentation and Vow never spawn spike traps.** **Embrion is the only moon with a guaranteed minimum of 1.** **Adamance has by far the highest ceiling at 35.**

## Strategy

Avoiding a spike trap is largely a matter of remembering where it is. **The sound of its descent is not weakened or blocked by any building structure**, so it always announces itself — its danger comes from appearing suddenly in the dark.

**Counterintuitively, low-frequency traps are more dangerous.** A trap that slams rarely produces sound rarely, making it much harder to locate. Carrying a **Flashlight** mitigates this by letting you spot traps in dark areas.

### Using traps as weapons

A spike trap kills any killable indoor entity almost instantly, so it can be used offensively. Documented approaches include placing scrap nearby to lure **Hoarding Bugs** under it, or baiting a **Thumper** into running beneath.

However, **interval traps are unreliable for this** — only high-frequency ones have real utility, since slow traps often miss their target. **Detection-mode traps are far more reliable**, because the player directly controls when they fire.

## Version History

All changes landed in **Version 50** across several updates:

- **Update 2:** Spike Trap added.
- **Update 3:** red light attached to traps; spawn rates altered across maps.
- **Update 4:** traps within 7 units of an entrance have their minimum slam interval set to 1.25s; traps halt for 0.5 seconds when an employee enters or leaves the facility; all traps received slightly different SFX through random pitching.

## Related Concepts

Map Hazard, Landmine, Turret, Terminal, Interior, Flashlight, Employee, Player Body, Guide:Camera duty

## Tags

lethal-company, spike-trap, map-hazard, instant-kill, interval-mode, detection-mode, slam-interval, entity-killing, adamance, embrion, seed-based, flashlight

---

Summary generated from: https://lethal-company.fandom.com/wiki/Spike_Trap
