using UnityEngine;

namespace XRMultiplayer
{
    public class PopoutUI : MonoBehaviour
    {
        [SerializeField] bool m_HideOnStart = false;
        [SerializeField] float m_DistanceFromFace = .25f;
        [SerializeField] float m_YOffset;
        Transform m_MainCamTransform;

        private void Start()
        {
            if (m_HideOnStart)
                gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (m_MainCamTransform == null)
            {
                m_MainCamTransform = Camera.main.transform;
            }

            transform.position = m_MainCamTransform.position;

            Vector3 rot = m_MainCamTransform.eulerAngles;
            rot.x = 0;
            rot.z = 0;
            transform.rotation = Quaternion.Euler(rot);

            transform.position += transform.forward * m_DistanceFromFace;
            transform.position += Vector3.up * -m_YOffset;
        }
    }
}
