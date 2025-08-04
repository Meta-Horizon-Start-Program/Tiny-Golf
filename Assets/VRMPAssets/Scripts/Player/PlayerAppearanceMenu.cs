using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XRMultiplayer
{
    /// <summary>
    /// A simple example of how to setup a player appearance menu and utilize the bindable variables.
    /// </summary>
    public class PlayerAppearanceMenu : MonoBehaviour
    {
        [SerializeField] Color[] m_PlayerColors;
        [SerializeField] TMP_InputField m_PlayerNameInputField;
        [SerializeField] Image m_PlayerIconColor;


        void Awake()
        {
            XRINetworkGameManager.LocalPlayerName.Subscribe(SetPlayerName);
            XRINetworkGameManager.LocalPlayerColor.Subscribe(SetPlayerColor);
        }

        void Start()
        {
            SetPlayerColor(XRINetworkGameManager.LocalPlayerColor.Value);
            SetPlayerName(XRINetworkGameManager.LocalPlayerName.Value);
        }

        void OnDestroy()
        {
            XRINetworkGameManager.LocalPlayerName.Unsubscribe(SetPlayerName);
            XRINetworkGameManager.LocalPlayerColor.Unsubscribe(SetPlayerColor);
        }

        /// <summary>
        /// Use this to set the player's name so it triggers the bindable variable
        /// </summary>
        /// <param name="text"></param>
        public void SubmitNewPlayerName(string text)
        {
            XRINetworkGameManager.LocalPlayerName.Value = text;
        }

        /// <summary>
        /// Use this to set the player's color so it triggers the bindable variable
        /// </summary>
        /// <param name="text"></param>
        public void SetRandomColor()
        {
            List<Color> availableColors = new(m_PlayerColors);
            if (availableColors.Remove(XRINetworkGameManager.LocalPlayerColor.Value))
            {

                XRINetworkGameManager.LocalPlayerColor.Value = availableColors[Random.Range(0, availableColors.Count)];
            }
            else
            {
                XRINetworkGameManager.LocalPlayerColor.Value = m_PlayerColors[Random.Range(0, m_PlayerColors.Length)];
            }
        }

        void SetPlayerName(string newName)
        {
            m_PlayerNameInputField.text = newName;
        }

        void SetPlayerColor(Color color)
        {
            m_PlayerIconColor.color = color;
        }
    }
}
