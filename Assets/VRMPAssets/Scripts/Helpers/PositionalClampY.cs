using UnityEngine;

namespace XRMultiplayer
{
    [ExecuteInEditMode]
    public class PositionalClampY : MonoBehaviour
    {
        [SerializeField] Vector2 m_minMaxHeight;

        private void Update()
        {
            ClampBounds();
        }

        void ClampBounds()
        {
            if (transform.position.y < m_minMaxHeight.x)
            {
                transform.position = new Vector3(transform.position.x, m_minMaxHeight.x, transform.position.z);
            }
            else if (transform.position.y > m_minMaxHeight.y)
            {
                transform.position = new Vector3(transform.position.x, m_minMaxHeight.y, transform.position.z);
            }
        }
    }
}
