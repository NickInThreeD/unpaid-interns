# 55 — Chase & Pathfinding

**Source:** [`core_components.md`](../core_components.md) §6 — Monsters & AI
**Status:** ❌ Not started · **[MVP]**
**Depends on:** [Runtime NavMesh Baking](30_runtime_navmesh_baking.md), [Perception System](53_perception_system.md), [Monster Data Definitions](48_monster_data_definitions.md)
**Blocks:** monsters being a threat rather than an obstacle, the whole feel of the game's worst moments

## Summary

Moving toward the player, losing them, looking for them, and giving up.

The chase is the game's climax, delivered several times a round. Everything else — the loot, the quota, the carry limit — exists to produce the moment where a crew member is running down a corridor with a full inventory and something behind them. That moment is this component, and its quality is decided almost entirely by the **losing** half rather than the pursuing half. A monster that follows perfectly until it kills you is not a chase, it is a countdown. A monster that can be lost, that searches where you were, that gives up somewhere you can hear it, is an encounter with a shape.

`core_components.md` names the three parts precisely: NavMesh pursuit, last-known-position search, and line-of-sight breaking with give-up conditions. It also notes the hard dependency — this **depends entirely on runtime NavMesh baking**, which is why [`30_runtime_navmesh_baking.md`](30_runtime_navmesh_baking.md) is flagged do-not-defer.

## How to Build

**Run it on the server, and only on the server**

- Pathfinding, target selection, and every timer are server-side ([`49_monster_ghost_and_replication.md`](49_monster_ghost_and_replication.md)). Clients receive a transform and a behaviour state and animate to it.
- `NavMeshAgent` lives on the server instance of the monster prefab. The client instance has no agent, no path, and nothing to disagree about.
- Monsters move by agent, players move by `CharacterController` — two different movement models in one world. That is fine, and it has one consequence worth planning for: **a monster cannot push a player and a player cannot block a monster with their body**. Decide whether that is acceptable; recommended yes, because the alternative is a predicted character resolving contacts against a server-only agent, which is the worst prediction case in the project.

**Build the state machine around losing the target**

Five states, and the interesting ones are the last three:

- **Patrol** — moving between points with no target. Should look purposeful; a monster wandering randomly reads as broken.
- **Alerted** — aware of something, not yet committed. This is the window the player has to react, and it must be legible from outside ([`53_perception_system.md`](53_perception_system.md) ramps awareness rather than toggling it).
- **Chase** — pursuing a perceived target. Path recalculated on an interval, not per frame.
- **Search** — the target is no longer perceivable. Move to the **last known position**, then search outward from it for a configured duration. This is the state that makes hiding work, and skipping it produces a monster that either never loses you or forgets instantly.
- **Give up** — return to patrol. It must be **audible and visible**: a distinct sound and a visible turn away, so the player knows they are safe. A chase that ends silently means the player keeps running, which wastes the relief the whole encounter was building toward.

Per-monster give-up distance, give-up time, and search duration come from data ([`48_monster_data_definitions.md`](48_monster_data_definitions.md)), and [`51_difficulty_escalation.md`](51_difficulty_escalation.md) scales them with normalized time so late-round chases are harder to shake without changing any monster's stats.

**Make the pursuit feel fair**

- **Speed relationships are the whole balance.** A monster faster than a sprinting player is unavoidable and must be a deliberate archetype ([`58_monster_variety_set.md`](58_monster_variety_set.md)), not the default. Most should be slightly slower than a sprint and faster than a walk, so escape is possible and costs stamina.
- That interacts directly with [`11_stamina.md`](11_stamina.md) and [`12_carry_weight.md`](12_carry_weight.md): a heavily loaded player *cannot* outrun what an empty-handed one can. That is the design working — the haul is what kills you — but it means chase speeds must be tuned against loaded movement speed, not against the base value.
- Recalculate paths on an interval and cap total path requests per tick. `NavMesh.CalculatePath` is not free and a dozen monsters recalculating every frame is a server frame-time problem that appears only under a full spawn budget.
- Never let a monster path through a closed door it is not authorised to open — [`30_runtime_navmesh_baking.md`](30_runtime_navmesh_baking.md) enforces this through links and obstacles, and doors are the primary tool players have for buying time (§7).

**Handle the cases that break naive pursuit**

- **Corners and loops.** [`28_procedural_interior_generator.md`](28_procedural_interior_generator.md) requires loop connections specifically so a chase is survivable. Verify that a monster does not simply cut every corner perfectly; a small pursuit lag at direction changes is what makes a loop escapable.
- **Vertical.** Ladders and drops are off-mesh links with per-monster permissions. A monster that cannot follow you up a ladder is a *feature* and must be legible — the player has to be able to learn which ones can.
- **Doors.** Per-monster open speed (§7) is a genuine tension generator. Hearing something work at a door you closed is better than either outcome.
- **The target disappears entirely** — dies, disconnects, or extracts. Fall to Search at the last known position rather than picking a new target instantly; instant retargeting reads as omniscience. The extraction case now has an event to hang on: [`105_departure_and_extraction_resolution.md`](105_departure_and_extraction_resolution.md) publishes one per intern as they resolve, so this is a subscription rather than a poll.
- **No path exists.** A monster stuck against unreachable geometry must detect it and return to patrol or despawn, not stand vibrating against a wall. Log it, because it means the generator produced navigation islands.
- **The player enters the extraction zone.** Whether monsters may follow is [`31_entry_point_extraction_zone.md`](31_entry_point_extraction_zone.md)'s decision; this component enforces it as a chase-termination rule.

**Do not let the chase be the only thing that happens**

- Multiple monsters chasing one player is the fastest way to an unsurvivable round. Consider a soft limit on simultaneous pursuers per target, with others falling to Search or Patrol.
- A monster chasing player A that perceives player B mid-chase should mostly **keep chasing A**. Constant retargeting produces a monster that oscillates between two players and catches neither, which is neither scary nor fair. [`56_threat_interest_targeting.md`](56_threat_interest_targeting.md) owns the choice; this component supplies the stickiness.

**Make it observable and testable**

- Extend the server-side debug overlay from [`49_monster_ghost_and_replication.md`](49_monster_ghost_and_replication.md) to draw the current path, the last known position, the search radius, and the give-up timers. Chase bugs are otherwise diagnosed from a first-person view of something that is behind you.
- Add a harness that spawns a monster and a scripted player path across many seeds and asserts: the monster reaches the player when it should, loses them when line of sight breaks for the configured time, searches the correct position, and gives up within the configured window. This is pure logic and it is where regressions in a heavily-tuned system get caught.
- Log every state transition with tick, monster, target, and reason.

## Acceptance Criteria

- [ ] Pathfinding and all chase state run on the server; clients hold no agent and no path.
- [ ] Monsters traverse a runtime-baked interior with no unreachable islands and no falling through geometry.
- [ ] The five states are implemented, and every transition has a documented trigger.
- [ ] Awareness ramps into Alerted with a window the player can react to.
- [ ] Losing line of sight leads to a search of the last known position, not instant forgetting or perfect tracking.
- [ ] Giving up is audibly and visibly legible to the player.
- [ ] Give-up distance, give-up time, and search duration come from monster data and scale with round progression.
- [ ] Default chase speed allows an unencumbered sprinting player to escape and a heavily loaded one to be caught.
- [ ] Any monster faster than a sprinting player is a documented archetype, not an accident of tuning.
- [ ] Path recalculation runs on an interval with a per-tick request cap; a full spawn budget holds the server frame budget.
- [ ] A monster never paths through a closed door it is not authorised to open.
- [ ] Loop connections are escapable; a monster does not cut every corner perfectly.
- [ ] Vertical links are traversed only by monsters authorised for them, and which ones can is learnable.
- [ ] A target that dies, disconnects, or extracts sends the monster to Search rather than to an instant new target.
- [ ] A monster with no valid path detects it, returns to patrol or despawns, and logs the failure.
- [ ] The extraction-zone chase-termination rule matches [`31_entry_point_extraction_zone.md`](31_entry_point_extraction_zone.md).
- [ ] Simultaneous pursuers per target are limited.
- [ ] Targets are sticky; a monster mid-chase does not oscillate between two nearby players.
- [ ] The debug overlay draws path, last known position, search radius, and give-up timers.
- [ ] An automated harness verifies pursuit, loss, search, and give-up across many seeds.
- [ ] Every state transition is logged with tick, monster, target, and reason.
- [ ] A chase looks identical on the chased player's client and on another client watching, within interpolation tolerance.
