# 98 — Tutorial / Onboarding

**Source:** [`core_components.md`](../core_components.md) §13 — Onboarding, Performance & Long Tail
**Status:** ❌ Not started
**Depends on:** [Terminal / Hub Interface](74_terminal_hub_interface.md), [Interaction Prompts](73_interaction_prompts.md), [Action Feed](77_action_feed.md)
**Blocks:** a new crew's first contract being anything other than a confusing failure

## Summary

Teaching a genre that is unusually bad at teaching itself.

`core_components.md` names the problem precisely: quota timing, carry limits, monster counterplay, and extraction rules are all **learned through expensive failure**. That is not a difficulty setting, it is an information problem — a crew can play perfectly well and still lose a run because nobody told them the quota escalates, or that unbanked loot is forfeit, or that the heavy thing they died carrying was worth less than the four light things they left.

The premise makes this unusually cheap to solve. `GAME_DESIGN.md` describes an employer that treats interns as expendable labour, and **a patronising corporate induction is free comedy** as well as free instruction. A tutorial that would be intrusive in another game is on-tone here: of course the company makes you watch an orientation. Of course it is condescending. Of course it omits the part where you die.

The design constraint is that this must teach **rules, not sequences**. A scripted tutorial level contradicts a game built on procedural unfamiliarity, and it teaches a map the player will never see again.

## How to Build

**Teach the four things failure teaches too late**

Ranked by how expensive they are to learn the hard way:

- **Unbanked loot is forfeit.** The single most costly misunderstanding available. A crew that carries a full haul until the round ends without depositing it loses everything, and nothing in the moment tells them that will happen ([`43_loot_banking_deposit.md`](43_loot_banking_deposit.md)).
- **The quota escalates and the deadline is real.** [`64_quota_system.md`](64_quota_system.md) makes missing it a total loss for everyone. A crew that treats day one casually has already spent a quarter of the cycle.
- **Carry limits force repeated trips.** Four slots and weight-versus-value is the core optimisation ([`40_inventory_item_bar.md`](40_inventory_item_bar.md), [`12_carry_weight.md`](12_carry_weight.md)), and it is invisible until the first trip back.
- **Monsters have counterplay.** Crouching helps against some, silence against others ([`53_perception_system.md`](53_perception_system.md), [`58_monster_variety_set.md`](58_monster_variety_set.md)). A player who believes monsters are unavoidable stops trying, which is the worst outcome the threat layer can produce.

Everything else can be discovered. These four cannot, cheaply.

**Deliver it as an induction, in the hub, skippable**

- The employer's orientation on the terminal ([`74_terminal_hub_interface.md`](74_terminal_hub_interface.md)) — a short briefing on the first run, in the company's voice, covering the loop and the four rules above.
- **Skippable, and re-readable.** A player who skips it must be able to find it again; a returning player must not sit through it. Put it in the terminal as a permanent view rather than as a one-time gate.
- Keep it under a minute. An induction that outlasts the joke stops being funny and starts being the thing between the player and the game.
- Do not gate deployment on completing it. A crew that wants to learn by dying should be allowed to.

**Use contextual first-time hints for the rest**

- One-shot prompts at the moment a mechanic first becomes relevant: the first time an inventory fills, the first time something is banked, the first time the quota display turns unfriendly.
- These ride the systems that already exist. [`73_interaction_prompts.md`](73_interaction_prompts.md) already requires distinct refusal messages naming the reason — *"Hands full — drop the generator to open this door"* is a tutorial sentence that costs nothing extra.
- [`77_action_feed.md`](77_action_feed.md)'s bank announcements with value are similarly instructional by construction: hearing "asset recovered: 340 credits" teaches the banking rule the first time it fires.
- Fire each hint **once per player, ever**, persisted in the local settings slot ([`78_settings_options_menu.md`](78_settings_options_menu.md), [`86_savesystem_integration.md`](86_savesystem_integration.md)) — not per run, and not per session. A hint that reappears every contract is noise.
- Provide a "reset hints" option and a way to disable them.

**Teach monster counterplay through design, not text**

- A text box saying "crouch to avoid the sight hunter" is worse than the encounter teaching it. [`58_monster_variety_set.md`](58_monster_variety_set.md) already specifies the arc: encounter one is survivable and teaches the sense, encounter two punishes the wrong instinct, encounter three rewards the right plan.
- What the induction should supply is the **general rule** — that monsters differ, that some hunt by sound and some by sight, and that counterplay exists. The specifics belong to the encounters.
- [`53_perception_system.md`](53_perception_system.md) makes the same argument: a monster that visibly turns toward a noise has taught the rule without a line of text, and no numeric detection meter should ever appear.

**Do not build a tutorial level**

- A scripted first location contradicts the procedural premise, costs a hand-built map, and teaches geometry the player will never encounter again.
- The debug location [`26_location_catalogue.md`](26_location_catalogue.md) requires for testing — fixed tiny layout, known loot — is *not* this. It is a test fixture, and repurposing it as a tutorial would ship a location that plays nothing like the real ones.
- If a safe first experience is wanted, make the **first destination an easy one** with a generous quota, and let the game teach itself. That is a data change, not a content build.

**Verify it by watching someone**

- The only meaningful test is a player who has never seen the game completing their first contract without an explanation from someone in the room.
- Watch for the four expensive misunderstandings specifically. If a first-time crew loses a haul to not banking it, the induction failed at its most important job regardless of how well it read.
- [`101_analytics_and_balance_telemetry.md`](101_analytics_and_balance_telemetry.md) can measure this at scale — first-run quota success rate and first-run unbanked-value-at-round-end are both direct indicators.

## Acceptance Criteria

- [ ] An in-fiction employer induction is available in the hub terminal on the first run.
- [ ] It covers the loop plus the four expensive rules: unbanked loot is forfeit, the quota escalates against a real deadline, carry is limited, and monsters have counterplay.
- [ ] It is skippable, re-readable at any time, and under a minute.
- [ ] Deployment is never gated on completing it.
- [ ] Contextual first-time hints fire at the moment each mechanic first becomes relevant.
- [ ] Each hint fires once per player ever, persisted in the local settings slot, not per run or per session.
- [ ] Hints can be reset and disabled.
- [ ] Interaction refusals and action feed announcements carry their instructional content without extra tutorial text.
- [ ] Monster counterplay is taught by encounters, not by text, and no numeric detection value is shown.
- [ ] No scripted tutorial level exists, and the debug location is not repurposed as one.
- [ ] A first-time player completes a full contract without verbal explanation from an observer.
- [ ] A first-time crew does not lose a haul to failing to bank it.
- [ ] First-run quota success rate and unbanked value at round end are instrumented.
- [ ] The induction reads as company satire and is skippable before the joke wears out.
