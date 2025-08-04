using Unity.Netcode;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XRMultiplayer
{
    /// <summary>
    /// Manages the network functionality for VR multiplayer.
    /// </summary>
    public class NetworkManagerVRMultiplayer : NetworkManager
    {
        [SerializeField, Tooltip("Set this to control how much logging is generated")]
        LogLevel m_LogLevel;

        [SerializeField, Tooltip("This should almost always be set to true")]
        bool m_RunInBackground = true;

        [SerializeField]
        NetworkConfig m_NetworkConfig;

        ///<inheritdoc/>
        void Awake()
        {
            LogLevel = m_LogLevel;
            RunInBackground = m_RunInBackground;
            NetworkConfig = m_NetworkConfig;
            Utils.s_LogLevel = LogLevel;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(NetworkManagerVRMultiplayer))]
    class VRMutliplayerTemplateNetworkManagerEditor : Editor
    {
        /// <summary>
        /// This function is called when the inspector is drawn.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying)
            {
                switch (XRINetworkGameManager.CurrentConnectionState.Value)
                {
                    case XRINetworkGameManager.ConnectionState.None:
                        GUILayout.Box("Authenticating");
                        break;
                    case XRINetworkGameManager.ConnectionState.Authenticating:
                        GUILayout.Box("Authenticating");
                        break;
                    case XRINetworkGameManager.ConnectionState.Authenticated:
                        if (GUILayout.Button("Connect"))
                        {
                            XRINetworkGameManager.Instance.QuickJoinLobby();
                        }
                        break;
                    case XRINetworkGameManager.ConnectionState.Connecting:
                        GUILayout.Box("Connecting");
                        break;
                    case XRINetworkGameManager.ConnectionState.Connected:
                        if (GUILayout.Button("Disconnect"))
                        {
                            XRINetworkGameManager.Instance.Disconnect();
                        }
                        break;
                }
            }
            else
            {
                GUILayout.Box("Game not running.");
            }
        }
    }
#endif
}
