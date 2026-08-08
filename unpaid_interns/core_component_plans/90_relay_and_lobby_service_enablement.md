# 90 — Relay & Lobby Service Enablement

**Source:** [`core_components.md`](../core_components.md) §12 — Build & Release Readiness
**Status:** ❌ The one remaining setup step, and it cannot be done from the codebase
**Depends on:** nothing
**Blocks:** anyone outside the developer's LAN ever connecting

## Summary

Turning on the two cloud services the game's entire connection strategy assumes.

This is the shortest component in the project and the only one whose work happens in a web dashboard. `core_components.md` states it precisely: **linking a project does not enable individual services.** Everything else on the Relay path is done — `ProjectSettings.asset:934` carries `cloudProjectId: bc8406a5-fddf-4bb6-b45f-ac19f6f0df6e` under `organizationId: nickinthreed`, `com.unity.services.core/Settings.json` pins the `production` environment, `EntityDriverConstructor.cs:67,92` applies `WithRelayParameters` to both drivers, `EntityNetworkHandler.cs:46-49` resolves the endpoints, `GameConnection.cs:49` requests `.WithRelayNetwork()`, anonymous authentication is wired at `GameConnection.cs:122`, and `MainMenu.cs:137` routes the "Create Game" button to it.

All of that fails at runtime if Relay and Lobby are not enabled on the dashboard, and it fails in a way that looks like a code bug: an exception from a service call, or a session that never allocates. Someone will spend an afternoon on it.

It earns a plan file because **it is a hard dependency of the first real playtest** and because the failure mode needs to be recognisable when it happens.

## How to Build

**Enable the services**

- In the Unity Cloud dashboard for project `bc8406a5-fddf-4bb6-b45f-ac19f6f0df6e` under organization `nickinthreed`, enable **Relay** and **Lobby** for the `production` environment that `com.unity.services.core/Settings.json` pins.
- Verify the environment name matches. A project can have several environments, and services enabled on `development` while the build points at `production` produces exactly the same failure as not enabling them at all.
- Check whether the account requires billing details before the free tier is usable. This is the step that turns a five-minute task into a two-day one when discovered on playtest night.

**Understand the free tier before relying on it**

- Relay bills on **bandwidth**, which is the same constraint §13's network bandwidth budget is about ([`100_network_bandwidth_budget.md`](100_network_bandwidth_budget.md)). A loot-dense procedural map replicating hundreds of item ghosts to four clients is a cost, not just a performance concern.
- Record the current free-tier limits and the overage pricing somewhere durable, and revisit them once the bandwidth budget has real measurements. A game that is fine in testing and expensive at launch is a discoverable problem, not a surprise.
- Lobby's limits are about request rates rather than bandwidth, and matter less at this scale.

**Do not confuse this with the analytics flag**

- `UnityConnectSettings.asset` has `m_Enabled: 0`, which `core_components.md` already flags as **not relevant** — that governs legacy Unity Analytics, Ads, and Crash Reporting, not UGS project linkage or Relay.
- It becomes relevant for [`101_analytics_and_balance_telemetry.md`](101_analytics_and_balance_telemetry.md) and [`104_crash_and_error_reporting.md`](104_crash_and_error_reporting.md), which are separate decisions. Do not flip it while doing this.

**Make the failure legible in the game**

This is the part that is actually code, and it is worth doing regardless of when the dashboard step happens:

- A service call that fails because Relay is disabled currently surfaces as an exception or a hang. `ConnectionStatusScreen` exists and should show a **specific, actionable message** — "the online service is unavailable" — rather than a generic connection error.
- Distinguish the cases the player can act on: no internet, service unavailable, authentication failed, session not found ([`91_join_by_code.md`](91_join_by_code.md) needs the last one anyway). [`25_reconnection.md`](25_reconnection.md) makes the same argument about naming the specific reason a reconnect failed.
- **Keep the direct-connect path visible as a fallback.** `HostGameAsync`, `ConnectGameAsync`, and `GetServerConnectionSettings` never touch Unity Services, and `GameManager.StartGameAsync:277` already handles the no-session case. That path is how you determine whether a failure is transport-level or service-level, and it is what keeps LAN testing possible while the dashboard question is unresolved.

**Verify it end to end**

- Two machines on **different networks** — not two clients on one LAN, which can succeed over local transport and prove nothing about Relay.
- Confirm from the dashboard that a Relay allocation was actually created. A session that works locally while silently falling back is the failure this verification exists to catch.
- Do it before the first real playtest, and do it again from a **standalone build**, since §12 notes Editor testing does not prove a build works.

## Acceptance Criteria

- [ ] Relay and Lobby are enabled on the Unity Cloud dashboard for the linked project.
- [ ] The enabled environment matches the one pinned in `com.unity.services.core/Settings.json`.
- [ ] Any billing or account prerequisite is resolved, not merely identified.
- [ ] Free-tier bandwidth limits and overage pricing are recorded and linked from the bandwidth budget component.
- [ ] `UnityConnectSettings.asset`'s `m_Enabled` flag is left unchanged by this work.
- [ ] Two players on different networks connect and play a full round through Relay.
- [ ] The dashboard confirms a Relay allocation was created for that session.
- [ ] Connection succeeds from a standalone build, not only from the Editor.
- [ ] A service outage or disabled service produces a specific, player-readable message rather than an exception or a hang.
- [ ] No internet, service unavailable, authentication failure, and session-not-found are distinguishable to the player.
- [ ] The direct-connect fallback remains functional and is usable to isolate transport-level from service-level failures.
