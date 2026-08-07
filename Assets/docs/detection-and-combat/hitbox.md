# Hitbox

**Source:** https://lethal-company.fandom.com/wiki/Hitbox

## Overview

The hitbox is *Lethal Company*'s collision and damage-judgment volume. It determines both where an entity can be hit and the physical space it actually occupies. Because hitboxes are built from simple primitives rather than the visual model, **every entity's hitbox differs from its appearance** — sometimes dramatically. Understanding this is what separates reliably landing hits from missing an enemy standing right in front of you.

## Core Principles

- Hitboxes are composed of **rectangular prisms, cylinders, spheres, frustums**, and combinations or cuts of those shapes.
- **Colliding hitboxes block each other's movement** — this applies to entities and employees alike. Some entities have hitboxes large enough to block employees in narrow corridors, notably the **Coil-Head**.
- For most entities, **the hitbox *is* the attack weapon**, since they attack by physical contact. Exceptions that attack by other means include the **Nutcracker, Butler, and Old Bird**.
- Employee weapons each have their own **attack hitbox**.
- An invincible entity's hitbox still matters for blocking, and **melee attacks on one produce a special hit sound effect**.
- **Different parts of the same entity can have different properties** — see Thumper and Eyeless Dog below.
- **Some entities have no hitbox at all** yet still deal damage (Circuit Bees), implying they attack via an invisible point of contact.

## Notable Entity Hitboxes

### Indoor

- **Barber** — a horizontal frustum with square top and bottom edges. **The scissors are the entire hitbox; the purple body has none.** Contact kills instantly.
- **Bracken** — a rectangular prism of consistent size whether standing or lying down; only its position on the body shifts.
- **Bunker Spider** — **very different from its appearance: the legs have no hitbox at all**, only a small prism containing the head and body. **Crouch when attacking one.**
- **Coil-Head** — a body-sized prism. **Critically, this hitbox is NOT the check for whether an employee is looking at it.** That judgment uses a single **point located between its legs, below the crotch**.
- **Ghost Girl** — no hitbox and no physical properties, though she cannot ignore terrain.
- **Hoarding Bug** — three parts: a body prism plus two small leg prisms. **Scrap theft is judged by both leg hitboxes contacting the scrap.**
- **Hygrodere** — commonly believed to have none, since employees and entities walk through it. However, fast-moving entities (aggro Hoarding Bugs, chasing Coil-Heads) shove it violently when they pass, implying it has a **very small** hitbox rather than none.
- **Jester** — a body-sized prism whose top is flush with its visual top, so **employees can genuinely stand on top of a Jester.**
- **Snare Flea** — a very small prism covering only about half its visible body.
- **Spore Lizard** — body-sized prism, but its **attack reaches several meters in front of the hitbox**; the hitbox itself deals no damage.
- **Thumper** — two stacked prisms, larger below and smaller above, with a single symmetry plane aligned to its head/movement direction. **Attacking the head guarantees a hit**, since the head is always inside the hitbox while the limbs and rear can rotate out of it. **However, the head hitbox is immune to Kitchen Knife damage** — which is why many players wrongly believe Thumpers can't be knifed.

### Outdoor

- **Baboon Hawk** — two upright prisms, shorter at the head and taller at the body. **The only entity whose hitbox volume is much larger than its visual volume** — the wings fall outside it, but the head box adds extra judgment space.
- **Earth Leviathan** — **no hitbox at all**, only textures and animation, which explains how it ignores terrain. Instead a **"devouring judgment circle"** travels along its path with its mouth; anything touching it is removed from the game outright. **Classic disaster case:** if the ship is taking off while an Earth Leviathan lunges skyward at a stranded employee and the paths intersect, **every employee aboard the ship dies.**
- **Eyeless Dog** — a capsule body (cylinder capped by two hemispheres) plus a **cube head**. **Melee weapons striking the head deal 2 HP of shovel damage instead of 1** — double damage for hitting the head.
- **Forest Keeper** — a capsule body plus two frustum arms. **The hitbox is only on the lower body**, so attacking the legs is the only thing that works.
- **Kidnapper Fox** — a body-sized prism, with a **separate long prism hitbox for its tongue** during tongue attacks.
- **Old Bird** — while a statue, the hitbox roughly matches its appearance and **you can stand on it**. Once activated, it becomes a prism containing the legs. Its artillery has its own separate hitbox.

### Daytime

- **Circuit Bee, Roaming Locust** — no hitbox, no physical properties, but cannot ignore terrain.
- **Manticoil** — body-sized prism; **immune to Double-Barrel damage**.
- **Tulip Snake** — body-sized prism.

## Weapon Hitboxes

**Shovel, Yield Sign, Stop Sign, and Kitchen Knife** all use the same shape: a capsule (cylinder capped by two hemispheres). The **Double-Barrel's misfire explosion hitbox is a sphere**.

## Practical Tips

- **An entity's true position is the geometric center of its hitbox** — this is where the red dot appears on the monitors, and the mathematical endpoint used for all distance calculations between entities.
- Because melee weapon hitboxes are large solids, **one swing can hit multiple targets.**
- **The Jester's hitbox is large enough to block a door.** If a Jester traps you in a dead end while winding its music box, the **only survival option is a teleporter extraction.**
- The Coil-Head blocks entity movement entirely — you can lure one to a chokepoint to **divide or block other entities** from reaching a safe room.
- **Crouch when attacking small or low entities** — Bunker Spiders, Snare Fleas, Thumpers — so your crosshair lines up with the actual hitbox.
- Because the **Nutcracker is much taller than most indoor entities**, its shots can only accidentally hit other Nutcrackers, Masked, and Brackens.
- **Kill a Thumper with a Kitchen Knife by attacking from the side**, avoiding the damage-immune head hitbox.

## Related Concepts

Entity, Entity Targeting, Weapon, Shovel, Kitchen Knife, Double-Barrel, Monitors, Teleporter, Employee

## Tags

lethal-company, hitbox, collision, damage-judgment, melee, crouch-attack, thumper, eyeless-dog-head, bunker-spider, earth-leviathan, jester, coil-head, weapon-hitbox, targeting

---

Summary generated from: https://lethal-company.fandom.com/wiki/Hitbox
