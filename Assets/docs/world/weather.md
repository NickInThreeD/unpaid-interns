# Weather

**Source:** https://lethal-company.fandom.com/wiki/Weather

## Overview

Weather is a set of conditions that can affect any moon in *Lethal Company*, adding hazards to the outdoor area. Weather is re-rolled for every moon each time an in-game day passes, and the forecast for every destination is visible in the Exomoons catalogue via the ship's terminal. Critically, **weather never changes the amount or value of scrap on a moon** — it only makes the job harder, so inexperienced crews should simply avoid inclement forecasts.

## Weather Types

Clear, Rainy, Stormy, Foggy, Flooded, and Eclipsed. A separate random event, the Meteor Shower, can occur on top of any of these.

## Rainy

Rainy moons spawn dark **quicksand** patches outdoors — a swift and lethal threat. Affected employees are slowed and sink completely within a few seconds, though escape is possible if they react fast enough. A patch resembles a large puddle of darkened dirt and is very hard to spot at night.

March, Experimentation, and Adamance spawn **permanent** quicksand patches regardless of weather.

**Strategy:** there is an audio cue when you enter mud — walk backwards the moment you hear it.

## Stormy

Conductive items left outdoors accumulate electric charge and are then struck by lightning. A charging item buzzes and visibly arcs; dropping it immediately and stepping away lets you survive. Otherwise, lightning strikes are overwhelmingly fatal.

- Lightning may strike the same object **multiple times in quick succession**, so wait a moment before picking items back up.
- Items under cover — notably the autopilot ship's roof — tend to redirect the strike to the roof instead. Overhangs and walls work sometimes but not reliably. A strike on the ship turns off its lights and monitor, which can be turned back on.
- **Conductive equipment ordered while on a stormy moon will not attract lightning for the rest of that day.**

**Conductive equipment:** Extension Ladder, Jetpack, Radar-Booster, Shovel, TZP-Inhalant, Zap Gun.

**Conductive scrap** includes the Apparatus, Bee Hive, Big Bolt, Brass Bell, Cash Register, Clown Horn, Control Pad, Cookie Pan, Egg Beater, Fancy Lamp, Garbage Lid, Gold Bar, Key, Kitchen Knife, Large Axle, Red Soda, Robot Toy, Stop Sign, Tattered Metal Sheet, Tea Kettle, V-type Engine, Wedding Ring, Yield Sign, and Zed Dog.

**Strategy:** carry as few conductive items per trip as possible, so a strike forces you to drop one item rather than your whole haul.

## Foggy

Thick fog makes the exterior extremely hard to navigate — visibility drops to a few meters. **Entities that cannot see through fog are limited to 30 units of sight**, which as of Version 49 means all outdoor entities.

Tools for finding your way back:
- The **Loud Horn** ship upgrade, or ordering items for Dropship delivery, gives an audible beacon.
- A scrap **Airhorn** is the budget alternative, but carries less far and attracts Eyeless Dogs to the holder.
- Activating the **teleporter** retrieves lost crew directly.
- With radio contact (Walkie-Talkie or Signal Translator), someone in the ship can guide employees using the corner arrow on the main monitor.
- The **Echo Scanner** helps once within 50 m of the ship or main entrance.

Rend, Dine, and Titan are in perpetual blizzard, so visibility is already poor there; of the three, only Titan can additionally be foggy.

**Strategy:** learn each moon's layout. Sight is only needed to gather information about your surroundings — with the layout memorized, you don't need it.

## Flooded

Rising water progressively cuts off low ground and eventually most of the landing area. Wading slows employees considerably, drains stamina, and prevents running; stamina regenerates by standing still. Being fully underwater long enough causes drowning.

- Flooding starts low and worsens through the day, generally **peaking around 5:00 p.m.**, though this varies and building entrances can flood earlier.
- **Rend, Titan, and Embrion cannot be flooded.**
- As of v80, **Adamance is no longer reverse-flooded but Dine is** — on a reverse-flooded moon, water starts high and drains over the day.

**Strategy:** stick to elevated routes; if the whole map submerges, manage your time underwater carefully.

## Eclipsed

Eclipsed moons carry drastically heightened entity danger. Outdoor entities can appear immediately, and the base number of indoor entities starts as though it were already night. Inexperienced crews should avoid eclipsed moons entirely — the risk is losing a worker at the very start of the mission, or losing an entire day's scrap.

Each moon has a set number of entities that spawn at 8:00 a.m. while eclipsed: Experimentation, Assurance, and Vow each spawn 1 outdoor and 1 indoor; March spawns 2 and 2; **Offense spawns 4 and 4**, making it the most punishing of the documented moons. Values for Rend, Dine, and Titan are not recorded on the page.

**Strategy:** an experienced, well-communicating crew largely nullifies the threat. Keeping one employee on the ship greatly reduces the risk of losing all items. Purchasing a cheap item before landing gives the Dropship's arrival a chance to distract Eyeless Dogs while the crew enters the facility, and Boomboxes can be used to draw multiple dogs away from the return route.

## Meteor Shower

A random event rather than a weather type. It has a small chance to begin at any point in the day and lasts 12 in-game hours (about 8 minutes 30 seconds real time). An EAS-style weather bulletin appears on the HUD when one is imminent.

Meteors appear as entry fireballs high above the surface and fall in waves. They move slowly enough that any attentive employee can outrun them. A meteor passes through water and destroys trees, then explodes on ground impact — anything at the impact point dies, while nearby employees are flung back without taking damage. No crater or meteorite remains, only a char decal.

**A meteor shower can occur regardless of what weather is already active.**

## Weather Selection Algorithm

At the start of each day, weather is determined in two steps.

First, the game decides **how many moons** will have weather, by evaluating a curve with a random input between 0 and 1. That evaluated value is multiplied by a random number between **1.5 and 2.5** if *both* of these are true:

- The crew has at least 2 employees (multiplayer).
- The number of consecutive days all employees survived without dying is a multiple of 3 (at least 3 days).

The result is converted to an integer and clamped between 0 and the number of moons — so a value above 8 means every moon gets weather. This is another escalation mechanic: surviving well makes the galaxy stormier.

Second, for each selected moon, a random index into that moon's list of possible weathers determines which type it gets.

### Re-roll exploit

Weather can be re-rolled by landing at the Company building, immediately taking off, then quitting and relaunching the session. No days are lost, but the weather assignments are recalculated.

## Version History

- **Version 50 (April 13, 2024):** Whoopie-Cushion is no longer conductive.
- **Version 64 (September 5, 2024):** Meteor Showers added as a moon event.
- **Version 70 (May 31, 2025):** Toilet Paper is no longer conductive.

## Related Concepts

Water, Moons, Terminal, Mechanics, Time, Map Hazard, Loud Horn, Teleporter, Scanner, Dropship, Interior

## Tags

lethal-company, weather, rainy, stormy, foggy, flooded, eclipsed, meteor-shower, quicksand, lightning, conductive-items, drowning, weather-algorithm, weather-reroll, exomoons-catalogue

---

Summary generated from: https://lethal-company.fandom.com/wiki/Weather
