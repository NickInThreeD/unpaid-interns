using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.MP_FPS
{
    /// <summary>
    /// Handles gameplay scene loading and unloading for a session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The entities version of this class loaded the game scene locally on every peer and then waited
    /// on <c>SceneReference</c> subscene entities to finish streaming. There are no subscenes any
    /// more, and NGO owns scene synchronisation itself: the <b>host</b> loads through
    /// <see cref="NetworkSceneManager"/>, and every client that connects — then or later — is brought
    /// to the same set of scenes automatically as part of its synchronisation handshake.
    /// </para>
    /// <para>
    /// So clients never call the load path here at all. They wait, which is what
    /// <see cref="WaitForClientSynchronizationAsync"/> is for.
    /// </para>
    /// </remarks>
    static class ScenesLoader
    {
        /// <summary>
        /// Host-side gameplay scene load, replicated to all clients by NGO.
        /// </summary>
        public static async Task LoadGameplayAsHostAsync(CancellationToken cancellationToken = default)
        {
            var manager = NetworkManager.Singleton;
            if (manager == null || !manager.IsServer)
            {
                Debug.LogError($"[{nameof(ScenesLoader)}] {nameof(LoadGameplayAsHostAsync)} called on a non-host.");
                return;
            }

            if (SceneManager.GetSceneByName(GameManager.GameSceneName).isLoaded)
                return;

            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.LoadGameScene);

            var completed = false;
            void OnLoadEventCompleted(string sceneName, LoadSceneMode _, System.Collections.Generic.List<ulong> __,
                System.Collections.Generic.List<ulong> ___)
            {
                if (sceneName == GameManager.GameSceneName)
                    completed = true;
            }

            manager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
            try
            {
                var status = manager.SceneManager.LoadScene(GameManager.GameSceneName, LoadSceneMode.Additive);
                if (status != SceneEventProgressStatus.Started)
                {
                    Debug.LogError($"[{nameof(ScenesLoader)}] Could not start loading " +
                                   $"'{GameManager.GameSceneName}': {status}.");
                    return;
                }

                while (!completed)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Awaitable.NextFrameAsync(cancellationToken);
                }
            }
            finally
            {
                manager.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
            }

            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.LoadGameScene, 1f);
        }

        /// <summary>
        /// Client-side wait for NGO to finish synchronising this client with the host's scenes and
        /// spawned objects.
        /// </summary>
        /// <remarks>
        /// This is the successor to <c>WaitForGhostReplicationAsync</c>. The intent it inherits is
        /// worth keeping: do not show the world until it is populated, or the player watches props
        /// and teammates pop in around them.
        /// </remarks>
        public static async Task WaitForClientSynchronizationAsync(CancellationToken cancellationToken = default)
        {
            var manager = NetworkManager.Singleton;
            if (manager == null || manager.IsServer)
                return;

            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.WorldReplication);

            var synchronized = false;
            void OnSynchronizeComplete(ulong clientId)
            {
                if (clientId == manager.LocalClientId)
                    synchronized = true;
            }

            manager.SceneManager.OnSynchronizeComplete += OnSynchronizeComplete;
            try
            {
                while (!synchronized)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // A host that vanishes mid-handshake must not leave us spinning here forever.
                    if (!manager.IsListening && !manager.IsConnectedClient)
                        throw new System.InvalidOperationException("Lost the connection while joining the session.");

                    await Awaitable.NextFrameAsync(cancellationToken);
                }
            }
            finally
            {
                manager.SceneManager.OnSynchronizeComplete -= OnSynchronizeComplete;
            }

            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.WorldReplication, 1f);
        }

        /// <summary>
        /// Unloads the gameplay scene. Only meaningful once netcode has already shut down — while a
        /// session is live, NGO owns the scene set.
        /// </summary>
        public static async Task UnloadGameplayScenesAsync()
        {
            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.UnloadingWorld);

            var gameplay = SceneManager.GetSceneByName(GameManager.GameSceneName);
            if (gameplay.IsValid() && gameplay.isLoaded && gameplay != SceneManager.GetActiveScene())
            {
                var unloadScene = SceneManager.UnloadSceneAsync(gameplay);
                UpdateLoadingStateAsync(LoadingData.LoadingSteps.UnloadingGameScene, unloadScene);
                await unloadScene;
            }
        }

        static async void UpdateLoadingStateAsync(LoadingData.LoadingSteps step, AsyncOperation loadingTask)
        {
            while (loadingTask != null && !loadingTask.isDone)
            {
                LoadingData.Instance.UpdateLoading(step, loadingTask.progress);
                await Awaitable.NextFrameAsync();
            }
        }
    }
}
