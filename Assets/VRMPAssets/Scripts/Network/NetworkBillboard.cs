using Unity.Netcode;
using Unity.VRTemplate;
using UnityEngine;
using XRMultiplayer;

/// <summary>
/// Represents a networked billboard that will always billboard towards the person who owns the object.
/// </summary>
[RequireComponent(typeof(TurnToFace))]
public class NetworkBillboard : NetworkBehaviour
{
    /// <summary>
    /// Billboard effect.
    /// </summary>
    TurnToFace m_TurnToFace;

    /// <inheritdoc/>
    private void Start()
    {
        m_TurnToFace = GetComponent<TurnToFace>();
    }
    /// <summary>
    /// Handles the change in the "isInteracting" state of the object.
    /// </summary>
    /// <param name="old">The previous value of the "isInteracting" state.</param>
    /// <param name="current">The current value of the "isInteracting" state.</param>
    public void IsHeldChanged(bool current)
    {
        if (current)
        {
            m_TurnToFace.enabled = true;

            if (XRINetworkGameManager.Instance.GetPlayerByID(NetworkObject.OwnerClientId, out XRINetworkPlayer player))
            {
                m_TurnToFace.faceTarget = player.head;
            }
        }
    }

    /// <summary>
    /// Called when the object is selected or deselected.
    /// </summary>
    /// <param name="selected">Indicates whether the object is selected or deselected.</param>
    public void Selected(bool selected)
    {
        if (!selected)
        {
            m_TurnToFace.enabled = false;
        }
    }
}
