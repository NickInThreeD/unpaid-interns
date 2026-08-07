# Performance Report

**Source:** https://lethal-company.fandom.com/wiki/Performance_report

## Overview

The Performance Report is the end-of-day summary screen shown whenever the ship takes off from a moon — with the exception of The Company building, where no report is generated. It lists each employee's survival status, assigns a joke "note" to individual employees, and grades the crew as a whole based on how much of the moon's available scrap was recovered and how many employees died. The crew grade is the source of XP that drives Company Ranks.

## Key Points

- Shown after takeoff from **any moon except The Company (Gordion)**.
- Reports each employee's **Life Support status**: Alive, Deceased, or Missing.
- Assigns each employee a descriptive note of "significant importance".
- Grades the crew from **F to S** based on scrap collected versus total scrap on the moon, combined with employee deaths.
- On a **Challenge Moon**, the normal report is followed by a second screen showing the crew's **worldwide ranking** for that challenge.

## Employee Status

- **Checkmark** — alive and aboard the ship at takeoff.
- **Missing** — alive, but not aboard when the ship left.
- **Deceased** — the employee died.

## Employee Notes

Individual employees may be singled out with one of these notes:

- **"Sustained the most injuries"** — took the highest total damage while still surviving.
- **"The laziest employee"** — took the fewest steps.
- **"The most paranoid employee"** — looked around the most.
- **"Most profitable"** — brought the greatest scrap value back to the ship.

## Crew Grade

The grade combines **percentage of the moon's total scrap collected** with **number of deaths**:

- **S** — at least 99% of all scrap collected **and** all employees survived.
- **A** — at least 99% collected with fewer than 2 deaths, **or** at least 60% collected with all employees surviving.
- **B** — at least 60% collected with fewer than 2 deaths, **or** at least 26% collected with all employees surviving.
- **C** — at least 26% collected with fewer than 2 deaths, **or** less than 25% collected with all employees surviving.
- **D** — less than 25% collected, **or** more than 1 employee died.
- **F** — all employees died.

The structure means a single death caps the achievable grade one tier below what the scrap percentage alone would earn, and two or more deaths cap it at D regardless of haul.

> **⚠ Gap in the source thresholds.** The tiers use **26%** as a lower bound (B and C) but **25%** as D's upper bound. A haul between 25% and 26% with exactly one death satisfies neither C's "at least 26% with fewer than 2 deaths" nor D's "less than 25%". If you are implementing a grading function from this, pick one boundary and apply it consistently — the wiki does not resolve which is correct.

## Total Wipe

If every employee dies during a day, the report displays a large black label reading **"NO SURVIVORS"** across the screen, the collected report reads **"ALL SCRAP LOST"**, and the crew receives grade **F**.

## Related Concepts

Employee, Company Ranks, Scrap, Challenge Moons, Player Body, The Company, The Ship

## Tags

lethal-company, performance-report, end-of-day, crew-grade, grading, xp, employee-status, deceased, missing, no-survivors, challenge-moons, leaderboard

---

Summary generated from: https://lethal-company.fandom.com/wiki/Performance_report
