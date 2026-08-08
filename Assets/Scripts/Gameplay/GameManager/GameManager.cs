using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.MP_FPS
{
    public partial class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        /// <summary>
        /// Crew size. Feeds the Steam lobby member limit, spawn-point buffer sizing, and the roster UI.
        /// </summary>
        /// <remarks>
        /// Was 32, inherited from the deathmatch sample this project started as. Four is the co-op
        /// extraction genre default — Lethal Company and R.E.P.O. both ship it. Monster power budgets,
        /// quota scaling, and loot density are all tuned against this number, so it lives here as the
        /// single source of truth rather than being repeated at each call site.
        /// </remarks>
        public const int MaxPlayer = 4;

        public const string MainMenuSceneName = "MainMenu";
        public const string GameSceneName = "GameScene";

        static public GameConnection GameConnection { get; private set; }

        /// <summary>
        /// Host-only mapping of SteamID64 to crew state. Null on clients.
        /// </summary>
        public static CrewRegistry Crew { get; private set; }

        Task m_LoadingGame;
        CancellationTokenSource m_LoadingGameCancel;
        Task m_LoadingMainMenu;
        CancellationTokenSource m_LoadingMainMenuCancel;

        public UnityEngine.Audio.AudioMixer AudioMixer;
        public int MaxSoundEmitters;
        public int MaxSoundGameObjects;
        public SoundGameObjectPool SoundGameObjects;

        ISoundSystem m_SoundSystem;
        public ISoundSystem SoundSystem => m_SoundSystem;

        bool m_IsHeadless = false;
        public bool IsHeadless => m_IsHeadless;

        bool m_SessionEventsBound;

        public static bool CanUseMainMenu => SceneManager.GetActiveScene().name == MainMenuSceneName;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Debug.LogError($"Multiple instances of '{this}' violates the Singleton pattern!", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;

#if UNITY_STANDALONE_LINUX
            m_IsHeadless = true;
#else
            var commandLineArgs = new List<string>(System.Environment.GetCommandLineArgs());
            m_IsHeadless = commandLineArgs.Contains("-batchmode");
#endif
            ConfigVar.Init();

            if (m_IsHeadless)
            {
                m_SoundSystem = new SoundSystemNull();
            }
            else
            {
                m_SoundSystem = new SoundSystem();
                AudioListener audioListener = MainCameraSingleton.Instance.GetComponent<AudioListener>();
                SoundGameObjects = new SoundGameObjectPool("SoundSystemSources", MaxSoundGameObjects);
                m_SoundSystem.Init(audioListener.transform, MaxSoundEmitters, SoundGameObjects, AudioMixer);
            }
        }

        async void Start()
        {
            Application.runInBackground = true; //Prevents dropped connections during multiplayer gameplay

            MainCameraSingleton.Instance.GetComponent<Camera>().enabled = true;
            var audioListener = MainCameraSingleton.Instance.GetComponent<AudioListener>();
            if (audioListener != null)
            {
                m_SoundSystem.SetListenerTransform(audioListener.transform);
            }

            GameSettings.Instance.MainMenuSceneLoaded = false;
            if (SceneManager.GetActiveScene().name == MainMenuSceneName)
            {
                m_LoadingMainMenuCancel = new CancellationTokenSource();
                try
                {
                    m_LoadingMainMenu = StartMainMenuAsync(m_LoadingMainMenuCancel.Token);
                    await m_LoadingMainMenu;
                }
                catch (OperationCanceledException)
                {
                    // Nothing to do when the task is cancelled.
                }
                finally
                {
                    m_LoadingMainMenuCancel.Dispose();
                    m_LoadingMainMenuCancel = null;
                }
            }

            // Ensures it only ever loads once
            if (!SceneManager.GetSceneByName("Persistents").isLoaded)
            {
                SceneManager.LoadScene("Scenes/Persistents", LoadSceneMode.Additive);
            }
        }

        public void Update()
        {
            if (m_SoundSystem != null)
            {
                m_SoundSystem.UpdateSoundSystem(false);
            }
        }

        /// <summary>
        /// Prepares the main menu.
        /// </summary>
        /// <remarks>
        /// The entities build created a throwaway client <c>World</c> here so the menu had somewhere to
        /// live. NGO needs nothing of the sort — the <see cref="NetworkManager"/> sits idle in the
        /// Persistents scene until somebody hosts or joins. What this does instead is arm the Steam
        /// overlay-invite callback, so an invite accepted from the menu goes straight into a session.
        /// </remarks>
        Task StartMainMenuAsync(CancellationToken cancellationToken)
        {
#if !DISABLESTEAMWORKS
            SteamLobby.Initialize();
            SteamLobby.JoinRequested -= OnSteamJoinRequested;
            SteamLobby.JoinRequested += OnSteamJoinRequested;
#endif
            GameSettings.Instance.MainMenuSceneLoaded = true;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

#if !DISABLESTEAMWORKS
        /// <summary>
        /// The player accepted an invite from the Steam overlay. This is the whole invite flow: there
        /// is no code to type and no browser to search.
        /// </summary>
        void OnSteamJoinRequested(Steamworks.CSteamID lobbyId)
        {
            if (GameSettings.Instance.GameState != GlobalGameState.MainMenu)
            {
                Debug.Log($"[{nameof(OnSteamJoinRequested)}] Ignoring invite; already in a session.");
                return;
            }

            ConnectionSettings.Instance.PendingLobbyId = lobbyId.m_SteamID;
            StartGameAsync(CreationType.JoinSteam);
        }
#endif

        /// <summary>
        /// This method start the Gameplay session.
        /// </summary>
        public async void StartGameAsync(CreationType creationType)
        {
            if (GameSettings.Instance.GameState != GlobalGameState.MainMenu)
            {
                Debug.Log("[StartGameAsync] Called but in-game, cannot start while in-game!");
                return;
            }

            Debug.Log($"[{nameof(StartGameAsync)}] Called with creation type '{creationType}'");

            // The direct-connect debug paths still ask for an address up front; the Steam paths do not
            // need any user input at all, which is the point of the invite model.
            if (creationType == CreationType.HostDirect || creationType == CreationType.JoinDirect)
            {
                GameSettings.Instance.CancellableUserInputPopUp = new AwaitableCompletionSource();
                GameSettings.Instance.MainMenuState = creationType == CreationType.HostDirect
                    ? MainMenuState.StartHostPopup
                    : MainMenuState.DirectConnectPopUp;
                try
                {
                    await GameSettings.Instance.CancellableUserInputPopUp.Awaitable;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                finally
                {
                    GameSettings.Instance.MainMenuState = MainMenuState.MainMenuScreen;
                }
            }

            BeginEnteringGame();

            m_LoadingGameCancel = new CancellationTokenSource();
            try
            {
                m_LoadingGame = StartGameAsync(creationType, m_LoadingGameCancel.Token);
                await m_LoadingGame;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[{nameof(StartGameAsync)}] Loading has been cancelled.");
                return;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(StartGameAsync)}] Loading has failed, returning to main menu");
                Debug.LogException(e);

                // Surface something the player can act on — "Steam is not running" is a fixable
                // problem, and a silent bounce to the menu does not tell them that.
                GameSettings.Instance.LastSessionMessage = e.Message;

                // Disposing the token here because the error has been handled and ReturnToMainMenu should not check it.
                m_LoadingGameCancel.Dispose();
                m_LoadingGameCancel = null;
                ReturnToMainMenuAsync();
                return;
            }
            finally
            {
                m_LoadingGameCancel?.Dispose();
                m_LoadingGameCancel = null;
            }

            FinishLoadingGame();
        }

        void BeginEnteringGame()
        {
            GameSettings.Instance.GameState = GlobalGameState.Loading;
            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.StartLoading);
        }

        /// <summary>
        /// Establishes the session and gets the world ready to show.
        /// </summary>
        /// <remarks>
        /// This replaces the block that created ECS server/client <c>World</c>s, installed a driver
        /// constructor, and called <c>Listen</c>/<c>Connect</c>. NGO collapses all of it into
        /// <c>StartHost</c>/<c>StartClient</c>; the surrounding loading-progress reporting and the
        /// main-menu popup flow around it are unchanged, because they were never the problem.
        /// </remarks>
        async Task StartGameAsync(CreationType creationType, CancellationToken cancellationToken)
        {
            if (m_LoadingMainMenuCancel != null)
            {
                m_LoadingMainMenuCancel.Cancel();
                try
                {
                    await m_LoadingMainMenu;
                }
                catch (OperationCanceledException)
                {
                    // We are ignoring the cancelled exception as it is expected.
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.InitializeConnection);
            ConnectionSettings.Instance.GameConnectionState = ConnectionState.State.Connecting;

            SubscribeSessionEvents();

            try
            {
                GameConnection = creationType switch
                {
                    CreationType.HostSteam => await GameConnection.HostSteamAsync(cancellationToken),
                    CreationType.JoinSteam => await GameConnection.JoinSteamAsync(
                        ConnectionSettings.Instance.PendingLobbyId, cancellationToken),
                    CreationType.HostDirect => await GameConnection.HostDirectAsync(cancellationToken),
                    CreationType.JoinDirect => await GameConnection.JoinDirectAsync(cancellationToken),
                    _ => throw new ArgumentOutOfRangeException(nameof(creationType), creationType, null),
                };
            }
            catch
            {
                UnsubscribeSessionEvents();
                throw;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (GameConnection.IsHost)
            {
                Crew = new CrewRegistry();
                await ScenesLoader.LoadGameplayAsHostAsync(cancellationToken);
            }
            else
            {
                LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.WaitingConnection);
                await WaitForClientConnectionAsync(cancellationToken);
                await ScenesLoader.WaitForClientSynchronizationAsync(cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            ConnectionSettings.Instance.GameConnectionState = ConnectionState.State.Connected;
        }

        /// <summary>
        /// Waits for NGO's connection approval round trip to land, or for the attempt to fail.
        /// </summary>
        public static async Task WaitForClientConnectionAsync(CancellationToken cancellationToken = default)
        {
            var manager = NetworkManager.Singleton;

            while (!manager.IsConnectedClient)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // StartClient having returned true only means the attempt began. If the transport gives
                // up — no Steam route to the host, host already gone — IsListening drops and nobody
                // will ever set IsConnectedClient. Without this the loading screen hangs forever.
                if (!manager.IsListening)
                    throw new InvalidOperationException(
                        "Could not reach the host. They may have ended the session.");

                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }

        void SubscribeSessionEvents()
        {
            if (m_SessionEventsBound || NetworkManager.Singleton == null)
                return;

            m_SessionEventsBound = true;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.Singleton.OnClientStopped += OnClientStopped;
        }

        void UnsubscribeSessionEvents()
        {
            if (!m_SessionEventsBound || NetworkManager.Singleton == null)
                return;

            m_SessionEventsBound = false;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
        }

        /// <summary>
        /// Host-side: bind the arriving NGO client id to the stable SteamID64 behind it.
        /// </summary>
        /// <remarks>
        /// The Steam transport reports each peer's SteamID64 as its transport-level client id, and NGO
        /// preserves that as the client id here. That correspondence is what lets the host recognise a
        /// returning player instead of treating them as somebody new — see <see cref="CrewRegistry"/>
        /// for why keying on the client id alone is a bug rather than a shortcut.
        /// </remarks>
        void OnClientConnected(ulong clientId)
        {
            if (Crew == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
                return;

            var steamId = ResolveSteamId(clientId);
            if (steamId == 0ul)
            {
                Debug.LogWarning($"[{nameof(OnClientConnected)}] No SteamID64 for client {clientId}; " +
                                 "their state cannot be preserved across a reconnect.");
                return;
            }

            var member = Crew.Bind(steamId, clientId, ResolvePersonaName(steamId), out var isReconnect);
            Debug.Log($"[{nameof(OnClientConnected)}] {member.PersonaName} ({steamId}) " +
                      $"{(isReconnect ? "reconnected" : "joined")} as client {clientId}.");
        }

        void OnClientDisconnected(ulong clientId)
        {
            if (Crew == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
                return;

            var member = Crew.Unbind(clientId);
            if (member != null)
            {
                Debug.Log($"[{nameof(OnClientDisconnected)}] {member.PersonaName} ({member.SteamId}) " +
                          "dropped; their state is held for a rejoin.");
            }
        }

        static ulong ResolveSteamId(ulong clientId)
        {
#if !DISABLESTEAMWORKS
            // The host's own local client id is NGO-assigned and is not a SteamID64.
            if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
                return SteamManager.LocalSteamId;

            if (GameConnection != null && GameConnection.Transport == SessionTransport.Steam)
                return clientId;
#endif
            return 0ul;
        }

        static string ResolvePersonaName(ulong steamId)
        {
#if !DISABLESTEAMWORKS
            if (!SteamManager.Initialized || steamId == 0ul)
                return null;

            return steamId == SteamManager.LocalSteamId
                ? SteamManager.LocalPersonaName
                : Steamworks.SteamFriends.GetFriendPersonaName(new Steamworks.CSteamID(steamId));
#else
            return null;
#endif
        }

        void FinishLoadingGame()
        {
            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.LoadingDone);
            GameSettings.Instance.GameState = GlobalGameState.InGame;
        }

        public static void SetGameConnection(GameConnection gameConnection)
        {
            GameConnection = gameConnection;
        }
    }
}
