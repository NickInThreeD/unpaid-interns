using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Unity.MP_FPS
{
    public partial class GameManager : MonoBehaviour
    {
        /// <summary>
        /// Safe return to main menu, can be called by the pause menu button.
        /// </summary>
        public async void ReturnToMainMenuAsync()
        {
            Debug.Log($"[{nameof(ReturnToMainMenuAsync)}] Called.");
            if (!CanUseMainMenu)
            {
                QuitAsync();
                return;
            }

            if (m_LoadingGameCancel != null)
            {
                Debug.Log($"[{nameof(ReturnToMainMenuAsync)}] Cancelling loading game.");
                m_LoadingGameCancel.Cancel();
                try
                {
                    await m_LoadingGame;
                }
                catch (OperationCanceledException)
                {
                    // Discarding this exception because we're the one asking for it.
                }
                catch (Exception e)
                {
                    // The load was already failing; we are tearing down regardless.
                    Debug.LogException(e);
                }
                Debug.Log($"[{nameof(ReturnToMainMenuAsync)}] Loading Cancelled, start returning to main menu.");
            }

            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.UnloadingGame);
            GameSettings.Instance.GameState = GlobalGameState.Loading;

            GameSettings.Instance.IsPauseMenuOpen = false;
            await DisconnectAndUnloadWorlds();

            // Restart the main menu scene.
            Start();

            Utils.SetCursorVisible(true);

            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.BackToMainMenu);
            GameSettings.Instance.GameState = GlobalGameState.MainMenu;
        }

        /// <summary>
        /// Safe shutdown of the game. Saves everything that needs to be saved.
        /// </summary>
        public async void QuitAsync()
        {
            await DisconnectAndUnloadWorlds();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Shuts the session down: stop netcode, leave the Steam lobby, unload the gameplay scene.
        /// </summary>
        /// <remarks>
        /// Named for its predecessor's job. There are no ECS worlds to destroy any more — NGO's own
        /// shutdown despawns everything it owns — but the ordering still matters: netcode first, then
        /// the lobby, then the scene, because unloading a scene NGO still believes it owns logs errors.
        /// </remarks>
        async Task DisconnectAndUnloadWorlds()
        {
            ConnectionSettings.Instance.GameConnectionState = ConnectionState.State.Disconnected;

            UnsubscribeSessionEvents();

            GameConnection.Shutdown();
            GameConnection = null;

            // NGO tears down over the following frame; unloading the scene before that races it.
            await Awaitable.NextFrameAsync();

            await ScenesLoader.UnloadGameplayScenesAsync();
        }

        /// <summary>
        /// Host went away, or we were disconnected. Report it and get back to the menu rather than
        /// leaving the player staring at a frozen world.
        /// </summary>
        void OnClientStopped(bool wasHost)
        {
            if (GameSettings.Instance.GameState == GlobalGameState.MainMenu)
                return;

            Debug.Log($"[{nameof(OnClientStopped)}] Session ended (wasHost: {wasHost}).");

            if (!wasHost)
                GameSettings.Instance.LastSessionMessage = "The host ended the session.";

            ReturnToMainMenuAsync();
        }
    }
}
