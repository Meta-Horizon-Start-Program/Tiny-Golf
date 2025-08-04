using UnityEngine;

namespace XRMultiplayer
{
    [ExecuteInEditMode]
    public class SnapToPlayerHeight : MonoBehaviour
    {
        [SerializeField] float m_heightOffset = -.25f;
        [SerializeField] float m_ZOffset;
        [SerializeField] Transform m_CameraTransform;


        void Start() => SetupReferences();
        void OnValidate() => SetupReferences();

        void SetupReferences()
        {
            if (m_CameraTransform == null && Camera.main != null)
                m_CameraTransform = Camera.main.transform;
        }

        // Update is called once per frame
        void Update()
        {
            if (m_CameraTransform != null)
            {
                transform.position = new Vector3(transform.position.x, m_CameraTransform.position.y + m_heightOffset, transform.position.z);
                transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, m_ZOffset);
            }
        }
    }
}
