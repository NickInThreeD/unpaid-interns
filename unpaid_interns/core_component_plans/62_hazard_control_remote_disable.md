# 62 — Hazard Control / Remote Disable

**Source:** [`core_components.md`](../core_components.md) §7 — Hazards & Environment Interaction
**Status:** ❌ Not started
**Depends on:** [Static Map Hazards](59_static_map_hazards.md), [Door System](60_door_system.md), [Terminal / Hub Interface](74_terminal_hub_interface.md), [Proximity Voice / Comms](21_proximity_voice_comms.md)
**Blocks:** the stay-behind role being worth playing

## Summary

Someone who did not go inside, doing something useful.

`core_components.md` frames the value exactly: it *"gives the stay-behind role something meaningful to do."* That role otherwise does not exist. In a four-person crew where everyone deploys, the hub is empty and the terminal is a menu; in one where somebody stays, that person is currently a spectator with better lighting. This component is what turns "I'll hang back" from a waste of a crew slot into a position with real leverage.

It is also the component that makes **communication a mechanic rather than a convenience**. A hub-bound player who can disable a turret but cannot see which turret matters has to be told, over voice, by someone being chased. That exchange — imprecise, urgent, easily botched — is some of the best material this genre produces, and it exists only if the remote operator's information is incomplete.

The design lever that makes it work is scarcity. If the operator can disable everything, the field team is playing a different, easier game. If they can disable one thing at a time, briefly, with a cost, then every use is a decision made under pressure by someone who cannot see the room.

## How to Build

**Build on state machines that already accept external control**

- [`59_static_map_hazards.md`](59_static_map_hazards.md) requires every hazard to expose an externally-settable `Disabled` state, and [`60_door_system.md`](60_door_system.md) requires door state to be settable by an external controller. Both were written that way for this component. If either was skipped, retrofitting it across three hazard types and every door is several times the work.
- The controller is a server-side authority: the terminal sends a **request**, the server validates and applies. Never let a client set hazard state directly — a modified client that can disable every hazard has removed §7 from the game.
- Validate: does the target exist this round, is the operator actually in the hub, is the phase `Active`, is the target on cooldown, does the operator have power.

**Give the operator a limited, imperfect view**

- The operator needs to identify targets, and **how much they can see is the entire balance of this component.**
- Recommended: a **schematic**, not a live view. Hazards and doors appear as labelled markers on a coarse layout — the reference's secure doors use exactly this, short codes like `m6` visible on a map ([`Assets/docs/hazards/secure-door.md`](../../Assets/docs/hazards/secure-door.md)) — with no monster positions and no live player detail beyond a rough marker.
- A schematic forces the voice exchange. A live camera feed answers the question for the operator and removes it. If a camera view is wanted, it belongs in the Monitoring / Camera System (§9) as a separate, more expensive capability with its own tradeoffs.
- Labels must be **visible in the field too** — a code stencilled on the hazard itself — or the field team cannot say which one they mean. This is the single detail that decides whether the mechanic is usable, and it is easy to omit.

**Make every action cost something**

- **Temporary, not permanent.** A disable lasts a configured few seconds and re-arms. Permanent disabling turns the operator into a hazard-removal service and the map loses its personality by the second round.
- **Cooldown per target and a global budget.** The operator should be choosing which thing to disable, not disabling everything in sequence.
- **Power-gated.** Tie it to the facility's power grid ([`36_lighting_and_power_grid.md`](36_lighting_and_power_grid.md)): a blackout takes the operator's tools offline. That single dependency creates the situation where the field team must restore power to be helped, which is a much better structure than an operator whose capability is constant.
- Consider a credit or resource cost so remote support competes with the store. Optional, and worth prototyping before committing — a cost that makes the operator hesitate is good, one that makes them never act is not.

**Make it legible from both ends**

- The field team must know a disable happened: a distinct sound at the hazard, a light changing, the turret visibly powering down. An operator whose help is invisible cannot be trusted or thanked.
- The operator must get confirmation and failure reasons — "target not powered", "on cooldown", "no such code". [`41_interaction_system.md`](41_interaction_system.md) requires refusals to be explained; the same rule applies to a remote request, and more strongly, because the operator cannot see why nothing happened.
- Announce significant remote actions through the repurposed `ActionFeed` (§9), so a crew that is not on voice still finds out.

**Handle the awkward cases honestly**

- **Nobody stays behind.** The common case, and the component must degrade to nothing rather than to a penalty. Field hazards work normally; the crew simply has no support.
- **The operator dies or disconnects.** They are in the hub, so death should not apply — but a disconnect must release any held state cleanly and not leave a hazard permanently disabled.
- **Everyone is dead except the operator.** The round should end via the total-crew-loss path ([`02_day_cycle_controller.md`](02_day_cycle_controller.md)) based on who is in the field, and an operator in the hub is not "alive in the field". Confirm the roster's `AnyAliveInField()` treats a hub-bound player correctly ([`19_crew_roster.md`](19_crew_roster.md)) — this is a real edge case and it decides whether one person staying home can hang a round forever.
- **Spectators.** Dead players wanting to use the terminal is tempting and should be refused: [`22_spectator_mode.md`](22_spectator_mode.md) requires spectators to be unable to influence the world, and remote hazard control is influence. Giving the dead a job is a different feature with different balance.

**Keep the scope honest**

- This is a **post-MVP component** and should be built after the hazards and doors it controls are tuned. Building the control layer first produces a system with nothing worth controlling.
- Build it so terminal-controlled doors and remote hazard disabling share one request path and one authority check. They are the same feature with two target types, and splitting them produces two sets of validation that will diverge.

## Acceptance Criteria

- [ ] A hub-bound player can disable a hazard and operate a door remotely, through a server-validated request path.
- [ ] Hazards and doors share one control request path and one authority check.
- [ ] A client cannot set hazard or door state directly; a forged request changes nothing.
- [ ] Requests are validated for target existence, operator location, round phase, cooldown, and power.
- [ ] The operator sees a schematic with labelled targets, and no monster positions.
- [ ] Target labels are visible both on the schematic and physically in the field, so the crew can name them over voice.
- [ ] Disabling is temporary and re-arms after a configured duration.
- [ ] Per-target cooldowns and a global budget prevent disabling everything in sequence.
- [ ] A facility blackout takes remote control offline, and restoring power restores it.
- [ ] A remote action is audibly and visibly legible at the hazard to anyone nearby.
- [ ] The operator receives confirmation, and every failure states its specific reason.
- [ ] Significant remote actions are announced to the whole crew.
- [ ] With nobody in the hub, hazards behave normally and nothing is penalised.
- [ ] An operator disconnecting releases all held state and leaves no hazard permanently disabled.
- [ ] A hub-bound operator does not count as alive in the field for the total-crew-loss check.
- [ ] Spectators cannot use remote control.
- [ ] No remote action can permanently remove a hazard for the remainder of the round.
- [ ] All remote-control state is cleared at round end.
