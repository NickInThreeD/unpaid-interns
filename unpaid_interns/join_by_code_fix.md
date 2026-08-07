# Note — Join-by-Code Is Unreachable

**Status:** Not started. Wire before the first real playtest.

## The problem

`GameConnection.JoinGameAsync()` implements joining a specific Relay session by code, but nothing can call it. No `CreationType` maps to it and no menu button invokes it — it is dead code.

The consequence: players currently match by **session *name***, not by code. `CreateorJoinGameAsync` passes `GameSettings.Instance.SessionName` to `CreateOrJoinSessionAsync`, so anyone typing the same name lands in the same session. Two unrelated groups picking the same name collide, and there is no way to join a specific friend's session.

Half the UI already exists — `SessionInfo.cs:211` displays a copyable session code. Only the join half was never built.

## Evidence

| What | Where |
|---|---|
| Implemented but uncalled | `Assets/Scripts/Networking/GameConnection/GameConnection.cs:58` — `JoinGameAsync()` |
| Enum missing the case | `Assets/Scripts/Networking/Client/ConnectionSettings.cs:29` — only `CreateOrJoin`, `Host`, `ConnectAndJoin` |
| Switch missing the branch | `Assets/Scripts/Gameplay/GameManager/GameManager.cs:247` |
| No button | `Assets/Scripts/UI/Game/MainMenu.cs:137-141` |
| Code already displayed | `Assets/Scripts/UI/SessionInfo/SessionInfo.cs:211` |
| Code already read from here | `ConnectionSettings.Instance.SessionCode` (set at `GameManager.cs:272`) |
| **UI state already scaffolded** | `GameSettings.cs:21` — `MainMenuState.JoinCodePopUp` |
| **Style binding already scaffolded** | `GameSettings.cs:117-121` — `JoinSessionStyle` / `JoinSessionStylePropertyName` |

## The fix

Smaller than it first appears — the popup's *state plumbing* already exists. `MainMenuState.JoinCodePopUp` and its `JoinSessionStyle` display binding are both present and unused, so someone started this and stopped before building the view.

1. Add `JoinByCode` to the `CreationType` enum (`ConnectionSettings.cs:29`).
2. Create `JoinCodePopup.uxml` and a `JoinCodePopUp.cs` binding to the **existing** `GameSettings.JoinSessionStylePropertyName`. Model both on `DirectConnectPopup.uxml` / `DirectConnectPopUp.cs`. The field writes to `ConnectionSettings.Instance.SessionCode`, which is what `JoinGameAsync` already reads.
3. Add a `creationType == CreationType.JoinByCode` branch to the popup-await block at `GameManager.cs:145-178`, setting `MainMenuState = MainMenuState.JoinCodePopUp` — the enum value is already there.
4. Add `case CreationType.JoinByCode: GameConnection = await GameConnection.JoinGameAsync(); break;` to the switch at `GameManager.cs:247`.
5. Add a "Join by Code" button in `MainMenu.uxml` / `MainMenu.cs` calling `StartGameAsync(CreationType.JoinByCode)`.

No new networking code is needed — the transport and session layers already handle this path. This is enum, UXML, and wiring only.

## Related

- Also worth doing at the same time: `SessionOptions.MaxPlayers` uses `GameManager.MaxPlayer` (32, a deathmatch number). Set it to the real crew size.
- See [`core_components.md`](core_components.md) §12 for the full build and release picture.
