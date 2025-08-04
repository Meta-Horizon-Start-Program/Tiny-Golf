using System.Collections;
using UnityEngine;

namespace Unity.VRTemplate
{
    /// <summary>
    /// Makes the object this is attached to follow a target with a slight delay
    /// </summary>
    public class LazyFollow : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        [Tooltip("The object being followed.")]
        Transform m_Target;
#pragma warning restore 649

        [SerializeField]
        [Tooltip("Whether to always follow or only when in-view.")]
        bool m_FOV = false;

        [SerializeField]
        [Tooltip("Whether rotation is locked to the z-axis for can move in any direction.")]
        bool m_ZRot = true;

        [SerializeField]
        [Tooltip("Adjusts the follow point from the target by this amount.")]
        Vector3 m_TargetOffset = Vector3.forward;

        [SerializeField]
        [Tooltip("Snap to target position when this component is enabled.")]
        bool m_SnapOnEnable = true;

        public bool followActive = true;

        Vector3 m_TargetLastPos;
        Camera m_Camera;
        public float smoothTime = 0.3F;
        private Vector3 velocity = Vector3.zero;
        bool m_InFOV = false;

        Vector3 targetPosition => m_Target.position + m_Target.TransformVector(m_TargetOffset);
        Quaternion targetRotation
        {
            get
            {
                if (!m_ZRot)
                {
                    var eulerAngles = m_Target.eulerAngles;
                    eulerAngles = new Vector3(eulerAngles.x, eulerAngles.y, 0f);
                    return Quaternion.Euler(eulerAngles);
                }

                return m_Target.rotation;
            }
        }

        void Awake()
        {
            if (m_Camera == null)
                m_Camera = Camera.main;

            // Default to main camera
            if (m_Target == null)
                if (m_Camera != null)
                    m_Target = m_Camera.transform;
        }

        void Start()
        {
            var targetPos = targetPosition;
            m_TargetLastPos = targetPos;
        }

        void OnEnable()
        {
            if (m_SnapOnEnable)
            {
                transform.position = targetPosition;
                velocity = Vector3.zero;
            }
        }

        void Update()
        {
            if (m_FOV)
            {
                Vector3 screenPoint = m_Camera.WorldToViewportPoint(this.gameObject.transform.position);
                var inFov = screenPoint.z > 0f && screenPoint.x > 0f && screenPoint.x < 1f && screenPoint.y > 0f && screenPoint.y < 1f;
                if (inFov)
                    return;
            }

            var targetPos = targetPosition;
            if (m_TargetLastPos == targetPos)
                return;

            if (followActive)
            {
                transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
                m_TargetLastPos = targetPos;
            }
        }

        public void Summon()
        {
            m_InFOV = false;
            if (!followActive)
                StartCoroutine(OneTimeSummonPosition());
        }

        IEnumerator OneTimeSummonFOV()
        {
            while (!m_InFOV)
            {
                Vector3 screenPoint = m_Camera.WorldToViewportPoint(this.gameObject.transform.position);
                var inFov = screenPoint.z > 0f && screenPoint.x > 0.3f && screenPoint.x < 0.7f && screenPoint.y > 0.3f && screenPoint.y < 0.7f;
                if (inFov)
                {
                    m_InFOV = true;
                }
                else
                {
                    m_InFOV = false;

                    var targetPos = targetPosition;
                    if (m_TargetLastPos != targetPos)
                    {
                        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
                        m_TargetLastPos = targetPos;
                    }
                }
                yield return null;
            }
        }

        IEnumerator OneTimeSummonPosition()
        {
            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                var targetPos = targetPosition;
                if (m_TargetLastPos != targetPos)
                {
                    transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
                    m_TargetLastPos = targetPos;
                }
                yield return null;
            }
        }
    }
}
