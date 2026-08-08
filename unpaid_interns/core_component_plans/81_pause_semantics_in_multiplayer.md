# 81 — Pause Semantics in Multiplayer

**Source:** [`core_components.md`](../core_components.md) §9 — UI & Feedback
**Status:** ⚠️ `PauseMenu` exists and has never been tested against a live session
**Depends on:** [Settings / Options Menu](78_settings_options_menu.md)
**Blocks:** players trusting what the game tells them

## Summary

The menu that says "Paused" while a monster walks toward you.

`PauseMenu.cs` exists and works as a screen. What it cannot do — what nothing can do — is stop a networked simulation. The server keeps ticking, the round clock keeps advancing, monsters keep pathing, and the player standing in the menu is standing still in a dangerous building with their hands off the controls.

`core_components.md` states the requirement precisely: the menu *"must be explicit that the world keeps running, and must not imply safety"*, and notes it is **currently untested against a live session**. That untested part is the real work here. A pause menu in a single-player game is a solved widget; in a session with three other people it is a small trap, and the trap is entirely in what the player believes.

This is a small component with an outsized honesty requirement. Every other system in the game can be forgiving about a player's mental model. This one cannot, because the consequence of a wrong model is dying while reading a menu.

## How to Build

**Rename it and say what is actually true**

- Do not call it "Pause". Call it what it is — a menu — and state the fact plainly on the screen: *the world keeps running*. In-fiction is better than a warning box: an employer that reminds you your break is unpaid and unauthorised does the same job and fits the premise.
- No dimming, no blur, no time-freeze visual language. Every one of those is a learned signal for "you are safe now", and borrowing them here is the lie.
- **Keep the game visible behind the menu.** A full-screen opaque menu removes the player's ability to notice they are being approached; a compact panel over a live view lets them see the thing that kills them and close the menu.
- Show the round clock and the crew roster on it if they are visible elsewhere. The player pausing has the most to gain from knowing how much time they just spent.

**Make leaving fast and make leaving free**

- One key to close, the same key that opened it, always. A player who needs to move *now* must not navigate a menu tree to get back.
- No confirmation prompts between the player and the world.
- Close the menu automatically on any event that demands attention: taking damage, a monster entering line of sight, or the departure warning. Recommended: **close on damage at minimum** — a player being attacked while in a menu is the worst case this component exists to prevent.
- Do not capture the mouse in a way that makes closing slow, and do not stop feeding input to the character in a way that produces a visible hitch when it resumes.

**Test it against a live session, because that is the gap**

The status is "untested", so the acceptance criteria have to be about actually running it:

- Two real clients, one opens the menu, verify the other sees them standing idle and fully vulnerable.
- Verify the round clock advances while the menu is open, and that the menu-opener's normalized time matches everyone else's when they close it ([`03_round_timer_clock.md`](03_round_timer_clock.md) is `NetworkTick`-derived, so it should — confirm it).
- Verify a monster kills a paused player. That should work, and if it silently does not, something in the input or prediction path is being suppressed in a way that will cause worse bugs elsewhere.
- Verify the menu behaves in the hub, in a location, while spectating ([`22_spectator_mode.md`](22_spectator_mode.md)), and during settlement — four states with different correct behaviour.
- Verify it in a **standalone build against a real host**, not only in the Editor. §12 flags that Editor multiplayer testing does not prove a build works.

**Do not let it become a safe state by accident**

- No invulnerability, no input suppression that stops movement being processed, no client-side time scaling. `Time.timeScale = 0` on a client is the specific temptation and it is wrong: it stops presentation, animation, and any `deltaTime`-driven client logic while the server keeps simulating, which produces a desynchronised mess on close.
- The character continues to be a valid target, continues to emit noise if they were making any, and continues to be perceivable ([`53_perception_system.md`](53_perception_system.md) reads server state, which the menu does not touch — verify nothing accidentally suppresses it).
- **Disconnect is the only real exit**, and it costs what disconnecting costs ([`24_mid_round_disconnect_handling.md`](24_mid_round_disconnect_handling.md)). The menu should say so before quitting mid-round: a confirmation that explains the loot and penalty consequence, since that plan distinguishes a deliberate quit from a drop and applies consequences immediately for it.

**Cover the single-player case honestly**

- If a solo session ever exists — one player hosting alone — the same rules should apply rather than special-casing a real pause. A game that pauses when you are alone and does not when you are not teaches two different models, and players will carry the wrong one into a group.
- If a genuine pause is wanted for solo play, it must be a **host-authoritative pause of the whole simulation**, visible to everyone, and it is a different feature with its own consent problem in a group.

**Make it the route to settings**

- [`78_settings_options_menu.md`](78_settings_options_menu.md) needs to be reachable mid-round, because that is when a player discovers their sensitivity is wrong. This menu is that route, and the same "the world keeps running" framing applies while they are in there.

## Acceptance Criteria

- [ ] The menu is not labelled "Pause" and states explicitly that the world keeps running.
- [ ] No dimming, blur, or time-freeze visual language is used.
- [ ] The game remains visible behind a compact menu panel.
- [ ] One key opens and closes it, with no confirmation between the player and the world.
- [ ] The menu closes automatically on taking damage.
- [ ] `Time.timeScale` is never modified, and no input or simulation path is suppressed while the menu is open.
- [ ] A player with the menu open remains a valid target, continues to emit noise, and can be killed.
- [ ] Other clients see a menu-opener as a normal idle character with no indication of safety.
- [ ] The round clock advances while the menu is open, and normalized time matches all clients on close.
- [ ] The menu behaves correctly in the hub, in a location, while spectating, and during settlement.
- [ ] Quitting mid-round shows a confirmation explaining the loot and penalty consequence.
- [ ] Solo sessions use identical semantics, or a genuine host-authoritative pause is implemented and visible to everyone.
- [ ] The settings menu is reachable from here mid-round, under the same framing.
- [ ] All of the above is verified with two standalone builds against a real host, not only in the Editor.
- [ ] A playtester who used the menu mid-round can correctly state afterwards that they were not protected.
