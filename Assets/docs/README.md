# Lethal Company — Core Mechanics Reference

Summaries of the core game-mechanics pages from the [Lethal Company Wiki](https://lethal-company.fandom.com/wiki/Lethal_Company_Wiki).

Scope is **game systems and rules only**. Creature/entity pages, individual scrap items, individual moon destinations, decor item pages, and version patch notes are deliberately excluded. Each file carries its source URL and can be read standalone.

**51 documents across 7 categories.**

---

## core-loop/ — the run, the player, the employer

| Document | Covers |
|---|---|
| [mechanics](core-loop/mechanics.md) | Global game variables, normalized time, animation curves, full entity spawning algorithms |
| [employee](core-loop/employee.md) | Health, critical injury, damage table, stamina, fear, death causes, Echo Scanner |
| [player-body](core-loop/player-body.md) | Ragdolls, the 8%/20% insurance penalty, unrecoverable bodies |
| [fear](core-loop/fear.md) | The "shocked" status effect — triggers, and why it's more useful than harmful |
| [time](core-loop/time.md) | The 700-second day, clock phases, departure rules, extraction timing |
| [profit-quota](core-loop/profit-quota.md) | Quota formula, furniture luck, overtime bonus, optimal-sale maths |
| [credits](core-loop/credits.md) | Currency, the 30/53/77/100% sell rates, death fees |
| [company-ranks](core-loop/company-ranks.md) | XP progression, Intern → Boss |
| [performance-report](core-loop/performance-report.md) | End-of-day grading (F through S), employee notes |
| [the-company](core-loop/the-company.md) | 71-Gordion, selling, the Company Monster, the drill |
| [orbit](core-loop/orbit.md) | Between-day state, routing, SCAN behavior |

## world/ — moons, interiors, weather, events

| Document | Covers |
|---|---|
| [moons](world/moons.md) | Moon properties, full exomoon catalogue, average-profit analysis |
| [challenge-moons](world/challenge-moons.md) | Weekly competitive mode, seeds, modifiers, leaderboard |
| [interior](world/interior.md) | Factory / Mansion / Mineshaft layouts, vents, power grid, navigation rules |
| [outdoor](world/outdoor.md) | Fixed exteriors, data chips, terrain features, interactive objects |
| [out-of-bounds-areas](world/out-of-bounds-areas.md) | Terrain outside the play space |
| [weather](world/weather.md) | All six types, meteor showers, the weather-selection algorithm |
| [water](world/water.md) | Terrain vs. flood water, drowning, entity/item behavior underwater |
| [infestations](world/infestations.md) | Rare single-species interior events |
| [single-item-day](world/single-item-day.md) | 5.2% one-scrap-type days and value clamping |

## ship/ — the autopilot ship and its systems

| Document | Covers |
|---|---|
| [the-ship](ship/the-ship.md) | FORTUNE-9, autopilot, pressure door, magnet, upgrades, entity intrusion |
| [terminal](ship/terminal.md) | Full command reference including hidden commands |
| [monitors](ship/monitors.md) | Radar cams, CCTV, quota monitors |
| [storage](ship/storage.md) | Storing ship objects, what can and can't be stored |
| [decor](ship/decor.md) | Furnishings, suits, quota luck |
| [dropship](ship/dropship.md) | Delivery rules, the 30-second window, Eyeless Dog distraction |
| [intercom](ship/intercom.md) | Company broadcasts and triggers |
| [company-cruiser](ship/company-cruiser.md) | The vehicle — driving, boost, eject, entity interactions, strategy |
| [electric-coil](ship/electric-coil.md) | Battery recharging |

## items/ — loot, inventory, purchasing

| Document | Covers |
|---|---|
| [items](items/items.md) | The four item categories |
| [item-bar](items/item-bar.md) | Inventory slots, two-handed rules, blocked actions |
| [scrap](items/scrap.md) | Rarity/spawn maths, scrap properties, special scrap |
| [store](items/store.md) | Full equipment/upgrade/decor catalogue, delivery, discounts |
| [weapon](items/weapon.md) | Damage and stun weapons, threat-level effects |
| [scanner](items/scanner.md) | Echo Scanner behavior and limitations |
| [advertisements](items/advertisements.md) | V70 in-game store ads |

## hazards/ — traps, doors, access

| Document | Covers |
|---|---|
| [map-hazard](hazards/map-hazard.md) | Overview of all three hazards |
| [turret](hazards/turret.md) | Five modes, berserk state, per-moon counts |
| [landmine](hazards/landmine.md) | Step-off trigger, chain reactions, the standing-still rescue |
| [spike-trap](hazards/spike-trap.md) | Interval vs. detection mode, using traps as weapons |
| [door](hazards/door.md) | Door types, locks, per-entity open speeds, haunted doors |
| [secure-door](hazards/secure-door.md) | Terminal-controlled doors, entity containment |
| [curtained-door](hazards/curtained-door.md) | Artifice warehouse doors, trapping Old Birds |
| [fire-exit](hazards/fire-exit.md) | Exterior locations per moon, interior placement |
| [elevator](hazards/elevator.md) | Mineshaft elevator, original cut design |
| [breaker-box](hazards/breaker-box.md) | Facility power, the five switches, consequences of cutting power |

## detection-and-combat/ — how entities perceive and are hit

| Document | Covers |
|---|---|
| [entity-targeting](detection-and-combat/entity-targeting.md) | Visibility, threat level, interest level |
| [hitbox](detection-and-combat/hitbox.md) | Collision volumes, weak points, weapon hitboxes |
| [audible-sounds](detection-and-combat/audible-sounds.md) | Noise range/volume table, which entities hear |

## guides/ — role and workflow guides

| Document | Covers |
|---|---|
| [guide-camera-duty](guides/guide-camera-duty.md) | Ship duty, radar dot identification, hazard toggling |
| [guide-contract](guides/guide-contract.md) | New-hire orientation to a full run |

---

## Suggested reading order

New to the game: `guide-contract` → `time` → `profit-quota` → `credits` → `item-bar` → `scrap`.

Running the ship: `the-ship` → `terminal` → `monitors` → `guide-camera-duty`.

Optimizing: `mechanics` → `entity-targeting` → `hitbox` → `moons` → `weather`.
