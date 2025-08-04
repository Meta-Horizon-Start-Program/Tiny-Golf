using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XRMultiplayer
{
    public class OfflineMenu : MonoBehaviour
    {
        /// <summary>
        /// Colors to choose from for the player.
        /// </summary>
        [SerializeField, Tooltip("Default name for the player")] Color[] m_PlayerColors;

        [Header("Player Info")]
        /// <summary>
        /// Default name for the player.
        /// </summary>
        [SerializeField, Tooltip("Default name for the player")] string m_DefaultPlayerName = "Unity Creator";
        [SerializeField] TMP_Text m_PlayerNameText;
        [SerializeField] TMP_Text m_PlayerInitialText;
        [SerializeField] Image[] m_PlayerColorIcons;
        [SerializeField] Image m_VolumeIndicator;
        [SerializeField] Image m_MicIcon;
        [SerializeField] Sprite m_MutedSprite;
        [SerializeField] Sprite m_UnmutedSprite;

        [Header("Panel Objects")]
        [SerializeField] GameObject m_CustomizationPanel;
        [SerializeField] GameObject m_ConnectionPanel;

        VoiceChatManager m_VoiceChatManager;

        private void Awake()
        {
            XRINetworkGameManager.Connected.Subscribe(OnConnected);
            XRINetworkGameManager.LocalPlayerName.Subscribe(SetPlayerName);
            XRINetworkGameManager.LocalPlayerColor.Subscribe(SetPlayerColor);

            OfflinePlayerAvatar.voiceAmp.Subscribe(UpdateMicIcon);

            m_VoiceChatManager = FindFirstObjectByType<VoiceChatManager>();
            m_VoiceChatManager.selfMuted.Subscribe(MutedChanged);
            SetupPlayerDefaults();
        }

        private void Start()
        {
            ShowCustomization();
            XRINetworkGameManager.Instance.connectionFailedAction += ConnectionFailed;
        }

        private void OnDestroy()
        {
            XRINetworkGameManager.Connected.Unsubscribe(OnConnected);
            XRINetworkGameManager.LocalPlayerName.Unsubscribe(SetPlayerName);
            XRINetworkGameManager.LocalPlayerColor.Unsubscribe(SetPlayerColor);
            OfflinePlayerAvatar.voiceAmp.Unsubscribe(UpdateMicIcon);
            m_VoiceChatManager.selfMuted.Subscribe(MutedChanged);

            XRINetworkGameManager.Instance.connectionFailedAction -= ConnectionFailed;
        }

        void SetupPlayerDefaults()
        {
            XRINetworkGameManager.LocalPlayerName.Value = m_DefaultPlayerName;
            XRINetworkGameManager.LocalPlayerColor.Value = m_PlayerColors[Random.Range(0, m_PlayerColors.Length)];
        }

        void SetPlayerName(string name)
        {
            if (name == string.Empty)
            {
                SetupPlayerDefaults();
                return;
            }

            m_PlayerNameText.text = name;
            m_PlayerInitialText.text = name.Substring(0, 1);
            m_PlayerNameText.rectTransform.sizeDelta = new Vector2(m_PlayerNameText.preferredWidth * .25f, m_PlayerNameText.rectTransform.sizeDelta.y);
        }

        void SetPlayerColor(Color color)
        {
            foreach (var c in m_PlayerColorIcons)
            {
                c.color = color;
            }
        }

        void UpdateMicIcon(float amp)
        {
            m_VolumeIndicator.fillAmount = amp;
        }

        void ShowCustomization()
        {
            m_CustomizationPanel.SetActive(true);
            m_ConnectionPanel.SetActive(false);
        }

        public void CompleteCustomization()
        {
            m_CustomizationPanel.SetActive(false);
            m_ConnectionPanel.SetActive(true);
        }

        void OnConnected(bool connected)
        {
            if (connected)
            {
                m_CustomizationPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
                ShowCustomization();
            }
        }

        void MutedChanged(bool muted)
        {
            m_MicIcon.sprite = muted ? m_MutedSprite : m_UnmutedSprite;
        }

        void ConnectionFailed(string reason)
        {
            CompleteCustomization();
        }
    }
}
