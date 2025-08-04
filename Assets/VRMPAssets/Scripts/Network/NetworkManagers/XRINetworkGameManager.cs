using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine;
using Unity.Services.Lobbies;
using UnityEditor;

namespace XRMultiplayer
{
#if USE_FORCED_BYTE_SERIALIZATION
    /// <summary>
    /// Workaround for a bug introduced in NGO 1.9.1.
    /// </summary>
    /// <remarks> Delete this class once the bug is fixed in NGO.</remarks>
    class ForceByteSerialization : NetworkBehaviour
    {
        NetworkVariable<byte> m_ForceByteSerialization;
    }
#endif

    /// <summary>
    /// Manages the high level connection for a networked game session.
    /// </summary>
    [RequireComponent(typeof(LobbyManager)), RequireComponent(typeof(AuthenticationManager))]
    public class XRINetworkGameManager : NetworkBehaviour
    {
        /// <summary>
        /// Determines the current state of the networked game connection.
        /// </summary>
        ///<remarks>
        /// None: No connection state.
        /// Authenticating: Currently authenticating.
        /// Authenticated: Authenticated.
        /// Connecting: Currently connecting to a lobby.
        /// Connected: Connected to a lobby.
        /// </remarks>
        public enum ConnectionState
        {
            None,
            Authenticating,
            Authenticated,
            Connecting,
            Connected
        }

        /// <summary>
        /// Max amount of players allowed when creating a new room.
        /// </summary>
        public const int maxPlayers = 20;

        /// <summary>
        /// Singleton Reference for access to this manager.
        /// </summary>
        public static XRINetworkGameManager Instance => s_Instance;
        static XRINetworkGameManager s_Instance;

        /// <summary>
        /// OwnerClientId that gets set for the local player when connecting to a game.
        /// </summary>
        public static ulong LocalId;

        /// <summary>
        /// Authentication Id that gets passed once Authenticated.
        /// </summary>
        public static string AuthenicationId;

        /// <summary>
        /// Internal Room Code set by Lobby.
        /// </summary>
        public static string ConnectedRoomCode;

        /// <summary>
        /// Current connected region set by Lobby and Relay.
        /// </summary>
        public static string ConnectedRoomRegion;

        /// <summary>
        /// Bindable Variable that gets updated when changing the the currently connected room.
        /// </summary>
        public static BindableVariable<string> ConnectedRoomName = new("");

        /// <summary>
        /// Bindable Variable that gets updated when the local player changes name.
        /// </summary>
        public static BindableVariable<string> LocalPlayerName = new("Player");

        /// <summary>
        /// Bindable Variable that gets updated when the local player changes color.
        /// </summary>
        public static BindableVariable<Color> LocalPlayerColor = new(Color.white);

        /// <summary>
        /// Bindable Variable that gets updated when a player connects or disconnects from a networked game.
        /// </summary>
        public static IReadOnlyBindableVariable<bool> Connected
        {
            get => m_Connected;
        }
        static BindableVariable<bool> m_Connected = new BindableVariable<bool>(false);

        /// <summary>
        /// Bindable Variable that gets updated throughout the authentication and connection process.
        /// See <see cref="ConnectionState"/>
        /// </summary>
        public static IReadOnlyBindableVariable<ConnectionState> CurrentConnectionState
        {
            get => m_ConnectionState;
        }
        static BindableEnum<ConnectionState> m_ConnectionState = new BindableEnum<ConnectionState>(ConnectionState.None);

        /// <summary>
        /// Auto connects to the player to a networked game session once they connect to a lobby.
        /// Uncheck if you want to handle joining a networked session separately.
        /// </summary>
        public bool autoConnectOnLobbyJoin { get => m_AutoConnectOnLobbyJoin; }
        [SerializeField] bool m_AutoConnectOnLobbyJoin = true;

        /// <summary>
        /// Flag for updating positional voice chat.
        /// </summary>
        /// <remarks>
        /// This will be removed in the future with the Vivox v16 update.
        /// </remarks>
        public bool positionalVoiceChat = false;

        /// <summary>
        /// Action for when a player connects or disconnects.
        /// </summary>
        public Action<ulong, bool> playerStateChanged;

        /// <summary>
        /// Action for when connection status is updated.
        /// </summary>
        public Action<string> connectionUpdated;

        /// <summary>
        /// Action for when connection fails.
        /// </summary>
        public Action<string> connectionFailedAction;

        /// <summary>
        /// Lobby Manager handles the Lobby and Relay work between players.
        /// </summary>
        public LobbyManager lobbyManager => m_LobbyManager;
        LobbyManager m_LobbyManager;

        /// <summary>
        /// Lobby Manager handles the Lobby and Relay work between players.
        /// </summary>
        public AuthenticationManager authenticationManager => m_AuthenticationManager;
        AuthenticationManager m_AuthenticationManager;

        /// <summary>
        /// List that handles all current players by ID.
        /// Useful for getting specific players.
        /// See <see cref="GetPlayerByID"/>
        /// </summary>
        readonly List<ulong> m_CurrentPlayerIDs = new();

        /// <summary>
        /// Flagged whenever the application is in the process of shutting down.
        /// </summary>
        bool m_IsShuttingDown = false;

        const string k_DebugPrepend = "<color=#FAC00C>[Network Game Manager]</color> ";

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual async void Awake()
        {
            // Check for existing singleton reference. If once already exists early out.
            if (s_Instance != null)
            {
                Utils.Log($"{k_DebugPrepend}Duplicate XRINetworkGameManager found, destroying.", 2);
                Destroy(gameObject);
                return;
            }
            s_Instance = this;

            // Check for Lobby Manager, if none exist, early out.
            if (TryGetComponent(out m_LobbyManager) && TryGetComponent(out m_AuthenticationManager))
            {
                m_LobbyManager.OnLobbyFailed += ConnectionFailed;
            }
            else
            {
                Utils.Log($"{k_DebugPrepend}Missing Managers, Disabling Component", 2);
                enabled = false;
                return;
            }

#if UNITY_EDITOR
            if(!CloudProjectSettings.projectBound)
            {
                Utils.Log($"{k_DebugPrepend}Project has not been linked to Unity Cloud." +
                               "\nThe VR Multiplayer Template utilizes Unity Gaming Services and must be linked to Unity Cloud." +
                               "\nGo to <b>Settings -> Project Settings -> Services</b> and link your project.", 2);
                return;
            }
#endif

            // Initialize bindable variables.
            m_Connected.Value = false;
            // Update connection state.
            m_ConnectionState.Value = ConnectionState.Authenticating;

            // Wait for Authentication to complete.
            bool signedIn = await Authenticate();
            if (!signedIn)
            {
                Utils.Log($"{k_DebugPrepend}Failed to Authenticate.", 1);
                ConnectionFailed("Failed to Authenticate.");
                PlayerHudNotification.Instance.ShowText($"Failed to Authenticate.");
            }
            else
            {
                // Update connection state.
                m_ConnectionState.Value = ConnectionState.Authenticated;
            }
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void Start()
        {
            NetworkManager.Singleton.OnClientStopped += OnLocalClientStopped;
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        public override void OnDestroy()
        {
            base.OnDestroy();

            ShutDown();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        private void OnApplicationQuit()
        {
            ShutDown();
        }

        async void ShutDown()
        {
            if (m_IsShuttingDown) return;
            m_IsShuttingDown = true;

            // Remove callbacks
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientStopped -= OnLocalClientStopped;
            }

            // Shutdown lobby if owner, remove from lobby if not owner.
            await m_LobbyManager.RemovePlayerFromLobby(AuthenicationId);
        }

        public async Task<bool> Authenticate()
        {
            return await m_AuthenticationManager.Authenticate();
        }
        public bool IsAuthenticated()
        {
            return AuthenticationManager.IsAuthenticated();
        }
        /// <summary>
        /// Called from XRINetworkPlayer once they have spawned.
        /// </summary>
        /// <param name="localPlayerId">Sets based on <see cref="NetworkObject.OwnerClientId"/> from the local player</param>
        public virtual void LocalPlayerConnected(ulong localPlayerId)
        {
            m_Connected.Value = true;
            LocalId = localPlayerId;
            PlayerHudNotification.Instance.ShowText($"<b>Status:</b> Connected");
        }

        /// <summary>
        /// Called when disconnected from any networked game.
        /// </summary>
        /// <param name="id">
        /// Local player id.
        /// </param>
        protected virtual void OnLocalClientStopped(bool id)
        {
            m_Connected.Value = false;
            m_CurrentPlayerIDs.Clear();
            PlayerHudNotification.Instance.ShowText($"<b>Status:</b> Disconnected");
            // Check if authenticated on disconnect.
            if (IsAuthenticated())
            {
                m_ConnectionState.Value = ConnectionState.Authenticated;
            }
            else
            {
                m_ConnectionState.Value = ConnectionState.None;
            }
        }

        /// <summary>
        /// Finds all <see cref="XRINetworkPlayer"/>'s existing in the scene and gets the <see cref="XRINetworkPlayer"/>
        /// based on <see cref="NetworkObject.OwnerClientId"/> for that player.
        /// </summary>
        /// <param name="id">
        /// <see cref="NetworkObject.OwnerClientId"/> of the player.
        /// </param>
        /// <param name="player">
        /// Out <see cref="XRINetworkPlayer"/>.
        /// </param>
        /// <returns>
        /// Returns true based on whether or not a player with that Id exists.
        /// </returns>
        public virtual bool GetPlayerByID(ulong id, out XRINetworkPlayer player)
        {
            // Find all existing players in scene. This is a workaround until NGO exposes client side player list (2.x I believe - JG).
            XRINetworkPlayer[] allPlayers = FindObjectsByType<XRINetworkPlayer>(FindObjectsSortMode.None);

            //Loops through existing players and returns true if player with id is found.
            foreach (XRINetworkPlayer p in allPlayers)
            {
                if (p.NetworkObject.OwnerClientId == id)
                {
                    player = p;
                    return true;
                }
            }
            player = null;
            return false;
        }

        [ContextMenu("Show All NetworkClients")]
        void ShowAllNetworkClients()
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                Debug.Log($"Client: {client.Key}, {client.Value.PlayerObject.name}");
            }
        }

        /// <summary>
        /// This function will set the player ID in the list <see cref="m_CurrentPlayerIDs"/> and
        /// invokes the callback <see cref="playerStateChanged"/>.
        /// </summary>
        /// <param name="playerID"><see cref="NetworkObject.OwnerClientId"/> of the joined player.</param>
        /// <remarks>Called from <see cref="XRINetworkPlayer.CompleteSetup"/>.</remarks>
        public virtual void PlayerJoined(ulong playerID)
        {
            // If playerID is not already registered, then add.
            if (!m_CurrentPlayerIDs.Contains(playerID))
            {
                m_CurrentPlayerIDs.Add(playerID);
                playerStateChanged?.Invoke(playerID, true);
            }
            else
            {
                Utils.Log($"{k_DebugPrepend}Trying to Add a player ID [{playerID}] that already exists", 1);
            }
        }

        /// <summary>
        /// Called from <see cref="XRINetworkPlayer.OnDestroy"/>.
        /// </summary>
        /// <param name="playerID"><see cref="NetworkObject.OwnerClientId"/> of the player who left.</param>
        public virtual void PlayerLeft(ulong playerID)
        {
            // Check to make sure player has been registerd.
            if (m_CurrentPlayerIDs.Contains(playerID))
            {
                m_CurrentPlayerIDs.Remove(playerID);
                playerStateChanged?.Invoke(playerID, false);
            }
            else
            {
                Utils.Log($"{k_DebugPrepend}Trying to remove a player ID [{playerID}] that doesn't exist", 1);
            }
        }

        /// <summary>
        /// Called whenever there is a problem with connecting to game or lobby.
        /// </summary>
        /// <param name="reason">Failure message.</param>
        public virtual void ConnectionFailed(string reason)
        {
            connectionFailedAction?.Invoke(reason);
            m_ConnectionState.Value = AuthenticationManager.IsAuthenticated() ? ConnectionState.Authenticated : ConnectionState.None;
        }

        /// <summary>
        /// Called whenever there is an update to connection status.
        /// </summary>
        /// <param name="update">Status update message.</param>
        public virtual void ConnectionUpdated(string update)
        {
            connectionUpdated?.Invoke(update);
        }

        /// <summary>
        /// Joins a random lobby. If no lobbies exist, it will create a new one.
        /// </summary>
        public virtual async void QuickJoinLobby()
        {
            Utils.Log($"{k_DebugPrepend}Joining Lobby by Quick Join.");
            if (await AbleToConnect())
            {
                ConnectToLobby(await m_LobbyManager.QuickJoinLobby());
            }
        }

        /// <summary>
        /// Called when trying to join a Lobby by Room Code.
        /// </summary>
        /// <param name="lobby">Lobby to join.</param>
        public virtual async void JoinLobbyByCode(string code)
        {
            Utils.Log($"{k_DebugPrepend}Joining Lobby by room code: {code}.");
            if (await AbleToConnect())
            {
                ConnectToLobby(await m_LobbyManager.JoinLobby(roomCode: code));
            }
        }

        /// <summary>
        /// Called when trying to join a specific Lobby.
        /// </summary>
        /// <param name="lobby">Lobby to join.</param>
        public virtual async void JoinLobbySpecific(Lobby lobby)
        {
            Utils.Log($"{k_DebugPrepend}Joining specific Lobby: {lobby.Name}.");
            if (await AbleToConnect())
            {
                ConnectToLobby(await m_LobbyManager.JoinLobby(lobby: lobby));
            }
        }

        /// <summary>
        /// Creates a new Lobby.
        /// </summary>
        /// <param name="roomName">Name of the lobby.</param>
        /// <param name="isPrivate">Whether or not the lobby is private.</param>
        /// <param name="playerCount">Maximum allowed players.</param>
        public virtual async void CreateNewLobby(string roomName = null, bool isPrivate = false, int playerCount = maxPlayers)
        {
            Utils.Log($"{k_DebugPrepend}Creating New Lobby: {roomName}.");
            if (await AbleToConnect())
            {
                ConnectToLobby(await m_LobbyManager.CreateLobby(roomName, isPrivate, playerCount));
            }
        }


        /// <summary>
        /// Checks if a we are currently able to connect to a lobby.
        /// If already connected it will disconnect in attempt to "Hot Join" a new lobby.
        /// </summary>
        /// <returns>Whether or not we are able to connect.</returns>
        protected virtual async Task<bool> AbleToConnect()
        {
            // If in the process of trying to connect, send failure message and return false.
            if (m_ConnectionState.Value == ConnectionState.Connecting)
            {
                string failureMessage = "Connection attempt still in progress.";
                Utils.Log($"{k_DebugPrepend}{failureMessage}", 1);
                ConnectionFailed(failureMessage);
                return false;
            }

            // If already connected to a lobby, disconnect in attempt to "Hot Join".
            if (Connected.Value || m_ConnectionState.Value == ConnectionState.Connected)
            {
                Utils.Log($"{k_DebugPrepend}Already Connected to a Lobby. Disconnecting.", 0);
                await DisconnectAsync();

                // Small wait while everything finishes disconnecting.
                // This isn't technically needed, but makes the flow feel better.
                await Task.Delay(100);
            }

            m_ConnectionState.Value = ConnectionState.Connecting;
            return true;
        }

        /// <summary>
        /// Connect to a lobby.
        /// </summary>
        /// <param name="lobby">Lobby to connect to.</param>
        protected virtual void ConnectToLobby(Lobby lobby)
        {
            // Send failure message if we can't connect.
            if (lobby == null || !ConnectedToLobby())
            {
                FailedToConnect();
            }
        }

        /// <summary>
        /// Checks if we successfully connected to a Lobby.
        /// If <see cref="autoConnectOnLobbyJoin"/> is enabled, join networked game here.
        /// </summary>
        /// <returns>Whether or not we connected to a lobby and / or networked game.</returns>
        protected virtual bool ConnectedToLobby()
        {
            bool connected;
            if (autoConnectOnLobbyJoin)
            {
                ConnectedRoomRegion = m_LobbyManager.connectedLobby.Data[LobbyManager.k_RegionKeyIdentifier].Value;
                ConnectedRoomCode = m_LobbyManager.connectedLobby.LobbyCode;
                ConnectedRoomName.Value = m_LobbyManager.connectedLobby.Name;

                if (m_LobbyManager.connectedLobby.HostId == AuthenicationId)
                {
                    connected = NetworkManager.Singleton.StartHost();
                }
                else
                {
                    connected = NetworkManager.Singleton.StartClient();
                }
            }
            else
            {
                connected = true;
                //Players are connected to the lobby, but have not started a Networked Game session.
            }

            if (connected)
            {
                Utils.Log($"{k_DebugPrepend}Connected to game session. Lobby: {m_LobbyManager.connectedLobby.Name}.");
                m_ConnectionState.Value = ConnectionState.Connected;
                SubscribeToLobbyEvents();
            }
            else
            {
                Utils.Log($"{k_DebugPrepend}Failed to connect to lobby {m_LobbyManager.connectedLobby.Name}.");
                m_LobbyManager.OnLobbyFailed?.Invoke($"Failed to connect to lobby {m_LobbyManager.connectedLobby.Name}.");
            }

            return connected;

        }

        /// <summary>
        /// Subscribe to lobby update events. This needed to be informed of Lobby changes (name, privacy, etc...).
        /// </summary>
        /// <remarks>See <see cref="OnLobbyChanged(ILobbyChanges)"/>.</remarks>
        protected virtual async void SubscribeToLobbyEvents()
        {
            var callbacks = new LobbyEventCallbacks();
            callbacks.LobbyChanged += OnLobbyChanged;
            callbacks.LobbyEventConnectionStateChanged += OnLobbyEventConnectionStateChanged;
            try
            {
                await LobbyService.Instance.SubscribeToLobbyEventsAsync(m_LobbyManager.connectedLobby.Id, callbacks);
            }
            catch (LobbyServiceException ex)
            {
                switch (ex.Reason)
                {
                    case LobbyExceptionReason.AlreadySubscribedToLobby: Utils.Log($"{k_DebugPrepend}Already subscribed to lobby[{m_LobbyManager.connectedLobby.Id}]. We did not need to try and subscribe again. Exception Message: {ex.Message}", 1); break;
                    case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy: Utils.Log($"{k_DebugPrepend}Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}", 2); throw;
                    case LobbyExceptionReason.LobbyEventServiceConnectionError: Utils.Log($"{k_DebugPrepend}Failed to connect to lobby events. Exception Message: {ex.Message}", 2); throw;
                    default: throw;
                }
            }
        }

        /// <summary>
        /// Callabacks for anytime the lobby event connection state has changed.
        /// </summary>
        /// <param name="state"></param>
        private void OnLobbyEventConnectionStateChanged(LobbyEventConnectionState state)
        {
            switch (state)
            {
                case LobbyEventConnectionState.Unsubscribed: Utils.Log($"{k_DebugPrepend}Lobby event now Unsubscribed"); break;
                case LobbyEventConnectionState.Subscribing: Utils.Log($"{k_DebugPrepend}Attempting to subscribe to lobby events"); break;
                case LobbyEventConnectionState.Subscribed: Utils.Log($"{k_DebugPrepend}Subscribing to lobby events now"); break;
                case LobbyEventConnectionState.Unsynced:
                    m_LobbyManager.ReconnectToLobby();
                    Utils.Log($"{k_DebugPrepend}Lobby Events now unsynced.\n\n{state}", 1);
                    break;
                case LobbyEventConnectionState.Error: Utils.Log($"{k_DebugPrepend}Lobby event error.\n\n{state}", 2); break;
            }
        }

        /// <summary>
        /// Callback for anytime a lobby is updated via <see cref="LobbyService.Instance.SubscribeToLobbyEventsAsync"/>.
        /// </summary>
        /// <param name="changes"></param>
        protected virtual void OnLobbyChanged(ILobbyChanges changes)
        {
            // Check for lobby deletion.
            if (!changes.LobbyDeleted)
            {
                changes.ApplyToLobby(m_LobbyManager.connectedLobby);

                // Update values based on lobby changes.
                if (changes.Name.Changed)
                {
                    ConnectedRoomName.Value = m_LobbyManager.connectedLobby.Name;
                }
            }
        }


        /// <summary>
        /// Generic failure message.
        /// </summary>
        protected virtual void FailedToConnect(string reason = null)
        {
            string failureMessage = "Failed to connect to lobby.";
            if (reason != null)
            {
                failureMessage = $"{reason}";
            }
            Utils.Log($"{k_DebugPrepend}{failureMessage}", 1);
        }

        /// <summary>
        /// Cancel current matchmaking.
        /// Called from the Lobby UI.
        /// </summary>
        public virtual async void CancelMatchmaking()
        {
            if (IsAuthenticated())
            {
                m_ConnectionState.Value = ConnectionState.Authenticated;
            }
            await m_LobbyManager.RemovePlayerFromLobby(AuthenicationId);
        }

        /// <summary>
        /// High Level Disconnect call.
        /// </summary>
        public virtual async void Disconnect()
        {
            await DisconnectAsync();
        }

        /// <summary>
        /// Awaitable Disconnect call, used for Hot Joining.
        /// </summary>
        /// <returns></returns>
        public virtual async Task<bool> DisconnectAsync()
        {
            bool fullyDisconnected = await m_LobbyManager.RemovePlayerFromLobby(AuthenicationId);
            m_Connected.Value = false;
            NetworkManager.Shutdown();
            if (IsAuthenticated())
            {
                m_ConnectionState.Value = ConnectionState.Authenticated;
            }
            else
            {
                m_ConnectionState.Value = ConnectionState.None;
            }
            Utils.Log($"{k_DebugPrepend}Disconnected from Game.");
            return fullyDisconnected;
        }
    }
}
