using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace XRMultiplayer
{
    public class PlayerListUI : MonoBehaviour
    {
        [SerializeField] TMP_Text m_PlayerCountText;
        [SerializeField] Transform m_ConnectedPlayersViewportContentTransform;
        [SerializeField] GameObject m_PlayerSlotPrefab;

        [SerializeField] bool m_AutoInitializeCallbacks = true;

        readonly Dictionary<PlayerSlot, XRINetworkPlayer> m_PlayerDictionary = new();

        bool m_CallbacksInitialized = false;

        private void Start()
        {
            if (!m_CallbacksInitialized && m_AutoInitializeCallbacks)
                InitializeCallbacks();
        }

        private void Update()
        {
            if (XRINetworkGameManager.Connected.Value)
            {
                foreach (var kvp in m_PlayerDictionary)
                {
                    kvp.Key.voiceChatFillImage.fillAmount = kvp.Value.playerVoiceAmp;
                }
            }
        }

        public void OnDestroy()
        {
            XRINetworkGameManager.Instance.playerStateChanged -= ConnectedPlayerStateChange;
            XRINetworkGameManager.Connected.Unsubscribe(OnConnected);
        }

        /// <summary>
        /// Use this function to initialize the callbacks on objects that start out disabled or inactive.
        /// </summary>
        public void InitializeCallbacks()
        {
            if (m_CallbacksInitialized) return;
            m_CallbacksInitialized = true;

            //Remove Prefab placeholders
            foreach (Transform t in m_ConnectedPlayersViewportContentTransform)
            {
                Destroy(t.gameObject);
            }

            XRINetworkGameManager.Instance.playerStateChanged += ConnectedPlayerStateChange;
            XRINetworkGameManager.Connected.Subscribe(OnConnected);
        }

        void OnConnected(bool connected)
        {
            if (!connected)
            {
                foreach (Transform t in m_ConnectedPlayersViewportContentTransform)
                {
                    Destroy(t.gameObject);
                }

                m_PlayerDictionary.Clear();
            }
        }

        void ConnectedPlayerStateChange(ulong playerId, bool connected)
        {
            if (connected)
            {
                SetupPlayerSlotUI(playerId);
            }
            else
            {
                RemovePlayerSlotUI(playerId);
            }
        }

        void RemovePlayerSlotUI(ulong playerId)
        {
            PlayerSlot slotToRemove = null;
            foreach (PlayerSlot slot in m_PlayerDictionary.Keys)
            {
                if (slot.playerID == playerId)
                {
                    slotToRemove = slot;
                    break;
                }
            }

            if (slotToRemove != null)
            {
                m_PlayerDictionary.Remove(slotToRemove);
                Destroy(slotToRemove.gameObject);
                m_PlayerCountText.text = $"{m_PlayerDictionary.Keys.Count}/{XRINetworkGameManager.Instance.lobbyManager.connectedLobby.MaxPlayers}";
            }
        }

        void SetupPlayerSlotUI(ulong playerId)
        {
            PlayerSlot slot = Instantiate(m_PlayerSlotPrefab, m_ConnectedPlayersViewportContentTransform).GetComponent<PlayerSlot>();
            slot.playerID = playerId;

            if (XRINetworkGameManager.Instance.GetPlayerByID(playerId, out XRINetworkPlayer player))
            {
                if (m_PlayerDictionary.TryAdd(slot, player))
                {
                    slot.Setup(player);

                    if (player.IsLocalPlayer)
                    {
                        slot.playerSlotName.text += " (You)";
                    }
                    slot.playerIconImage.color = player.playerColor;

                    m_PlayerCountText.text = $"{m_PlayerDictionary.Keys.Count}/{XRINetworkGameManager.Instance.lobbyManager.connectedLobby.MaxPlayers}";
                }
            }
            else
            {
                Utils.Log($"Player with id {playerId} is null. This is a bug.", 2);
            }
        }
    }
}
