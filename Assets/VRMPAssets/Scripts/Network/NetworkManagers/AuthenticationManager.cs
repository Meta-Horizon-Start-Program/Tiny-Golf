using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

#if UNITY_EDITOR

// Unity 6 Only
#if HAS_MPPM
using Unity.Multiplayer.Playmode;
using UnityEngine.XR.Interaction.Toolkit.UI;
#endif

#if HAS_PARRELSYNC
using ParrelSync;
#endif

#endif

namespace XRMultiplayer
{
    public class AuthenticationManager : MonoBehaviour
    {
        const string k_DebugPrepend = "<color=#938FFF>[Authentication Manager]</color> ";

        /// <summary>
        /// The argument ID to search for in the command line args.
        /// </summary>
        const string k_playerArgID = "PlayerArg";

        /// <summary>
        /// Determines if the AuthenticationManager should use command line args to determine the player ID when launching a build.
        /// </summary>
        [SerializeField] bool m_UseCommandLineArgs = true;


        /// <summary>
        /// Simple Authentication function. This uses bare bones authentication and anonymous sign in.
        /// </summary>
        /// <returns></returns>
        public virtual async Task<bool> Authenticate()
        {
            // Check if UGS has not been initialized yet, and initialize.
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                var options = new InitializationOptions();
                string playerId = "Player";
                // Check for editor clones (MPPM or ParrelSync).
                // This allows for multiple instances of the editor to connect to UGS.
#if UNITY_EDITOR
                playerId = "Editor";

#if HAS_MPPM
                //Check for MPPM
                playerId += CheckMPPM();
#elif HAS_PARRELSYNC
                // Check for ParrelSync
                playerId += CheckParrelSync();
#endif
#endif
                // Check for command line args in builds
                if (!Application.isEditor && m_UseCommandLineArgs)
                {
                    playerId += GetPlayerIDArg();
                }

                options.SetProfile(playerId);
                Utils.Log($"{k_DebugPrepend}Signing in with profile {playerId}");

                // Initialize UGS using any options defined
                await UnityServices.InitializeAsync(options);
            }

            // If not already signed on then do so.
            if (!AuthenticationService.Instance.IsAuthorized)
            {
                // Signing in anonymously for simplicity sake.
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            // Cache PlayerId.
            XRINetworkGameManager.AuthenicationId = AuthenticationService.Instance.PlayerId;
            return UnityServices.State == ServicesInitializationState.Initialized;
        }

        public static bool IsAuthenticated()
        {
            try
            {
                return AuthenticationService.Instance.IsSignedIn;
            }
            catch (System.Exception e)
            {
                Utils.Log($"{k_DebugPrepend}Checking for AuthenticationService.Instance before initialized.{e}");
                return false;
            }
        }

        string GetPlayerIDArg()
        {
            string playerID = "";
            string[] args = System.Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                arg.ToLower();
                if (arg.ToLower().Contains(k_playerArgID.ToLower()))
                {
                    var splitArgs = arg.Split(':');
                    if (splitArgs.Length > 0)
                    {
                        playerID += splitArgs[1];
                    }
                }
            }
            return playerID;
        }

#if UNITY_EDITOR
#if HAS_MPPM
        string CheckMPPM()
        {
            Utils.Log($"{k_DebugPrepend}MPPM Found");
            string mppmString = "";
            if(CurrentPlayer.ReadOnlyTags().Length > 0)
            {
                mppmString += CurrentPlayer.ReadOnlyTags()[0];

                // Force input module to disable mouse and touch input to suppress MPPM startup errors.
                var inputModule = FindFirstObjectByType<XRUIInputModule>();
                inputModule.enableMouseInput = false;
                inputModule.enableTouchInput = false;
            }

            return mppmString;
        }
#endif

#if HAS_PARRELSYNC
        string CheckParrelSync()
        {
            Utils.Log($"{k_DebugPrepend}ParrelSync Found");
            string pSyncString = "";
            if (ClonesManager.IsClone()) pSyncString += ClonesManager.GetArgument();
            return pSyncString;
        }
#endif
#endif
    }
}
