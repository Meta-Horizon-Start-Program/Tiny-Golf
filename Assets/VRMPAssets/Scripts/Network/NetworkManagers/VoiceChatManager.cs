using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using Unity.Services.Vivox;
using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine;
using UnityEngine.Android;
using System.Collections;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XRMultiplayer
{
    /// <summary>
    /// Manages the Vivox Voice Chat functionality in the VR Multiplayer template.
    /// </summary>
    public class VoiceChatManager : MonoBehaviour
    {
        /// <summary>
        /// String used to notify the player that they need to enable microphone permissions.
        /// </summary>
        const string k_MicrophonePersmissionDialogue = "Microphone Permissions Required.";

        /// <summary>
        public static BindableVariable<bool> s_HasMicrophonePermission = new(false);
        /// <summary>
        /// Dictionary of all the <see cref="XRINetworkPlayer"/>'s in the voice chat.
        /// </summary>
        public static Dictionary<string, XRINetworkPlayer> m_PlayersDictionary = new();

        /// <summary>
        /// This is the bindable variable for subscribing to the local player muting themselves.
        /// </summary>
        public IReadOnlyBindableVariable<bool> selfMuted
        {
            get => m_SelfMuted;
        }
        readonly BindableVariable<bool> m_SelfMuted = new(false);

        /// <summary>
        /// This is the bindable variable for subscribing to the connection status of the voice chat service.
        /// </summary>
        public IReadOnlyBindableVariable<string> connectionStatus
        {
            get => m_ConnectionStatus;
        }
        readonly BindableVariable<string> m_ConnectionStatus = new();

        /// <summary>
        /// The chat capability of the channel, by default it should Audio Only.
        /// </summary>
        [SerializeField, Tooltip("The chat capability of the channel, by default it should Audio Only")] ChatCapability m_ChatCapability = ChatCapability.AudioOnly;

        /// <summary>
        /// Update frequency for audio callbacks.
        /// </summary>
        [SerializeField, Tooltip("Update frequency for audio callbacks")] ParticipantPropertyUpdateFrequency m_UpdateFrequency = ParticipantPropertyUpdateFrequency.TenPerSecond;

        /// <summary>
        /// The maximum distance from the listener that a speaker can be heard.
        /// </summary>
        public int AudibleDistance
        {
            get => m_AudibleDistance;
            set => m_AudibleDistance = value;
        }
        [Header("Voice Chat Properties")]
        [SerializeField, Tooltip("The maximum distance from the listener that a speaker can be heard.")]
        int m_AudibleDistance = 32;

        /// <summary>
        /// The distance from the listener within which a speaker’s voice is heard at its original volume, and beyond which the speaker's voice begins to fade.
        /// </summary>
        public int ConversationalDistance
        {
            get => m_ConversationalDistance;
            set => m_ConversationalDistance = value;
        }
        [SerializeField, Tooltip("The distance from the listener within which a speaker’s voice is heard at its original volume, and beyond which the speaker's voice begins to fade.")]
        int m_ConversationalDistance = 7;

        /// <summary>
        /// The strength of the audio fade effect as the speaker moves away from the listener past the conversational distance.
        /// </summary>
        public float AudioFadeIntensity
        {
            get => m_AudioFadeIntensity;
            set => m_AudioFadeIntensity = value;
        }
        [SerializeField, Tooltip("The strength of the audio fade effect as the speaker moves away from the listener past the conversational distance.")]
        float m_AudioFadeIntensity = 1.0f;

        /// <summary>
        /// The model that determines the distance falloff of the voice chat.
        /// </summary>
        /// The strength of the audio fade effect as the speaker moves away from the listener past the conversational distance.
        /// </summary>
        public AudioFadeModel AudioFadeModel
        {
            get => m_AudioFadeModel;
            set => m_AudioFadeModel = value;
        }
        [SerializeField, Tooltip("The model that determines the distance falloff of the voice chat.")]
        AudioFadeModel m_AudioFadeModel = AudioFadeModel.LinearByDistance;

        /// <summary>
        /// The minimum and maximum volume for the voice output.
        /// </summary>
        [SerializeField, Tooltip("The minimum and maximum volume for the voice output.")] Vector2 m_MinMaxVoiceOutputVolume = new Vector2(-10.0f, 10.0f);

        /// <summary>
        /// The minimum and maximum volume for the voice input.
        /// </summary>
        [SerializeField, Tooltip("The minimum and maximum volume for the voice input.")] Vector2 m_MinMaxVoiceInputVolume = new Vector2(-10.0f, 10.0f);

        /// <summary>
        /// The local participant in the voice chat.
        /// </summary>
        VivoxParticipant m_LocalParticpant;

        /// <summary>
        /// The current lobby id the player is connected to.
        /// </summary>
        string m_CurrentLobbyId;

        /// <summary>
        /// If the player is connected to a room.
        /// </summary>
        bool m_ConnectedToRoom;

        /// <summary>
        /// If the voice chat service is initialized.
        /// </summary>
        bool m_IsInitialized;
        const string k_DebugPrepend = "<color=#0CFAFA>[Voice Chat Manager]</color> ";
        ///<inheritdoc/>
        private void Awake()
        {
            m_ConnectedToRoom = false;

            XRINetworkGameManager.CurrentConnectionState.Subscribe(ConnectionStateUpdated);
            XRINetworkGameManager.Connected.Subscribe(ConnectedToGame);
        }

        ///<inheritdoc/>
        private void OnDestroy()
        {
            if (VivoxService.Instance != null)
            {
                VivoxService.Instance.LoggedIn -= LocalUserLoggedIn;
                UnbindParticipantEvents();
            }
        }

        /// <summary>
        /// Callback for when the local player connection state is updated.
        /// </summary>
        /// <param name="connected">Wether or not a player is connected.</param>
        void ConnectedToGame(bool connected)
        {
            if (!m_IsInitialized) return;

            if (connected)
            {
                Login(XRINetworkGameManager.AuthenicationId, XRINetworkGameManager.Instance.lobbyManager.connectedLobby.Id);
            }
            else
            {
                LogOut();
            }
        }

        void ConnectionStateUpdated(XRINetworkGameManager.ConnectionState connectionState)
        {
            if (!m_IsInitialized && connectionState == XRINetworkGameManager.ConnectionState.Authenticated)
            {
                Utils.Log($"{k_DebugPrepend}Initializing Voice Chat");
                m_ConnectionStatus.Value = "Initializing Voice Service";
                m_IsInitialized = true;
                EnableVoiceChat();
                if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
                {
                    StartCoroutine(ShowPermissionsAfterDelay());
                }
                else
                {
                    MicrophonePermissionGranted();
                }
            }
        }

        IEnumerator ShowPermissionsAfterDelay(float delay = 1.0f)
        {
            Utils.Log($"{k_DebugPrepend}Requesting Microphone Permissions");
            PlayerHudNotification.Instance.ShowText("Requesting Microphone Permissions", 3.0f);
            yield return new WaitForSeconds(delay);
            PermissionCallbacks permissionCallbacks = new();
            permissionCallbacks.PermissionDenied += PermissionDeniedCallback;
            permissionCallbacks.PermissionGranted += PermissionGrantedCallback;
            Permission.RequestUserPermission(Permission.Microphone, permissionCallbacks);
        }

        void PermissionGrantedCallback(string permissionName)
        {
            if (permissionName == Permission.Microphone)
            {
                MicrophonePermissionGranted();
            }
        }

        void PermissionDeniedCallback(string permissionName)
        {
            if (permissionName == Permission.Microphone)
            {
                PlayerHudNotification.Instance.ShowText("Microphone Permissions Denied", 3.0f);
            }
        }

        void MicrophonePermissionGranted()
        {
            Utils.Log($"{k_DebugPrepend}Microphone Permissions Granted");
            s_HasMicrophonePermission.Value = true;
            PlayerHudNotification.Instance.ShowText("Microphone Permissions Granted", 3.0f);
        }

        public async void EnableVoiceChat()
        {
            try
            {
                await VivoxService.Instance.InitializeAsync();
                m_ConnectionStatus.Value = "Voice Service Initialized";
                VivoxService.Instance.LoggedIn += LocalUserLoggedIn;
                BindToParticipantEvents();
            }
            catch (System.Exception e)
            {
#if UNITY_EDITOR
                EditorGUI.hyperLinkClicked += HyperlinkClicked;
                Utils.Log($"{k_DebugPrepend}Vivox Initialization Failed. Please check the Vivox Service Window <a data=\"OpenVivoxSettings\"><b>Project Settings > Services > Vivox</b></a>\n\n{e}", 2);
#else
                Utils.Log($"{k_DebugPrepend}Vivox Initialization Failed.\n\n{e}", 2);
#endif
            }
        }

#if UNITY_EDITOR
        void HyperlinkClicked(EditorWindow window, HyperLinkClickedEventArgs args)
        {
            if(args.hyperLinkData.ContainsValue("OpenVivoxSettings"))
            {
                SettingsService.OpenProjectSettings("Project/Services/Vivox");
            }
        }
#endif


        public async void Login(string displayName, string roomCode)
        {
            m_CurrentLobbyId = roomCode;

            LoginOptions loginOptions = new()
            {
                DisplayName = displayName,
                ParticipantUpdateFrequency = m_UpdateFrequency
            };

            if (VivoxService.Instance.IsLoggedIn)
            {
                Utils.Log($"{k_DebugPrepend}Logging out of Voice Chat");
                m_ConnectionStatus.Value = "Logging out of Voice Chat";
                await VivoxService.Instance.LogoutAsync();
            }

            if (!VivoxService.Instance.IsLoggedIn)
            {
                Utils.Log($"{k_DebugPrepend}Logging In to room {roomCode} as {displayName}");
                m_ConnectionStatus.Value = "Logging In To Voice Service";
                await VivoxService.Instance.LoginAsync(loginOptions);
            }
            else
            {
                Utils.Log($"{k_DebugPrepend}Attempting to login to voice chat while already logged in.", 1);
            }
        }

        void LocalUserLoggedIn()
        {
            if (VivoxService.Instance.IsLoggedIn)
            {
                Utils.Log($"{k_DebugPrepend}Local User Logged In to Voice Chat.");
                m_ConnectionStatus.Value = "Joining Voice Channel";
                ConnectToVoiceChannel();
            }
        }

        public async void ConnectToVoiceChannel()
        {
            if (NetworkManager.Singleton.IsConnectedClient & !m_ConnectedToRoom)
            {
                Channel3DProperties properties = new(AudibleDistance, ConversationalDistance, AudioFadeIntensity, AudioFadeModel);
                Utils.Log($"{k_DebugPrepend}Joining Voice Channel: {m_CurrentLobbyId}, properties: {properties}");
                await VivoxService.Instance.JoinPositionalChannelAsync(m_CurrentLobbyId, m_ChatCapability, properties);

                // Once connecting, make sure we are still in the game session, if not, disconnect from the voice chat.
                if (!NetworkManager.Singleton.IsConnectedClient)
                {
                    Disconnect();
                }
            }
            else
            {
                Utils.Log($"{k_DebugPrepend}Failed to join Voice Chat, Player is not connected to a game", 1);
            }
        }

        void BindToParticipantEvents()
        {
            VivoxService.Instance.ParticipantAddedToChannel += OnParticipantAdded;
            VivoxService.Instance.ParticipantRemovedFromChannel += OnParticipantRemoved;
        }

        void UnbindParticipantEvents()
        {
            VivoxService.Instance.ParticipantAddedToChannel -= OnParticipantAdded;
            VivoxService.Instance.ParticipantRemovedFromChannel -= OnParticipantRemoved;
        }

        async void DisconnectAsync()
        {
            m_ConnectionStatus.Value = "Leaving current channel";
            await VivoxService.Instance.LeaveAllChannelsAsync();
        }

        [ContextMenu("Reconnect")]
        public void Reconnect()
        {
            ReconnectAsync();
        }

        async void ReconnectAsync()
        {
            m_ConnectionStatus.Value = "Leaving current channel";
            await VivoxService.Instance.LeaveAllChannelsAsync();

            if (VivoxService.Instance.IsLoggedIn)
            {
                ConnectToVoiceChannel();
            }
            else
            {
                m_ConnectionStatus.Value = "Reconnecting to Voice Chat";
                Login(XRINetworkGameManager.AuthenicationId, XRINetworkGameManager.Instance.lobbyManager.connectedLobby.Id);
            }
        }

        public void LogOut()
        {
            Utils.Log($"{k_DebugPrepend}Logging out of Voice Chat.");
            if (VivoxService.Instance.IsLoggedIn && m_ConnectedToRoom)
            {
                m_ConnectedToRoom = false;
                VivoxService.Instance.LeaveAllChannelsAsync();
                VivoxService.Instance.LogoutAsync();
            }

            m_PlayersDictionary.Clear();
        }

        public void Set3DAudio(Transform localPlayerHeadTransform)
        {
            if (VivoxService.Instance.IsLoggedIn && VivoxService.Instance.ActiveChannels.Count > 0 && VivoxService.Instance.TransmittingChannels[0] == m_CurrentLobbyId)
            {
                VivoxService.Instance.Set3DPosition(localPlayerHeadTransform.position,
                    localPlayerHeadTransform.position,
                    localPlayerHeadTransform.forward,
                    localPlayerHeadTransform.up,
                    m_CurrentLobbyId);
            }
        }


        public void ToggleSelfMute(bool setManual = false, bool mutedOverrideValue = false)
        {
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                m_SelfMuted.Value = setManual ? mutedOverrideValue : !m_SelfMuted.Value;
            }
            else
            {
                m_SelfMuted.Value = false;
            }

            if (VivoxService.Instance.IsLoggedIn)
            {
                if (m_SelfMuted.Value)
                {
                    VivoxService.Instance.MuteInputDevice();
                }
                else
                {
                    VivoxService.Instance.UnmuteInputDevice();
                }
            }
            else
            {
                OfflinePlayerAvatar.muted = m_SelfMuted.Value;
            }

            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                PlayerHudNotification.Instance.ShowText(k_MicrophonePersmissionDialogue, 3.0f);
            }
        }

        public void SetInputVolume(float volume)
        {
            volume = Mathf.Clamp(volume, m_MinMaxVoiceInputVolume.x, m_MinMaxVoiceInputVolume.y);

            VivoxService.Instance.SetInputDeviceVolume((int)volume);

            // Since the slider goes to .001 percent, add a buffer to mute the mic
            if (volume <= (m_MinMaxVoiceInputVolume.x + .05f))
            {
                ToggleSelfMute(true, true);
            }
            else
            {
                ToggleSelfMute(true, false);
            }
        }

        public void SetOutputVolume(float volume)
        {
            volume = Mathf.Clamp(volume, m_MinMaxVoiceOutputVolume.x, m_MinMaxVoiceOutputVolume.y);
            VivoxService.Instance.SetOutputDeviceVolume((int)volume);
        }

        void OnParticipantAdded(VivoxParticipant participant)
        {
            if (participant.IsSelf)
            {
                m_ConnectedToRoom = true;
                m_LocalParticpant = participant;
                m_SelfMuted.Value = false;
                XRINetworkPlayer.LocalPlayer.SetVoiceId(m_LocalParticpant.PlayerId);
                Utils.Log($"{k_DebugPrepend}Joined Voice Channel: {m_CurrentLobbyId}");
                m_ConnectionStatus.Value = "Joined Voice Channel";
                PlayerHudNotification.Instance.ShowText("Joined Voice Chat", 3.0f);
            }
            else
            {
                Utils.Log($"{k_DebugPrepend}Non-Local Player Joined Voice Channel: {participant.PlayerId}");
                foreach (XRINetworkPlayer player in FindObjectsByType<XRINetworkPlayer>(FindObjectsSortMode.None))
                {
                    if (player.playerVoiceId == participant.PlayerId)
                    {
                        player.SetupVoicePlayer();
                    }
                }
            }
        }

        void OnParticipantRemoved(VivoxParticipant participant)
        {
            RemoveVivoxPlayer(participant.PlayerId);
            if (participant.IsSelf)
            {
                Utils.Log($"{k_DebugPrepend}Left Voice Channel: {m_CurrentLobbyId}");
                m_ConnectionStatus.Value = "Left Voice Channel";
                m_ConnectedToRoom = false;
                m_LocalParticpant = null;
                PlayerHudNotification.Instance.ShowText("Voice Chat Disconnected", 3.0f);
            }
        }

        public VivoxParticipant GetVivoxParticipantById(string participantPlayerId)
        {
            foreach (var participant in VivoxService.Instance.ActiveChannels[m_CurrentLobbyId])
            {
                if (participantPlayerId == participant.PlayerId)
                    return participant;
            }
            return null;
        }

        // Gets called as soon as participant ID is synced
        public static void AddNewVivoxPlayer(string participantID, XRINetworkPlayer networkPlayer)
        {
            if (!m_PlayersDictionary.ContainsKey(participantID))
            {
                m_PlayersDictionary.Add(participantID, networkPlayer);
            }
            else
            {
                Utils.Log($"{k_DebugPrepend}Attempting to load multiple players with same id {participantID}", 1);
            }
        }

        public static void RemoveVivoxPlayer(string participantID)
        {
            if (participantID == XRINetworkPlayer.LocalPlayer.playerVoiceId)
            {
                Utils.Log($"{k_DebugPrepend}Local Player Left Voice Chat.");
                return;
            }
            if (m_PlayersDictionary.ContainsKey(participantID))
            {
                m_PlayersDictionary.Remove(participantID);
            }
        }

        [ContextMenu("Disconnect")]
        public void Disconnect()
        {
            DisconnectAsync();
        }

        [ContextMenu("Debug Particpants")]
        void DebugParticipants()
        {
            StringBuilder output = new StringBuilder();
            output.Append($"[Room Type: Positional\n[Room Code: {m_CurrentLobbyId}]");
            foreach (var participant in VivoxService.Instance.ActiveChannels[m_CurrentLobbyId])
            {
                output.Append($"\n[ParticipantID: {participant.PlayerId}]\n[AudioEnergy: {participant.AudioEnergy}]");
            }
            Utils.Log($"{k_DebugPrepend}{output}");
        }
    }
}
