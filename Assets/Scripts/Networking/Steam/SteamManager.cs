#if !DISABLESTEAMWORKS
using System;
using Steamworks;
#endif
using UnityEngine;

namespace Unity.MP_FPS
{
    /// <summary>
    /// Owns the Steam API lifecycle: initialise once, pump callbacks every frame, shut down on quit.
    /// </summary>
    /// <remarks>
    /// Steamworks.NET's UPM package does <b>not</b> ship the <c>SteamManager.cs</c> that comes with the
    /// standalone <c>.unitypackage</c>, so this is written from scratch against the same contract.
    /// <para>
    /// Nothing in the game may touch a <c>Steam*</c> API before <see cref="Initialized"/> is true.
    /// Every failure path here is designed to leave <see cref="Initialized"/> false and
    /// <see cref="FailureReason"/> populated with something a player can read, rather than throwing —
    /// "Steam client absent or logged out produces a readable message rather than an exception or hang"
    /// is an acceptance criterion of the migration.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class SteamManager : MonoBehaviour
    {
        /// <summary>
        /// Spacewar. Valve's shared test app id. P2P and lobbies work on it, but lobby *listing* is
        /// useless because every developer testing Steamworks shares it. Invite-based joining — the
        /// only flow this game uses — is unaffected.
        /// </summary>
        public const uint SpacewarAppId = 480;

        static SteamManager s_Instance;
        public static SteamManager Instance => s_Instance;

        /// <summary>True only when the Steam API is up and safe to call.</summary>
        public static bool Initialized { get; private set; }

        /// <summary>Player-readable reason initialisation failed, or null if it did not.</summary>
        public static string FailureReason { get; private set; }

        /// <summary>The local player's SteamID64. Zero when Steam is unavailable.</summary>
        public static ulong LocalSteamId =>
#if !DISABLESTEAMWORKS
            Initialized ? SteamUser.GetSteamID().m_SteamID : 0ul;
#else
            0ul;
#endif

        /// <summary>The local player's Steam persona name, or a placeholder when Steam is unavailable.</summary>
        public static string LocalPersonaName =>
#if !DISABLESTEAMWORKS
            Initialized ? SteamFriends.GetPersonaName() : "Offline Intern";
#else
            "Offline Intern";
#endif

#if !DISABLESTEAMWORKS
        SteamAPIWarningMessageHook_t m_WarningHook;

        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Debug.LogError($"Multiple instances of '{nameof(SteamManager)}' violates the Singleton pattern!", this);
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            DontDestroyOnLoad(gameObject);

            if (Initialized)
                return;

            // Packsize/DllCheck catch the two failure modes that otherwise present as an
            // unexplained native crash later: a mismatched Steamworks SDK and a stale/32-bit
            // steam_api DLL. Both are developer errors, so they log loudly and abort init.
            if (!Packsize.Test())
            {
                Fail("Steamworks.NET packsize test failed. The wrong version of Steamworks.NET is being run in this platform.");
                return;
            }

            if (!DllCheck.Test())
            {
                Fail("Steamworks.NET DllCheck test failed. One or more of the Steamworks binaries seems to be the wrong version.");
                return;
            }

            try
            {
                // InitEx over Init: it hands back a diagnosable reason instead of a bare false.
                var result = SteamAPI.InitEx(out var steamErrMsg);
                if (result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
                {
                    Fail(DescribeInitFailure(result, steamErrMsg));
                    return;
                }
            }
            catch (DllNotFoundException e)
            {
                Fail("Could not load [lib]steam_api.dll/so/dylib. It must be in the output folder alongside the executable.");
                Debug.LogException(e, this);
                return;
            }

            Initialized = true;
            FailureReason = null;

            m_WarningHook = SteamAPIDebugTextHook;
            SteamClient.SetWarningMessageHook(m_WarningHook);

            Debug.Log($"[{nameof(SteamManager)}] Steam initialised as '{LocalPersonaName}' ({LocalSteamId}).");
        }

        /// <summary>
        /// Turns an <see cref="ESteamAPIInitResult"/> into something worth showing a player.
        /// Steam's own message is appended because it is occasionally more specific than the enum.
        /// </summary>
        static string DescribeInitFailure(ESteamAPIInitResult result, string steamErrMsg)
        {
            var reason = result switch
            {
                ESteamAPIInitResult.k_ESteamAPIInitResult_NoSteamClient =>
                    "Steam is not running. Start Steam and log in, then launch the game again.",
                ESteamAPIInitResult.k_ESteamAPIInitResult_VersionMismatch =>
                    "The Steam client is out of date. Update Steam and try again.",
                ESteamAPIInitResult.k_ESteamAPIInitResult_FailedGeneric =>
                    "Steam could not be reached. Make sure Steam is running and you are logged in.",
                _ => "Steam could not be initialised.",
            };

            return string.IsNullOrWhiteSpace(steamErrMsg) ? reason : $"{reason} ({steamErrMsg.Trim()})";
        }

        static void Fail(string reason)
        {
            Initialized = false;
            FailureReason = reason;
            Debug.LogError($"[{nameof(SteamManager)}] {reason}");
        }

        /// <summary>
        /// Steam requires callbacks to be pumped from the main thread. Everything asynchronous in the
        /// Steam layer — lobby creation, joins, invites, and the transport's own connection-status
        /// callback — is delivered from here, so if this stops running, networking silently stalls.
        /// </summary>
        void Update()
        {
            if (Initialized)
                SteamAPI.RunCallbacks();
        }

        void OnDestroy()
        {
            if (s_Instance != this)
                return;

            s_Instance = null;

            if (!Initialized)
                return;

            Initialized = false;
            SteamAPI.Shutdown();
        }

        [AOT.MonoPInvokeCallback(typeof(SteamAPIWarningMessageHook_t))]
        static void SteamAPIDebugTextHook(int severity, System.Text.StringBuilder debugText)
        {
            Debug.LogWarning($"[Steam] {debugText}");
        }
#else
        void Awake()
        {
            Fail("This build was compiled with DISABLESTEAMWORKS; Steam networking is unavailable.");
        }

        static void Fail(string reason)
        {
            Initialized = false;
            FailureReason = reason;
            Debug.LogError($"[{nameof(SteamManager)}] {reason}");
        }
#endif
    }
}
