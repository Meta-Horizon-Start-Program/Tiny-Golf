using System.Collections;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WebSocketSharp;
using Unity.Services.Vivox;

namespace XRMultiplayer
{
    public class LobbyUI : MonoBehaviour
    {
        [Header("Lobby List")]
        [SerializeField] Transform m_LobbyListParent;
        [SerializeField] GameObject m_LobbyListPrefab;
        [SerializeField] Button m_RefreshButton;
        [SerializeField] Image m_CooldownImage;
        [SerializeField] float m_AutoRefreshTime = 5.0f;
        [SerializeField] float m_RefreshCooldownTime = .5f;

        [Header("Connection Texts")]
        [SerializeField] TMP_Text m_ConnectionUpdatedText;
        [SerializeField] TMP_Text m_ConnectionSuccessText;
        [SerializeField] TMP_Text m_ConnectionFailedText;

        [Header("Room Creation")]
        [SerializeField] TMP_InputField m_RoomNameText;
        [SerializeField] Toggle m_PrivacyToggle;

        [SerializeField] GameObject[] m_ConnectionSubPanels;

        VoiceChatManager m_VoiceChatManager;

        Coroutine m_UpdateLobbiesRoutine;
        Coroutine m_CooldownFillRoutine;

        bool m_Private = false;
        int m_PlayerCount;

        private void Awake()
        {
            m_VoiceChatManager = FindFirstObjectByType<VoiceChatManager>();
            LobbyManager.status.Subscribe(ConnectedUpdated);
            m_CooldownImage.enabled = false;
        }

        private void Start()
        {
            m_PrivacyToggle.onValueChanged.AddListener(TogglePrivacy);

            m_PlayerCount = XRINetworkGameManager.maxPlayers / 2;

            XRINetworkGameManager.Instance.connectionFailedAction += FailedToConnect;
            XRINetworkGameManager.Instance.connectionUpdated += ConnectedUpdated;

            foreach (Transform t in m_LobbyListParent)
            {
                Destroy(t.gameObject);
            }
        }

        private void OnEnable()
        {
            CheckInternetAsync();
        }

        private void OnDisable()
        {
            HideLobbies();
        }

        private void OnDestroy()
        {
            XRINetworkGameManager.Instance.connectionFailedAction -= FailedToConnect;
            XRINetworkGameManager.Instance.connectionUpdated -= ConnectedUpdated;

            LobbyManager.status.Unsubscribe(ConnectedUpdated);
        }
        public async void CheckInternetAsync()
        {
            if (!XRINetworkGameManager.Instance.IsAuthenticated())
            {
                ToggleConnectionSubPanel(5);
                await XRINetworkGameManager.Instance.Authenticate();
            }
            CheckForInternet();
        }

        void CheckForInternet()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                ToggleConnectionSubPanel(5);
            }
            else
            {
                ToggleConnectionSubPanel(0);
            }
        }

        public void CreateLobby()
        {
            XRINetworkGameManager.Connected.Subscribe(OnConnected);
            if (m_RoomNameText.text.IsNullOrEmpty() || m_RoomNameText.text == "<Room Name>")
            {
                m_RoomNameText.text = $"{XRINetworkGameManager.LocalPlayerName.Value}'s Room";
            }
            XRINetworkGameManager.Instance.CreateNewLobby(m_RoomNameText.text, m_Private, m_PlayerCount);
            m_ConnectionSuccessText.text = $"Joining {m_RoomNameText.text}";
        }

        public void UpdatePlayerCount(int count)
        {
            m_PlayerCount = Mathf.Clamp(count, 1, XRINetworkGameManager.maxPlayers);
        }

        public void CancelConnection()
        {
            XRINetworkGameManager.Instance.CancelMatchmaking();
        }

        /// <summary>
        /// Set the room name
        /// </summary>
        /// <param name="roomName">The name of the room</param>
        /// <remarks> This function is called from <see cref="XRIKeyboardDisplay"/>
        public void SetRoomName(string roomName)
        {
            if (!string.IsNullOrEmpty(roomName))
            {
                m_RoomNameText.text = roomName;
            }
        }

        /// <summary>
        /// Join a room by code
        /// </summary>
        /// <param name="roomCode">The room code to join</param>
        /// <remarks> This function is called from <see cref="XRIKeyboardDisplay"/>
        public void EnterRoomCode(string roomCode)
        {
            ToggleConnectionSubPanel(2);
            XRINetworkGameManager.Connected.Subscribe(OnConnected);
            XRINetworkGameManager.Instance.JoinLobbyByCode(roomCode.ToUpper());
            m_ConnectionSuccessText.text = $"Joining Room: {roomCode.ToUpper()}";
        }

        public void JoinLobby(Lobby lobby)
        {
            ToggleConnectionSubPanel(2);
            XRINetworkGameManager.Connected.Subscribe(OnConnected);
            XRINetworkGameManager.Instance.JoinLobbySpecific(lobby);
            m_ConnectionSuccessText.text = $"Joining {lobby.Name}";
        }

        public void QuickJoinLobby()
        {
            XRINetworkGameManager.Connected.Subscribe(OnConnected);
            XRINetworkGameManager.Instance.QuickJoinLobby();
            m_ConnectionSuccessText.text = "Joining Random";
        }

        public void SetVoiceChatAudidibleDistance(int audibleDistance)
        {
            if (audibleDistance <= m_VoiceChatManager.ConversationalDistance)
            {
                audibleDistance = m_VoiceChatManager.ConversationalDistance + 1;
            }
            m_VoiceChatManager.AudibleDistance = audibleDistance;
        }

        public void SetVoiceChatConversationalDistance(int conversationalDistance)
        {
            m_VoiceChatManager.ConversationalDistance = conversationalDistance;
        }

        public void SetVoiceChatAudioFadeIntensity(float fadeIntensity)
        {
            m_VoiceChatManager.AudioFadeIntensity = fadeIntensity;
        }

        public void SetVoiceChatAudioFadeModel(int fadeModel)
        {
            m_VoiceChatManager.AudioFadeModel = (AudioFadeModel)fadeModel;
        }

        public void TogglePrivacy(bool toggle)
        {
            m_Private = toggle;
        }

        public void ToggleConnectionSubPanel(int panelId)
        {
            for (int i = 0; i < m_ConnectionSubPanels.Length; i++)
            {
                m_ConnectionSubPanels[i].SetActive(i == panelId);
            }


            if (panelId == 0)
            {
                ShowLobbies();
            }
            else
            {
                HideLobbies();
            }
        }

        void OnConnected(bool connected)
        {
            if (connected)
            {
                ToggleConnectionSubPanel(3);
                XRINetworkGameManager.Connected.Unsubscribe(OnConnected);
            }
        }

        void ConnectedUpdated(string update)
        {
            m_ConnectionUpdatedText.text = $"<b>Status:</b> {update}";
        }

        public void FailedToConnect(string reason)
        {
            ToggleConnectionSubPanel(4);
            m_ConnectionFailedText.text = $"<b>Error:</b> {reason}";
        }

        public void HideLobbies()
        {
            EnableRefresh();
            if (m_UpdateLobbiesRoutine != null) StopCoroutine(m_UpdateLobbiesRoutine);
        }

        public void ShowLobbies()
        {
            GetAllLobbies();
            if (m_UpdateLobbiesRoutine != null) StopCoroutine(m_UpdateLobbiesRoutine);
            m_UpdateLobbiesRoutine = StartCoroutine(UpdateAvailableLobbies());
        }

        IEnumerator UpdateAvailableLobbies()
        {
            while (true)
            {
                yield return new WaitForSeconds(m_AutoRefreshTime);
                GetAllLobbies();
            }
        }

        void EnableRefresh()
        {
            m_CooldownImage.enabled = false;
            m_RefreshButton.interactable = true;
        }

        IEnumerator UpdateButtonCooldown()
        {
            m_RefreshButton.interactable = false;

            m_CooldownImage.enabled = true;
            for (float i = 0; i < m_RefreshCooldownTime; i += Time.deltaTime)
            {
                m_CooldownImage.fillAmount = Mathf.Clamp01(i / m_RefreshCooldownTime);
                yield return null;
            }
            EnableRefresh();
        }

        async void GetAllLobbies()
        {
            if (m_CooldownImage.enabled || (int)XRINetworkGameManager.CurrentConnectionState.Value < 2) return;
            if (m_CooldownFillRoutine != null) StopCoroutine(m_CooldownFillRoutine);
            m_CooldownFillRoutine = StartCoroutine(UpdateButtonCooldown());

            QueryResponse lobbies = await LobbyManager.GetLobbiesAsync();

            foreach (Transform t in m_LobbyListParent)
            {
                Destroy(t.gameObject);
            }

            if (lobbies.Results != null || lobbies.Results.Count > 0)
            {
                foreach (var lobby in lobbies.Results)
                {
                    if (LobbyManager.CheckForLobbyFilter(lobby))
                    {
                        continue;
                    }

                    if (LobbyManager.CheckForIncompatibilityFilter(lobby))
                    {
                        LobbyListSlotUI newLobbyUI = Instantiate(m_LobbyListPrefab, m_LobbyListParent).GetComponent<LobbyListSlotUI>();
                        newLobbyUI.CreateNonJoinableLobbyUI(lobby, this, "Version Conflict");
                        continue;
                    }

                    if (LobbyManager.CanJoinLobby(lobby))
                    {
                        LobbyListSlotUI newLobbyUI = Instantiate(m_LobbyListPrefab, m_LobbyListParent).GetComponent<LobbyListSlotUI>();
                        newLobbyUI.CreateLobbyUI(lobby, this);
                    }
                }
            }
        }
    }
}
