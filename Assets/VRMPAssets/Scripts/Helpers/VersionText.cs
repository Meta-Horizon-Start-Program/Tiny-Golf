using TMPro;
using UnityEngine;

namespace XRMultiplayer
{
    public class VersionText : MonoBehaviour
    {
        [SerializeField] TMP_Text[] m_VersionTextComponents;
        [SerializeField] string m_Prefix = "v";
        [SerializeField] string m_Suffix = "";

        // Start is called before the first frame update
        void Start()
        {
            SetText();
        }

        private void OnValidate()
        {
            SetText();
        }

        void SetText()
        {
            if (m_VersionTextComponents != null)
            {
                foreach (TMP_Text t in m_VersionTextComponents)
                {
                    t.text = $"{m_Prefix}{Application.version}{m_Suffix}";
                }
            }
            else
            {
                Utils.Log("Missing Text component on VersionText script", 2);
            }
        }
    }
}
