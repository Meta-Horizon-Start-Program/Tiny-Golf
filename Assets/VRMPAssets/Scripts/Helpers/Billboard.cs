using UnityEngine;

namespace XRMultiplayer
{
    public class Billboard : MonoBehaviour
    {
        [SerializeField] bool m_WorldUp;
        [SerializeField] bool m_FlipForward;

        protected Camera m_Camera;

        private void Awake()
        {
            m_Camera = Camera.main;
        }

        private void Update()
        {
            Quaternion lookRot = Quaternion.LookRotation(m_Camera.transform.position - transform.position);

            if (m_WorldUp)
            {
                Vector3 offset = lookRot.eulerAngles;
                offset.x = 0;
                offset.z = 0;

                if (m_FlipForward)
                    offset.y += 180;

                lookRot = Quaternion.Euler(offset);
            }

            transform.rotation = lookRot;
        }
    }
}
