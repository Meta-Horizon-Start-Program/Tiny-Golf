using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class IntButtonUI : MonoBehaviour
{
    [SerializeField] UnityEvent<int> m_ValueUpdated;
    [SerializeField] Vector2Int m_MinMaxValue;
    [SerializeField] Button m_IncrementButton;
    [SerializeField] Button m_DecrementButton;

    [SerializeField] int m_UpdateValue = 1;
    [SerializeField] int m_CurrentValue;
    [SerializeField] TMP_Text m_CurrentValueText;

    void Start()
    {
        m_IncrementButton.onClick.AddListener(() => UpdateValue(true));
        m_DecrementButton.onClick.AddListener(() => UpdateValue(false));

        m_CurrentValueText.text = m_CurrentValue.ToString();
        m_ValueUpdated.Invoke(m_CurrentValue);
    }

    void OnDestroy()
    {
        m_IncrementButton.onClick.RemoveAllListeners();
        m_DecrementButton.onClick.RemoveAllListeners();
    }

    public void UpdateValue(bool increment)
    {
        m_CurrentValue = Mathf.Clamp(m_CurrentValue + (increment ? m_UpdateValue : -m_UpdateValue), m_MinMaxValue.x, m_MinMaxValue.y);
        m_CurrentValueText.text = m_CurrentValue.ToString();
        m_ValueUpdated.Invoke(m_CurrentValue);
    }
}
