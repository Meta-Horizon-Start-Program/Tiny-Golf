using UnityEngine;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine.Events;

/// <summary>
/// Simple Network Trigger syncing the events over the network when the local player enters the trigger collider.
/// </summary>
public class NetworkTrigger : NetworkBehaviour
{
    [SerializeField, Tooltip("This event is triggered when the Local Player walks into this.")] protected UnityEvent<ulong> m_NetworkedTriggerUnityEvent;
    void OnTriggerEnter(Collider other)
    {
        // Local Player Triggered
        if (other.TryGetComponent(out XROrigin origin))
        {
            TriggerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    [Rpc(SendTo.Everyone)]
    void TriggerRpc(ulong clientId)
    {
        m_NetworkedTriggerUnityEvent?.Invoke(clientId);
    }
}
