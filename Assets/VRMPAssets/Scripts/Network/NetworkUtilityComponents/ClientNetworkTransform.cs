using UnityEngine;
using Unity.Netcode.Components;

namespace XRMultiplayer
{
    /// <summary>
    /// ClientNetworkTransform class is responsible for updating the
    /// <see cref="NetworkTransform"/> from the local owner perspective.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        /// <summary>
        /// If true, only the Server can update the transform of the object.
        /// </summary>
        [SerializeField, Tooltip("Determines Local or Server transform updating.")] bool isServerAuthoritative = false;

        ///<inheritdoc/>
        protected override bool OnIsServerAuthoritative()
        {
            return isServerAuthoritative;
        }
    }
}
