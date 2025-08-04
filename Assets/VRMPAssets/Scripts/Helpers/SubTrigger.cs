using System;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// A simple class used for callbacks when OnTriggerEnter or OnTriggerExit is called.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SubTrigger : MonoBehaviour
    {
        public Action<Collider, bool> OnTriggerAction;
        public Collider subTriggerCollider;

        private void Awake()
        {
            if (subTriggerCollider == null)
                TryGetComponent(out subTriggerCollider);
        }

        private void OnTriggerEnter(Collider other)
        {
            OnTriggerAction?.Invoke(other, true);
        }

        private void OnTriggerExit(Collider other)
        {
            OnTriggerAction?.Invoke(other, false);
        }
    }
}
