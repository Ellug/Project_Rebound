using TMPro;
using UnityEngine;

// 훈련 결과 팝업 내 스탯 변화 1행
public class StatChangeRow : MonoBehaviour
{
    [Header("스탯명")]
    [SerializeField] private TMP_Text _txtStatName;

    [Header("원래 수치")]
    [SerializeField] private TMP_Text _txtOriginal;

    [Header("변화 수치")]
    [SerializeField] private TMP_Text _txtChanged;

    // int 전용
    public void Setup(string statName, int original, int changed)
    {
        if (_txtStatName != null)
            _txtStatName.text = statName;

        if (_txtOriginal != null)
            _txtOriginal.text = original.ToString();

        if (_txtChanged != null)
            _txtChanged.text = changed.ToString();

        ApplyChangedColor(changed - original);
    }

    // float 대응
    public void Setup(string statName, float original, float changed, int decimals = 0)
    {
        if (_txtStatName != null)
            _txtStatName.text = statName;

        if (_txtOriginal != null)
            _txtOriginal.text = FormatFloat(original, decimals);

        if (_txtChanged != null)
            _txtChanged.text = FormatFloat(changed, decimals);

        ApplyChangedColor(changed - original);
    }

    // 증가/감소 색상 강조
    private void ApplyChangedColor(float delta)
    {
        if (_txtChanged == null) return;

        if (delta > 0f)
        {
            _txtChanged.color = new Color(0.90f, 0.25f, 0.25f); // 빨강
        }
        else if (delta < 0f)
        {
            _txtChanged.color = new Color(0.25f, 0.55f, 1.00f); // 파랑
        }
        else
        {
            _txtChanged.color = Color.white;
        }
    }

    private static string FormatFloat(float value, int decimals)
    {
        if (decimals <= 0)
            return Mathf.RoundToInt(value).ToString();

        return value.ToString($"F{decimals}");
    }
}