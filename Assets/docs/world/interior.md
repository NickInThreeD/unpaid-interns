# Interior

**Source:** https://lethal-company.fandom.com/wiki/Interior

## Overview

The Interior — also called the Indoor Map, Map Layout, or Facility — is the procedurally generated building found on almost every moon and the place employees spend most of their time scavenging scrap. Three interior types exist: **The Factory**, **The Mansion**, and **The Mineshaft**. This page documents the shared map features (entrances, vents, power grid), the per-moon spawn probabilities for each type, and the distinctive rooms and rules of each layout.

## Shared Map Features

### Main entrance

Locatable with the scanner, this is the primary way in and out. It always has a dedicated starting room with at least one exit.

### Fire exit

Every facility has at least one, leading into a random part of the map — it can replace essentially any room's doorway.

### Vents

Vents are the spawn points for **all indoor entities**. On the ship's monitor they appear as red lines along the walls. When an entity is assigned to a vent, it is given a random spawn delay, during which the vent emits a continuous rumbling/growling noise that grows louder until the entity emerges.

Vents have two visible states:

- **Closed** — nothing has spawned from that vent yet.
- **Open** — at least one entity has spawned there.

**An open vent does not stop further spawns.** Vents open on the first spawn and stay open for the rest of the day; the state is purely cosmetic.

### Power grid

Each interior has its own power grid controlled by the **Breaker Box**. Flipping switches **left** turns power on; flipping **any** switch right cuts power to the entire map. Breaker boxes emit a distinctive hum.

Cutting power turns off all lights in the area and **locks all secure doors in the open state** — they cannot be closed again, even from the ship terminal, until power is restored. Removing the **Apparatus** cuts interior power permanently. (Secure doors and the Apparatus exist only in the Factory layout.)

**Landmines and Turrets keep working with no power** despite being electronics, and still require the ship's terminal to disable temporarily.

## Interior Spawn Chances

Every moon except March can roll any of the three interiors. Some are near-deterministic, others vary widely:

- **Overwhelmingly Factory:** March (100%), Experimentation (~99%), Assurance (~87%), Embrion (~85%), Adamance (~84%), Titan (~64%), Vow (~61%).
- **Overwhelmingly Mansion:** Dine (~90%), Rend (~85%), Liquidation (~72%).
- **Overwhelmingly Mineshaft:** Offense (~82%).
- **Highly variable:** Artifice — Mineshaft ~50%, Mansion ~35%, Factory ~15%.

## The Factory

The most common variant and the first most players see. It consists of long concrete hallways and corridors punctuated by large rooms, with metal railings, catwalks, pipes, and machinery throughout.

**Unique to the Factory:**
- **Steam leaks** — a large area fills with steam, heavily obscuring vision. Fixed by finding and pulling the loose valve (marked by a jet of steam and loud hissing); the steam dissipates a few seconds later.
- **Breaker Boxes can spawn with some switches already off**, leaving areas unpowered from the start until manually fixed.
- **The Apparatus can only spawn here.**

### Notable Factory rooms

- **Main Entrance Room** — the only complex room with double doors; up to 3 doors, of which up to 2 may be locked or blocked. Can contain loot.
- **Gap Room** — two catwalks with broken railings and a metal beam spanning half the gap, jumpable. Up to 6 doors total.
- **Staircase** — vertical connector between floors, up to 2 doors per floor. Loot commonly sits in corners or on the side pipes.
- **Corridor Catwalk** — a long catwalk with doors at each end plus two off-shooting catwalks.
- **Apparatus Room** — two doors, houses the Apparatus, can hold other loot. **Both doors are sometimes locked, requiring a key.**
- **Server Room** — maze-like racks of servers, two door positions, usually rich in scrap.
- **Locker Room** — a balcony overlooking the room and a **central rectangular pit that kills on falling**. Six potential doors. May contain openable lockers (up to two, in opposite corners) whose scrap often clips into the bottom and is easy to miss. A single piece of scrap can rarely spawn atop the concrete pillar upstairs, reachable only by jumping from the catwalk railing.
- **Storage Rooms** — three variants: a rectangular "pill", an "elbow" of two intersecting rectangles, and a spacious "stair" version with a lower level. The stair version can contain the Apparatus. **The fire exit can appear in the pill and elbow variants but never in the stair variant.**
- **Yellow Office Room** — the "Backrooms" or "Bracken Room": yellow walls, carpeted floor, only 1 door. **The myth that Brackens spawn or prefer this room is false** — Brackens spawn from vents like every other indoor entity, and their favorite room is simply the one furthest from the main entrance. No entity enters this room unless chasing a player into it. Its lights stay on even with the breaker off or the Apparatus removed.
- **Piped Hallways** — labyrinthine grid-patterned corridors lined with piping, where most Secure Doors are found. Easy to get lost in. As of v47 they can turn at 45 degrees rather than only 90.
- **Factory Room** — three long paths from a shared platform to doors, with many gaps to fall into. Loot-rich; scan constantly.
- **Intersection Room** — added in v47, a roughly octagonal version of the Piped Hallways with several radiating hallways and sometimes a large central boiler.

## The Mansion

Rarer than the Factory and dominant on Dine and Rend. The main entrance is a large foyer with a staircase to a second-floor balcony, with doors leading off both levels. Compared to the Factory it has more open rooms and less linear hallways, filled with bookshelves, tables, and household furniture. **Toys and household items spawn as loot more commonly here.**

**The Mansion has no Secure Doors, no steam leaks, and no Apparatus Room**, but does have a Breaker Box.

Room types include the Library (two stories with a staircase), Fireplace room (whose crackling is often mistaken for an entity), Window room, Spiral Staircase (sometimes a dead end or blocked by a bookshelf), Kitchen (usually item-rich), Conservatory (projected countryside screens, white doors with inset windows), Bedroom, Bathroom, Pool room (items can be underwater; the floatie bobs when jumped on), Birthday room, Garage (scrap under the car hood, on the floor, or in cabinets), Staircase room with an under-stair closet, and the Alcove room (a hallway wrapping inaccessible space, with scrap in wall alcoves).

## The Mineshaft

Added in **Version 60**. Usual on Offense; common on Vow and Artifice; occasional on Adamance and Dine. **Because of its greater risk, the Mineshaft always has six extra loot spawns, ignoring the moon's upper limit.**

Entering the main entrance drops you into a small dirty room with a minecart, an **elevator**, and a button to recall the elevator if your crew left you behind. Steam leaks can occur in its corridor sections.

- **Main Entrance Room** — always identical, centered on the elevator. Doors can rarely spawn on the upper part of the lift. **Keys can spawn here.**
- **Intersections** — usually feature a central pit and lead to further rooms and yellow doors that may need a key.
- **Slopes** — the Mineshaft uses slopes instead of stairs, slowing employees going up and speeding them going down. In the Caves, vertical rock formations serve this role, with 0–2 connections onward.
- **Dead Ends** — more likely to contain a Breaker Box than other Mineshaft rooms.
- **Caves** — a subsection with very narrow paths, big rooms, **instantly lethal bottomless pits**, and underwater paths. **Most scrap is found here.** Entrances are marked by **blue lights** above the doors, which emit a higher-pitched elevator-like noise useful for locating them; these entrances may require a key or have no door at all.
- **Maneater holes** — replace vents inside the Caves. They are dark cracks in corners and walls that **make no noise when spawning entities**, though they appear identical to vents on the terminal.
- **Underwater paths** — cramped water-filled crevices linking cave passages. Employees can only hold their breath so long, so they must move fast.

## Navigation Rules

All interiors have door-swing heuristics for finding the main entrance beyond wall-hugging:

- **Mansion:** the old rule (a door opening into the room you enter means that room is closer to the main entrance) was **patched in v80** and no longer works.
- **Factory:** if a door swings between two rooms sharing certain attributes — brick walls to brick walls (excluding the Factory "Big" room), metal grated floors to metal grated floors, or white plaster floors to metal grated floors — the door swings toward the room **closer to the main entrance**. In all other cases the door is unreliable.
- **Mineshaft:** if a door opens away from you, you are heading away from the elevator; if it opens toward you, you are heading toward it.

## Notes and Warnings

- Procedural generation can place **bookcases that completely block Mansion doorways**.
- Items can spawn inside objects — notably inside Staircase-room metal pipes in the Factory, and inside kitchen tables and bookshelves in the Mansion.
- **Bring a key or lockpicker when entering via the fire exit**, since locked doors may block progress.
- The Mineshaft has a **higher key spawn rate** than other interiors, but also generates more locked doors.
- The Mineshaft's fire exit leads straight into the mines, **skipping the elevator**.
- The Mineshaft offers many spots entities cannot reach — crates, pipes, and minecarts.
- **The Mineshaft's fire exit can spawn completely blocked** by pipes or crates, making it unusable; employees are warned when trying to enter from outdoors. Mansion bookshelves may partially overlap a fire exit but never block it.

## Trivia

The Mansion is heavily reminiscent of the Spencer Mansion from *Resident Evil* (1996).

## Related Concepts

Moons, Breaker Box, Apparatus, Fire Exit, Secure Door, Elevator, Door, Key, Lockpicker, Mechanics, Scanner, Monitors, Map Hazard, Scrap

## Tags

lethal-company, interior, facility, factory, mansion, mineshaft, procedural-generation, vents, power-grid, breaker-box, steam-leak, caves, navigation, door-swing, apparatus-room, backrooms

---

Summary generated from: https://lethal-company.fandom.com/wiki/Interior
