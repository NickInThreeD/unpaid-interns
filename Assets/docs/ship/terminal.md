# Terminal

**Source:** https://lethal-company.fandom.com/wiki/Terminal

## Overview

The terminal is the central control interface of *Lethal Company* — a large interactable computer with monitor and keyboard inside the autopilot ship. It is required to select destinations, buy supplies, read creature and moon information, operate secure doors and radar boosters, and run camera duty. This page is a command reference covering the basic menus, the "other" commands, and the hidden commands.

## Key Points

- The terminal is **liberal about input**: commands can be shortened (`C` for `CONFIRM`, `D` for `DENY`), the syntax order can be rearranged, and some misspellings are accepted.
- `HELP` lists the five basic commands: **MOONS, STORE, BESTIARY, STORAGE, OTHER**.
- `OTHER` lists five more: **VIEW MONITOR, SWITCH, PING, TRANSMIT, SCAN**.
- Several commands are **hidden from all listings**: special door codes, FLASH, SIGURD, EJECT, and RESET CREDITS.

## Basic Commands

### MOONS

Opens the exomoon catalogue with each moon's current conditions. Typing a moon name prompts for confirmation; confirming puts the ship in orbit around it, after which a crew member pulls the lever to land.

- `ROUTE [moon name]` — routes the ship, with confirmation. Can be shortened to just the moon name.
- `INFO [moon name]` — shows conditions, history, and fauna. Also displayed on the main monitor for the orbited moon.

### STORE

Lists the full Company Store catalogue. **Purchased equipment arrives within the hour, or the next time the ship lands**, delivered by a rocket dropship that plays a loud cheerful tune. Ordering during downtime at 71-Gordion saves valuable mission time, since the clock doesn't run there.

- `BUY [item]` — orders an item with confirmation; shortens to just the item name and accepts a quantity before or after the name (e.g. `BUY 4 SHOVELS`).
- `INFO [item]` — blurb about equipment or ship upgrades.
- `UPGRADES` — ship upgrades catalogue (noted as outdated).
- `DECOR` — currently available ship decor.

### BESTIARY

Opens the Bestiary of all scanned entities.

- `[Creature name] INFO`, or just `[Creature name]` — opens that creature's file. Many shortened names are accepted.

### STORAGE

Views ship decor and upgrades that were moved with [B] and stored away with [X].

## Other Commands

- **`VIEW MONITOR`** — displays the radar map through the terminal itself, removing the need to run back and forth between the main monitor and the terminal during camera duty.
- **`SWITCH [player name]`** — cycles the radar between crew members or jumps to a named one. Identical to the upper button on the side of the monitor.
- **`PING [radar booster name]`** — makes an active Radar-Booster play a noise and say "Hello!". Audible at range, so it can guide crew toward an exit or deliberately lure Eyeless Dogs.
- **`TRANSMIT [message]`** — broadcasts a message of **max 9 characters** to all crew via the Signal Translator. Useful for passing information without Walkie-Talkies while on camera duty.
- **`SCAN`** — counts the items left on the current planet and gives an approximate sell value.

### The SCAN value correction

The displayed value is not accurate. **Multiply it by 0.41 if you are the lobby host, or by 0.5 if you are not**, to get much closer to the true figure.

While in orbit, `SCAN` instead reports the count and total value of everything **inside the ship** — the standard way to check your haul.

**Counting rules:** keys and shotgun shells are **excluded** from the count. Apparatuses, double-barrels, knives, dead employee bodies, and bee hives are **included**.

## Hidden Commands

### [Special code]

Typing the 2-digit codes shown on the radar map toggles **Secure Doors** or temporarily disables **Turrets** and **Landmines**. Codes are visible on the main monitor or through `VIEW MONITOR`.

**Limitation:** secure doors must be powered to stay shut. **Removing the Apparatus, or switching off a door's circuit at the Breaker Box, makes secure doors permanently inoperable from the terminal.**

### FLASH [radar booster name]

Flashes a bright light at the named Radar-Booster, **stunning nearby entities** — and blinding any crew member looking at it.

### SIGURD

Lists collected log entries from a former employee named Sigurd. Read them with `VIEW [log name]`.

### EJECT

**Destructive.** Initiates the disciplinary process and jettisons everything in the ship into deep space, resetting the run exactly as if the crew had missed their profit quota. It must be confirmed by the lobby host and can **only be used while orbiting a moon**.

### RESET CREDITS

A developer command usable **only if your Steam profile name is Zeekerss, Puffo, or Blueray**. It sets credits to **2,500**, despite the terminal reporting "Reset credits to 200."

## Related Concepts

The Ship, Monitors, Store, Storage, Bestiary, Moons, Scanner, Secure Door, Turret, Landmine, Radar-Booster, Signal Translator, Lore, Guide:Camera duty, Orbit, Breaker Box

## Tags

lethal-company, terminal, commands, moons, store, bestiary, storage, scan, view-monitor, transmit, ping, flash, eject, sigurd, secure-door-codes, camera-duty

---

Summary generated from: https://lethal-company.fandom.com/wiki/Terminal
