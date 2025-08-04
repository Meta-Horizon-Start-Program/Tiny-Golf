using System.Collections;
using TMPro;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// This class controls the display of the Player HUD Notification aka the Toast.
    /// </summary>
    public class PlayerHudNotification : MonoBehaviour
    {
        /// <summary>
        /// The singleton instance of this class.
        /// </summary>
        public static PlayerHudNotification Instance;

        [Header("Display Options")]
        [SerializeField] bool m_LockPitch = true;
        [SerializeField] bool m_LockRoll = true;
        /// <summary>
        /// The speed at which the toast follows the camera.
        /// </summary>
        [SerializeField] float m_FollowSpeed = 5.0f;

        /// <summary>
        /// The amount of time to display the toast.
        /// </summary>
        [SerializeField] float m_DisplayTime = 3.0f;

        /// <summary>
        /// The speed at which the toast fades in and out.
        /// </summary>
        [SerializeField] float m_ShowHideSpeed = 5.0f;

        [Header("Display References")]
        /// <summary>
        /// Text component to display the toast.
        /// </summary>
        [SerializeField] TMP_Text m_Text;

        /// <summary>
        /// The layout group transform that contains the toast.
        /// </summary>
        [SerializeField] Transform m_LayoutGroupTransform;

        /// <summary>
        /// The canvas group that contains the toast.
        /// </summary>
        [SerializeField] CanvasGroup m_CanvasGroup;

        /// <summary>
        /// The main camera.
        /// </summary>
        Camera m_Camera;

        /// <summary>
        /// The transform of this object.
        /// </summary>
        Transform m_Transform;

        ///<inheritdoc/>
        private void Awake()
        {
            if (Instance != null)
            {
                Utils.Log("Instance is not null for PlayerHudNotification.", 2);
                enabled = false;
                return;
            }

            Instance = this;
        }

        /// <inheritdoc/>
        private void Start()
        {
            m_Camera = Camera.main;
            m_Transform = transform;

            if (m_CanvasGroup == null)
                m_CanvasGroup = GetComponentInChildren<CanvasGroup>();

            m_CanvasGroup.alpha = 0.0f;
            m_LayoutGroupTransform.gameObject.SetActive(false);
        }

        [ContextMenu("Show Text Test")]
        void ShowTextTest()
        {
            ShowText("Test Text", m_DisplayTime);
        }

        /// <summary>
        /// Shows the toast with the given text.
        /// </summary>
        public void ShowText(string textToShow, float displayTime = 3.0f)
        {
            m_DisplayTime = displayTime;
            m_Text.text = textToShow;
            m_LayoutGroupTransform.gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(ShowRoutine());
        }

        /// <inheritdoc/>
        private void LateUpdate()
        {
            m_Transform.position = m_Camera.transform.position;

            Quaternion lookRot = Quaternion.LookRotation(m_Camera.transform.forward);

            Vector3 offset = lookRot.eulerAngles;

            if (m_LockPitch)
                offset.x = 0;
            if (m_LockRoll)
                offset.z = 0;

            lookRot = Quaternion.Euler(offset);

            m_Transform.rotation = Quaternion.Slerp(m_Transform.rotation, lookRot, Time.deltaTime * m_FollowSpeed);
        }

        /// <summary>
        /// Coroutine to show the toast.
        /// </summary>
        /// <returns></returns>
        IEnumerator ShowRoutine()
        {
            while (m_CanvasGroup.alpha < 1.0f)
            {
                m_CanvasGroup.alpha += Time.deltaTime * m_ShowHideSpeed;
                yield return null;
            }

            StartCoroutine(DisplayRoutine());
        }

        /// <summary>
        /// Coroutine to display the toast.
        /// </summary>
        /// <returns></returns>
        IEnumerator DisplayRoutine()
        {
            yield return new WaitForSeconds(m_DisplayTime);

            StartCoroutine(HideTime());
        }

        /// <summary>
        /// Coroutine to hide the toast.
        /// </summary>
        /// <returns></returns>
        IEnumerator HideTime()
        {
            while (m_CanvasGroup.alpha > 0.0f)
            {
                m_CanvasGroup.alpha -= Time.deltaTime * m_ShowHideSpeed;
                yield return null;
            }
            m_LayoutGroupTransform.gameObject.SetActive(false);
        }
    }
}
