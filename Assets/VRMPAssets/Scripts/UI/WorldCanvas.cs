using System.Collections.Generic;
using UnityEngine;

namespace XRMultiplayer
{
    public class WorldCanvas : MonoBehaviour
    {
        [SerializeField] Transform m_PlayerNameTagsParent;
        [SerializeField] float m_nameTagOffsetHeight = 0.3f;
        readonly Dictionary<PlayerNameTag, XRINetworkPlayer> playerDictionary = new Dictionary<PlayerNameTag, XRINetworkPlayer>();

        private void Start()
        {
            XRINetworkGameManager.Connected.Subscribe(OnConnectedUpdate);
            XRINetworkGameManager.Instance.playerStateChanged += ConnectedPlayerStateChange;
        }

        private void OnDestroy()
        {
            XRINetworkGameManager.Connected.Unsubscribe(OnConnectedUpdate);
            XRINetworkGameManager.Instance.playerStateChanged -= ConnectedPlayerStateChange;
        }

        void OnConnectedUpdate(bool connected)
        {
            if (!connected)
            {
                foreach (var kvp in playerDictionary)
                {
                    Destroy(kvp.Key.gameObject);
                }
                playerDictionary.Clear();
            }
        }

        void ConnectedPlayerStateChange(ulong playerId, bool connected)
        {
            if (!connected)
            {
                if (!RemovePlayerNameTag(playerId))
                {
                    Utils.Log($"Failed to Remove Player with id {playerId}.", 1);
                }
            }
        }

        bool RemovePlayerNameTag(ulong playerId)
        {
            foreach (var key in playerDictionary.Keys)
            {
                if (key.playerId == playerId)
                {
                    playerDictionary.Remove(key);
                    Destroy(key.gameObject);

                    return true;
                }
            }
            return false;
        }

        public void SetupPlayerNameTag(XRINetworkPlayer player, PlayerNameTag nameTag)
        {
            nameTag.SetupNameTag(player);
            nameTag.transform.SetParent(m_PlayerNameTagsParent);

            if (!playerDictionary.ContainsKey(nameTag))
            {
                playerDictionary.Add(nameTag, player);
            }

            if (player.IsLocalPlayer)
            {
                nameTag.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            foreach (var kvp in playerDictionary)
            {
                kvp.Key.transform.position = kvp.Value.head.position + Vector3.up * m_nameTagOffsetHeight;
                kvp.Key.UpdateVoice(kvp.Value.playerVoiceAmp);

            }
        }
    }
}
