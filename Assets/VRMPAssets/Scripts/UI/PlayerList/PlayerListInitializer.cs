using UnityEngine;

namespace XRMultiplayer
{
    public class PlayerListInitializer : MonoBehaviour
    {
        [SerializeField] PlayerListUI[] m_PlayerListUIs;

        void Start()
        {
            foreach (var l in m_PlayerListUIs)
            {
                l.InitializeCallbacks();
            }
        }
    }
}
