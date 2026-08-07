# Outdoor

**Source:** https://lethal-company.fandom.com/wiki/Outdoor

## Overview

Outdoors is the exterior half of every moon, complementing the procedurally generated Interior. Unlike the interior, **the outdoor layout is fixed** — it always has the same general appearance and arrangement for a given moon, including the positions of the ship, dropship, main entrance, and fire exits. This page documents that structure, the entities and hazards that occupy it, the interactive objects found there, and the terrain features that shape routing.

## Key Points

- Outdoor and Interior are connected by the **Main Entrance** and **Fire Exit(s)**.
- Every moon has **one main entrance and one fire exit**, except **61-March which has three fire exits** and **71-Gordion which has none**.
- The Ship and Dropship landing positions are **fixed** per moon.
- **71-Gordion has no interior at all** — its entire map is outdoors.

## Purpose and Value of Exploring

From a design standpoint, the outdoors exists mainly to make collecting scrap harder. Beyond the necessary route from the ship to the main entrance and fire exit, exploring is mostly unnecessary — the exceptions being **Bee Hives** and **Data Chips**.

That changed in **Version 55**: Kidnapper Foxes now spawn from **Vain Shrouds**, red weeds that appear randomly outdoors. Clearing Vain Shrouds with **Weed Killer** prevents Kidnapper Foxes from spawning, which makes deliberate outdoor exploration genuinely useful as threat reduction.

## Entities Outdoors

Outdoor and Daytime entities can **only** spawn outdoors. The number of **Circuit Bees and Old Birds is determined at the start of the day**, and their Bee Hives and Old Bird statues spawn in seed-determined fixed locations.

Two indoor entities can appear outdoors: the **Masked** and the **Ghost Girl**. **Snare Fleas** can also end up outside if they ensnare an employee who is then teleported — but the flea dies instantly.

### Outdoor entity roster

Eyeless Dog (12 HP, power 2, max 8, favorite moon Titan), Forest Keeper (20–30 HP, power 3, max 3, favorite Vow), Earth Leviathan (invincible, power 2, max 3, favorite Assurance), Old Bird (invincible, power 3, max 20, favorite Embrion), and Baboon Hawk (4 HP, power 0.5, max 15, favorite Adamance).

Standouts by moon: **Titan is overwhelmingly Eyeless Dogs (~65%)**, **Vow is overwhelmingly Forest Keepers (~65%)**, **Embrion is almost entirely Old Birds (~87%)**, and **Adamance is Baboon Hawk territory (~55%)**. Old Birds do not spawn at all on Assurance, Vow, March, or Rend; Baboon Hawks do not spawn on Experimentation, Rend, Dine, Titan, or Embrion.

### Daytime entity roster

Circuit Bee (invincible, power 1, max 6, favorite March), Manticoil (1 HP, power 1, max 16, favorite Offense), Roaming Locust (invincible, power 1, max 5, favorite Experimentation), and Tulip Snake (1 HP, power 0.5, max 12, favorite Adamance).

**Offense and Embrion have a 100% Manticoil rate.** Rend, Dine, and Titan have no daytime entities at all.

## Entity-Associated Objects

- **Old Bird statues** are the carriers for Old Bird spawning rather than a byproduct. An inactive statue has a collision box you can stand on and can be scanned to add Old Bird data to the terminal. On activation, a live Old Bird replaces the statue and the collision box disappears.
- **Baboon Hawk nests** appear when at least two groups (four hawks) spawn, and serve as the spawn point. Each hawk rolls a leadership value of 0–500 determining its size, and the highest becomes group leader.
- **Vain Shrouds** (*Phlebodium Ruber*) are small red weeds spawning in large, size-varied groups. They are **required for Kidnapper Foxes to spawn**, in the same way Baboon Hawks require a nest. Weed Killer eradicates them, both stopping further fox spawns and making it harder for existing foxes to kill.

## Scrap Outdoors

**The only scrap that spawns outdoors is the Bee Hive**, which appears alongside Circuit Bees. Its chance mirrors the Circuit Bee spawn chance: March (~35%), Vow (~26%), Artifice (~23%), Experimentation (~22%), Assurance (~21%), Adamance (~20%). Bee Hives never spawn on Offense, Rend, Dine, Titan, or Embrion.

## Data Chips

Data Chips are special outdoor interactive objects in **fixed locations** that can be **picked up only once** and never respawn. Collecting one adds the corresponding Sigurd lore entry to the terminal. Twelve are documented, spread across Assurance, Experimentation (water tower and pipe), Rend (fire exit, hill behind the cottage, and far beyond the fire exit), Gordion (catwalk), March (south fire exit), Vow (dam parapet wall), Dine (hill right of the fire exit), and Titan (platform left of the fire exit and atop the big pipe).

## Interactive Elements

- **Company Monster** (Gordion) — the only way to sell scrap. Ring the golden bell on the sales window to wake it; it collects everything placed in front of the window. **Ringing too often or making too much noise causes it to instantly kill a nearby employee.**
- **Catwalk** (Gordion) — an underground platform reached via a freely openable hatch and a long ladder. Holds the "Sound behind the wall" data chip. A nearby platform, reachable by parkour from the scaffolding, has a light controlled by a suspended switch. This is the area containing the drill speculated to be the game's future ending. Since **Version 70** the battery hatch on the container can be opened.
- **Cottage** (Rend and Adamance) — wooden houses with lights and doors. Adamance's is locked and contains the wall clue pointing to 68-Artifice.
- **Curtained Door** (Artifice) — horizontally sliding garage doors, each with an unlimited-use lever. **A closed curtained door blocks all entities and all projectiles, including Old Bird artillery.**
- **Ladders** — yellow-and-black fixed climbing objects, unusable while in two-handed mode; the Masked can use them too. The ship has three (one long to reach the roof, two short from the ground). Additional ladders spawn on Gordion (1), Experimentation (5), Assurance (2), Offense (1), and Titan (1).
- **Garages** (Artifice) — four buildings, two per side of the main road, varying in lighting, catwalk connections, steam, curtained door state, and interior rooms; the one on the right near the main entrance is completely sealed and has a purely decorative antenna tower.

## Terrain Features

- **Shutter Door** (Experimentation) — a rolling door before the main entrance governed by a hidden pass-through detection system. **Each time a player passes under it there is a 25% chance it drops slightly**; when fully closed it blocks the route between ship and main entrance.
- **Bridges** (Vow and Adamance) — Vow has two equal-length bridges, one sturdy and one breakable. Adamance has a long and a short bridge, both breakable under different rules: the **short bridge has 3 "lives"** and always breaks on the fourth touch, unaffected by weight or time (though jumping counts), while the long bridge and Vow's unstable bridge use a more complex durability system.
- **Pumpkins** — random outdoor decoration that can **block paths to the main entrance**, particularly on Offense and Titan.
- **Rocks** — much larger random decorations, generally impossible to climb without a Jetpack, extension ladder, or nearby elevated ground.
- **Barn** (Experimentation) — a large steam-filled, unlit building containing only two yellow-fenced platforms. **Its size prevents all outdoor entities from entering, including the Earth Leviathan**, making it a reliable panic room.
- **Lakes** (Vow and March) — submerging long enough causes drowning.
- **Quicksand** — slowly submerges an employee until death; retreating quickly enough before full submersion saves you. Permanent patches: Experimentation (5, around the fences fore of the ship's bow), March (4), and Adamance (1, near the main entrance).

## Weather Outdoors

The outdoors is the part of the map most affected by weather. **Eclipsed** alters entity spawn frequency and timing, while the other four types create non-entity threats: lightning (Stormy), reduced visibility (Foggy), quicksand (Rainy), and rising water (Flooded). Weather availability varies per moon — Embrion supports only Foggy and Eclipsed, Rend only Stormy and Eclipsed, while Experimentation, Assurance, and Adamance support all five.

## Related Concepts

Interior, Moons, Weather, Water, Vain Shroud, Weed Killer, Lore, Bee Hive, Curtained Door, Cottage, Fire Exit, The Company Monster, Pumpkin, Map Hazard

## Tags

lethal-company, outdoor, exterior, fixed-layout, fire-exit, main-entrance, data-chips, vain-shroud, weed-killer, bee-hive, quicksand, bridges, shutter-door, barn, ladders, curtained-door

---

Summary generated from: https://lethal-company.fandom.com/wiki/Outdoor
