using TMPro;
using UnityEngine;

// EffectRow Prefab에 부착 — AlwaysEffectPopup이 동적으로 Setup() 호출
public class AlwaysEffectRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _txtLabel;
    [SerializeField] private TextMeshProUGUI _txtValue;

    private static readonly Color COLOR_POSITIVE = new Color(0.33f, 0.78f, 1f);  // 파란색 — 긍정 수치
    private static readonly Color COLOR_NEGATIVE = new Color(1f, 0.35f, 0.35f); // 붉은색 — 부정 수치

    public void Setup(string label, string valueText, bool isNegative)
    {
        _txtLabel.text = label;
        _txtValue.text = valueText;
        _txtValue.color = isNegative ? COLOR_NEGATIVE : COLOR_POSITIVE;
    }
}