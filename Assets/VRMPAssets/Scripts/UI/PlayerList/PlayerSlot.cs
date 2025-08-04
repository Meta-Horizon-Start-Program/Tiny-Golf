using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

namespace XRMultiplayer
{
    public class PlayerSlot : MonoBehaviour
    {
        public TMP_Text playerSlotName;
        public TMP_Text playerInitial;
        public Image playerIconImage;

        [Header("Mic Button")]
        public Image voiceChatFillImage;
        [SerializeField] Button m_MicButton;
        [SerializeField] Image m_PlayerVoiceIcon;
        [SerializeField] Image m_SquelchedIcon;
        [SerializeField] Sprite[] micIcons;
        XRINetworkPlayer m_Player;
        internal ulong playerID = 0;

        public void Setup(XRINetworkPlayer player)
        {
            m_Player = player;
            m_Player.onColorUpdated += UpdateColor;
            m_Player.onNameUpdated += UpdateName;
            m_Player.selfMuted.OnValueChanged += UpdateSelfMutedState;
            m_MicButton.onClick.AddListener(Squelch);
            m_Player.squelched.Subscribe(UpdateSquelchedState);
            m_SquelchedIcon.enabled = false;
            if (m_Player.IsLocalPlayer)
            {
                m_MicButton.interactable = false;
            }

            if (m_Player.selfMuted.Value)
            {
                m_PlayerVoiceIcon.sprite = micIcons[1];
            }
        }

        void OnDestroy()
        {
            m_Player.onColorUpdated -= UpdateColor;
            m_Player.onNameUpdated -= UpdateName;
            m_Player.selfMuted.OnValueChanged -= UpdateSelfMutedState;
            m_MicButton.onClick.RemoveListener(Squelch);
            m_Player.squelched.Unsubscribe(UpdateSquelchedState);
        }

        void UpdateColor(Color newColor)
        {
            playerIconImage.color = newColor;
        }

        void UpdateName(string newName)
        {
            if (!newName.IsNullOrEmpty())
            {
                string playerName = newName;
                if (m_Player.IsLocalPlayer)
                {
                    playerName += " (You)";
                }
                else if (m_Player.IsOwnedByServer)
                {
                    playerName += " (Host)";
                }
                playerSlotName.text = playerName;
                playerInitial.text = newName.Substring(0, 1);
            }
        }

        #region Muting
        public void Squelch()
        {
            m_Player.ToggleSquelch();
        }

        void UpdateSelfMutedState(bool old, bool current)
        {
            m_PlayerVoiceIcon.sprite = micIcons[current ? 1 : 0];
        }

        void UpdateSquelchedState(bool squelched)
        {
            m_SquelchedIcon.enabled = squelched;
        }
        #endregion
    }
}
