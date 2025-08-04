using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using UnityEngine;
using System.Threading.Tasks;
using System;
using Unity.Services.Relay;
using Unity.Netcode.Transports.UTP;
using Unity.XR.CoreUtils.Bindings.Variables;
using Unity.Services.Authentication;
using UnityEngine.SceneManagement;

namespace XRMultiplayer
{
    /// <summary>
    /// This class manages the relationship between Lobby, Relay, and Unity Transport.
    /// </summary>
    public class LobbyManager : MonoBehaviour
    {
        // Constants for Lobby Data.
        public const string k_JoinCodeKeyIdentifier = "j";
        public const string k_RegionKeyIdentifier = "r";
        public const string k_BuildIdKeyIdentifier = "b";
        public const string k_SceneKeyIdentifier = "s";
        public const string k_EditorKeyIdentifier = "e";

        static bool s_HideEditorInLobbies;

        [Tooltip("This will prevent joining into rooms that are being hosted in different scenes.\nThis should almost always be false.")]
        public bool allowDifferentScenes = false;

        [Tooltip("This will hide editor created rooms from external builds.\nNOTE: This will not hide editor created rooms from other editors.")]
        public bool hideEditorFromLobby = false;

        // Action that gets invoked when you fail to connect to a lobby. Primarily used for noting failure messages.
        public Action<string> OnLobbyFailed;

        // The current connected lobby.
        public Lobby connectedLobby
        {
            get => m_ConnectedLobby;
            set => m_ConnectedLobby = value;
        }
        Lobby m_ConnectedLobby;

        // The Transport used for connection.
        UnityTransport m_Transport;

        // This routine keeps the lobby alive once joined (by default lobbies will close after 30 seconds of inactivity.
        Coroutine m_HeartBeatRoutine;

        /// <summary>
        /// Subscribe to this bindable string for status updates from this class
        /// </summary>
        public static IReadOnlyBindableVariable<string> status
        {
            get => m_Status;
        }
        readonly static BindableVariable<string> m_Status = new("");

        const string k_DebugPrepend = "<color=#EC0CFA>[Lobby Manager]</color> ";


        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        private void Awake()
        {
            m_Transport = FindFirstObjectByType<UnityTransport>();

            if (!Application.isEditor)
            {
                hideEditorFromLobby = false;
            }
            s_HideEditorInLobbies = hideEditorFromLobby;
        }

        /// <summary>
        /// Quick Join Function will try and find any lobbies via QuickJoinLobbyAsync().
        /// If no lobbies are found then a new lobby is created.
        /// </summary>
        /// <returns></returns>
        public async Task<Lobby> QuickJoinLobby()
        {
            m_Status.Value = "Checking For Existing Lobbies.";
            Utils.Log($"{k_DebugPrepend}{m_Status.Value}");
            Lobby lobby;
            try
            {
                Utils.Log($"{k_DebugPrepend} Getting lobby via Quick Join");
                lobby = await LobbyService.Instance.QuickJoinLobbyAsync(GetQuickJoinFilterOptions());
                await SetupRelay(lobby);
                ConnectedToLobby(lobby);

                if (lobby != null)
                {
                    m_ConnectedLobby = lobby;
                    return lobby;
                }
            }
            catch
            {
                m_Status.Value = "No Available Lobbies. Creating New Lobby.";
                Utils.Log($"{k_DebugPrepend}{m_Status.Value}");
            }

            // If no existing Lobbies, then create a new one.
            lobby = await CreateLobby();
            return lobby;
        }

        /// <summary>
        /// Joins a lobby.
        /// </summary>
        /// <param name="lobby">Lobby to join.</param>
        /// <param name="roomCode">Lobby Code to join with.</param>
        /// <returns>Returns the Lobby.</returns>
        public async Task<Lobby> JoinLobby(Lobby lobby = null, string roomCode = null)
        {
            try
            {
                // If Lobby is null, then get the new lobby based on room code
                lobby = await GetLobby(lobby, roomCode);
                await SetupRelay(lobby);
                ConnectedToLobby(lobby);

                return lobby;

            }
            catch (Exception e)
            {
                string failureMessage = "Failed to Join Lobby.";
                Utils.Log($"{k_DebugPrepend}{e.Message}", 1);

                if (e is LobbyServiceException)
                {
                    string message = e.Message.ToLower();

                    if (message.Contains("Rate limit".ToLower()))
                        failureMessage = "Rate limit exceeded. Please try again later.";
                    else if (message.Contains("Lobby not found".ToLower()))
                        failureMessage = "Lobby not found. Please try a new Lobby.";
                    else
                        failureMessage = e.Message;
                }
                Utils.Log($"{k_DebugPrepend}{failureMessage}\n\n{e}", 1);
                OnLobbyFailed?.Invoke($"{failureMessage}");
                return null;
            }
        }

        /// <summary>
        /// This function will try to create a lobby and host a networked session.
        /// </summary>
        /// <returns></returns>
        public async Task<Lobby> CreateLobby(string roomName = null, bool isPrivate = false, int playerCount = XRINetworkGameManager.maxPlayers)
        {
            try
            {
                m_Status.Value = "Creating Relay";
                // Creates a new Allocation based on the defined max players above
                var alloc = await RelayService.Instance.CreateAllocationAsync(XRINetworkGameManager.maxPlayers);

                m_Status.Value = "Creating Join Code";
                // Get a join code based on the Allocation
                var joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

                // Creates Lobby Options Dictionary for other clients to find and join
                var options = new CreateLobbyOptions
                {
                    // Set the Data to be used for lobby filtering
                    Data = new Dictionary<string, DataObject>
                    {
                        {
                            // Set Join Code Key
                            k_JoinCodeKeyIdentifier, new DataObject(DataObject.VisibilityOptions.Public, joinCode)
                        },
                        {
                            // Set Region Key
                            k_RegionKeyIdentifier, new DataObject(DataObject.VisibilityOptions.Public, alloc.Region)
                        },
                        {
                            // Set Build ID Key
                            k_BuildIdKeyIdentifier, new DataObject(DataObject.VisibilityOptions.Public, Application.version, DataObject.IndexOptions.S1)
                        },
                        {
                            // Set Scene Key
                            k_SceneKeyIdentifier, new DataObject(DataObject.VisibilityOptions.Public, SceneManager.GetActiveScene().name, DataObject.IndexOptions.S2)
                        },
                        {
                            // Set Editor Key
                            k_EditorKeyIdentifier, new DataObject(DataObject.VisibilityOptions.Public, hideEditorFromLobby.ToString(), DataObject.IndexOptions.S3)
                        },
                    },
                    IsPrivate = isPrivate,
                };


                m_Status.Value = "Creating Lobby";
                // Creates the Lobby with the specified max players and lobby options. Currently just naming "General Lobby"
                string lobbyName = string.IsNullOrEmpty(roomName) ? $"{XRINetworkGameManager.LocalPlayerName.Value}'s Room" : $"{roomName}";

                // RATE LIMIT: 2 request per 6 seconds
                var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, playerCount, options);
                Utils.Log($"{k_DebugPrepend}Created Lobby with Join Code: {joinCode}, Region: {alloc.Region}, Build ID: {Application.version}, Scene: {SceneManager.GetActiveScene().name}, Editor: {hideEditorFromLobby}");

                // Stop the heartbeat routine if one exists, and starts a new one. This keeps the lobby active for visibility
                if (m_HeartBeatRoutine != null) StopCoroutine(m_HeartBeatRoutine);
                m_HeartBeatRoutine = StartCoroutine(LobbyHeartbeatCoroutine(lobby.Id));

                //Populate the transport data with the relay info for the host (IP, port, etc...)
                m_Transport.SetHostRelayData(alloc.RelayServer.IpV4, (ushort)alloc.RelayServer.Port, alloc.AllocationIdBytes, alloc.Key, alloc.ConnectionData);
                ConnectedToLobby(lobby);

                return lobby;
            }
            catch (Exception e)
            {
                string failureMessage = "Failed to Create Lobby. Please try again.";
                Utils.Log($"{k_DebugPrepend}{failureMessage}\n\n{e}", 1);
                // Debug.LogWarning($"[XRMPT] {failureMessage}\n\n{e}");
                OnLobbyFailed?.Invoke(failureMessage);
                return null;
            }
        }

        async Task SetupRelay(Lobby lobby)
        {
            m_Status.Value = "Connecting To Relay";
            // Get the Join Allocation for the lobby based on the key
            var alloc = await RelayService.Instance.JoinAllocationAsync(lobby.Data[k_JoinCodeKeyIdentifier].Value);
            // Set the transport client data (IP, port, etc..)
            m_Transport.SetClientRelayData
            (
                alloc.RelayServer.IpV4, (ushort)alloc.RelayServer.Port,
                alloc.AllocationIdBytes, alloc.Key, alloc.ConnectionData, alloc.HostConnectionData
            );
            return;
        }

        QuickJoinLobbyOptions GetQuickJoinFilterOptions()
        {
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();

            // Create Filter Option to prevent showing any application versions that are not the same.
            QueryFilter applicationVersionIdFilter = new QueryFilter(field: QueryFilter.FieldOptions.S1, value: Application.version, QueryFilter.OpOptions.EQ);

            // Create Filter Option for different scenes.
            QueryFilter sceneNameFilter = new QueryFilter(field: QueryFilter.FieldOptions.S2, value: SceneManager.GetActiveScene().name, QueryFilter.OpOptions.EQ);

            // Create Filter Option for hiding editor created rooms from builds.
            QueryFilter editorFilter = new QueryFilter(field: QueryFilter.FieldOptions.S3, value: hideEditorFromLobby.ToString(), QueryFilter.OpOptions.EQ);

            options.Filter = new List<QueryFilter> { applicationVersionIdFilter, sceneNameFilter, editorFilter };

            return options;
        }

        public async void ReconnectToLobby()
        {
            if (Application.isPlaying)
            {
                await LobbyService.Instance.ReconnectToLobbyAsync(m_ConnectedLobby.Id);
            }
        }

        /// <summary>
        /// This function will get a lobby based on the passed in parameters.
        /// </summary>
        /// <param name="lobby">Lobby to join.</param>
        /// <param name="roomCode">Lobby Code to join with.</param>
        /// <returns>Returns the Lobby.</returns>
        async Task<Lobby> GetLobby(Lobby lobby = null, string roomCode = null)
        {
            if (roomCode != null)
            {
                // RATE LIMIT: 2 request per 6 seconds
                Utils.Log($"{k_DebugPrepend} Getting lobby via Code: {roomCode}");
                return await LobbyService.Instance.JoinLobbyByCodeAsync(roomCode);
            }
            else if (lobby != null)
            {
                // RATE LIMIT: 2 request per 6 seconds
                Utils.Log($"{k_DebugPrepend} Getting lobby via Lobby Id: {lobby.Id}");
                return await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);
            }
            else
            {
                // RATE LIMIT: 1 request per 10 seconds
                Utils.Log($"{k_DebugPrepend} Getting lobby via Quick Join");
                return await LobbyService.Instance.QuickJoinLobbyAsync(GetQuickJoinFilterOptions());
            }
        }

        /// <summary>
        /// Called after setting transport data with relay allocations when either Creating a new lobby or joining an existing lobby.
        /// </summary>
        void ConnectedToLobby(Lobby lobby)
        {
            m_ConnectedLobby = lobby;
            m_Status.Value = "Connected To Lobby";
        }

        /// <summary>
        /// Heartbeat used to keep the lobby alive. By default the lobby will shut down after 30 seconds on inactivity.
        /// </summary>
        /// <param name="lobbyId">Id for the specific lobby to keep alive.</param>
        /// <param name="waitTimeSeconds">Time to wait between pings.</param>
        /// <returns></returns>
        IEnumerator LobbyHeartbeatCoroutine(string lobbyId, float waitTimeSeconds = 15.0f)
        {
            // Setup a new wait based on wait time in seconds
            var delay = new WaitForSecondsRealtime(waitTimeSeconds);

            while (true)
            {
                // Continuously ping the lobby to keep it alive
                LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
                Utils.Log($"{k_DebugPrepend}Sending Heartbeat Ping for Lobby {lobbyId}");
                yield return delay;
            }
        }

        /// <summary>
        /// Changes the existing lobbies name.
        /// </summary>
        /// <param name="lobbyName">Name to change the lobby to.</param>
        public async void UpdateLobbyName(string lobbyName)
        {
            if (m_ConnectedLobby != null)
            {
                try
                {
                    UpdateLobbyOptions options = new()
                    {
                        Name = lobbyName,
                        HostId = AuthenticationService.Instance.PlayerId
                    };

                    await LobbyService.Instance.UpdateLobbyAsync(m_ConnectedLobby.Id, options);
                    XRINetworkGameManager.ConnectedRoomName.Value = lobbyName;
                }
                catch (LobbyServiceException e)
                {
                    Utils.Log($"{k_DebugPrepend}{e}");
                }
            }
            else
            {
                Utils.Log($"{k_DebugPrepend}Connected Lobby is null");
            }
        }

        /// <summary>
        /// Updates the privacy setting for the current room.
        /// </summary>
        /// <param name="privateRoom">Whether or not to make the room private.</param>
        public async void UpdateRoomPrivacy(bool privateRoom)
        {
            if (m_ConnectedLobby != null)
            {
                try
                {
                    UpdateLobbyOptions options = new()
                    {
                        IsPrivate = privateRoom
                    };
                    await LobbyService.Instance.UpdateLobbyAsync(m_ConnectedLobby.Id, options);
                }
                catch (LobbyServiceException e)
                {
                    Utils.Log($"{k_DebugPrepend}{e}");
                }
            }
            else
            {
                Utils.Log($"{k_DebugPrepend}Connected Lobby is null");
            }
        }

        /// <summary>
        /// Called when leaving a room.
        /// If Hosting, this function deletes the lobby for everyone.
        /// If a client, this function removes the client from the lobby.
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public async Task<bool> RemovePlayerFromLobby(string playerId)
        {
            // Stop heartbeat if active (only runs on host)
            if (m_HeartBeatRoutine != null) StopCoroutine(m_HeartBeatRoutine);

            try
            {
                if (m_ConnectedLobby != null)
                {
                    // Check if Lobby Host is current Player
                    if (m_ConnectedLobby.HostId == playerId)
                    {
                        // Delete Lobby if current owner
                        Utils.Log($"{k_DebugPrepend}Owner of lobby, shutting down.");
                        await LobbyService.Instance.DeleteLobbyAsync(m_ConnectedLobby.Id);
                        m_ConnectedLobby = null;
                    }
                    else
                    {
                        //Remove from lobby
                        await RemoveFromLobby(playerId);
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                Utils.Log($"{k_DebugPrepend}Error on Lobby Shutdown:\n\n {e}");
            }

            return false;
        }

        /// <summary>
        /// Attempts to remove the current player from the lobby
        /// </summary>
        async Task<bool> RemoveFromLobby(string playerId)
        {
            // If lobby id exists try to remove player from
            if (!string.IsNullOrEmpty(m_ConnectedLobby.Id))
            {
                try
                {
                    await LobbyService.Instance.RemovePlayerAsync(m_ConnectedLobby.Id, playerId);
                    m_ConnectedLobby = null;
                    Utils.Log($"{k_DebugPrepend}Successfully removed player from Lobby.");
                    return true;
                }
                catch (Exception e)
                {
                    Utils.Log($"{k_DebugPrepend}Failed to remove player from lobby.\n\n{e}");
                }
            }

            return false;
        }


        public static async Task<QueryResponse> GetLobbiesAsync()
        {
            // Use these options to apply things like filters, ordering, etc...
            // Additionally you can add your own filters like below to have more control over the data.
            QueryLobbiesOptions lobbyOptions = new QueryLobbiesOptions();
            return await LobbyService.Instance.QueryLobbiesAsync(lobbyOptions);
        }

        public static bool CheckForLobbyFilter(Lobby lobby)
        {
            // If the lobby is not in the same scene, skip it
            if (lobby.Data.TryGetValue(k_SceneKeyIdentifier, out DataObject sceneData))
            {
                if (sceneData.Value != SceneManager.GetActiveScene().name)
                {
                    return true;
                }
            }

            if (lobby.Data.TryGetValue(k_EditorKeyIdentifier, out DataObject editorData))
            {
                // If the lobby is an editor lobby is set to filter return true
                if (editorData.Value == "True" & !s_HideEditorInLobbies)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CheckForIncompatibilityFilter(Lobby lobby)
        {
            if (lobby.Data.TryGetValue(k_BuildIdKeyIdentifier, out DataObject data))
            {
                //Filter out lobbies that are on different build versions
                if (data.Value != Application.version)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool CanJoinLobby(Lobby lobby)
        {
            return (XRINetworkGameManager.Instance.lobbyManager.connectedLobby == null) ||
            (XRINetworkGameManager.Instance.lobbyManager.connectedLobby != null && lobby.Id != XRINetworkGameManager.Instance.lobbyManager.connectedLobby.Id);
        }
    }
}
