using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace XRMultiplayer
{
    public class DelayedUnityEvent : MonoBehaviour
    {
        [SerializeField] float m_TimeToEnable = 4.0f;
        [SerializeField] UnityEvent m_UnityEvent;
        Coroutine m_EnablingRoutine;

        private void OnEnable()
        {
            if (m_EnablingRoutine != null) StopCoroutine(m_EnablingRoutine);
            m_EnablingRoutine = StartCoroutine(EnableAfterTime());
        }

        private void OnDisable()
        {
            if (m_EnablingRoutine != null) StopCoroutine(m_EnablingRoutine);
        }

        IEnumerator EnableAfterTime()
        {
            yield return new WaitForSeconds(m_TimeToEnable);
            m_UnityEvent.Invoke();
        }
    }
}
