# 102 — Localization

**Source:** [`core_components.md`](../core_components.md) §13 — Onboarding, Performance & Long Tail
**Status:** ❌ Not started — safe to defer, expensive to retrofit
**Depends on:** nothing — the preparation is a convention, not a system
**Blocks:** nothing now; everything about shipping in another language later

## Summary

Not translating the game, but keeping the option to.

`core_components.md` is precise about the trade-off: localization is *"not needed for a playable build, but retrofitting it after UI text is scattered across UXML and C# is far more expensive than planning for it"*, and it is *"safe to defer if consciously decided."*

Both halves matter. The decision to defer is correct — a game with no players does not need a second language, and the localization package, translation pipeline, and font work are real cost against zero current benefit. But the **cheap preparation is not the same as the expensive implementation**, and skipping the preparation is what turns a two-week job into a two-month one.

The preparation is essentially one rule: **strings live in a table, not in the code or the markup that displays them.** That costs nothing when applied from the start and is tedious archaeology when applied afterwards, because by then the strings are in UXML `text` attributes, in `new Label($"{killer} killed {victim}")`, and in a hundred inline literals nobody can enumerate.

Several plans have already adopted the convention independently — [`73_interaction_prompts.md`](73_interaction_prompts.md), [`74_terminal_hub_interface.md`](74_terminal_hub_interface.md), [`77_action_feed.md`](77_action_feed.md), and [`78_settings_options_menu.md`](78_settings_options_menu.md) each require their strings in a shared table. This component is where that becomes one table with one owner.

## How to Build

**Decide, explicitly, and write the decision down**

- The instruction is to defer **consciously**. That means recording the decision here — deferred, with the preparation done — rather than leaving it undone by default.
- If localization is genuinely never happening, say that too. It changes what is worth doing below.
- Revisit at the point a publisher, a platform requirement, or a player base makes it concrete.

**Do the cheap preparation now**

- **One string table.** Every player-visible string gets a key and lives in one asset. No literals in UXML `text` attributes, no interpolated strings built in C# for display.
- **Format strings with named placeholders**, not concatenation. `"{player} banked {item} for {value} credits"` survives a language with different word order; `name + " banked " + item` does not. This single rule is the difference between translatable and not.
- **Never build a sentence from fragments.** The action feed's current `$"{killer} killed {victim}"` is fine as a format string and fatal as three concatenated pieces.
- **Keep numbers and dates out of hand-built strings** — use culture-aware formatting even in English, because it costs nothing now.
- Audit where strings currently live. The UI Toolkit assets in `Assets/UI Toolkit/GameUI/` and the screens that drive them (`MainMenu.cs`, `PauseMenu.cs`, `ActionFeed.cs`, `LeaderboardUi.cs`, `RespawnScreen.cs`, `InGameHUD.cs`) are the whole current surface, and it is small today. It will not be small after §9 lands.

**Design the UI for text that is 40% longer**

- This is the other half of cheap preparation and it is pure layout discipline. German and Finnish strings run substantially longer than English; a button sized to its English label breaks.
- Avoid fixed-width text containers, allow wrapping, and test with an artificially lengthened pseudo-locale rather than waiting for a real translation.
- A **pseudo-localization mode** — every string wrapped and padded, e.g. `[!!! Pick up !!!]` — is a few hours of work and catches every hardcoded literal and every too-narrow container at once. It is the highest-value single thing in this component and it works before any translation exists.
- [`71_hud.md`](71_hud.md) already requires named HUD regions and legibility at reduced scale; longer text is another pressure on the same layout, and testing both together is cheaper than twice.

**Know what will still be expensive**

Being honest about this prevents the preparation from being mistaken for the whole job:

- **Fonts and glyph coverage.** Cyrillic, CJK, and diacritics need font assets the project does not have, and CJK in particular changes atlas size and rendering cost meaningfully.
- **Audio and subtitles.** Voice lines would need re-recording; subtitles ([`79_accessibility.md`](79_accessibility.md)) need translating, and the subtitle surface is large because it covers non-speech sounds too.
- **Tone.** The employer's voice is the game's identity — [`98_tutorial_and_onboarding.md`](98_tutorial_and_onboarding.md), [`70_performance_report.md`](70_performance_report.md), [`77_action_feed.md`](77_action_feed.md), and [`74_terminal_hub_interface.md`](74_terminal_hub_interface.md) all depend on corporate-satire register. That does not survive literal translation; it needs a translator who is writing rather than converting, which is a different and more expensive kind of hire.
- **Right-to-left layout**, if ever in scope, is a UI rework rather than a string swap.

**Do not install the localization package yet**

- `com.unity.localization` is not in `Packages/manifest.json` and does not need to be. Adding a package, a locale table asset, and a settings pipeline is the expensive half, and it can be adopted later against a string table that already exists.
- The string table can be a plain ScriptableObject keyed by string until then. Migrating a well-formed table into the localization package is mechanical; extracting literals from a hundred call sites is not.
- Structure the table so migration is obvious: a key, an English value, and a comment field for translator context. That is the same shape the package expects.

## Acceptance Criteria

- [ ] The decision to defer full localization is recorded here, along with the trigger for revisiting it.
- [ ] Every player-visible string lives in one table with a key; no literals remain in UXML `text` attributes or in C#.
- [ ] All composed sentences use format strings with named placeholders; no display string is built by concatenation.
- [ ] Numbers and dates use culture-aware formatting.
- [ ] The table carries a key, an English value, and a translator-context comment per entry.
- [ ] A pseudo-localization mode exists that lengthens and marks every string.
- [ ] Running in pseudo-localization reveals no hardcoded literals and no clipped or overflowing UI.
- [ ] No UI element is sized to the width of its English label; all text containers wrap or expand.
- [ ] Layout holds with strings 40% longer than English, tested at both default and reduced UI scale.
- [ ] The remaining expensive work — fonts, subtitles, tonal translation, RTL — is enumerated so its cost is not underestimated later.
- [ ] The localization package is deliberately not installed, and the migration path from the plain string table is documented.
- [ ] Every new player-visible string added after this point goes into the table, enforced by review.
