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

## The fix

1. Add `JoinByCode` to the `CreationType` enum.
2. Add a popup to capture the code, writing it to `ConnectionSettings.Instance.SessionCode` — which is what `JoinGameAsync` already reads. Model it on `DirectConnectPopUp`, and follow the `MainMenuState` + `CancellableUserInputPopUp` pattern used for the other two popups at `GameManager.cs:145-178`.
3. Add the `case CreationType.JoinByCode: GameConnection = await GameConnection.JoinGameAsync(); break;` branch to the switch at `GameManager.cs:247`.
4. Add a "Join by Code" button in `MainMenu.cs` calling `StartGameAsync(CreationType.JoinByCode)`.

No new networking code is needed — the transport and session layers already handle this path. This is enum, UI, and wiring only.

## Related

- Also worth doing at the same time: `SessionOptions.MaxPlayers` uses `GameManager.MaxPlayer` (32, a deathmatch number). Set it to the real crew size.
- See [`core_components.md`](core_components.md) §12 for the full build and release picture.
