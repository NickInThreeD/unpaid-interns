# Entity Targeting

**Source:** https://lethal-company.fandom.com/wiki/Entity_Targeting

## Overview

This page documents the selection and targeting algorithms some entities use in *Lethal Company*. Three numeric properties drive them: **visibility** (can I see it?), **threat level** (how dangerous is it?), and **interest level** (how appealing is it?). Understanding these lets employees deliberately manipulate how entities perceive them — crouching to become invisible, holding a weapon to look dangerous, or dropping scrap to become uninteresting.

## Visibility

Visibility determines whether an entity can perceive another entity, an employee, or scrap. **Only Baboon Hawks and Old Birds use visibility in their targeting.**

### Visibility values

Most targetable objects have a fixed visibility of **1**. Custom values:

- **Employee** — 0 if dead; **-0.25 when crouching**; **-0.16 when standing still for 0.5s** (minimum 0.59).
- **Baboon Hawk** — 0 if dead; 0.6 at their camp; 1 otherwise.
- **Forest Keeper** — 0 if dead; 1 when moving; 0.75 when standing still.
- **Dramatic Mask** — 0 if pocketed; 1 otherwise.
- **Masked** — 0 if dead; 0.5 when crouching; 1 otherwise.
- **Eyeless Dog** — 0 if dead; 1 if in chase; 0.75 otherwise.
- **Old Bird** — 0 if dead; 0.85 when spotlight is on; 1 when spotlight is on and alerted; 0.5 otherwise.

### Baboon Hawk visibility rules

A Baboon Hawk ignores any target in line of sight that meets one of these conditions:

- Visibility is exactly **zero**.
- Visibility below **0.2** and distance greater than **10 units**.
- Visibility below **0.6**, distance greater than **20 units**, and view angle greater than **30°**.
- Visibility below **0.8**, distance greater than **16 units**, and view angle greater than **80°**.

The practical takeaway: **crouching and standing still stack** to push your visibility low enough that hawks lose track of you at moderate range.

### Old Bird visibility rules

Old Birds check visibility at multiple points:

- **While flying and looking for a landing spot**, the target employee must have visibility of **at least 0.8**.
- **On the ground**, they only target something with visibility of at least **0.2** within **30 units**, or **0.58** beyond that.

**This means crouching employees cannot be seen by flying Old Birds** — a directly actionable defense on Artifice and Embrion.

Additionally, an Old Bird's **alert timer increases faster in linear proportion to its target's visibility**, so staying low delays their aggression as well as their detection.

## Threat Level

Threat level tells an entity how dangerous a target is. **Only Baboon Hawks use threat level in their targeting.**

Most objects have a fixed threat level of **0**.

### Employee threat level modifiers

Starting from 0:

- **+2** — holding a defensive weapon.
- **+1** — after using a noise-maker item (clown horn, airhorn, hairdryer, or cash register). **This bonus is removed immediately when the sound effect ends** — the basis of the "hairdryer strat".
- **-1** if your viewing angle is greater than 100°; **+1** if it is smaller than 45°. In short, **looking directly at the hawk makes you more threatening.**
- **+1** before **9:36 AM**; **-1** after **8:24 PM**. You are inherently safer early in the day.
- **+1** if inside the ship.
- **-1** if outside the ship and further than **30 units** from the entity.
- **+1** if health is above 29; **-2** if critically injured. **Being hurt makes you a preferred target.**
- **+1** in singleplayer.

### Entity threat levels

- **Baboon Hawk** — 1 by default; +1 if aggressive; +1 if part of a scouting group.
- **Forest Keeper** — fixed **18**, by far the highest.
- **Dramatic Mask** — 1 by default; 4 if held; 2 if picked up in the last frame.
- **Masked** — fixed 3.
- **Eyeless Dog** — 5 by default; dropped to 3 at 1 HP; +3 if in chase.
- **Old Bird** — 7 by default; +3 if alerted.

## Interest Level

Interest level is used mainly by **Baboon Hawks** to choose between available targets. **All scrap items and all entities have a default interest level of 0.**

Employees start at zero, and each of these adds **+1**:

- Holding a scrap item.
- Carrying more than **23 lb**.
- Carrying more than **53 lb**.

A fully laden employee is therefore up to **3 interest levels** more appealing than an empty-handed one — which is exactly why Baboon Hawks converge on whoever is hauling loot.

## Practical Summary

Against Baboon Hawks specifically, the recommended posture combines all three systems: **hold a weapon (+2 threat), look directly at them (+1 threat), carry as little scrap as possible (low interest)**, and where evasion is preferable, **crouch and stand still to drop visibility below their detection thresholds**. Against Old Birds, **crouching alone defeats aerial detection entirely.**

## Related Concepts

Baboon Hawk, Old Bird, Weapon, Employee, Fear, Audible Sounds, Scrap, Item Bar, Mechanics, Hitbox

## Tags

lethal-company, entity-targeting, visibility, threat-level, interest-level, baboon-hawk, old-bird, crouching, hairdryer-strat, defensive-weapon, detection, ai-algorithms

---

Summary generated from: https://lethal-company.fandom.com/wiki/Entity_Targeting
