using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

namespace XRMultiplayer
{
    /// <summary>
    /// Simple implmentation of a Networked Slider.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class NetworkedSlider : NetworkBehaviour
    {
        [SerializeField, Tooltip("Broadcast the value of the dropdown to all clients when a new client joins.")]
        bool m_BroadcastValueOnJoin = false;

        [SerializeField, Tooltip("Reset current value when despawning.")]
        bool m_ResetValueOnDespawn = true;

        /// <summary>
        /// Networked Variable to sync the state of the Slider on new clients joining.
        /// </summary>
        NetworkVariable<float> m_NetworkSliderValue;

        /// <summary>
        /// Slider associated with this component.
        /// </summary>
        Slider m_Slider;

        float m_StartValue;

        ///<inheritdoc/>
        private void Awake()
        {
            m_Slider = GetComponent<Slider>();
            m_Slider.onValueChanged.AddListener(SliderChanged);
            m_StartValue = m_Slider.value;
            m_NetworkSliderValue = new NetworkVariable<float>(m_StartValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        }

        ///<inheritdoc/>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (m_BroadcastValueOnJoin)
            {
                // Sync the value of the dropdown.
                m_Slider.value = m_NetworkSliderValue.Value;
            }
            else
            {
                m_Slider.SetValueWithoutNotify(m_NetworkSliderValue.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsServer && m_ResetValueOnDespawn)
            {
                if (m_NetworkSliderValue != null)
                    m_NetworkSliderValue.Value = m_StartValue;
            }
        }

        /// <summary>
        /// Called when the Slider is updated by the local user.
        /// </summary>
        /// <param name="newValue">Value of the slider.</param>
        void SliderChanged(float newValue)
        {
            SliderChangedServerRpc(newValue, NetworkManager.Singleton.LocalClientId);
        }

        /// <summary>
        /// Called from the local user to the Server when the local user has updated the slider.
        /// </summary>
        /// <param name="newValue">Value of the slider.</param>
        /// <param name="clientId">Local user Id.</param>
        [ServerRpc(RequireOwnership = false)]
        void SliderChangedServerRpc(float newValue, ulong clientId)
        {
            m_NetworkSliderValue.Value = newValue;
            SliderChangedClientRpc(newValue, clientId);
        }

        /// <summary>
        /// Called from the Server on all clients after a local user has updated the slider.
        /// </summary>
        /// <param name="newValue">Value of the slider.</param>
        /// <param name="clientId">Local user Id.</param>
        [ClientRpc]
        void SliderChangedClientRpc(float newValue, ulong clientId)
        {
            // Don't update on the local client if they sent the call.
            if (NetworkManager.Singleton.LocalClientId != clientId)
            {
                //Remove listener here before updating value to prevent continuous looping
                m_Slider.onValueChanged.RemoveListener(SliderChanged);
                m_Slider.value = newValue;
                m_Slider.onValueChanged.AddListener(SliderChanged);
            }
        }
    }
}
