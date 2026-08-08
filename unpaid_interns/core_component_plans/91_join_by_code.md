# 91 — Join by Code

**Source:** [`core_components.md`](../core_components.md) §12 — Build & Release Readiness
**Status:** ❌ Implemented but unreachable — dead code · **[MVP]**
**Depends on:** [Relay & Lobby Service Enablement](90_relay_and_lobby_service_enablement.md)
**Blocks:** playing with a specific friend; the first real playtest

## Summary

The ability to join your friend's game rather than a stranger's.

Right now there is none. `CreateorJoinGameAsync` passes `GameSettings.Instance.SessionName` to `MultiplayerService.Instance.CreateOrJoinSessionAsync`, so **players match by session *name***. Two unrelated groups who both type "test" land in the same session, and there is no way to reach a specific friend's game at all.

The join-by-code implementation exists and nothing can call it. `GameConnection.JoinGameAsync()` builds a `JoinSessionOptions`, attaches the network handler, and calls `MultiplayerService.Instance.JoinSessionByCodeAsync(ConnectionSettings.Instance.SessionCode, options)` — a complete, correct implementation with **no caller**. No `CreationType` maps to it and no menu button invokes it.

A detailed working note already exists at [`../join_by_code_fix.md`](../join_by_code_fix.md) with the full evidence table. This file is the canonical plan; that note is its source and remains accurate.

`core_components.md` marks this **[MVP]** and says to wire it before the first real playtest, which is correct — a playtest where testers cannot reliably join each other produces no useful data about anything else.

## The scaffolding is already there

This is the reason the component is small. Someone started it and stopped before building the view:

| Piece | Where | State |
| --- | --- | --- |
| `JoinGameAsync()` | `GameConnection.cs:58` | **Implemented**, uncalled |
| `SessionCode` property | `ConnectionSettings.cs:149` | **Exists**, with change notification |
| Code format validation | `ConnectionSettings.cs:136,162` | **`IsSessionCodeFormatValid` and `CheckIsSessionCodeFormatValid` both exist** |
| `MainMenuState.JoinCodePopUp` | `GameSettings.cs:21` | **Exists**, unused |
| `JoinSessionStyle` binding | `GameSettings.cs:117-121` | **Exists**, unused |
| Session code display | `SessionInfo.cs:211` | **Working**, click-to-copy at `SessionInfo.cs:111-117` |
| `CreationType` enum | `ConnectionSettings.cs:29` | Missing `JoinByCode` |
| Switch branch | `GameManager.cs:247` | Missing the case |
| Popup view | — | Does not exist |
| Menu button | `MainMenu.cs:137-141` | Does not exist |

**No new networking code is required.** The transport and session layers already handle this path end to end. This is enum, UXML, and wiring.

## How to Build

**Wire the path**

1. Add `JoinByCode` to `CreationType` (`ConnectionSettings.cs:29`).
2. Create `JoinCodePopup.uxml` and `JoinCodePopUp.cs`, modelled directly on `DirectConnectPopup.uxml` / `DirectConnectPopUp.cs`. Bind visibility to the **existing** `GameSettings.JoinSessionStylePropertyName`. The input field writes to `ConnectionSettings.Instance.SessionCode`, which `JoinGameAsync` already reads.
3. Add a `CreationType.JoinByCode` branch to the popup-await block at `GameManager.cs:145-178`, setting `MainMenuState = MainMenuState.JoinCodePopUp` — the enum value already exists.
4. Add `case CreationType.JoinByCode: GameConnection = await GameConnection.JoinGameAsync(); break;` to the switch at `GameManager.cs:247`.
5. Add a "Join by Code" button to `MainMenu.uxml` / `MainMenu.cs` calling `StartGameAsync(CreationType.JoinByCode)`.

**Use the validation that already exists**

- `ConnectionSettings.SessionCode`'s setter already calls `CheckIsSessionCodeFormatValid` and updates `IsSessionCodeFormatValid`. Bind the join button's enabled state to that property rather than validating again in the popup — a second validator will disagree with the first eventually.
- Normalise input before validating: session codes are typically case-insensitive and players will paste them with whitespace. Strip and upper-case in the setter so every consumer sees the same form.

**Decide what happens to session names**

- Keeping both paths is fine and probably right: **create-or-join by name** for open play, **join by code** for a specific friend. But the name-matching collision is a real problem — two groups typing "test" is not a hypothetical.
- **Recommended:** create a session with a generated code and no shared name, and make join-by-code the primary path. If name-based matching is kept, generate a random suffix so a plain name cannot collide.
- The host's code must be visible and copyable *before* anyone needs it. `SessionInfo.cs` already displays it with click-to-copy; surface it prominently in the lobby, not only in a status line.

**Fail legibly**

- A wrong or expired code must produce a **specific** message — "no session with that code" — not a generic connection failure. [`90_relay_and_lobby_service_enablement.md`](90_relay_and_lobby_service_enablement.md) requires session-not-found to be one of the distinguishable cases, and this is its main consumer.
- A full session needs its own message, which [`08_late_join_rejoin_policy.md`](08_late_join_rejoin_policy.md) already requires ("joining is refused once the crew is at the configured size, with a clear message rather than a silent failure").
- A session that has deployed needs a third message, per whichever join policy component 08 selects.
- Reuse `ConnectionStatusScreen` rather than adding a bespoke error path.

**Fix the crew size while here**

- `SessionOptions.MaxPlayers` is `GameManager.MaxPlayer`, which is **32** — a deathmatch number consumed in three places (`GameConnection.cs:48`, `:130`, `UGS_ServerBootstrap.cs:73`). [`19_crew_roster.md`](19_crew_roster.md) makes the real crew size concrete and requires this to change; [`92_session_lifecycle.md`](92_session_lifecycle.md) owns the surrounding lifecycle rules.
- Doing it here is cheap because the session-creation call is already open in front of you.

## Acceptance Criteria

- [ ] `CreationType.JoinByCode` exists and is handled in the `GameManager` switch.
- [ ] A "Join by Code" button exists in the main menu and opens a code-entry popup.
- [ ] The popup binds to the existing `JoinSessionStyle` display binding and `MainMenuState.JoinCodePopUp`.
- [ ] Entered codes write to `ConnectionSettings.Instance.SessionCode` and are normalised for case and whitespace.
- [ ] The join button's enabled state is driven by the existing `IsSessionCodeFormatValid`, with no second validator.
- [ ] `JoinGameAsync` is reached and successfully joins a specific host's session.
- [ ] Two players on different networks connect using a code, verified from standalone builds.
- [ ] The host's session code is prominently visible and copyable before anyone needs it.
- [ ] Two groups using the same session name cannot collide.
- [ ] An invalid or expired code produces a specific "no session with that code" message.
- [ ] A full session and a deployed session each produce their own distinct message.
- [ ] All failures route through `ConnectionStatusScreen` rather than a bespoke error path.
- [ ] `SessionOptions.MaxPlayers` reflects the real crew size in all three call sites, not 32.
- [ ] No new networking code was added; the change is enum, UXML, and wiring only.
